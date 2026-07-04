using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Local.CouchbaseLite
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.CouchBaseLite"/>.
    /// Drives the Couchbase.Lite SDK via the owning <see cref="CouchBaseLiteDataSource"/>'s
    /// <c>Database</c>: create collection (delegated) and drop collection (delegated). Couchbase
    /// Lite documents are schemaless JSON.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.CouchBaseLite, DatasourceCategory.NOSQL)]
    public class CouchBaseLiteMigrationProvider : ISchemaMigrationProvider
    {
        private readonly CouchBaseLiteDataSource _owner;

        public CouchBaseLiteMigrationProvider(IDataSource owner)
        {
            _owner = owner as CouchBaseLiteDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a CouchBaseLiteDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.CouchBaseLite;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real: materialises a collection)
            SupportsDropEntity = true,     // delegates to datasource (real DeleteCollection)
            SupportsAddColumn = true,      // schemaless JSON document → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created Couchbase Lite collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create Couchbase Lite collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped Couchbase Lite collection '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop Couchbase Lite collection '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"Couchbase Lite is schemaless; field '{column?.FieldName}' is accepted as a document field.");

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