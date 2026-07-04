using System;
using System.Collections.Generic;
using System.Threading;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using RocksDbSharp;

namespace RocksDBDataSourceCore
{
    public partial class RocksDBDataSource
    {
        // ── IInMemoryDB (Phase 12) ──
        // RocksDB has no native in-memory mode. We expose IInMemoryDB by routing through a
        // temp-dir-backed open engine — i.e. the engine lives in Path.GetTempPath() but the
        // IInMemoryDB contract still applies. All entity operations route to the regular
        // IDataSource surface (which already targets _db / _pendingBatch).

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
                var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "beep-rocksdb-im-" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(tmp);
                Dataconnection!.ConnectionProp.IsInMemory = true;
                Dataconnection.ConnectionProp.FilePath = tmp;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                if (Openconnection() == System.Data.ConnectionState.Open)
                {
                    IsCreated = true;
                    IsStructureLoaded = true;
                    StateChanged?.Invoke(this, new PassedArgs { EventType = "OpenInMemory", Messege = $"Opened RocksDB in-memory (temp-dir backed) '{databaseName}'." });
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Failed to open RocksDB temp-dir engine.";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
            return ErrorObject;
        }

        public string GetInMemoryConnectionString() => "rocksdb-im://" + (DatasourceName ?? "RocksDB") + "@" + (DatabasePath ?? Path.GetTempPath());

        public IErrorsInfo ResetInMemory()
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (_db == null) return ErrorObject;
                foreach (var name in RocksDb.ListColumnFamilies(_options ?? new DbOptions(), DatabasePath ?? ""))
                {
                    try { _db.DropColumnFamily(name); } catch { }
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
                if (_db == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }
                foreach (var es in Entities.ToList())
                {
                    if (!HasColumnFamily(es.EntityName))
                    {
                        _db.CreateColumnFamily(new ColumnFamilyOptions(), es.EntityName);
                    }
                }
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
                if (_db == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }
                Entities.Clear();
                EntitiesNames.Clear();
                foreach (var name in RocksDb.ListColumnFamilies(_options ?? new DbOptions(), DatabasePath ?? ""))
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

        public IErrorsInfo SaveStructure()
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            IsSaved = true;
            return ErrorObject;
        }

        public IErrorsInfo LoadData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
            ErrorObject ??= new ErrorsInfo();
            ErrorObject.Flag = Errors.Ok;
            IsLoaded = true;
            DataChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
            StateChanged?.Invoke(this, new PassedArgs { EventType = "LoadData" });
            return ErrorObject;
        }

        public IErrorsInfo SyncData(IProgress<PassedArgs>? progress = null, CancellationToken token = default)
        {
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
                if (_db == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }

                foreach (var entity in source.Entities.ToList())
                {
                    var rows = source.GetEntity(entity.EntityName, null).ToList();
                    if (rows.Count == 0) continue;
                    var cf = GetOrCreateCf(entity.EntityName);
                    using var wb = new WriteBatch();
                    foreach (var row in rows.OfType<Dictionary<string, object>>())
                    {
                        var key = ExtractKey(row, out var k) ? k : Guid.NewGuid().ToString();
                        wb.Put(EncodingKey(key), SerializeJson(row), cf);
                    }
                    _db.Write(wb, writeOptions: null);
                }
                IsLoaded = true;
                IsSynced = true;
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
                if (_db == null) { ErrorObject.Flag = Errors.Failed; ErrorObject.Message = "Open in-memory engine first."; return ErrorObject; }

                foreach (var name in RocksDb.ListColumnFamilies(_options ?? new DbOptions(), DatabasePath ?? ""))
                {
                    var rows = new List<Dictionary<string, object>>();
                    using (var iter = _db.NewIterator(GetOrCreateCf(name), readOptions: null))
                    {
                        for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                        {
                            var d = DeserializeJson(iter.Value());
                            if (d != null) rows.Add(d);
                        }
                    }
                    if (rows.Count == 0) continue;
                    target.InsertEntity(name, rows);
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
    }
}