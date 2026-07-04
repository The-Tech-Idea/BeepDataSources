using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.NOSQL.CouchDB
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.CouchDB"/>.
    /// Translates logical operations into native CouchDB calls via the owning <see cref="CouchDBDataSource"/>'s
    /// driver: createEntity creates (or gets) a dedicated database; dropEntity deletes it. Documents
    /// are schemaless JSON, so column add is a no-op. Design-doc views (CouchDB's "indexes") and FKs
    /// are not handled here — those are document-shape concerns, not structure concerns.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.CouchDB, DatasourceCategory.NOSQL)]
    public class CouchDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly CouchDBDataSource _owner;

        public CouchDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as CouchDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a CouchDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.CouchDB;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // creates dedicated database
            SupportsDropEntity = true,     // drops the database
            SupportsAddColumn = true,      // schemaless JSON → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
            // (CouchDB indexes are design-doc views; per-document field changes are data-level).
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // Real DB creation via the datasource (now using GetOrCreateDatabaseAsync, not in-memory).
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Ensured CouchDB database '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create CouchDB database '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                _owner.MigrationClient.DeleteDatabaseAsync(entityName).GetAwaiter().GetResult();
                return SchemaMigrationResults.Ok($"Dropped CouchDB database '{entityName}'.");
            }
            catch (System.Exception ex)
            {
                return SchemaMigrationResults.Fail(ex.Message, ex);
            }
        }

        // CouchDB documents are schemaless JSON — adding a field needs no structural change.
        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"CouchDB is schemaless; field '{column?.FieldName}' is accepted implicitly.");

        public IErrorsInfo TruncateEntity(string entityName)
            => SchemaMigrationResults.Unsupported(nameof(TruncateEntity), DataSourceType);
        public IErrorsInfo RenameEntity(string oldName, string newName)
            => SchemaMigrationResults.Unsupported(nameof(RenameEntity), DataSourceType);
        public IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn)
            => SchemaMigrationResults.Unsupported(nameof(AlterColumn), DataSourceType);
        public IErrorsInfo DropColumn(string entityName, string columnName)
            => SchemaMigrationResults.Unsupported(nameof(DropColumn), DataSourceType);
        public IErrorsInfo RenameColumn(string entityName, string oldColumnName, string newColumnName)
            => SchemaMigrationResults.Unsupported(nameof(RenameColumn), DataSourceType);
        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
            => SchemaMigrationResults.Unsupported(nameof(CreateIndex), DataSourceType);
        public IErrorsInfo DropIndex(string entityName, string indexName)
            => SchemaMigrationResults.Unsupported(nameof(DropIndex), DataSourceType);
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    }
}
