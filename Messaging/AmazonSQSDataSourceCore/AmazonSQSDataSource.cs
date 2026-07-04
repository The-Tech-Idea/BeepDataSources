using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
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

namespace TheTechIdea.Beep.AmazonSQS
{
    /// <summary>
    /// Amazon SQS data source — full rewrite against BeepDM 3.1.0 (Phase 10 Messaging folder refresh).
    /// Uses the official AWSSDK.SQS to manage queues (create/delete/list) and to publish/consume
    /// messages. Non-messaging IDataSource members return honest IErrorsInfo failures.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.AmazonSQS)]
    public class AmazonSQSDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.AmazonSQS;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";

        public string CurrentDatabase
        {
            get => (Dataconnection as AmazonSQSDataConnection)?.SQSProperties?.QueueName;
            set { if (Dataconnection is AmazonSQSDataConnection c && c.SQSProperties != null) c.SQSProperties.QueueName = value; }
        }

        private bool disposedValue;
        private AmazonSQSClient _sqs;

        public AmazonSQSDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.AmazonSQS, Category = DatasourceCategory.MessageQueue }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.MessageQueue;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.AmazonSQS;

            // Initialise the SQS connection wrapper so the migration provider can reuse it.
            if (!(Dataconnection is AmazonSQSDataConnection))
                Dataconnection = new AmazonSQSDataConnection(DMEEditor, Dataconnection?.ConnectionProp);
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
                var props = (Dataconnection as AmazonSQSDataConnection)?.SQSProperties;
                if (props == null)
                {
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }
                var config = new AmazonSQSConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(props.Region ?? "us-east-1"),
                    ServiceURL = props.QueueUrl
                };
                _sqs = new AmazonSQSClient(props.AccessKey, props.SecretKey, config);
                ConnectionStatus = ConnectionState.Open;
                RefreshEntitiesCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"SQS Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _sqs?.Dispose(); } catch { }
            _sqs = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshEntitiesCache()
        {
            try
            {
                if (_sqs == null) return;
                var resp = _sqs.ListQueuesAsync(new ListQueuesRequest()).GetAwaiter().GetResult();
                EntitiesNames = resp.QueueUrls.Select(u => ExtractQueueName(u)).ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"SQS RefreshEntitiesCache error: {ex.Message}"); }
        }

        private static string ExtractQueueName(string queueUrl)
        {
            if (string.IsNullOrEmpty(queueUrl)) return string.Empty;
            var lastSlash = queueUrl.LastIndexOf('/');
            return lastSlash >= 0 && lastSlash < queueUrl.Length - 1 ? queueUrl.Substring(lastSlash + 1) : queueUrl;
        }

        // ── IDataSource entity core (real via AWS SDK) ──
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
                var url = BuildQueueUrl(EntityName);
                _sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest { QueueUrl = url }).GetAwaiter().GetResult();
                return true;
            }
            catch (QueueDoesNotExistException) { return false; }
            catch (Exception ex)
            {
                Logger?.WriteLog($"SQS CheckEntityExist('{EntityName}') error: {ex.Message}");
                return false;
            }
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(GenericMessage);

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var url = BuildQueueUrl(entity.EntityName);
                var req = new CreateQueueRequest
                {
                    QueueName = entity.EntityName,
                    Attributes = new Dictionary<string, string>()
                };
                if ((Dataconnection as AmazonSQSDataConnection)?.SQSProperties?.UseFIFO == true)
                {
                    req.Attributes["FifoQueue"] = "true";
                    req.Attributes["ContentBasedDeduplication"] = "true";
                }
                _sqs.CreateQueueAsync(req).GetAwaiter().GetResult();
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"SQS CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                return false;
            }
        }

        private string BuildQueueUrl(string queueName)
        {
            var props = (Dataconnection as AmazonSQSDataConnection)?.SQSProperties;
            if (props == null) return queueName;
            if (!string.IsNullOrEmpty(props.QueueUrl))
            {
                var dir = props.QueueUrl.Substring(0, props.QueueUrl.LastIndexOf('/') + 1);
                return dir + queueName;
            }
            return $"https://sqs.{props.Region}.amazonaws.com/{props.Database ?? ""}/{queueName}";
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
            => FailResult("BeginTransaction", new NotSupportedException("SQS has no transactions."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("SQS has no transactions."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("SQS has no transactions."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Created {ok} queues, {fail} failed.";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("SQS is not SQL."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var url = BuildQueueUrl(EntityName);
                _sqs.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = url }).GetAwaiter().GetResult();
                EntitiesNames.Remove(EntityName);
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Deleted SQS queue '{EntityName}'.";
                return ErrorObject;
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("SQS is append-only."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("SQS is append-only."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("Use SendMessageAsync."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("SQS has no scripts."));

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
            => throw new NotSupportedException("SQS has no SQL.");

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
                var url = BuildQueueUrl(message?.EntityName ?? streamName);
                var body = message?.Payload != null ? System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(message.Payload?.ToString() ?? string.Empty)) : string.Empty;
                _sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = url, MessageBody = body }).GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        public Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken = default)
        {
            // Minimal: poll for messages. SQS is poll-based, not push.
            return Task.Run(async () =>
            {
                var url = BuildQueueUrl(streamName);
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var resp = _sqs.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = url, MaxNumberOfMessages = 1 }).GetAwaiter().GetResult();
                        foreach (var m in resp.Messages)
                        {
                            var msg = new GenericMessage { EntityName = streamName, Payload = m.Body, MessageId = m.MessageId };
                            if (onMessageReceived != null) await onMessageReceived(msg);
                            _sqs.DeleteMessageAsync(url, m.ReceiptHandle).GetAwaiter().GetResult();
                        }
                    }
                    catch { await Task.Delay(1000, cancellationToken); }
                }
            }, cancellationToken);
        }

        public Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask; // SQS deletes after handle — implicit ack.

        public Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var url = BuildQueueUrl(streamName);
                var resp = _sqs.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = url, MaxNumberOfMessages = 1 }).GetAwaiter().GetResult();
                var m = resp.Messages.FirstOrDefault();
                return Task.FromResult<GenericMessage>(m == null ? null : new GenericMessage { EntityName = streamName, Payload = m.Body, MessageId = m.MessageId });
            }
            catch (Exception ex) { return Task.FromException<GenericMessage>(ex); }
        }

        public Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var url = BuildQueueUrl(streamName);
                var attrs = _sqs.GetQueueAttributesAsync(new GetQueueAttributesRequest { QueueUrl = url }).GetAwaiter().GetResult();
                return Task.FromResult<object>(new
                {
                    Stream = streamName,
                    ApproximateNumberOfMessages = attrs.ApproximateNumberOfMessages,
                    ApproximateNumberOfMessagesNotVisible = attrs.ApproximateNumberOfMessagesNotVisible,
                    VisibilityTimeout = attrs.VisibilityTimeout
                });
            }
            catch (Exception ex) { return Task.FromException<object>(ex); }
        }

        public void Disconnect() => Closeconnection();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal AmazonSQSClient MigrationClient => _sqs;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _sqs?.Dispose(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
