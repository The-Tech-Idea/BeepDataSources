using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;
using RocksDbSharp;

namespace RocksDBDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.RocksDB"/>.
    /// Drives RocksDB via the owning <see cref="RocksDBDataSource"/>'s <c>RocksDb</c> instance:
    /// create column family, drop column family, truncate column family, rename column family.
    /// RocksDB is schemaless so AddColumn is a no-op; CreateIndex/DropIndex are unsupported
    /// (no built-in secondary index API in RocksDB; secondary indexes require separate CFs).
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.RocksDB, DatasourceCategory.KVStore)]
    public class RocksDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly RocksDBDataSource _owner;

        public RocksDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as RocksDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a RocksDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.RocksDB;
        public DatasourceCategory Category => DatasourceCategory.KVStore;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,           // delegates to datasource (real: CreateColumnFamily)
            SupportsDropEntity = true,             // delegates to datasource (real DropColumnFamily)
            SupportsAddColumn = true,              // schemaless; no-op Ok
            SupportsTruncateEntity = true,         // real: iterate-remove on the CF
            SupportsRenameEntity = true,           // real: Drop + Create + iterate-copy
            SupportsCreateIndex = false,           // RocksDB has no first-class secondary index API
            SupportsDropIndex = false,
            SupportsTransactionalDdl = false       // CF ops are metadata-only, not transactional
            // Alter/DropCol/RenameCol/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created RocksDB column family '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create RocksDB column family '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped RocksDB column family '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop RocksDB column family '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"RocksDB is schemaless; field '{column?.FieldName}' is accepted as a document field.");

        public IErrorsInfo TruncateEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("RocksDB is not open.");
                if (!HasColumnFamily(db, entityName))
                    return SchemaMigrationResults.Fail($"RocksDB column family '{entityName}' not found.");

                int removed = 0;
                using (var iter = db.NewIterator(db.GetColumnFamily(entityName), readOptions: null))
                {
                    var keys = new List<byte[]>();
                    for (iter.SeekToFirst(); iter.Valid(); iter.Next())
                    {
                        keys.Add(iter.Key());
                    }
                    foreach (var k in keys)
                    {
                        db.Remove(k, db.GetColumnFamily(entityName));
                        removed++;
                    }
                }
                return SchemaMigrationResults.Ok($"Truncated RocksDB column family '{entityName}' ({removed} entries removed).");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"TruncateEntity('{entityName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo RenameEntity(string oldName, string newName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("RocksDB is not open.");
                if (!HasColumnFamily(db, oldName))
                    return SchemaMigrationResults.Fail($"RocksDB column family '{oldName}' not found.");
                if (HasColumnFamily(db, newName))
                    return SchemaMigrationResults.Fail($"RocksDB column family '{newName}' already exists.");

                db.CreateColumnFamily(new ColumnFamilyOptions(), newName);
                int copied = 0;
                using (var srcIter = db.NewIterator(db.GetColumnFamily(oldName), readOptions: null))
                {
                    for (srcIter.SeekToFirst(); srcIter.Valid(); srcIter.Next())
                    {
                        db.Put(srcIter.Key(), srcIter.Value(), db.GetColumnFamily(newName), writeOptions: null);
                        copied++;
                    }
                }
                db.DropColumnFamily(oldName);
                _owner.EntitiesNames.Remove(oldName);
                if (!_owner.EntitiesNames.Contains(newName, System.StringComparer.OrdinalIgnoreCase))
                    _owner.EntitiesNames.Add(newName);
                return SchemaMigrationResults.Ok($"Renamed RocksDB column family '{oldName}' → '{newName}' ({copied} entries copied).");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"RenameEntity('{oldName}' → '{newName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => SchemaMigrationResults.Unsupported(nameof(CreateIndex), DataSourceType);

        public IErrorsInfo DropIndex(string entityName, string indexName)
            => SchemaMigrationResults.Unsupported(nameof(DropIndex), DataSourceType);

        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo DropColumn(string entityName, string columnName)
            => SchemaMigrationResults.Unsupported(nameof(DropColumn), DataSourceType);
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
            => SchemaMigrationResults.Unsupported(nameof(RenameColumn), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    private static bool HasColumnFamily(RocksDb db, string name)
        {
            try { _ = db.GetColumnFamily(name); return true; }
            catch { return false; }
        }
    }
}