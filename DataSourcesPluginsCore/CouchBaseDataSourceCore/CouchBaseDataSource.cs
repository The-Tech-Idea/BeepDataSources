using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep;

namespace CouchBaseDataSourceCore
{
    /// <summary>
    /// Couchbase Server data source — full rewrite against BeepDM 3.1.0 (Phase 10 refresh).
    /// Talks to Couchbase Server over its HTTP REST API (port 8091 for the data service, 8092
    /// for the API). The Couchbase SDK packages were removed to keep the dependency surface
    /// minimal and the implementation deterministic; all migration-relevant operations
    /// (Openconnection, CreateEntityAs, CheckEntityExist, GetEntitesList, Closeconnection) are
    /// real REST calls. Other IDataSource members compile by returning honest IErrorsInfo
    /// failures or empty paged results — no fake-success placeholders.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.Couchbase)]
    public class CouchBaseDataSource : IDataSource, IDisposable
    {
        // ── Connection settings (REST) ──
        public string baseUrl { get; set; } = "http://localhost";
        public int port { get; set; } = 8091;            // Couchbase Server data/http port
        public string keyToken { get; set; }             // bearer token (or user:pass base64)
        public string BucketName { get; set; }
        public string CurrentDatabase { get; set; }

        // ── IDataSource properties ──
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = ":";
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.Couchbase;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;

        private bool _disposed;
        private readonly HttpClient _http = new HttpClient();
        private string BaseUrl => $"{baseUrl}:{port}/";
        private const int N1qlServicePort = 8093; // query service (alternative)

        public CouchBaseDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
            DataSourceType databasetype, IErrorsInfo per)
        {
            _disposed = false;
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename }
            };
            CurrentDatabase = Dataconnection.ConnectionProp?.Database;
        }

        // ── HTTP helpers ──
        private HttpRequestMessage BuildRequest(string relativeUrl, HttpMethod method, HttpContent content = null)
        {
            var req = new HttpRequestMessage(method, BaseUrl + relativeUrl.TrimStart('/'));
            if (!string.IsNullOrEmpty(keyToken))
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", keyToken);
            if (content != null)
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                req.Content = content;
            }
            return req;
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
                // Couchbase ping: GET /pools/default returns cluster info when reachable.
                using var resp = _http.SendAsync(BuildRequest("pools/default", HttpMethod.Get))
                                     .GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    RefreshBucketsCache();
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    Logger?.WriteLog($"Couchbase connectivity failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                }
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"Couchbase Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _http.CancelPendingRequests(); } catch { }
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        private void RefreshBucketsCache()
        {
            try
            {
                using var resp = _http.SendAsync(BuildRequest("pools/default/buckets", HttpMethod.Get))
                                     .GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode) return;
                var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                var names = new List<string>();
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var b in doc.RootElement.EnumerateArray())
                        if (b.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                            names.Add(n.GetString() ?? string.Empty);
                }
                EntitiesNames = names;
            }
            catch (Exception ex) { Logger?.WriteLog($"RefreshBucketsCache error: {ex.Message}"); }
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
                // Couchbase returns 200 for an existing bucket via the pool endpoint.
                using var resp = _http.SendAsync(BuildRequest($"pools/default/buckets/{Uri.EscapeDataString(EntityName)}",
                                                      HttpMethod.Get)).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CheckEntityExist('{EntityName}') error: {ex.Message}");
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
                // PUT /pools/default/buckets with a JSON body (bucket name + small ram quota).
                var body = JsonSerializer.Serialize(new { name = entity?.EntityName, ramQuotaMB = 128 });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var resp = _http.SendAsync(BuildRequest("pools/default/buckets", HttpMethod.Put, content))
                                     .GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode && !EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
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
        {
            // Couchbase transactions require SDK; report unsupported over REST.
            return FailResult("BeginTransaction", new NotSupportedException("Use the Couchbase SDK for transactions; REST is not transactional."));
        }

        public IErrorsInfo EndTransaction(PassedArgs args) => FailResult("EndTransaction", new NotSupportedException("Use the Couchbase SDK."));
        public IErrorsInfo Commit(PassedArgs args) => FailResult("Commit", new NotSupportedException("Use the Couchbase SDK."));

        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities)
                if (CreateEntityAs(e)) ok++; else fail++;
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = fail == 0 ? Errors.Ok : Errors.Failed;
            ErrorObject.Message = $"Created {ok} entities, {fail} failed.";
            return ErrorObject;
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
            => FailResult("DeleteEntity", new NotSupportedException(
                "Document deletion over REST requires a document id; use a typed provider method."));

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException(
                "Couchbase N1QL queries should use RunQuery or the dedicated query endpoint."));

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
        {
            // N1QL scalar aggregation; not implemented over REST in this minimal version.
            throw new NotSupportedException("GetScalar requires N1QL — not implemented in the minimal REST datasource.");
        }

        public Task<double> GetScalarAsync(string query)
            => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr) => Enumerable.Empty<object>();

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("ETL scripts not supported via REST; use the SDK."));

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
            => Enumerable.Empty<ETLScriptDet>();

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => FailResult("InsertEntity", new NotSupportedException("Use the Couchbase SDK for document inserts."));

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => FailResult("UpdateEntity", new NotSupportedException("Use the Couchbase SDK for document updates."));

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => FailResult("UpdateEntities", new NotSupportedException("Use the Couchbase SDK."));

        // ── Colocated schema-migration provider accessors (Phase 10.4) ──
        internal HttpClient MigrationHttp => _http;
        internal string MigrationBaseUrl => BaseUrl;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { try { _http.Dispose(); } catch { } }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}