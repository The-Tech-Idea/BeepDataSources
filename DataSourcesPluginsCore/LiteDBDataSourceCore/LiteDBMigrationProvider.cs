using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;
using LiteDB;

namespace LiteDBDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.LiteDB"/>.
    /// Drives the LiteDB SDK via the owning <see cref="LiteDBDataSource"/>'s <c>LiteDatabase</c>:
    /// create collection, drop collection, truncate collection, rename collection, create index,
    /// drop index. LiteDB is a schemaless document store so AddColumn is a no-op Ok and the
    /// strict DDL ops (AlterColumn / DropColumn / RenameColumn / AddFK / DropFK) remain unsupported.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.LiteDB, DatasourceCategory.NOSQL)]
    public class LiteDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly LiteDBDataSource _owner;

        public LiteDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as LiteDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a LiteDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.LiteDB;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,           // delegates to datasource (real: materialises a collection)
            SupportsDropEntity = true,             // delegates to datasource (real DropCollection)
            SupportsAddColumn = true,              // schemaless JSON document → no-op Ok
            SupportsTruncateEntity = true,         // real: collection.Delete(Query.All)
            SupportsRenameEntity = true,           // real: LiteDatabase.RenameCollection
            SupportsCreateIndex = true,            // real: ILiteCollection.EnsureIndex
            SupportsDropIndex = true,              // real: ILiteCollection.DropIndex
            SupportsTransactionalDdl = false
            // Alter/DropCol/RenameCol/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created LiteDB collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create LiteDB collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped LiteDB collection '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop LiteDB collection '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"LiteDB is schemaless; field '{column?.FieldName}' is accepted as a document field.");

        public IErrorsInfo TruncateEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("LiteDB is not open.");
                if (!db.CollectionExists(entityName))
                    return SchemaMigrationResults.Fail($"LiteDB collection '{entityName}' not found.");
                int removed = db.GetCollection<BsonDocument>(entityName).DeleteAll();
                return SchemaMigrationResults.Ok($"Truncated LiteDB collection '{entityName}' ({removed} documents removed).");
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
                if (db == null) return SchemaMigrationResults.Fail("LiteDB is not open.");
                if (!db.CollectionExists(oldName))
                    return SchemaMigrationResults.Fail($"LiteDB collection '{oldName}' not found.");
                if (db.CollectionExists(newName))
                    return SchemaMigrationResults.Fail($"LiteDB collection '{newName}' already exists.");
                bool ok = db.RenameCollection(oldName, newName);
                if (!ok) return SchemaMigrationResults.Fail($"LiteDB RenameCollection returned false for '{oldName}' → '{newName}'.");
                _owner.EntitiesNames.Remove(oldName);
                if (!_owner.EntitiesNames.Contains(newName, System.StringComparer.OrdinalIgnoreCase))
                    _owner.EntitiesNames.Add(newName);
                return SchemaMigrationResults.Ok($"Renamed LiteDB collection '{oldName}' → '{newName}'.");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"RenameEntity('{oldName}' → '{newName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            try
            {
                if (columns == null || columns.Length == 0)
                    return SchemaMigrationResults.Fail("CreateIndex requires at least one column.");
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("LiteDB is not open.");
                if (!db.CollectionExists(entityName))
                    return SchemaMigrationResults.Fail($"LiteDB collection '{entityName}' not found.");

                bool unique = false;
                if (options != null && options.TryGetValue("Unique", out var u))
                    unique = u is bool b ? b : (u != null && u.ToString().Equals("true", System.StringComparison.OrdinalIgnoreCase));

                var col = db.GetCollection<BsonDocument>(entityName);
                int created = 0;
                foreach (var column in columns)
                {
                    if (string.IsNullOrEmpty(column)) continue;
                    // If a custom index name was supplied, use it; otherwise LiteDB names the index by field.
                    var fieldExpr = column.StartsWith("$") ? column : $"$.{column}";
                    if (!string.IsNullOrEmpty(indexName) && columns.Length == 1)
                        col.EnsureIndex(indexName, fieldExpr, unique);
                    else
                        col.EnsureIndex(fieldExpr, unique);
                    created++;
                }
                return SchemaMigrationResults.Ok($"Created {created} LiteDB index entry/entries on '{entityName}'.");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"CreateIndex('{entityName}') failed: {ex.Message}");
            }
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var db = _owner.MigrationDb;
                if (db == null) return SchemaMigrationResults.Fail("LiteDB is not open.");
                if (!db.CollectionExists(entityName))
                    return SchemaMigrationResults.Fail($"LiteDB collection '{entityName}' not found.");
                bool dropped = db.GetCollection<BsonDocument>(entityName).DropIndex(indexName);
                return dropped
                    ? SchemaMigrationResults.Ok($"Dropped LiteDB index '{indexName}' from '{entityName}'.")
                    : SchemaMigrationResults.Fail($"LiteDB index '{indexName}' not found on '{entityName}'.");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail($"DropIndex('{entityName}', '{indexName}') failed: {ex.Message}");
            }
        }

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
    }
}