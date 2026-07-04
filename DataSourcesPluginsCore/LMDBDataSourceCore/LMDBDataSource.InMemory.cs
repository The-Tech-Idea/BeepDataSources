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
using LightningDB;

namespace LMDBDataSourceCore
{
    public partial class LMDBDataSource
    {
        // ── IInMemoryDB (Phase 12) ──
        // LMDB has no in-memory mode; IInMemoryDB routes through a temp-dir-backed env.

        public bool IsCreated { get; set; } = false;
        public bool IsLoaded { get; set; } = false;
        public bool IsSaved { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public bool IsStructureCreated { get; set; } = false;
        public bool IsStructureLoaded { get; set; } = false;
        public List<EntityStructure> InMemoryStructures
        {
            get => Entities;
            set => Entities = value ?? new List<EntityStructure>();
        }

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
                var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep-lmdb-im-" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(tmp);
                Dataconnection!.ConnectionProp.IsInMemory = true;
                Dataconnection.ConnectionProp.FilePath = tmp;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                if (Openconnection() == System.Data.ConnectionState.Open)
                {
                    IsCreated = true;
                    IsStructureLoaded = true;
                    StateChanged?.Invoke(this, new PassedArgs { EventType = "OpenInMemory", Messege = $"Opened LMDB in-memory (temp-dir backed) '{databaseName}'." });
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Failed to open LMDB temp-dir env.";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public string GetInMemoryConnectionString() => "lmdb-im://" + (DatasourceName ?? "LMDB") + "@" + (DatabasePath ?? Path.GetTempPath());

        public IErrorsInfo ResetInMemory()
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_env == null) return ErrorObject;
                using var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
                foreach (var name in EnumerateDbNames(tx).ToList())
                {
                    try
                    {
                        using var db = tx.OpenDatabase(name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true);
                        using var dropTx = _env.BeginTransaction(TransactionBeginFlags.None);
                        dropTx.DropDatabase(db);
                        dropTx.Commit();
                    }
                    catch { }
                }
                Entities.Clear();
                EntitiesNames.Clear();
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
                if (_env == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory env first."; return ErrorObject; }
                using var tx = _env.BeginTransaction(TransactionBeginFlags.None);
                foreach (var es in Entities.ToList())
                {
                    if (!CheckEntityExist(es.EntityName))
                    {
                        _ = tx.OpenDatabase(es.EntityName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }, closeOnDispose: true);
                    }
                }
                tx.Commit();
                IsStructureCreated = true;
                StructureChanged?.Invoke(this, new PassedArgs { EventType = "CreateStructure" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "CreateStructure" });
            }
            catch (Exception ex) { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; }
            return ErrorObject;
        }

        public IErrorsInfo LoadStructure(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_env == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory env first."; return ErrorObject; }
                Entities.Clear();
                EntitiesNames.Clear();
                using var tx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly);
                foreach (var name in EnumerateDbNames(tx))
                {
                    EntitiesNames.Add(name);
                }
                IsStructureLoaded = true;
                StructureChanged?.Invoke(this, new PassedArgs { EventType = "LoadStructure" });
            }
            catch (Exception ex) { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; }
            return ErrorObject;
        }

        public IErrorsInfo LoadStructureWithData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
            => LoadStructure(progress, token);

        public IErrorsInfo SaveStructure() { ErrorObject ??= new ErrorsInfo(); ErrorObject.Flag = Errors.Ok; IsSaved = true; return ErrorObject; }
        public IErrorsInfo LoadData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo(); ErrorObject.Flag = Errors.Ok;
            IsLoaded = true;
            DataChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
            StateChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        { ErrorObject ??= new ErrorsInfo(); ErrorObject.Flag = Errors.Ok; IsSynced = true; return ErrorObject; }
        public IErrorsInfo SyncData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default) => SyncData(progress, token);
        public IErrorsInfo RefreshData(IProgress<PassedArgs>? progress = null, CancellationToken token = default) => LoadData(progress, token);
        public IErrorsInfo RefreshData(string entityName, IProgress<PassedArgs>? progress = null, CancellationToken token = default) => LoadData(progress, token);

        public IErrorsInfo FillFromDataSource(IDataSource source, IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (source == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Source is null."; return ErrorObject; }
                if (source.ConnectionStatus != System.Data.ConnectionState.Open && source.Openconnection() != System.Data.ConnectionState.Open)
                { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = $"Could not open source '{source.DatasourceName}'."; return ErrorObject; }
                if (_env == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory env first."; return ErrorObject; }

                using (var tx = _env.BeginTransaction(TransactionBeginFlags.None))
                {
                    foreach (var entity in source.Entities.ToList())
                    {
                        if (!CheckEntityExist(entity.EntityName))
                        {
                            using var db = tx.OpenDatabase(entity.EntityName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }, closeOnDispose: true);
                        }
                        using var collDb = tx.OpenDatabase(entity.EntityName, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true);
                        var rows = source.GetEntity(entity.EntityName, null).ToList();
                        foreach (var row in rows.OfType<Dictionary<string, object>>())
                        {
                            var key = ExtractKey(row, out var k) ? k : Guid.NewGuid().ToString();
                            var putRc = tx.Put(collDb, EncodingKey(key), SerializeJson(row), PutOptions.None);
                            if (putRc != MDBResultCode.Success) break;
                        }
                    }
                    tx.Commit();
                }
                IsLoaded = true; IsSynced = true;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "FillFromDataSource" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "FillFromDataSource" });
            }
            catch (Exception ex) { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; }
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
                if (_env == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory env first."; return ErrorObject; }

                using (var srcTx = _env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                foreach (var name in EnumerateDbNames(srcTx).ToList())
                {
                    using var srcDb = srcTx.OpenDatabase(name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true);
                    using var cursor = srcTx.CreateCursor(srcDb);
                    var rows = new List<Dictionary<string, object>>();
                    var (rc, _, value) = cursor.First();
                    while (rc == MDBResultCode.Success)
                    {
                        var d = DeserializeJson(value.CopyToNewArray());
                        if (d != null) rows.Add(d);
                        (rc, _, value) = cursor.Next();
                    }
                    if (rows.Count > 0) target.InsertEntity(name, rows);
                }
                IsSaved = true;
                DataChanged?.Invoke(this, new PassedArgs { EventType = "ExportToDataSource" });
                StateChanged?.Invoke(this, new PassedArgs { EventType = "ExportToDataSource" });
            }
            catch (Exception ex) { ErrorObject.Flag = Errors.Failed; ErrorObject.Ex = ex; }
            return ErrorObject;
        }

        public event EventHandler<PassedArgs>? StructureChanged;
        public event EventHandler<PassedArgs>? DataChanged;
        public event EventHandler<PassedArgs>? StateChanged;

        private IEnumerable<string> EnumerateDbNames(LightningTransaction tx)
        {
            if (_env == null) yield break;
            // LMDB doesn't expose ListDatabases; use the env's MaxDatabases to iterate candidates.
            int max = _env.MaxDatabases;
            for (int i = 0; i < max; i++)
            {
                string name = $"entity_{i:D6}";
                bool exists = false;
                try
                {
                    using var db = tx.OpenDatabase(name, new DatabaseConfiguration { Flags = DatabaseOpenFlags.None }, closeOnDispose: true);
                    exists = true;
                }
                catch { /* skip */ }
                if (exists) yield return name;
            }
            // Also yield the actual entity names from Entities (preferred when available)
            foreach (var e in Entities)
            {
                if (e != null && !string.IsNullOrEmpty(e.EntityName)) yield return e.EntityName;
            }
        }
    }
}