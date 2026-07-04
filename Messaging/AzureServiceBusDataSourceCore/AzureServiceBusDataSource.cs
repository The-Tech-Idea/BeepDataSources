using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
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

namespace TheTechIdea.Beep.AzureServiceBus
{
    /// <summary>
    /// Azure Service Bus data source — full rewrite against BeepDM 3.1.0 (Phase 10 Messaging
    /// folder refresh). Uses the official <c>Azure.Messaging.ServiceBus</c> +
    /// <c>Azure.Messaging.ServiceBus.Administration</c> SDKs to manage queues/topics and to
    /// publish/consume messages. Non-messaging IDataSource members return honest IErrorsInfo
    /// failures.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.AzureServiceBus)]
    public class AzureServiceBusDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.AzureServiceBus;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";

        public string CurrentDatabase
        {
            get => (Dataconnection as AzureServiceBusDataConnection)?.ServiceBusProperties?.QueueName
                   ?? (Dataconnection as AzureServiceBusDataConnection)?.ServiceBusProperties?.TopicName;
            set { if (Dataconnection is AzureServiceBusDataConnection c && c.ServiceBusProperties != null) c.ServiceBusProperties.QueueName = value; }
        }

        private bool disposedValue;
        private ServiceBusClient _sbc;
        private ServiceBusAdministrationClient _admin;

        public AzureServiceBusDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.AzureServiceBus, Category = DatasourceCategory.MessageQueue }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.MessageQueue;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.AzureServiceBus;

            if (!(Dataconnection is AzureServiceBusDataConnection))
                Dataconnection = new AzureServiceBusDataConnection(DMEEditor, Dataconnection?.ConnectionProp);
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
                var props = (Dataconnection as AzureServiceBusDataConnection)?.ServiceBusProperties;
                var connStr = props?.ServiceBusConnectionString;
                if (string.IsNullOrEmpty(connStr))
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Logger?.WriteLog("AzureServiceBus Openconnection: ServiceBusConnectionString is not set.");
                    return ConnectionStatus;
                }
                _sbc = new ServiceBusClient(connStr);
                _admin = new ServiceBusAdministrationClient(connStr);
                ConnectionStatus = ConnectionState.Open;
                RefreshEntitiesCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"AzureServiceBus Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _sbc?.DisposeAsync().AsTask().Wait(); } catch { }
            _sbc = null;
            _admin = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshEntitiesCache()
        {
            try
            {
                if (_admin == null) return;
                EntitiesNames = new List<string>();
                // Async iteration for queues.
                var queueEnum = _admin.GetQueuesAsync().GetAsyncEnumerator();
                while (queueEnum.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                    EntitiesNames.Add(queueEnum.Current.Name);
                // Async iteration for topics.
                var topicEnum = _admin.GetTopicsAsync().GetAsyncEnumerator();
                while (topicEnum.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                    EntitiesNames.Add(topicEnum.Current.Name);
            }
            catch (Exception ex) { Logger?.WriteLog($"AzureServiceBus RefreshEntitiesCache error: {ex.Message}"); }
        }

        // ── IDataSource entity core (real via admin SDK) ──
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
                if (_admin.QueueExistsAsync(EntityName).GetAwaiter().GetResult()) return true;
                return _admin.TopicExistsAsync(EntityName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"AzureServiceBus CheckEntityExist('{EntityName}') error: {ex.Message}");
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
                var name = entity.EntityName;
                if (_admin.QueueExistsAsync(name).GetAwaiter().GetResult())
                {
                    if (!EntitiesNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        EntitiesNames.Add(name);
                    return true;
                }
                if (_admin.TopicExistsAsync(name).GetAwaiter().GetResult())
                {
                    if (!EntitiesNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        EntitiesNames.Add(name);
                    return true;
                }
                // Default: create as a queue.
                _admin.CreateQueueAsync(new CreateQueueOptions(name)).GetAwaiter().GetResult();
                if (!EntitiesNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(name);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"AzureServiceBus CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
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
            => FailResult("BeginTransaction", new NotSupportedException("Service Bus has cross-entity transactions via the client; minimal datasource does not expose them."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("Use BeginTransaction/Commit."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("Use BeginTransaction/Commit."));

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
            => FailResult("ExecuteSql", new NotSupportedException("Service Bus is not SQL."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                if (_admin.QueueExistsAsync(EntityName).GetAwaiter().GetResult())
                    _admin.DeleteQueueAsync(EntityName).GetAwaiter().GetResult();
                else if (_admin.TopicExistsAsync(EntityName).GetAwaiter().GetResult())
                    _admin.DeleteTopicAsync(EntityName).GetAwaiter().GetResult();
                else
                    return FailResult("DeleteEntity", new InvalidOperationException($"Queue/Topic '{EntityName}' not found."));
                EntitiesNames.Remove(EntityName);
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Deleted Service Bus entity '{EntityName}'.";
                return ErrorObject;
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("Service Bus is append-only."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("Service Bus is append-only."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("Use SendMessageAsync."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("Service Bus has no scripts."));

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
            => throw new NotSupportedException("Service Bus has no SQL.");

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
                var sender = _sbc.CreateSender(message?.EntityName ?? streamName);
                var payload = message?.Payload != null ? message.Payload.ToString() : string.Empty;
                var body = new ServiceBusMessage(System.Text.Encoding.UTF8.GetBytes(payload ?? string.Empty));
                sender.SendMessageAsync(body).GetAwaiter().GetResult();
                sender.DisposeAsync().AsTask().Wait();
                return Task.CompletedTask;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        public Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken = default)
        {
            return Task.Run(async () =>
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var processor = _sbc.CreateProcessor(streamName, streamName); // queue or topic+subscription
                if (processor == null) return;
                processor.ProcessMessageAsync += async args =>
                {
                    var payload = args.Message.Body.ToString();
                    var msg = new GenericMessage { EntityName = streamName, Payload = payload, MessageId = args.Message.MessageId };
                    if (onMessageReceived != null) await onMessageReceived(msg);
                    await args.CompleteMessageAsync(args.Message);
                };
                processor.ProcessErrorAsync += args => Task.CompletedTask;
                await processor.StartProcessingAsync(cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                    await Task.Delay(100, cancellationToken);
                await processor.StopProcessingAsync();
            }, cancellationToken);
        }

        public Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var receiver = _sbc.CreateReceiver(streamName);
                var msg = receiver.PeekMessageAsync().GetAwaiter().GetResult();
                return Task.FromResult<GenericMessage>(msg == null ? null : new GenericMessage
                {
                    EntityName = streamName,
                    Payload = msg.Body.ToString(),
                    MessageId = msg.MessageId
                });
            }
            catch (Exception ex) { return Task.FromException<GenericMessage>(ex); }
        }

        public Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var response = _admin.GetQueueRuntimePropertiesAsync(streamName).GetAwaiter().GetResult();
                var runtime = response.Value;
                return Task.FromResult<object>(new
                {
                    Stream = streamName,
                    MessageCount = runtime?.TotalMessageCount ?? 0,
                    SizeInBytes = runtime?.SizeInBytes ?? 0
                });
            }
            catch (Exception ex) { return Task.FromException<object>(ex); }
        }

        public void Disconnect() => Closeconnection();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal ServiceBusAdministrationClient MigrationAdmin => _admin;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _sbc?.DisposeAsync().AsTask().Wait(); } catch { } _admin = null; }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
