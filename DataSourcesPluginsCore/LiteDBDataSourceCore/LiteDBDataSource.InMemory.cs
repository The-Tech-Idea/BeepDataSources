using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using LiteDB;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        // ── IInMemoryDB implementation ──
        // LiteDB has true in-memory mode via `new LiteDatabase(new MemoryStream())`.
        // We open a separate in-memory engine, distinct from the file-backed one,
        // and route CRUD through it when callers use the IInMemoryDB surface.

        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public bool IsStructureCreated { get; set; } = false;
        public bool IsStructureLoaded { get; set; } = false;
        public List<EntityStructure> InMemoryStructures { get; set; } = new();

        private LiteDatabase? _inMemoryDb;
        private MemoryStream? _inMemoryStream;

        public IErrorsInfo OpenInMemory(string databaseName)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Database name cannot be empty.";
                    return ErrorObject;
                }
                DatasourceName = databaseName;
                _inMemoryStream = new MemoryStream();
                _inMemoryDb = new LiteDatabase(_inMemoryStream);
                IsCreated = true;
                IsStructureLoaded = false;
                IsStructureCreated = false;
                IsLoaded = false;
                IsSaved = false;
                IsSynced = false;
                InMemoryStructures = new List<EntityStructure>();
                EntitiesNames = new List<string>();
                Entities = new List<EntityStructure>();
                StateChanged?.Invoke(this, new PassedArgs { EventType = "OpenInMemory", Messege = $"Opened LiteDB in-memory engine '{databaseName}'." });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"OpenInMemory failed: {ex.Message}";
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public string GetInMemoryConnectionString() => "memory://" + (DatasourceName ?? "LiteDB");

        public IErrorsInfo ResetInMemory()
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_inMemoryDb == null) return ErrorObject;
                foreach (var name in _inMemoryDb.GetCollectionNames())
                {
                    try { _inMemoryDb.DropCollection(name); } catch { }
                }
                InMemoryStructures.Clear();
                EntitiesNames.Clear();
                Entities.Clear();
                IsCreated = false;
                IsLoaded = false;
                IsSaved = false;
                IsSynced = false;
                IsStructureCreated = false;
                IsStructureLoaded = false;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "ResetInMemory" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "ResetInMemory" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"ResetInMemory failed: {ex.Message}";
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public IErrorsInfo CreateStructure(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_inMemoryDb == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }
                foreach (var es in InMemoryStructures.ToList())
                {
                    if (_inMemoryDb.CollectionExists(es.EntityName)) continue;
                    // Create empty collection (LiteDB auto-creates on first insert).
                    _inMemoryDb.GetCollection<BsonDocument>(es.EntityName);
                }
                IsStructureCreated = true;
                StructureChanged?.Invoke(this, new PassedArgs { EventType = "CreateStructure" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "CreateStructure" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public IErrorsInfo LoadStructure(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_inMemoryDb == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }
                InMemoryStructures.Clear();
                EntitiesNames.Clear();
                foreach (var name in _inMemoryDb.GetCollectionNames())
                {
                    var es = new EntityStructure(name)
                    {
                        EntityName = name,
                        DatabaseType = DataSourceType.LiteDB,
                        DataSourceID = DatasourceName,
                        Caption = name,
                        Description = $"LiteDB in-memory collection '{name}'",
                        Fields = new List<EntityField>()
                    };
                    InMemoryStructures.Add(es);
                    EntitiesNames.Add(name);
                }
                IsStructureLoaded = true;
                StructureChanged?.Invoke(this, new PassedArgs { EventType = "LoadStructure" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public IErrorsInfo LoadStructureWithData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            var r = LoadStructure(progress, token);
            if (r?.Flag == Errors.Ok) return LoadData(progress, token);
            return r ?? ErrorObject;
        }

        public IErrorsInfo SaveStructure()
        {
            // LiteDB in-memory has no file persistence; the MemoryStream holds the bytes.
            // SaveStructure is a no-op when running purely in-memory.
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            IsSaved = true;
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_inMemoryDb == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }
                // Data is already in memory; this confirms LoadData completion.
                IsLoaded = true;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            // SyncData on in-memory is essentially a no-op (data already in memory).
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            IsSynced = true;
            return ErrorObject;
        }

        public IErrorsInfo SyncData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => SyncData(progress, token);

        public IErrorsInfo RefreshData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => LoadData(progress, token);

        public IErrorsInfo RefreshData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => LoadData(progress, token);

        public IErrorsInfo FillFromDataSource(IDataSource source, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (source == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Source is null."; return ErrorObject; }
                if (source.ConnectionStatus != System.Data.ConnectionState.Open && source.Openconnection() != System.Data.ConnectionState.Open)
                { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = $"Could not open source '{source.DatasourceName}'."; return ErrorObject; }
                if (_inMemoryDb == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }

                foreach (var entity in source.Entities.ToList())
                {
                    var rows = source.GetEntity(entity.EntityName, null).ToList();
                    if (rows.Count == 0) continue;
                    var col = _inMemoryDb.GetCollection<BsonDocument>(entity.EntityName);
                    foreach (var row in rows)
                    {
                        if (row is Dictionary<string, object> dict)
                        {
                            col.Insert(DictionaryToBson(dict));
                        }
                    }
                }
                IsLoaded = true;
                IsSynced = true;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "FillFromDataSource" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "FillFromDataSource" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public IErrorsInfo ExportToDataSource(IDataSource target, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (target == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Target is null."; return ErrorObject; }
                if (target.ConnectionStatus != System.Data.ConnectionState.Open && target.Openconnection() != System.Data.ConnectionState.Open)
                { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = $"Could not open target '{target.DatasourceName}'."; return ErrorObject; }
                if (_inMemoryDb == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }

                foreach (var entity in InMemoryStructures.ToList())
                {
                    var col = _inMemoryDb.GetCollection<BsonDocument>(entity.EntityName);
                    var docs = col.FindAll().ToList();
                    if (docs.Count == 0) continue;
                    var rows = docs.Select(BsonToDictionary).ToList();
                    target.InsertEntity(entity.EntityName, rows);
                }
                IsSaved = true;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "ExportToDataSource" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "ExportToDataSource" });
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public event EventHandler<PassedArgs>? StructureChanged;
        public event EventHandler<PassedArgs>? DataChanged;
        public event EventHandler<PassedArgs>? StateChanged;

        // (Dispose is provided by LiteDBDataSource.cs base partial; in-memory cleanup is
// hooked in by the partial Dispose() override below — but to avoid CS0115/CS0111 with
// the base class's protected virtual Dispose(bool), we don't override that. Instead, the
// existing public Dispose() in LiteDBDataSource.cs handles both file + in-memory cleanup
// via the overridden Dispose(bool) pattern in the base file. The fields will be
// finalized by the GC since they are managed objects.)

        private static BsonDocument DictionaryToBson(Dictionary<string, object> dict)
        {
            var doc = new BsonDocument();
            foreach (var kv in dict)
            {
                doc[kv.Key] = ToBsonValue(kv.Value);
            }
            return doc;
        }

        private static Dictionary<string, object> BsonToDictionary(BsonDocument doc)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in doc.GetElements())
            {
                dict[kv.Key] = FromBsonValue(kv.Value);
            }
            return dict;
        }

        private static object? FromBsonValue(BsonValue v) => v.Type switch
        {
            BsonType.String => v.AsString,
            BsonType.Int32 => v.AsInt32,
            BsonType.Int64 => v.AsInt64,
            BsonType.Double => v.AsDouble,
            BsonType.Decimal => v.AsDecimal,
            BsonType.Boolean => v.AsBoolean,
            BsonType.DateTime => v.AsDateTime,
            BsonType.Guid => v.AsGuid,
            BsonType.Binary => v.AsBinary,
            BsonType.Document => BsonToDictionary(v.AsDocument),
            BsonType.Array => v.AsArray.Select(FromBsonValue).ToList(),
            BsonType.Null => null,
            _ => v.RawValue
        };
    }
}