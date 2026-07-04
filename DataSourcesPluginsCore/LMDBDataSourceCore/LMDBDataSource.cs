#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Connections;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using LightningDB;

namespace LMDBDataSourceCore
{
    /// <summary>
    /// LMDB (Lightning Memory-Mapped Database) data source — Phase 11 implementation against BeepDM 3.1.0.
    /// LMDB is the fastest transactional embedded KV store (OpenLDAP, Monero). Uses one env file with
    /// multiple named databases (one per entity). Single-writer / many-reader; uses transactions
    /// for all DDL and DML.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.KVStore, DatasourceType = DataSourceType.LMDB)]
    public partial class LMDBDataSource : IDataSource, ILocalDB, IInMemoryDB, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.LMDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.KVStore;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = " ";

        public string DatabasePath { get; set; }
        public long MapSize { get; set; } = 100L * 1024 * 1024; // 100 MB default
        public int MaxDatabases { get; set; } = 64;
        public bool disposedValue;

        private LightningEnvironment? _env;
        private readonly object _writerLock = new();
        private const int SampleSizeForIntrospection = 100;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public LMDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
            DataSourceType databasetype, IErrorsInfo per)
        {
            disposedValue = false;
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.KVStore;

            Dataconnection = new DefaulDataConnection
            {
                Logger = logger,
                ErrorObject = ErrorObject,
                DMEEditor = pDMEEditor,
                ConnectionProp = DMEEditor?.ConfigEditor?.DataConnections?
                    .FirstOrDefault(c => c.ConnectionName == datasourcename)
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.LMDB, Category = DatasourceCategory.KVStore }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.KVStore;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.LMDB;

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
            Logger?.WriteLog($"LMDB {op} error: {ex.Message}");
            return ErrorObject;
        }

        private IErrorsInfo OkResult(string message)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = message;
            return ErrorObject;
        }

        private static byte[] EncodingKey(string key) => Encoding.UTF8.GetBytes(key ?? string.Empty);
        private static byte[] EmptyJson => Encoding.UTF8.GetBytes("{}");

        // ── Connection lifecycle ──
        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(DatabasePath))
                {
                    ConnectionStatus = ConnectionState.Broken;
                    FailResult("Openconnection", new InvalidOperationException("DatabasePath is empty — set ConnectionProperties.FilePath/FileName."));
                    return ConnectionStatus;
                }
                var dir = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                _env = new LightningEnvironment(DatabasePath, new EnvironmentConfiguration
                {
                    MapSize = MapSize,
                    MaxDatabases = MaxDatabases
                });
                _env.Open(EnvironmentOpenFlags.None, UnixAccessMode.Default);
                ConnectionStatus = ConnectionState.Open;
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                FailResult("Openconnection", ex);
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try { _env?.Dispose(); } catch { }
            _env = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        internal bool EnsureOpen()
        {
            if (ConnectionStatus != ConnectionState.Open || _env == null) Openconnection();
            return _env != null && ConnectionStatus == ConnectionState.Open;
        }

        internal LightningDatabase OpenDb(LightningTransaction tx, string name)
        {
            try
            {
                return tx.OpenDatabase(name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true);
            }
            catch
            {
                // First-time: create via read-write TX
                if (!tx.IsReadOnly)
                {
                    var cfg = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };
                    return tx.OpenDatabase(name, cfg, closeOnDispose: true);
                }
                throw;
            }
        }

        // ── Entity core ──
        public IEnumerable<string> GetEntitesList()
        {
            // LMDB does not expose a "list databases" API; we track them ourselves.
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            if (string.IsNullOrEmpty(EntityName)) return false;
            return EntitiesNames.Contains(EntityName, StringComparer.OrdinalIgnoreCase);
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(Dictionary<string, object>);

        public bool CreateEntityAs(EntityStructure entity)
        {
            lock (_writerLock)
            {
                try
                {
                    if (!EnsureOpen()) return false;
                    if (string.IsNullOrEmpty(entity?.EntityName)) return false;
                    if (CheckEntityExist(entity.EntityName)) return true;
                    using var tx = _env!.BeginTransaction(TransactionBeginFlags.None);
                    _ = OpenDb(tx, entity.EntityName);
                    tx.Commit();
                    if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                        EntitiesNames.Add(entity.EntityName);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"LMDB CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                    return false;
                }
            }
        }

        // ── Schema introspection ──
        public EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            if (string.IsNullOrEmpty(EntityName)) return null;
            if (!refresh)
            {
                var cached = Entities.FirstOrDefault(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                if (cached != null) return cached;
            }
            try
            {
                if (!EnsureOpen() || !CheckEntityExist(EntityName)) return null;

                var sampled = new List<Dictionary<string, object>>();
                using (var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly))
                using (var db = OpenDb(tx, EntityName))
                using (var cursor = tx.CreateCursor(db))
                {
                    var (rc, _, value) = cursor.First();
                    int n = 0;
                    while (rc == MDBResultCode.Success && n < SampleSizeForIntrospection)
                    {
                        var bytes = value.CopyToNewArray();
                        var dict = DeserializeJson(bytes);
                        if (dict != null) sampled.Add(dict);
                        n++;
                        (rc, _, value) = cursor.Next();
                    }
                }

                var es = new EntityStructure(EntityName)
                {
                    EntityName = EntityName,
                    DatabaseType = DataSourceType.LMDB,
                    DataSourceID = DatasourceName,
                    Caption = EntityName,
                    Description = $"LMDB named database '{EntityName}'",
                    OriginalEntityName = EntityName,
                    DatasourceEntityName = EntityName,
                    Fields = new List<EntityField>()
                };

                var fieldTypes = new Dictionary<string, (Type Type, int Count)>(StringComparer.OrdinalIgnoreCase);
                foreach (var doc in sampled)
                {
                    foreach (var kv in doc)
                    {
                        var t = kv.Value?.GetType() ?? typeof(object);
                        if (fieldTypes.TryGetValue(kv.Key, out var prev))
                            fieldTypes[kv.Key] = (t, prev.Count + 1);
                        else
                            fieldTypes[kv.Key] = (t, 1);
                    }
                }
                foreach (var (name, stat) in fieldTypes.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                {
                    es.Fields.Add(BuildEntityField(name, stat.Type, isKey: string.Equals(name, "_id", StringComparison.OrdinalIgnoreCase), allowNull: true));
                }
                var idx = Entities.FindIndex(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) Entities[idx] = es; else Entities.Add(es);
                return es;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LMDB GetEntityStructure('{EntityName}') error: {ex.Message}");
                return null;
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        private static EntityField BuildEntityField(string name, Type clrType, bool isKey, bool allowNull)
        {
            var f = new EntityField
            {
                FieldName = name,
                Fieldtype = clrType.Name,
                FieldCategory = MapClrTypeToDbFieldCategory(clrType),
                AllowDBNull = allowNull,
                IsKey = isKey,
                IsAutoIncrement = false,
                IsRequired = !allowNull,
                IsUnique = false,
                Size = 0,
                Size1 = 0,
                Size2 = 0,
                ColumnName = name,
                ColumnTypeName = clrType.Name
            };
            return f;
        }

        private static DbFieldCategory MapClrTypeToDbFieldCategory(Type t)
        {
            if (t == typeof(string)) return DbFieldCategory.String;
            if (t == typeof(int) || t == typeof(short)) return DbFieldCategory.Integer;
            if (t == typeof(long)) return DbFieldCategory.Long;
            if (t == typeof(float) || t == typeof(double)) return DbFieldCategory.Double;
            if (t == typeof(decimal)) return DbFieldCategory.Decimal;
            if (t == typeof(bool)) return DbFieldCategory.Boolean;
            if (t == typeof(DateTime)) return DbFieldCategory.DateTime;
            if (t == typeof(byte[])) return DbFieldCategory.Binary;
            if (t == typeof(Guid)) return DbFieldCategory.Guid;
            return DbFieldCategory.String;
        }

        // ── Transactions ──
        public IErrorsInfo BeginTransaction(PassedArgs args)
            => FailResult("BeginTransaction", new NotSupportedException("LMDB transactions are managed per-op by the LMDB data source; explicit BeginTransaction is not supported. Each write wraps a short-lived RW transaction."));

        public IErrorsInfo Commit(PassedArgs args)
            => FailResult("Commit", new NotSupportedException("LMDB transactions are managed per-op; see BeginTransaction."));

        public IErrorsInfo EndTransaction(PassedArgs args)
            => FailResult("EndTransaction", new NotSupportedException("LMDB transactions are managed per-op; see BeginTransaction."));

        // ── DDL ──
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            return fail == 0
                ? OkResult($"Created {ok} LMDB named databases.")
                : FailResult("CreateEntities", new InvalidOperationException($"Created {ok} databases, {fail} failed."));
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("LMDB has no SQL dialect; use Put/Get/Delete via RunQuery or the typed CRUD methods."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            lock (_writerLock)
            {
                try
                {
                    if (!EnsureOpen()) return FailResult("DeleteEntity", new InvalidOperationException("Database is not open."));
                    if (string.IsNullOrEmpty(EntityName)) return FailResult("DeleteEntity", new ArgumentNullException(nameof(EntityName)));
                    if (!CheckEntityExist(EntityName))
                        return FailResult("DeleteEntity", new InvalidOperationException($"Named database '{EntityName}' not found."));

                    using var tx = _env!.BeginTransaction(TransactionBeginFlags.None);
                    using (var db = OpenDb(tx, EntityName))
                    {
                        if (UploadDataRow != null)
                        {
                            int removed = 0;
                            foreach (var key in ToKeyStrings(UploadDataRow))
                            {
                                var rc = tx.Delete(db, EncodingKey(key));
                                if (rc == MDBResultCode.Success) removed++;
                            }
                            tx.Commit();
                            return OkResult($"Removed {removed} entries from LMDB named database '{EntityName}'.");
                        }
                        // Drop the entire named database.
                        var dropRc = tx.DropDatabase(db);
                        if (dropRc != MDBResultCode.Success)
                        {
                            tx.Abort();
                            return FailResult("DeleteEntity", new InvalidOperationException($"DropDatabase returned {dropRc}."));
                        }
                    }
                    tx.Commit();
                    EntitiesNames.Remove(EntityName);
                    Entities.RemoveAll(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                    return OkResult($"Dropped LMDB named database '{EntityName}'.");
                }
                catch (Exception ex) { return FailResult("DeleteEntity", ex); }
            }
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
            => PutEntries(EntityName, UploadDataRow, "UpdateEntity");

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
            => PutEntries(EntityName, UploadData, "UpdateEntities", progress: progress);

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
            => PutEntries(EntityName, InsertedData, "InsertEntity");

        private IErrorsInfo PutEntries(string EntityName, object data, string op, IProgress<PassedArgs>? progress = null)
        {
            lock (_writerLock)
            {
                try
                {
                    if (!EnsureOpen()) return FailResult(op, new InvalidOperationException("Database is not open."));
                    if (string.IsNullOrEmpty(EntityName)) return FailResult(op, new ArgumentNullException(nameof(EntityName)));
                    if (!CreateEntityAs(new EntityStructure(EntityName)))
                        return FailResult(op, new InvalidOperationException($"Failed to ensure LMDB named database '{EntityName}'."));

                    var dicts = ToDictionaries(data).ToList();
                    int done = 0;
                    using var tx = _env!.BeginTransaction(TransactionBeginFlags.None);
                    using var db = OpenDb(tx, EntityName);
                    foreach (var d in dicts)
                    {
                        var key = ExtractKey(d, out var k) ? k : Guid.NewGuid().ToString();
                        var val = SerializeJson(d);
                        var rc = tx.Put(db, EncodingKey(key), val, PutOptions.None);
                        if (rc != MDBResultCode.Success)
                        {
                            tx.Abort();
                            return FailResult(op, new InvalidOperationException($"Put returned {rc} after {done} entries."));
                        }
                        done++;
                        progress?.Report(new PassedArgs { ParameterString1 = $"{done}/{dicts.Count}", ParameterInt1 = done });
                    }
                    tx.Commit();
                    return OkResult($"{op} wrote {done} entries to LMDB named database '{EntityName}'.");
                }
                catch (Exception ex) { return FailResult(op, ex); }
            }
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("LMDB has no ETL scripts; use Put/Delete directly."));

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
        {
            if (entities == null) yield break;
            foreach (var e in entities)
            {
                yield return new ETLScriptDet
                {
                    Ddl = $"-- LMDB: named database '{e.EntityName}' is created on first Put/DDL.",
                    ScriptType = DDLScriptType.CreateEntity,
                    DestinationEntityName = e.EntityName,
                    DestinationDataSourceName = DatasourceName
                };
            }
        }

        public IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
            => Enumerable.Empty<ChildRelation>();

        public IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
            => Enumerable.Empty<RelationShipKeys>();

        // ── Query ──
        public IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            if (!EnsureOpen() || string.IsNullOrEmpty(EntityName)) yield break;
            if (!CheckEntityExist(EntityName)) yield break;
            var filterFn = BuildFilterFn(filter);
            using var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = OpenDb(tx, EntityName);
            using var cursor = tx.CreateCursor(db);
            var (rc, _, value) = cursor.First();
            while (rc == MDBResultCode.Success)
            {
                var dict = DeserializeJson(value.CopyToNewArray());
                if (dict != null)
                {
                    if (filterFn == null || filterFn(dict)) yield return dict;
                }
                (rc, _, value) = cursor.Next();
            }
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);
            if (!EnsureOpen() || string.IsNullOrEmpty(EntityName))
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);
            if (!CheckEntityExist(EntityName))
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);

            var filterFn = BuildFilterFn(filter);
            int total = 0;
            int skip = (pageNumber - 1) * pageSize;
            var slice = new List<object>(pageSize);
            using var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = OpenDb(tx, EntityName);
            using var cursor = tx.CreateCursor(db);
            var (rc, _, value) = cursor.First();
            while (rc == MDBResultCode.Success)
            {
                var dict = DeserializeJson(value.CopyToNewArray());
                if (dict != null && (filterFn == null || filterFn(dict)))
                {
                    if (total >= skip && slice.Count < pageSize) slice.Add(dict);
                    total++;
                }
                (rc, _, value) = cursor.Next();
            }
            return new PagedResult(slice, pageNumber, pageSize, total);
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
            => Task.FromResult(GetEntity(EntityName, Filter));

        public double GetScalar(string query)
        {
            if (!EnsureOpen()) return 0d;
            if (string.IsNullOrWhiteSpace(query)) return 0d;
            var m = Regex.Match(query, @"^\s*COUNT\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && CheckEntityExist(m.Groups[1].Value))
            {
                using var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = OpenDb(tx, m.Groups[1].Value);
                return tx.GetEntriesCount(db);
            }
            return 0d;
        }

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr)
        {
            if (!EnsureOpen() || string.IsNullOrWhiteSpace(qrystr)) yield break;
            var m = Regex.Match(qrystr, @"^\s*GET\s+([A-Za-z_]\w*)\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && CheckEntityExist(m.Groups[1].Value))
            {
                using var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = OpenDb(tx, m.Groups[1].Value);
                var (rc, _, value) = tx.Get(db, EncodingKey(m.Groups[2].Value));
                if (rc == MDBResultCode.Success)
                {
                    var d = DeserializeJson(value.CopyToNewArray());
                    if (d != null) yield return d;
                }
                yield break;
            }
            m = Regex.Match(qrystr, @"^\s*(SCAN|COUNT)\s+([A-Za-z_]\w*)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && CheckEntityExist(m.Groups[2].Value))
            {
                using var tx = _env!.BeginTransaction(TransactionBeginFlags.ReadOnly);
                using var db = OpenDb(tx, m.Groups[2].Value);
                using var cursor = tx.CreateCursor(db);
                var (rc, _, value) = cursor.First();
                while (rc == MDBResultCode.Success)
                {
                    var d = DeserializeJson(value.CopyToNewArray());
                    if (d != null) yield return d;
                    (rc, _, value) = cursor.Next();
                }
            }
        }

        // ── Colocated schema-migration provider accessors ──
        internal LightningEnvironment? MigrationEnv => _env;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) { try { _env?.Dispose(); } catch { } }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ── Helpers ──
        private static byte[] SerializeJson(Dictionary<string, object> dict)
            => JsonSerializer.SerializeToUtf8Bytes(dict, JsonOpts);

        private static Dictionary<string, object>? DeserializeJson(byte[]? bytes)
        {
            try
            {
                if (bytes == null || bytes.Length == 0) return null;
                using var doc = JsonDocument.Parse(bytes);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in root.EnumerateObject())
                {
                    result[prop.Name] = JsonElementToObject(prop.Value);
                }
                return result;
            }
            catch { return null; }
        }

        private static object JsonElementToObject(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => el.Deserialize<Dictionary<string, object>>(),
            JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToObject).ToList(),
            _ => el.ToString()
        };

        private static bool ExtractKey(Dictionary<string, object> dict, out string key)
        {
            if (dict.TryGetValue("_id", out var v) && v != null) { key = v.ToString() ?? Guid.NewGuid().ToString(); return true; }
            if (dict.TryGetValue("id", out var v2) && v2 != null) { key = v2.ToString() ?? Guid.NewGuid().ToString(); return true; }
            key = Guid.NewGuid().ToString();
            return false;
        }

        private static IEnumerable<string> ToKeyStrings(object data)
        {
            if (data == null) yield break;
            if (data is string s) { yield return s; yield break; }
            if (data is IEnumerable<string> ss) { foreach (var x in ss) yield return x; yield break; }
            if (data is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    if (item is string ss2) { yield return ss2; continue; }
                    if (item is IDictionary<string, object> d)
                    {
                        if (d.TryGetValue("_id", out var v) && v != null) { yield return v.ToString() ?? Guid.NewGuid().ToString(); continue; }
                        if (d.TryGetValue("id", out var v2) && v2 != null) { yield return v2.ToString() ?? Guid.NewGuid().ToString(); continue; }
                    }
                }
            }
        }

        private static IEnumerable<Dictionary<string, object>> ToDictionaries(object data)
        {
            if (data == null) yield break;
            switch (data)
            {
                case Dictionary<string, object> single:
                    yield return single;
                    break;
                case IDictionary ndSingle:
                    yield return DictionaryFromNonGeneric(ndSingle);
                    break;
                case DataTable dt:
                    foreach (DataRow r in dt.Rows) yield return DataRowToDictionary(r);
                    break;
                case DataRow dr:
                    yield return DataRowToDictionary(dr);
                    break;
                case IEnumerable<Dictionary<string, object>> listOfDict:
                    foreach (var d in listOfDict) yield return d;
                    break;
                case IEnumerable enumerable:
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        if (item is Dictionary<string, object> d2) { yield return d2; continue; }
                        if (item is IDictionary nd) { yield return DictionaryFromNonGeneric(nd); continue; }
                        if (item is string || item.GetType().IsPrimitive) continue;
                        yield return JsonToDictionary(JsonSerializer.Serialize(item, JsonOpts));
                    }
                    break;
                default:
                    yield return JsonToDictionary(JsonSerializer.Serialize(data, JsonOpts));
                    break;
            }
        }

        private static Dictionary<string, object> DataRowToDictionary(DataRow r)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in r.Table.Columns)
            {
                dict[c.ColumnName] = r.IsNull(c) ? null : r[c];
            }
            return dict;
        }

        private static Dictionary<string, object> DictionaryFromNonGeneric(IDictionary d)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry kv in d)
            {
                var key = kv.Key?.ToString();
                if (string.IsNullOrEmpty(key)) continue;
                dict[key] = kv.Value;
            }
            return dict;
        }

        private static Dictionary<string, object> JsonToDictionary(string json)
        {
            try
            {
                var d = JsonSerializer.Deserialize<Dictionary<string, object>>(json, JsonOpts);
                return d ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static Func<Dictionary<string, object>, bool>? BuildFilterFn(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0) return null;
            return dict =>
            {
                foreach (var f in filters)
                {
                    if (string.IsNullOrEmpty(f?.FieldName)) continue;
                    if (!dict.TryGetValue(f.FieldName, out var actual)) return false;
                    var op = (f.Operator ?? "=").Trim().ToLowerInvariant();
                    var wanted = f.FilterValue;
                    int cmp = CompareScalars(actual, wanted);
                    bool pass = op switch
                    {
                        "=" or "==" or "eq" => cmp == 0,
                        "!=" or "<>" or "ne" => cmp != 0,
                        ">" or "gt" => cmp > 0,
                        ">=" or "gte" => cmp >= 0,
                        "<" or "lt" => cmp < 0,
                        "<=" or "lte" => cmp <= 0,
                        _ => cmp == 0
                    };
                    if (!pass) return false;
                }
                return true;
            };
        }

        private static int CompareScalars(object actual, string wanted)
        {
            try
            {
                if (actual == null) return wanted == null ? 0 : -1;
                if (actual is IConvertible || actual is string)
                {
                    var a = Convert.ToDouble(actual);
                    var b = string.IsNullOrEmpty(wanted) ? 0 : Convert.ToDouble(wanted);
                    return a.CompareTo(b);
                }
                return string.CompareOrdinal(actual.ToString(), wanted);
            }
            catch { return 0; }
        }

        // ── ILocalDB (file-lifecycle for embedded DB) ──
        public bool CanCreateLocal { get; set; } = true;
        public bool InMemory { get; set; } = false;
        public string Extension { get; set; } = ".lmdb";

        public bool CreateDB()
        {
            try
            {
                var path = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(path)) return false;
                return CreateDB(path);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LMDB CreateDB error: {ex.Message}");
                return false;
            }
        }

        public bool CreateDB(bool inMemory)
        {
            InMemory = inMemory;
            if (inMemory)
            {
                // LMDB has no in-memory mode. Use a temp dir.
                var tmp = Path.Combine(Path.GetTempPath(), "beep-lmdb-" + Guid.NewGuid().ToString("N"));
                try { Directory.CreateDirectory(tmp); } catch { }
                Dataconnection!.ConnectionProp.FilePath = tmp;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                DatabasePath = tmp;
                return Openconnection() == ConnectionState.Open;
            }
            return CreateDB();
        }

        public bool CreateDB(string filepathandname)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filepathandname)) return false;
                var dir = Path.GetDirectoryName(filepathandname);
                var name = Path.GetFileName(filepathandname);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                Closeconnection();
                Dataconnection!.ConnectionProp.FilePath = dir ?? string.Empty;
                Dataconnection.ConnectionProp.FileName = name;
                DatabasePath = filepathandname;
                return Openconnection() == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LMDB CreateDB('{filepathandname}') error: {ex.Message}");
                return false;
            }
        }

        public bool DeleteDB()
        {
            try
            {
                Closeconnection();
                var path = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;
                Directory.Delete(path, recursive: true);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LMDB DeleteDB error: {ex.Message}");
                return false;
            }
        }

        public bool CopyDB(string DestDbName, string DesPath)
        {
            try
            {
                Closeconnection();
                var src = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(src) || !Directory.Exists(src)) return false;
                if (string.IsNullOrWhiteSpace(DesPath)) return false;
                if (!Directory.Exists(DesPath)) Directory.CreateDirectory(DesPath);
                var targetName = string.IsNullOrWhiteSpace(DestDbName) ? new DirectoryInfo(src).Name : DestDbName;
                var dest = Path.Combine(DesPath, targetName);
                CopyDirectoryRecursive(src, dest);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LMDB CopyDB error: {ex.Message}");
                return false;
            }
        }

        public IErrorsInfo DropEntity(string EntityName)
            => DeleteEntity(EntityName, null);

        private string ResolveLocalDbPath()
        {
            var p = Dataconnection?.ConnectionProp;
            if (p == null) return null;
            if (!string.IsNullOrEmpty(p.FileName))
                return string.IsNullOrEmpty(p.FilePath) ? p.FileName : Path.Combine(p.FilePath, p.FileName);
            return p.FilePath;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                var destFile = Path.Combine(destDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                File.Copy(file, destFile, overwrite: true);
            }
        }
    }
}