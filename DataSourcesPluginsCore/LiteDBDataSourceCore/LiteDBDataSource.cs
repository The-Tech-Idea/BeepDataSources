using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
using LiteDB;

namespace LiteDBDataSourceCore
{
    /// <summary>
    /// LiteDB embedded document database data source — Phase 10.5 enhanced rewrite against
    /// BeepDM 3.1.0. LiteDB is an embedded .NET NoSQL file database (or in-memory) accessed
    /// via the LiteDB SDK (no REST). The schema-migration pipeline is real:
    ///   - CreateEntity / DropEntity → LiteDB collection materialisation / DropCollection
    ///   - AddColumn               → schemaless no-op (LiteDB is document-based)
    ///   - TruncateEntity          → Delete(Query.All)
    ///   - RenameEntity            → LiteDatabase.RenameCollection
    ///   - CreateIndex / DropIndex → ILiteCollection.EnsureIndex / DropIndex
    /// Other IDataSource members are wired to the SDK where possible: GetEntity returns real
    /// documents as Dictionary&lt;string,object&gt;, GetEntityStructure introspects the
    /// collection by sampling BsonDocument fields, InsertEntity / UpdateEntity use
    /// ILiteCollection.BsonDocument ops, ExecuteSql passes through to LiteDB SQL dialect,
    /// and BeginTransaction/Commit/EndTransaction use LiteDatabase.BeginTransaction().
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.LiteDB)]
    public partial class LiteDBDataSource : IDataSource, ILocalDB, IInMemoryDB, IDisposable
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
        public DataSourceType DatasourceType { get; set; } = DataSourceType.LiteDB;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.NOSQL;
        public IDataConnection Dataconnection { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public event EventHandler<PassedArgs> PassEvent;
        public string ColumnDelimiter { get; set; } = ",";
        public string ParameterDelimiter { get; set; } = " ";

        public string DatabasePath { get; set; }
        public string Password { get; set; }
        public bool disposedValue;

        private LiteDatabase _db;
        private bool _inTransaction;
        private const int SampleSizeForIntrospection = 100;

        public LiteDBDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
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
                    ?? new ConnectionProperties { ConnectionName = datasourcename, DatabaseType = DataSourceType.LiteDB, Category = DatasourceCategory.NOSQL }
            };
            Dataconnection.ConnectionProp.Category = DatasourceCategory.NOSQL;
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.LiteDB;

            // Resolve the on-disk database path from the connection properties.
            var p = Dataconnection.ConnectionProp;
            if (!string.IsNullOrEmpty(p?.FileName))
                DatabasePath = string.IsNullOrEmpty(p.FilePath) ? p.FileName : Path.Combine(p.FilePath, p.FileName);
            else
                DatabasePath = p?.FilePath;
            Password = p?.Password;
        }

        private IErrorsInfo FailResult(string op, Exception ex)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"{op} failed: {ex.Message}";
            ErrorObject.Ex = ex;
            Logger?.WriteLog($"LiteDB {op} error: {ex.Message}");
            return ErrorObject;
        }

        private IErrorsInfo OkResult(string message)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = message;
            return ErrorObject;
        }

        private string BuildConnectionString()
        {
            // LiteDB connection string format: "Filename=<path>;Password=<pwd>;Connection=shared;Journal=true"
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(DatabasePath))
                parts.Add($"Filename={DatabasePath}");
            if (!string.IsNullOrEmpty(Password))
                parts.Add($"Password={Password}");
            parts.Add("Connection=shared");
            parts.Add("Journal=true");
            return string.Join(";", parts);
        }

        // ── Connection lifecycle ──
        public ConnectionState Openconnection()
        {
            try
            {
                if (string.IsNullOrEmpty(DatabasePath))
                {
                    // In-memory fallback when no path configured.
                    _db = new LiteDatabase(new MemoryStream());
                }
                else
                {
                    var dir = Path.GetDirectoryName(DatabasePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _db = new LiteDatabase(BuildConnectionString());
                }
                ConnectionStatus = ConnectionState.Open;
                RefreshCollectionsCache();
                return ConnectionStatus;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Broken;
                Logger?.WriteLog($"LiteDB Openconnection error: {ex.Message}");
                return ConnectionStatus;
            }
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (_inTransaction)
                {
                    try { _db?.Rollback(); } catch { }
                    _inTransaction = false;
                }
                _db?.Checkpoint();
                _db?.Dispose();
            }
            catch { }
            _db = null;
            ConnectionStatus = ConnectionState.Closed;
            return ConnectionStatus;
        }

        internal bool EnsureOpen()
        {
            if (ConnectionStatus != ConnectionState.Open || _db == null)
                Openconnection();
            return _db != null && ConnectionStatus == ConnectionState.Open;
        }

        internal ILiteCollection<BsonDocument> Collection(string name)
            => _db.GetCollection<BsonDocument>(name);

        private void RefreshCollectionsCache()
        {
            try
            {
                if (_db == null) return;
                EntitiesNames = _db.GetCollectionNames().Where(n => !string.IsNullOrEmpty(n)).ToList();
            }
            catch (Exception ex) { Logger?.WriteLog($"LiteDB RefreshCollectionsCache error: {ex.Message}"); }
        }

        // ── Entity core ──
        public IEnumerable<string> GetEntitesList()
        {
            if (!EnsureOpen()) return Array.Empty<string>();
            RefreshCollectionsCache();
            return EntitiesNames;
        }

        public bool CheckEntityExist(string EntityName)
        {
            try
            {
                if (!EnsureOpen()) return false;
                return _db.CollectionExists(EntityName);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB CheckEntityExist('{EntityName}') error: {ex.Message}");
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
                // Materialising a collection creates it (LiteDB also auto-creates on first insert).
                _db.GetCollection<BsonDocument>(entity.EntityName);
                if (!EntitiesNames.Contains(entity.EntityName, StringComparer.OrdinalIgnoreCase))
                    EntitiesNames.Add(entity.EntityName);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB CreateEntityAs('{entity?.EntityName}') error: {ex.Message}");
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
                if (!_db.CollectionExists(EntityName)) return null;

                var col = Collection(EntityName);
                var sampled = col.Find(Query.All(), 0, SampleSizeForIntrospection).ToList();
                var es = new EntityStructure(EntityName)
                {
                    EntityName = EntityName,
                    DatabaseType = DataSourceType.LiteDB,
                    DataSourceID = DatasourceName,
                    Caption = EntityName,
                    Description = $"LiteDB document collection '{EntityName}'",
                    OriginalEntityName = EntityName,
                    DatasourceEntityName = EntityName,
                    Fields = new List<EntityField>()
                };

                if (sampled.Count == 0)
                {
                    // Empty collection — produce a stub entity with just the _id field marker.
                    es.Fields.Add(BuildEntityField("_id", "ObjectId", DbFieldCategory.Guid, isKey: true, allowNull: false));
                }
                else
                {
                    // Merge field name → (bsonType, count) across the sample.
                    var fieldStats = new Dictionary<string, (BsonType Type, int Count)>(StringComparer.OrdinalIgnoreCase);
                    foreach (var doc in sampled)
                    {
                        foreach (var kv in doc.GetElements())
                        {
                            if (kv.Value.Type == BsonType.Document && kv.Value is BsonDocument nested)
                            {
                                // Flatten one level of nested doc into "parent.child" synthetic fields.
                                foreach (var inner in nested.GetElements())
                                {
                                    var fk = $"{kv.Key}.{inner.Key}";
                                    if (fieldStats.TryGetValue(fk, out var prev))
                                        fieldStats[fk] = (inner.Value.Type, prev.Count + 1);
                                    else
                                        fieldStats[fk] = (inner.Value.Type, 1);
                                }
                                continue;
                            }
                            if (fieldStats.TryGetValue(kv.Key, out var prev2))
                                fieldStats[kv.Key] = (kv.Value.Type, prev2.Count + 1);
                            else
                                fieldStats[kv.Key] = (kv.Value.Type, 1);
                        }
                    }

                    foreach (var (name, stat) in fieldStats.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        var clrType = MapBsonTypeToClr(stat.Type);
                        es.Fields.Add(BuildEntityField(name, clrType, MapBsonTypeToDbFieldCategory(stat.Type), isKey: string.Equals(name, "_id", StringComparison.OrdinalIgnoreCase), allowNull: true));
                    }
                }

                // Upsert into the entities cache.
                var idx = Entities.FindIndex(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) Entities[idx] = es; else Entities.Add(es);
                return es;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"LiteDB GetEntityStructure('{EntityName}') error: {ex.Message}");
                return null;
            }
        }

        public EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
            => fnd == null ? null : GetEntityStructure(fnd.EntityName, refresh);

        private static string MapBsonTypeToClr(BsonType t) => t switch
        {
            BsonType.String => "string",
            BsonType.Int32 => "int",
            BsonType.Int64 => "long",
            BsonType.Double => "double",
            BsonType.Decimal => "decimal",
            BsonType.Boolean => "bool",
            BsonType.DateTime => "DateTime",
            BsonType.Binary => "byte[]",
            BsonType.ObjectId => "ObjectId",
            BsonType.Guid => "Guid",
            BsonType.Document => "BsonDocument",
            BsonType.Array => "BsonArray",
            BsonType.Null => "object",
            _ => "object"
        };

        private static DbFieldCategory MapBsonTypeToDbFieldCategory(BsonType t) => t switch
        {
            BsonType.String => DbFieldCategory.String,
            BsonType.Int32 => DbFieldCategory.Integer,
            BsonType.Int64 => DbFieldCategory.Long,
            BsonType.Double => DbFieldCategory.Double,
            BsonType.Decimal => DbFieldCategory.Decimal,
            BsonType.Boolean => DbFieldCategory.Boolean,
            BsonType.DateTime => DbFieldCategory.DateTime,
            BsonType.Binary => DbFieldCategory.Binary,
            BsonType.ObjectId => DbFieldCategory.Guid,
            BsonType.Guid => DbFieldCategory.Guid,
            BsonType.Document => DbFieldCategory.Json,
            BsonType.Array => DbFieldCategory.Json,
            _ => DbFieldCategory.String
        };

        private static EntityField BuildEntityField(string name, string clrType, DbFieldCategory category, bool isKey, bool allowNull)
        {
            var f = new EntityField
            {
                FieldName = name,
                Fieldtype = clrType,
                FieldCategory = category,
                AllowDBNull = allowNull,
                IsKey = isKey,
                IsAutoIncrement = false,
                IsRequired = !allowNull,
                IsUnique = false,
                Size = 0,
                Size1 = 0,
                Size2 = 0,
                ColumnName = name,
                ColumnTypeName = clrType
            };
            return f;
        }

        // ── Transactions (real) ──
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("BeginTransaction", new InvalidOperationException("Database is not open."));
                if (_inTransaction) return OkResult("LiteDB transaction already in progress.");
                _inTransaction = _db.BeginTrans();
                return OkResult("LiteDB transaction started.");
            }
            catch (Exception ex) { return FailResult("BeginTransaction", ex); }
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                if (!_inTransaction) return FailResult("Commit", new InvalidOperationException("No active LiteDB transaction."));
                _db.Commit();
                _inTransaction = false;
                return OkResult("LiteDB transaction committed.");
            }
            catch (Exception ex) { return FailResult("Commit", ex); }
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                if (!_inTransaction) return OkResult("No active LiteDB transaction to end.");
                _db.Rollback();
                _inTransaction = false;
                return OkResult("LiteDB transaction rolled back.");
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
                ? OkResult($"Created {ok} LiteDB collections.")
                : FailResult("CreateEntities", new InvalidOperationException($"Created {ok} collections, {fail} failed."));
        }

        public IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("ExecuteSql", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrWhiteSpace(sql)) return OkResult("Empty SQL — no-op.");
                _db.Execute(sql, new BsonDocument());
                RefreshCollectionsCache();
                return OkResult("LiteDB SQL executed.");
            }
            catch (Exception ex) { return FailResult("ExecuteSql", ex); }
        }

        public IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("DeleteEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("DeleteEntity", new ArgumentNullException(nameof(EntityName)));
                if (!_db.CollectionExists(EntityName))
                    return FailResult("DeleteEntity", new InvalidOperationException($"Collection '{EntityName}' not found."));

                // If a row is supplied, delete by _id; otherwise drop the whole collection.
                if (UploadDataRow != null)
                {
                    var col = Collection(EntityName);
                    int removed = DeleteMatchingDocuments(col, UploadDataRow);
                    return OkResult($"Removed {removed} document(s) from LiteDB collection '{EntityName}'.");
                }

                _db.DropCollection(EntityName);
                EntitiesNames.Remove(EntityName);
                Entities.RemoveAll(e => string.Equals(e.EntityName, EntityName, StringComparison.OrdinalIgnoreCase));
                return OkResult($"Dropped LiteDB collection '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("DeleteEntity", ex); }
        }

        private static int DeleteMatchingDocuments(ILiteCollection<BsonDocument> col, object row)
        {
            var docs = ToBsonDocuments(row).ToList();
            int removed = 0;
            foreach (var d in docs)
            {
                if (d.TryGetValue("_id", out var id))
                {
                    if (col.Delete(id)) removed++;
                }
                else
                {
                    // No _id — fall back to a bulk query match.
                    removed += col.DeleteMany(BuildQueryFromDocument(d));
                }
            }
            return removed;
        }

        public IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("UpdateEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("UpdateEntity", new ArgumentNullException(nameof(EntityName)));
                var col = Collection(EntityName);
                var docs = ToBsonDocuments(UploadDataRow).ToList();
                int updated = 0;
                foreach (var d in docs)
                {
                    if (d.TryGetValue("_id", out var id) && col.Update(d)) updated++;
                    else if (col.Upsert(d)) updated++;
                }
                return OkResult($"Upserted {updated} document(s) into LiteDB collection '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("UpdateEntity", ex); }
        }

        public IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("UpdateEntities", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("UpdateEntities", new ArgumentNullException(nameof(EntityName)));
                var col = Collection(EntityName);
                var docs = ToBsonDocuments(UploadData).ToList();
                int done = 0;
                foreach (var d in docs)
                {
                    col.Upsert(d);
                    done++;
                    progress?.Report(new PassedArgs { ParameterString1 = $"{done}/{docs.Count}", ParameterInt1 = done });
                }
                return OkResult($"Bulk-upserted {done} document(s) into LiteDB collection '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("UpdateEntities", ex); }
        }

        public IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            try
            {
                if (!EnsureOpen()) return FailResult("InsertEntity", new InvalidOperationException("Database is not open."));
                if (string.IsNullOrEmpty(EntityName)) return FailResult("InsertEntity", new ArgumentNullException(nameof(EntityName)));
                var col = Collection(EntityName);
                var docs = ToBsonDocuments(InsertedData).ToList();
                int inserted = col.InsertBulk(docs);
                return OkResult($"Inserted {inserted} document(s) into LiteDB collection '{EntityName}'.");
            }
            catch (Exception ex) { return FailResult("InsertEntity", ex); }
        }

        public IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            try
            {
                if (dDLScripts == null) return FailResult("RunScript", new ArgumentNullException(nameof(dDLScripts)));
                if (!EnsureOpen()) return FailResult("RunScript", new InvalidOperationException("Database is not open."));
                _db.Execute(dDLScripts.Ddl ?? string.Empty, new BsonDocument());
                RefreshCollectionsCache();
                return OkResult("LiteDB ETL script executed.");
            }
            catch (Exception ex) { return FailResult("RunScript", ex); }
        }

        public IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
        {
            if (entities == null) yield break;
            foreach (var e in entities)
            {
                yield return new ETLScriptDet
                {
                    Ddl = $"-- LiteDB: collection '{e.EntityName}' is created lazily on first insert; no DDL required.",
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
            if (!_db.CollectionExists(EntityName)) yield break;
            var col = Collection(EntityName);
            IEnumerable<BsonDocument> results = filter != null && filter.Count > 0
                ? col.Find(BuildQueryFromFilters(filter))
                : col.FindAll();
            foreach (var d in results) yield return BsonDocumentToDictionary(d);
        }

        public PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);
            if (!EnsureOpen() || string.IsNullOrEmpty(EntityName))
                return new PagedResult(Array.Empty<object>(), pageNumber, pageSize, 0);

            var col = Collection(EntityName);
            var query = filter != null && filter.Count > 0 ? BuildQueryFromFilters(filter) : BsonExpression.Root;
            var total = col.Count(query);
            var slice = col.Find(query, (pageNumber - 1) * pageSize, pageSize)
                          .Select(BsonDocumentToDictionary)
                          .ToList();
            return new PagedResult(slice, pageNumber, pageSize, total);
        }

        public Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
            => Task.FromResult(GetEntity(EntityName, Filter));

        public double GetScalar(string query)
        {
            if (!EnsureOpen()) return 0d;
            if (string.IsNullOrWhiteSpace(query)) return 0d;
            // Convention: "COUNT(collection)" returns the count of documents in that collection.
            var m = Regex.Match(query, @"^\s*COUNT\s*\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)\s*$", RegexOptions.IgnoreCase);
            if (m.Success && _db.CollectionExists(m.Groups[1].Value))
                return Collection(m.Groups[1].Value).Count();
            return 0d;
        }

        public Task<double> GetScalarAsync(string query) => Task.FromResult(GetScalar(query));

        public IEnumerable<object> RunQuery(string qrystr)
        {
            if (!EnsureOpen() || string.IsNullOrWhiteSpace(qrystr)) yield break;
            // LiteDB supports a SQL-like dialect via db.Execute() returning IBsonDataReader.
            // BsonDataReaderExtensions.ToEnumerable yields BsonValue rows; for SELECT * each row is a BsonDocument.
            foreach (var bv in _db.Execute(qrystr, new BsonDocument()).ToEnumerable())
            {
                if (bv.Type == BsonType.Document)
                    yield return BsonDocumentToDictionary((BsonDocument)bv.RawValue);
                else
                    yield return bv.RawValue;
            }
        }

        // ── Colocated schema-migration provider accessors ──
        internal LiteDatabase MigrationDb => _db;
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
                try { if (_inTransaction) { _db?.Rollback(); _inTransaction = false; } } catch { }
                try { _db?.Dispose(); } catch { }
            }
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // ── Conversion helpers ──
        private static IEnumerable<BsonDocument> ToBsonDocuments(object data)
        {
            if (data == null) yield break;
            // Specific dict cases first — both IDictionary and IDictionary<string,object> implement IEnumerable,
            // so a single-dict input would otherwise match the generic IEnumerable branch.
            switch (data)
            {
                case BsonDocument bd:
                    yield return bd;
                    break;
                case IDictionary<string, object> singleDict:
                    yield return DictionaryToBsonDocument(singleDict);
                    break;
                case IDictionary singleNd:
                    yield return DictionaryToBsonDocument(singleNd);
                    break;
                case IEnumerable<BsonDocument> bds:
                    foreach (var d in bds) yield return d;
                    break;
                case DataTable dt:
                    foreach (DataRow r in dt.Rows) yield return DataRowToBsonDocument(r);
                    break;
                case DataRow dr:
                    yield return DataRowToBsonDocument(dr);
                    break;
                case IEnumerable enumerable:
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        if (item is BsonDocument b) yield return b;
                        else if (item is IDictionary<string, object> dict) yield return DictionaryToBsonDocument(dict);
                        else if (item is IDictionary nd) yield return DictionaryToBsonDocument(nd);
                        else yield return BsonMapper.Global.ToDocument(item);
                    }
                    break;
                default:
                    yield return BsonMapper.Global.ToDocument(data);
                    break;
            }
        }

        private static BsonDocument DataRowToBsonDocument(DataRow r)
        {
            var doc = new BsonDocument();
            foreach (DataColumn c in r.Table.Columns)
            {
                doc[c.ColumnName] = r.IsNull(c) ? BsonValue.Null : ToBsonValue(r[c]);
            }
            return doc;
        }

        private static BsonDocument DictionaryToBsonDocument(IDictionary<string, object> d)
        {
            var doc = new BsonDocument();
            foreach (var kv in d)
            {
                if (kv.Value == null) { doc[kv.Key] = BsonValue.Null; continue; }
                doc[kv.Key] = ToBsonValue(kv.Value);
            }
            return doc;
        }

        private static BsonDocument DictionaryToBsonDocument(IDictionary d)
        {
            var doc = new BsonDocument();
            foreach (DictionaryEntry kv in d)
            {
                var key = kv.Key?.ToString();
                if (string.IsNullOrEmpty(key)) continue;
                doc[key] = kv.Value == null ? BsonValue.Null : ToBsonValue(kv.Value);
            }
            return doc;
        }

        private static BsonValue ToBsonValue(object o)
        {
            if (o == null) return BsonValue.Null;
            if (o is BsonValue bv) return bv;
            switch (o)
            {
                case string s: return new BsonValue(s);
                case bool b: return new BsonValue(b);
                case int i: return new BsonValue(i);
                case long l: return new BsonValue(l);
                case double d: return new BsonValue(d);
                case decimal dec: return new BsonValue(dec);
                case DateTime dt: return new BsonValue(dt);
                case Guid g: return new BsonValue(g);
                case byte[] bytes: return new BsonValue(bytes);
                case IDictionary<string, object> d1: return DictionaryToBsonDocument(d1);
                case IDictionary d2: return DictionaryToBsonDocument(d2);
                case IEnumerable enumerable when o is not string:
                    {
                        var arr = new BsonArray();
                        foreach (var item in enumerable) arr.Add(ToBsonValue(item));
                        return arr;
                    }
                default:
                    try { return new BsonValue(o.ToString()); }
                    catch { return BsonValue.Null; }
            }
        }

        internal static Dictionary<string, object> BsonDocumentToDictionary(BsonDocument doc)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in doc.GetElements())
            {
                result[kv.Key] = kv.Value.RawValue;
            }
            return result;
        }

        private static BsonExpression BuildQueryFromFilters(List<AppFilter> filters)
        {
            if (filters == null || filters.Count == 0) return BsonExpression.Root;
            // Combine with AND semantics — LiteDB queries compose with Query.And.
            BsonExpression combined = null;
            foreach (var f in filters)
            {
                if (string.IsNullOrEmpty(f?.FieldName)) continue;
                var value = ToBsonValue(f.FilterValue);
                var expr = $"$.{f.FieldName}";
                BsonExpression one = (f.Operator ?? "=").Trim().ToLowerInvariant() switch
                {
                    "=" or "==" or "eq" => Query.EQ(expr, value),
                    "!=" or "<>" or "ne" => Query.Not(expr, value),
                    ">" or "gt" => Query.GT(expr, value),
                    ">=" or "gte" => Query.GTE(expr, value),
                    "<" or "lt" => Query.LT(expr, value),
                    "<=" or "lte" => Query.LTE(expr, value),
                    _ => Query.EQ(expr, value)
                };
                combined = combined == null ? one : Query.And(combined, one);
            }
            return combined ?? BsonExpression.Root;
        }

        private static BsonExpression BuildQueryFromDocument(BsonDocument d)
        {
            if (d.Count == 0) return BsonExpression.Root;
            BsonExpression combined = null;
            foreach (var kv in d.GetElements())
            {
                var q = Query.EQ($"$.{kv.Key}", kv.Value);
                combined = combined == null ? q : Query.And(combined, q);
            }
            return combined ?? BsonExpression.Root;
        }
    }
}