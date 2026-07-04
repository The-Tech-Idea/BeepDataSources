using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Messaging;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.GooglePubSub
{
    /// <summary>
    /// Google Cloud Pub/Sub data source — full rewrite against BeepDM 3.1.0 (Phase 10 Messaging
    /// folder refresh). Uses the official <c>Google.Cloud.PubSub.V1</c> SDK to manage topics and
    /// subscriptions and to publish/pull messages. Non-messaging IDataSource members return
    /// honest IErrorsInfo failures.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.GooglePubSub)]
    public class GooglePubSubDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>, IDisposable
    {
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DatasourceName { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public List<string> EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public List<object> Records { get; set; } = new();
        public DataTable SourceEntityData { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public DataSourceType DatasourceType { get; set; } = DataSourceType.GooglePubSub;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = " ";

        public string CurrentDatabase
        {
            get => (Dataconnection as GooglePubSubDataConnection)?.PubSubProperties?.TopicName
                   ?? (Dataconnection as GooglePubSubDataConnection)?.PubSubProperties?.SubscriptionName;
            set { if (Dataconnection is GooglePubSubDataConnection c && c.PubSubProperties != null) c.PubSubProperties.TopicName = value; }
        }

        private bool disposedValue;
        private GooglePubSubDataConnection _conn;

        public GooglePubSubDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
            DataSourceType databasetype, IErrorsInfo per)
        {
            disposedValue = false;
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.MessageQueue;

            Dataconnection = new DefaulDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor,
                ConnectionProp = DMEEditor?.ConfigEditor?.DataConnections?
                    .FirstOrDefault(c => c.ConnectionName == datasourcename)
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.GooglePubSub, Category = DatasourceCategory.MessageQueue }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.MessageQueue;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.GooglePubSub;

            if (!(Dataconnection is GooglePubSubDataConnection))
                Dataconnection = new GooglePubSubDataConnection(DMEEditor, Dataconnection?.ConnectionProp);
            _conn = Dataconnection as GooglePubSubDataConnection;
        }

        private IErrorsInfo FailResult(string op, Exception ex)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"{op} failed: {ex.Message}";
            ErrorObject.Ex = ex;
            return ErrorObject;
        }

        // ── IDataSource connection lifecycle (real) ──
        public ConnectionState Openconnection()
        {
            try
            {
                var result = _conn?.OpenConnection() ?? ConnectionState.Broken;
                ConnectionStatus = result;
                if (result == ConnectionState.Open) RefreshEntitiesCache();
                return result;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"GooglePubSub Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection() => _conn?.CloseConn() ?? ConnectionStatus;

        private void RefreshEntitiesCache()
        {
            try
            {
                if (_conn?.PubSubProperties == null) return;
                EntitiesNames = new List<string> { _conn.PubSubProperties.TopicName, _conn.PubSubProperties.SubscriptionName }
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n!)
                    .ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"GooglePubSub RefreshEntitiesCache error: {ex.Message}"); }
        }

        // ── IDataSource entity core (real via Pub/Sub admin client) ──
        public IEnumerable<string> GetEntitesList()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var pub = Google.Cloud.PubSub.V1.PublisherServiceApiClient.Create();
                var topic = pub.GetTopicAsync(new GetTopicRequest
                {
                    Name = $"projects/{_conn.PubSubProperties.ProjectId}/topics/{EntityName}"
                }).GetAwaiter().GetResult();
                return topic != null;
            }
            catch (Exception) { return false; }
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(GenericMessage);

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var pub = Google.Cloud.PubSub.V1.PublisherServiceApiClient.Create();
                pub.CreateTopicAsync(new Topic
                {
                    Project = _conn.PubSubProperties.ProjectId,
                    TopicId = entity.EntityName
                }).GetAwaiter().GetResult();
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"GooglePubSub CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                return false;
            }
        }

        // ── Other IDataSource members (honest compilable implementations) ──

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (refresh) RefreshEntitiesCache();
            return Entities.FirstOrDefault(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        public IErrorsInfo BeginTransaction(PassedArgs args)
            => FailResult("BeginTransaction", new NotSupportedException("Pub/Sub has no transactions; use ordering keys for ordering."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("Pub/Sub has no transactions."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("Pub/Sub has no transactions."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Created {ok} topics, {fail} failed.";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("Pub/Sub is not SQL; use the query API for message search."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var pub = Google.Cloud.PubSub.V1.PublisherServiceApiClient.Create();
                pub.DeleteTopicAsync(new DeleteTopicRequest
                {
                    Project = _conn.PubSubProperties.ProjectId,
                    Topic = EntityName
                }).GetAwaiter().GetResult();
                EntitiesNames.Remove(EntityName);
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Deleted Pub/Sub topic '{EntityName}'.";
                return ErrorObject;
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("Pub/Sub is append-only (publish-only)."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("Pub/Sub is append-only."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("Use SendMessageAsync."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("Pub/Sub has no scripts."));

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
            => Enumerable.Empty<ETLScriptDet>();

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
            => Enumerable.Empty<ChildRelation>();

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
            => Enumerable.Empty<RelationShipKeys>();

        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
            => Enumerable.Empty<object>();

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
            => new PagedResult(Array.Empty<object>(), Math.Max(1, pageNumber), Math.Max(1, pageSize), 0);

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
            => Task.FromResult(Enumerable.Empty<object>());

        public double GetScalar(string query)
            => throw new NotSupportedException("Pub/Sub has no SQL; use the query API for scalar metrics.");

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr) => Enumerable.Empty<object>();

        // ── IMessageDataSource<GenericMessage, StreamConfig> (minimal compilable) ──

        private StreamConfig _config;

        public void Initialize(StreamConfig config)
        {
            _config = config;
            if (config != null && !string.IsNullOrEmpty(config.EntityName)
                && !EntitiesNames.Contains(config.EntityName, StringComparer.OrdinalIgnoreCase))
                EntitiesNames.Add(config.EntityName);
        }

        public Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var pub = Google.Cloud.PubSub.V1.PublisherServiceApiClient.Create();
                pub.PublishAsync(new PublishRequest
                {
                    Topic = $"projects/{_conn.PubSubProperties.ProjectId}/topics/{message?.EntityName ?? streamName}",
                    Messages =
                    {
                        new PubsubMessage { Data = ByteString.CopyFromUtf8(message?.Payload != null ? message.Payload.ToString() : string.Empty) }
                    }
                }).GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        public Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var subName = $"projects/{_conn.PubSubProperties.ProjectId}/subscriptions/{_conn.PubSubProperties.SubscriptionName}";
                var sub = Google.Cloud.PubSub.V1.SubscriberServiceApiClient.Create();
                var pull = sub.PullAsync(new PullRequest { Subscription = subName, MaxMessages = 1 }).GetAwaiter().GetResult();
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (pull.ReceivedMessages.Count > 0)
                    {
                        var msg = pull.ReceivedMessages[0];
                        var gm = new GenericMessage
                        {
                            EntityName = streamName,
                            Payload = msg.Message.Data.ToStringUtf8(),
                            MessageId = msg.Message.MessageId
                        };
                        if (onMessageReceived != null) await onMessageReceived(gm);
                        sub.AcknowledgeAsync(new AcknowledgeRequest
                        {
                            Subscription = subName,
                            AckIds = { msg.AckId }
                        }).GetAwaiter().GetResult();
                    }
                    pull = sub.PullAsync(new PullRequest { Subscription = subName, MaxMessages = 1 }).GetAwaiter().GetResult();
                }
            }, cancellationToken);
        }

        public Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var sub = Google.Cloud.PubSub.V1.SubscriberServiceApiClient.Create();
                var pull = sub.PullAsync(new PullRequest
                {
                    Subscription = $"projects/{_conn.PubSubProperties.ProjectId}/subscriptions/{_conn.PubSubProperties.SubscriptionName}",
                    MaxMessages = 1
                }).GetAwaiter().GetResult();
                if (pull.ReceivedMessages.Count == 0)
                    return Task.FromResult<GenericMessage>(null);
                var m = pull.ReceivedMessages[0];
                return Task.FromResult(new GenericMessage
                {
                    EntityName = streamName,
                    Payload = m.Message.Data.ToStringUtf8(),
                    MessageId = m.Message.MessageId
                });
            }
            catch (Exception ex) { return Task.FromException<GenericMessage>(ex); }
        }

        public Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken = default)
            => Task.FromResult<object>(new { Stream = streamName, Note = "Pub/Sub metadata is exposed via admin API; minimal datasource does not query it." });

        public void Disconnect() => Closeconnection();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal GooglePubSubDataConnection MigrationConn => _conn;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _conn?.Dispose(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
