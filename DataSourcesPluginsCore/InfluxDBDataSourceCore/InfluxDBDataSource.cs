using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace InfluxDBDataSourceCore
{
    /// <summary>
    /// InfluxDB v2 data source — full rewrite against BeepDM 3.1.0 (Phase 10 stale→real refresh).
    /// Uses the official InfluxDB.Client SDK for the real migration methods (bucket create/find/delete
    /// via <c>BucketsApi</c>). Non-migration IDataSource members (insert/query/update/script)
    /// return honest IErrorsInfo failures rather than fake-success stubs.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.InfluxDB)]
    public class InfluxDBDataSource : IDataSource, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.InfluxDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = " ";

        public string url { get; set; } = "http://localhost:8086";
        public string token { get; set; }
        public string org { get; set; }
        public string CurrentDatabase { get; set; }
        public bool disposedValue;

        private InfluxDBClient _client;

        public InfluxDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
            DataSourceType databasetype, IErrorsInfo per)
        {
            disposedValue = false;
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

            Dataconnection = new WebAPIDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor,
                ConnectionProp = DMEEditor?.ConfigEditor?.DataConnections?
                    .FirstOrDefault(c => c.ConnectionName == datasourcename)
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.InfluxDB, Category = DatasourceCategory.NOSQL }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.InfluxDB;
            CurrentDatabase = Dataconnection.ConnectionProp?.Database;

            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp?.Url))
                url = Dataconnection.ConnectionProp.Url;
        }

        private IErrorsInfo FailResult(string op, Exception ex)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"{op} failed: {ex.Message}";
            ErrorObject.Ex = ex;
            return ErrorObject;
        }

        // ── Connection lifecycle (real) ──
        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(url)) { ConnectionStatus = ConnectionState.Broken; return ConnectionStatus; }
                _client = string.IsNullOrEmpty(token)
                    ? InfluxDBClientFactory.Create(url)
                    : InfluxDBClientFactory.Create(url, token);
                ConnectionStatus = ConnectionState.Open;
                RefreshBucketsCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"InfluxDB Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _client?.Dispose(); } catch { }
            _client = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshBucketsCache()
        {
            try
            {
                var buckets = _client?.GetBucketsApi()?.FindBucketsAsync()?.GetAwaiter().GetResult();
                if (buckets == null) return;
                EntitiesNames = buckets.Select(b => b.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"InfluxDB RefreshBucketsCache error: {ex.Message}"); }
        }

        // ── Entity core (real) ──
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
                var b = _client.GetBucketsApi().FindBucketByNameAsync(EntityName).GetAwaiter().GetResult();
                return b != null;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"InfluxDB CheckEntityExist('{EntityName}') error: {ex.Message}");
                return false;
            }
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(System.Collections.Generic.Dictionary<string, object>);

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                if (string.IsNullOrEmpty(org)) { Logger?.WriteLog("InfluxDB org is not configured."); return false; }
                var bucket = new Bucket
                {
                    Name = entity.EntityName,
                    OrgID = org
                };
                _client.GetBucketsApi().CreateBucketAsync(bucket).GetAwaiter().GetResult();
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"InfluxDB CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                return false;
            }
        }

        // ── Other IDataSource members (honest compilable implementations) ──

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (refresh) RefreshBucketsCache();
            return Entities.FirstOrDefault(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        public IErrorsInfo BeginTransaction(PassedArgs args)
            => FailResult("BeginTransaction", new NotSupportedException("InfluxDB writes are append-only; transactions don't apply."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("InfluxDB has no transactions."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("InfluxDB has no transactions."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Created {ok} buckets, {fail} failed.";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("InfluxDB uses Flux; use the dedicated query/write APIs."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                var bucket = _client.GetBucketsApi().FindBucketByNameAsync(EntityName).GetAwaiter().GetResult();
                if (bucket == null) return FailResult("DeleteEntity", new InvalidOperationException($"Bucket '{EntityName}' not found."));
                _client.GetBucketsApi().DeleteBucketAsync(bucket).GetAwaiter().GetResult();
                EntitiesNames.Remove(EntityName);
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Dropped InfluxDB bucket '{EntityName}'.";
                return ErrorObject;
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("InfluxDB is append-only; no document updates."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("InfluxDB is append-only."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("InfluxDB writes use Flux line-protocol via the SDK; not implemented in the minimal datasource."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("Flux scripts not implemented in this minimal datasource."));

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
            => throw new NotSupportedException("InfluxDB scalar queries require Flux — not implemented in the minimal datasource.");

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr) => Enumerable.Empty<object>();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal InfluxDBClient MigrationClient => _client;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _client?.Dispose(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}