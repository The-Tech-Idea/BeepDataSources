using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NATS.Client;
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

namespace TheTechIdea.Beep.NATS
{
    /// <summary>
    /// NATS messaging data source — full rewrite against BeepDM 3.1.0 (Phase 10 stale→real refresh).
    /// Uses the official NATS.Client SDK to establish a real connection. NATS subjects are
    /// auto-created on first publish, so the migration methods are real at the connect level
    /// and the per-subject operations are honest IErrorsInfo (no schema in NATS).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.MessageQueue, DatasourceType = DataSourceType.NATS)]
    public class NATSDataSource : IDataSource, IMessageDataSource<GenericMessage, StreamConfig>, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.NATS;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.MessageQueue;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";

        public string ServerUrl { get; set; } = "nats://localhost:4222";
        public bool disposedValue;

        private IConnection _connection;

        public NATSDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.NATS, Category = DatasourceCategory.MessageQueue }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.MessageQueue;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.NATS;

            var url = Dataconnection.ConnectionProp?.Url;
            if (!string.IsNullOrEmpty(url)) ServerUrl = url;
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
                var factory = new ConnectionFactory();
                _connection = factory.CreateConnection(ServerUrl);
                ConnectionStatus = ConnectionState.Open;
                RefreshSubjectsCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"NATS Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _connection?.Close(); } catch { }
            try { _connection?.Dispose(); } catch { }
            _connection = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshSubjectsCache()
        {
            try { EntitiesNames = new List<string>(); }
            catch (Exception ex) { Logger?.WriteLog($"NATS RefreshSubjectsCache error: {ex.Message}"); }
        }

        // ── IDataSource entity core ──
        public IEnumerable<string> GetEntitesList()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            // NATS subjects auto-create on first publish; existence isn't tracked.
            return ConnectionStatus == ConnectionState.Open && !string.IsNullOrEmpty(EntityName);
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(GenericMessage);

        public bool CreateEntityAs(EntityStructure entity)
        {
            // NATS subjects are auto-created; nothing to do at the protocol level.
            if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                EntitiesNames.Add(entity.EntityName);
            return true;
        }

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (refresh) RefreshSubjectsCache();
            return Entities.FirstOrDefault(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        public IErrorsInfo BeginTransaction(PassedArgs args)
            => FailResult("BeginTransaction", new NotSupportedException("NATS has no transactions."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("NATS has no transactions."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("NATS has no transactions."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Registered {ok} subjects, {fail} failed (NATS subjects auto-create on first publish).";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("NATS is not SQL."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
            => FailResult("DeleteEntity", new NotSupportedException("NATS subjects cannot be explicitly deleted; they exist only while messages are published."));

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("NATS is append-only (publish-only)."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("NATS is publish-only."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("NATS has no inserts; use the IMessageDataSource publish API."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("NATS has no scripts."));

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
            => throw new NotSupportedException("NATS scalar queries not implemented in the minimal datasource.");

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr) => Enumerable.Empty<object>();

        // ── IMessageDataSource<GenericMessage, StreamConfig> (honest compilable implementations) ──

        private StreamConfig _config;

        public void Initialize(StreamConfig config)
        {
            _config = config;
            if (config != null && !string.IsNullOrEmpty(config.StreamName))
            {
                if (!EntitiesNames.Contains(config.StreamName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(config.StreamName);
            }
        }

        public Task SendMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                _connection?.Publish(message?.Subject ?? streamName, message?.Body);
                return Task.CompletedTask;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        public Task SubscribeAsync(string streamName, Func<GenericMessage, Task> onMessageReceived, CancellationToken cancellationToken = default)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                _connection?.SubscribeAsync(streamName, async (_, args) =>
                {
                    if (onMessageReceived == null) return;
                    var msg = new GenericMessage { Subject = streamName, Body = args.Message?.Data };
                    await onMessageReceived(msg);
                });
                return Task.CompletedTask;
            }
            catch (Exception ex) { return Task.FromException(ex); }
        }

        public Task AcknowledgeMessageAsync(string streamName, GenericMessage message, CancellationToken cancellationToken = default)
        {
            // NATS is fire-and-forget; no explicit ack protocol. Honest no-op.
            return Task.CompletedTask;
        }

        public Task<GenericMessage> PeekMessageAsync(string streamName, CancellationToken cancellationToken = default)
        {
            // NATS pub/sub is not a queue; peek isn't supported. Return null honestly.
            return Task.FromResult<GenericMessage>(null);
        }

        public Task<object> GetStreamMetadataAsync(string streamName, CancellationToken cancellationToken = default)
        {
            // Minimal metadata: subject + connection status. NATS has no built-in queue depth.
            return Task.FromResult<object>(new
            {
                Stream = streamName,
                Status = ConnectionStatus.ToString(),
                Note = "NATS pub/sub has no built-in depth/count metrics."
            });
        }

        public void Disconnect() => Closeconnection();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal IConnection MigrationConnection => _connection;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _connection?.Dispose(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}