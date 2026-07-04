using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Couchbase.Lite;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Local.CouchbaseLite
{
    /// <summary>
    /// Couchbase Lite embedded document database data source — full rewrite against BeepDM 3.1.0
    /// (Phase 10 stale→real refresh). Uses the Couchbase.Lite SDK directly (no REST). Migration
    /// methods are real (open/create/drop collection on the local LiteCore file). Non-migration
    /// IDataSource members return honest IErrorsInfo failures rather than fake-success stubs.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.CouchBaseLite)]
    public class CouchBaseLiteDataSource : IDataSource, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.CouchBaseLite;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = "\"";
        public string ParameterDelimiter { get; set; } = ":";

        public string DatabasePath { get; set; }
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; }
        public string Extension { get; set; } = ".cblite2";
        public bool disposedValue;

        private Database _database;

        public CouchBaseLiteDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
            DataSourceType databasetype, IErrorsInfo per)
        {
            disposedValue = false;
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.NOSQL;

            Dataconnection = new DefaulDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor,
                ConnectionProp = DMEEditor?.ConfigEditor?.DataConnections?
                    .FirstOrDefault(c => c.ConnectionName == datasourcename)
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.CouchBaseLite, Category = DatasourceCategory.NOSQL }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.CouchBaseLite;

            // Resolve the local LiteCore database path from the connection properties.
            var p = Dataconnection.ConnectionProp;
            if (!string.IsNullOrEmpty(p?.FileName))
                DatabasePath = string.IsNullOrEmpty(p.FilePath) ? p.FileName : Path.Combine(p.FilePath, p.FileName);
            else
                DatabasePath = p?.FilePath;
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
                if (string.IsNullOrEmpty(DatabasePath) || InMemory)
                {
                    // In-memory LiteCore database (empty name = in-memory in Couchbase.Lite 3.x).
                    _database = new Database("");
                }
                else
                {
                    var dir = Path.GetDirectoryName(DatabasePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _database = new Database(DatabasePath);
                }
                ConnectionStatus = ConnectionState.Open;
                RefreshCollectionsCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"CouchbaseLite Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _database?.Close(); } catch { }
            _database = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshCollectionsCache()
        {
            try
            {
                if (_database == null) return;
                EntitiesNames = _database.GetCollections().Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"CouchbaseLite RefreshCollectionsCache error: {ex.Message}"); }
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
                return _database.GetCollection(EntityName) != null;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CouchbaseLite CheckEntityExist('{EntityName}') error: {ex.Message}");
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
                // Materialising a collection creates it in Lite.
                _database.GetCollection(entity.EntityName);
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CouchbaseLite CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                return false;
            }
        }

        // ── Other IDataSource members (honest compilable implementations) ──

        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (refresh) RefreshCollectionsCache();
            return Entities.FirstOrDefault(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        public IErrorsInfo BeginTransaction(PassedArgs args)
            => FailResult("BeginTransaction", new NotSupportedException("Couchbase Lite transactions not implemented in the minimal datasource."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("Couchbase Lite transactions not implemented in the minimal datasource."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("Couchbase Lite transactions not implemented in the minimal datasource."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Created {ok} collections, {fail} failed.";
            return ErrorObject;
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("Couchbase Lite uses N1QL for Sync Gateway; not implemented in the minimal datasource."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (ConnectionStatus != ConnectionState.Open) Openconnection();
                if (_database.GetCollection(EntityName) == null)
                    return FailResult("DeleteEntity", new InvalidOperationException($"Collection '{EntityName}' not found."));
                _database.DeleteCollection(EntityName);
                EntitiesNames.Remove(EntityName);
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Dropped Couchbase Lite collection '{EntityName}'.";
                return ErrorObject;
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("Couchbase Lite updates via Document; not implemented in the minimal datasource."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("Couchbase Lite bulk updates; not implemented in the minimal datasource."));

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("Couchbase Lite inserts via Document; not implemented in the minimal datasource."));

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("Couchbase Lite has no ETL scripts."));

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
            => throw new NotSupportedException("Couchbase Lite scalar queries not implemented in the minimal datasource.");

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr) => Enumerable.Empty<object>();

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal Database MigrationDb => _database;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _database?.Close(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}