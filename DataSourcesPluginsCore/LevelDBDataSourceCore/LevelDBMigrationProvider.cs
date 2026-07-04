using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;
using LevelDB;

namespace LevelDBDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.LevelDB"/>.
    /// LevelDB has no column families, no transactions, and no indexes — we use a key-prefix
    /// scheme per entity. Capabilities: create prefix, drop prefix, truncate prefix,
    /// rename prefix. AddColumn is a no-op (schemaless); CreateIndex/DropIndex are unsupported.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.LevelDB, DatasourceCategory.KVStore)]
    public class LevelDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly LevelDBDataSource _owner;

        public LevelDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as LevelDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a LevelDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.LevelDB;
        public DatasourceCategory Category => DatasourceCategory.KVStore;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,           // delegates to datasource (real: prefix marker)
            SupportsDropEntity = true,             // delegates to datasource (real prefix wipe)
            SupportsAddColumn = true,              // schemaless; no-op Ok
            SupportsTruncateEntity = true,         // real: prefix wipe
            SupportsRenameEntity = true,           // real: prefix wipe + iterate-copy under new prefix
            SupportsCreateIndex = false,           // LevelDB has no built-in secondary index API
            SupportsDropIndex = false,
            SupportsTransactionalDdl = false       // LevelDB has NO transactions at all
            // Alter/DropCol/RenameCol/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created LevelDB prefix entity '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create LevelDB prefix entity '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped LevelDB prefix entity '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop LevelDB prefix entity '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"LevelDB is schemaless; field '{column?.FieldName}' is accepted as a document field.");

        public IErrorsInfo TruncateEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("LevelDB is not open.");
                if (!_owner.CheckEntityExist(entityName))
                    return SchemaMigrationResults.Fail($"LevelDB prefix entity '{entityName}' not found.");

                int removed = _owner.DeleteEntity(entityName, null).Flag == Errors.Ok ? CountEntries(db, entityName) : 0;
                // After delete, recreate the marker so entity still exists post-truncate.
                _owner.CreateEntityAs(new EntityStructure(entityName));
                return SchemaMigrationResults.Ok($"Truncated LevelDB prefix entity '{entityName}'.");
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
                if (db == null) return SchemaMigrationResults.Fail("LevelDB is not open.");
                if (!_owner.CheckEntityExist(oldName))
                    return SchemaMigrationResults.Fail($"LevelDB prefix entity '{oldName}' not found.");
                if (_owner.CheckEntityExist(newName))
                    return SchemaMigrationResults.Fail($"LevelDB prefix entity '{newName}' already exists.");

                int copied = 0;
                using (var iter = db.CreateIterator())
                {
                    iter.Seek(System.Text.Encoding.UTF8.GetBytes(oldName + "\x00"));
                    while (iter.IsValid())
                    {
                        var keyBytes = iter.Key();
                        var keyStr = iter.KeyAsString();
                        if (string.IsNullOrEmpty(keyStr) || !keyStr.StartsWith(oldName + "\x00", System.StringComparison.Ordinal))
                            break;
                        // Translate to new prefix: replace oldName with newName in the key bytes.
                        var newKeyStr = newName + keyStr.Substring(oldName.Length);
                        var newKeyBytes = System.Text.Encoding.UTF8.GetBytes(newKeyStr);
                        db.Put(newKeyBytes, iter.Value());
                        db.Delete(keyBytes);
                        copied++;
                        iter.Next();
                    }
                }
                _owner.EntitiesNames.Remove(oldName);
                if (!_owner.EntitiesNames.Contains(newName, System.StringComparer.OrdinalIgnoreCase))
                    _owner.EntitiesNames.Add(newName);
                return SchemaMigrationResults.Ok($"Renamed LevelDB prefix entity '{oldName}' → '{newName}' ({copied} entries copied).");
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

        private static int CountEntries(DB? db, string entityName)
        {
            if (db == null) return 0;
            int n = 0;
            using var iter = db.CreateIterator();
            iter.Seek(System.Text.Encoding.UTF8.GetBytes(entityName + "\x00"));
            while (iter.IsValid())
            {
                var keyStr = iter.KeyAsString();
                if (string.IsNullOrEmpty(keyStr) || !keyStr.StartsWith(entityName + "\x00", System.StringComparison.Ordinal))
                    break;
                if (!keyStr.EndsWith("\x00\x00__schema__", System.StringComparison.Ordinal)) n++;
                iter.Next();
            }
            return n;
        }
    }
}