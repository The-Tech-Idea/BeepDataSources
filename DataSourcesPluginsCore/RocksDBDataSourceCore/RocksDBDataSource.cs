#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
using RocksDbSharp;

namespace RocksDBDataSourceCore
{
    /// <summary>
    /// RocksDB embedded key-value store data source — Phase 11 implementation against BeepDM 3.1.0.
    /// RocksDB is a high-performance embedded KV from Facebook (LSM tree, column families).
    /// Each entity maps to a column family inside one RocksDB instance on disk.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.KVStore, DatasourceType = DataSourceType.RocksDB)]
    public partial class RocksDBDataSource : IDataSource, ILocalDB, IInMemoryDB, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.RocksDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.KVStore;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = " ";

        public string DatabasePath { get; set; }
        public bool disposedValue;

        private RocksDb _db;
        private DbOptions _options;
        private WriteBatch _pendingBatch;
        private bool _inTransaction;
        private const int SampleSizeForIntrospection = 100;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public RocksDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.RocksDB, Category = DatasourceCategory.KVStore }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.KVStore;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.RocksDB;

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
            Logger?.WriteLog($"RocksDB {op} error: {ex.Message}");
            return ErrorObject;
        }

        private IErrorsInfo OkResult(string message)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = message;
            return ErrorObject;
        }

        // ── Connection lifecycle ──
        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(DatabasePath))
                {
                    ErrorObject = FailResult("Openconnection", new InvalidOperationException("DatabasePath is empty — set ConnectionProperties.FilePath/FileName."));
                    ConnectionStatus = ConnectionState.Broken;
                    return ConnectionStatus;
                }
                var dir = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                _options = new DbOptions()
                    .SetCreateIfMissing(true)
                    .SetCreateMissingColumnFamilies(true);
                _db = RocksDb.Open(_options, DatabasePath);
                RefreshEntitiesCache();
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
            try
            {
                if (_pendingBatch != null) { try { _pendingBatch.Dispose(); } catch { } _pendingBatch = null; }
                _inTransaction = false;
                _db?.Dispose();
                _db = null;
            }
            catch { }
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        internal bool EnsureOpen()
        {
            if (ConnectionStatus != ConnectionState.Open || _db == null) Openconnection();
            return _db != null && ConnectionStatus == ConnectionState.Open;
        }

        internal ColumnFamilyHandle GetOrCreateCf(string name)
        {
            try { return _db.GetColumnFamily(name); }
            catch { return _db.CreateColumnFamily(new ColumnFamilyOptions(), name); }
        }

        internal bool HasColumnFamily(string name)
        {
            try { _ = _db.GetColumnFamily(name); return true; }
            catch { return false; }
        }

        private void RefreshEntitiesCache()
        {
            try
            {
                if (_db == null) return;
                EntitiesNames = RocksDb.ListColumnFamilies(_options, DatabasePath)
                    .Where(n => !string.IsNullOrEmpty(n)).ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"RocksDB RefreshEntitiesCache error: {ex.Message}"); }
        }

        // ── Entity core ──
        public IEnumerable<string> GetEntitesList()
        {
            if (!EnsureOpen()) return Array.Empty<string>();
            RefreshEntitiesCache();
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (!EnsureOpen()) return false;
                return HasColumnFamily(EntityName);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"RocksDB CheckEntityExist('{EntityName}') error: {ex.Message}");
                return false;
            }
        }

        public int GetEntityIdx(string entityName)
            => EntitiesNames.FindIndex(e => string.Equals(e, entityName, StringComparison.OrdinalIgnoreCase));

        public Type GetEntityType(string EntityName) => typeof(Dictionary<string, object>);

        public bool CreateEntityAs(EntityStructure entity)
        {
            try
            {
                if (!EnsureOpen()) return false;
                if (string.IsNullOrEmpty(entity?.EntityName)) return false;
                GetOrCreateCf(entity.EntityName);
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"RocksDB CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
                return false;
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
                if (!EnsureOpen()) return null;
                if (!HasColumnFamily(EntityName)) return null;

                var cf = _db.GetColumnFamily(EntityName);
                var sampled = new List<Dictionary<string, object>>();
                using var iter = _db.NewIterator(cf, readOptions: null);
                int n = 0;
                for (iter.SeekToFirst(); iter.Valid() && n < SampleSizeForIntrospection; iter.Next())
                {
                    var valBytes = iter.Value();
                    var dict = DeserializeJson(valBytes);
                    if (dict != null) sampled.Add(dict);
                    n++;
                }

                var es = new EntityStructure(EntityName)
                {
                    EntityName = EntityName,
                    DatabaseType = DataSourceType.RocksDB,
                    DataSourceID = DatasourceName,
                    Caption = EntityName,
                    Description = $"RocksDB column family '{EntityName}'",
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
                Logger?.WriteLog($"RocksDB GetEntityStructure('{EntityName}') error: {ex.Message}");
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

        // ── Transactions (WriteBatch-based; RocksDB has no SQL transactions, only atomic batch writes) ──
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("BeginTransaction", new InvalidOperationException("Database is not open."));
                if (_inTransaction) return OkResult("RocksDB transaction already in progress.");
                _pendingBatch = new WriteBatch();
                _inTransaction = true;
                return OkResult("RocksDB transaction started (WriteBatch).");
            }
            catch (Exception ex) { return FailResult("BeginTransaction", ex); }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                if (!_inTransaction || _pendingBatch == null)
                    return FailResult("Commit", new InvalidOperationException("No active RocksDB transaction."));
                _db.Write(_pendingBatch, writeOptions: null);
                _pendingBatch.Dispose();
                _pendingBatch = null;
                _inTransaction = false;
                return OkResult("RocksDB transaction committed.");
            }
            catch (Exception ex) { return FailResult("Commit", ex); }
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                if (!_inTransaction) return OkResult("No active RocksDB transaction to end.");
                _pendingBatch?.Dispose();
                _pendingBatch = null;
                _inTransaction = false;
                return OkResult("RocksDB transaction rolled back (batch discarded).");
            }
            catch (Exception ex) { return FailResult("EndTransaction", ex); }
        }

        // ── DDL ──
        public IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            if (entities == null) return FailResult("CreateEntities", new ArgumentNullException(nameof(entities)));
            int ok = 0, fail = 0;
            foreach (var e in entities) if (CreateEntityAs(e)) ok++; else fail++;
            return fail == 0
                ? OkResult($"Created {ok} RocksDB column families.")
                : FailResult("CreateEntities", new InvalidOperationException($"Created {ok} column families, {fail} failed."));
        }

        public IErrorsInfo ExecuteSql(string sql)
            => FailResult("ExecuteSql", new NotSupportedException("RocksDB has no SQL dialect; use the native Put/Get/Remove API or pass a JSON filter to RunQuery."));

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("DeleteEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("DeleteEntity", new ArgumentNullException(nameof(EntityName)));
                if (!HasColumnFamily(EntityName))
                    return FailResult("DeleteEntity", new InvalidOperationException($"Column family '{EntityName}' not found."));

                if (UploadDataRow != null)
                {
                    var cf = _db.GetColumnFamily(EntityName);
                    int removed = DeleteMatching(cf, UploadDataRow);
                    return OkResult($"Removed {removed} entries from RocksDB column family '{EntityName}'.");
                }
                _db.DropColumnFamily(EntityName);
                EntitiesNames.Remove(EntityName);
                Entities.RemoveAll(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                return OkResult($"Dropped RocksDB column family '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        private int DeleteMatching(ColumnFamilyHandle cf, object row)
        {
            int removed = 0;
            var keys = ToKeyStrings(row);
            foreach (var key in keys)
            {
                var bytes = EncodingKey(key);
                try { _db.Remove(bytes, cf); removed++; }
                catch (Exception ex) { Logger?.WriteLog($"RocksDB Remove({key}) error: {ex.Message}"); }
            }
            return removed;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("UpdateEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("UpdateEntity", new ArgumentNullException(nameof(EntityName)));
                var cf = GetOrCreateCf(EntityName);
                var dicts = ToDictionaries(UploadDataRow).ToList();
                int updated = 0;
                foreach (var d in dicts)
                {
                    var key = ExtractKey(d, out var k) ? k : Guid.NewGuid().ToString();
                    var val = SerializeJson(d);
                    if (_inTransaction) _pendingBatch.Put(EncodingKey(key), val, cf);
                    else _db.Put(EncodingKey(key), val, cf, writeOptions: null);
                    updated++;
                }
                return OkResult($"Upserted {updated} entries into RocksDB column family '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("UpdateEntity", ex); }
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("UpdateEntities", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("UpdateEntities", new ArgumentNullException(nameof(EntityName)));
                var cf = GetOrCreateCf(EntityName);
                var dicts = ToDictionaries(UploadData).ToList();
                int done = 0;
                foreach (var d in dicts)
                {
                    var key = ExtractKey(d, out var k) ? k : Guid.NewGuid().ToString();
                    var val = SerializeJson(d);
                    if (_inTransaction) _pendingBatch.Put(EncodingKey(key), val, cf);
                    else _db.Put(EncodingKey(key), val, cf, writeOptions: null);
                    done++;
                    progress?.Report(new PassedArgs { ParameterString1 = $"{done}/{dicts.Count}", ParameterInt1 = done });
                }
                return OkResult($"Bulk-upserted {done} entries into RocksDB column family '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("UpdateEntities", ex); }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("InsertEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("InsertEntity", new ArgumentNullException(nameof(EntityName)));
                var cf = GetOrCreateCf(EntityName);
                var dicts = ToDictionaries(InsertedData).ToList();
                int inserted = 0;
                foreach (var d in dicts)
                {
                    var key = ExtractKey(d, out var k) ? k : Guid.NewGuid().ToString();
                    var val = SerializeJson(d);
                    if (_inTransaction) _pendingBatch.Put(EncodingKey(key), val, cf);
                    else _db.Put(EncodingKey(key), val, cf, writeOptions: null);
                    inserted++;
                }
                return OkResult($"Inserted {inserted} entries into RocksDB column family '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("InsertEntity", ex); }
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
            => FailResult("RunScript", new NotSupportedException("RocksDB has no ETL scripts; use Put/Remove directly."));

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
        {
            if (entities == null) yield break;
            foreach (var e in entities)
            {
                yield return new ETLScriptDet
                {
                    Ddl = $"-- RocksDB: column family '{e.EntityName}' is created lazily on first Put; no DDL required.",
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
            if (!HasColumnFamily(EntityName)) yield break;
            var cf = _db.GetColumnFamily(EntityName);
            var filterFn = BuildFilterFn(filter);
            using var iter = _db.NewIterator(cf, readOptions: null);
            for (iter.SeekToFirst(); iter.Valid(); iter.Next())
            {
                var dict = DeserializeJson(iter.Value());
                if (dict == null) continue;
                if (filterFn != null && !filterFn(dict)) continue;
                yield return dict;
            }
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);
            if (!EnsureOpen() || string.IsNullOrEmpty(EntityName))
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);
            if (!HasColumnFamily(EntityName))
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);
            var cf = _db.GetColumnFamily(EntityName);
            var filterFn = BuildFilterFn(filter);
            int total = 0;
            int skip = (pageNumber - 1) * pageSize;
            var slice = new List<object>(pageSize);
            using var iter = _db.NewIterator(cf, readOptions: null);
            for (iter.SeekToFirst(); iter.Valid(); iter.Next())
            {
                var dict = DeserializeJson(iter.Value());
                if (dict == null) continue;
                if (filterFn != null && !filterFn(dict)) continue;
                if (total >= skip && slice.Count < pageSize) slice.Add(dict);
                total++;
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
            if (m.Success && HasColumnFamily(m.Groups[1].Value))
            {
                int n = 0;
                using var iter = _db.NewIterator(_db.GetColumnFamily(m.Groups[1].Value), readOptions: null);
                for (iter.SeekToFirst(); iter.Valid(); iter.Next()) n++;
                return n;
            }
            return 0d;
        }

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr)
        {
            if (!EnsureOpen() || string.IsNullOrWhiteSpace(qrystr)) yield break;
            // Recognised: "GET <cf>:<key>" / "SCAN <cf>" / "COUNT <cf>"
            var m = Regex.Match(qrystr, @"^\s*GET\s+([A-Za-z_]\w*)\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && HasColumnFamily(m.Groups[1].Value))
            {
                var cf = _db.GetColumnFamily(m.Groups[1].Value);
                var val = _db.Get(EncodingKey(m.Groups[2].Value), cf, readOptions: null);
                if (val != null && val.Length > 0)
                {
                    var d = DeserializeJson(val);
                    if (d != null) yield return d;
                }
                yield break;
            }
            m = Regex.Match(qrystr, @"^\s*(SCAN|COUNT)\s+([A-Za-z_]\w*)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && HasColumnFamily(m.Groups[2].Value))
            {
                var cf = _db.GetColumnFamily(m.Groups[2].Value);
                using var iter = _db.NewIterator(cf, readOptions: null);
                for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                {
                    var d = DeserializeJson(iter.Value());
                    if (d != null) yield return d;
                }
            }
        }

        // ── Colocated schema-migration provider accessors ──
        internal RocksDb MigrationDb => _db;
        internal bool HasActiveTransaction => _inTransaction;
        internal void EnsureMigrationConnected()
        {
            if (ConnectionStatus != ConnectionState.Open) Openconnection();
        }

        // ── Dispose ──
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing)
            {
                try { if (_pendingBatch != null) { _pendingBatch.Dispose(); _pendingBatch = null; } } catch { }
                try { _db?.Dispose(); } catch { }
            }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ── Helpers ──
        private static byte[] EncodingKey(string key)
            => System.Text.Encoding.UTF8.GetBytes(key ?? string.Empty);

        private static byte[] SerializeJson(Dictionary<string, object> dict)
            => JsonSerializer.SerializeToUtf8Bytes(dict, JsonOpts);

        private static Dictionary<string, object>? DeserializeJson(byte[] bytes)
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
                        if (item is string || item.GetType().IsPrimitive) continue; // skip bare scalars
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
        public string Extension { get; set; } = ".rocksdb";

        public bool CreateDB()
        {
            try
            {
                var p = Dataconnection?.ConnectionProp;
                if (p == null) return false;
                if (string.IsNullOrEmpty(p.FileName) && string.IsNullOrEmpty(p.FilePath)) return false;
                var path = ResolveLocalDbPath();
                if (string.IsNullOrEmpty(path)) return false;
                return CreateDB(path);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"RocksDB CreateDB error: {ex.Message}");
                return false;
            }
        }

        public bool CreateDB(bool inMemory)
        {
            InMemory = inMemory;
            if (inMemory)
            {
                // RocksDB doesn't have a clean in-memory mode without a temporary dir.
                // Create a temp dir under LocalApplicationData and use it.
                var tmp = Path.Combine(Path.GetTempPath(), "beep-rocksdb-" + Guid.NewGuid().ToString("N"));
                Dataconnection!.ConnectionProp.FilePath = tmp;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                DatabasePath = tmp;
                try { Directory.CreateDirectory(tmp); } catch { }
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
                // Open with create-if-missing to materialize the DB.
                return Openconnection() == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"RocksDB CreateDB('{filepathandname}') error: {ex.Message}");
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
                Logger?.WriteLog($"RocksDB DeleteDB error: {ex.Message}");
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
                Logger?.WriteLog($"RocksDB CopyDB error: {ex.Message}");
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