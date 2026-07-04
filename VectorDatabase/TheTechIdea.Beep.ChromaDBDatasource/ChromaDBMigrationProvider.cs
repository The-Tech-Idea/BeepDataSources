using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ChromaDBDatasource
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.ChromaDB"/>.
    /// Drives the ChromaDB REST API via the owning <see cref="ChromaDBDataSource"/>'s HTTP client:
    /// collection create (delegated to the datasource, which now POSTs for real) and
    /// <c>DELETE /api/v1/collections/{name}</c> for drop. ChromaDB collections index their own embeddings;
    /// there is no separate index endpoint.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.ChromaDB, DatasourceCategory.VectorDB)]
    public class ChromaDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly ChromaDBDataSource _owner;

        public ChromaDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as ChromaDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a ChromaDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.ChromaDB;
        public DatasourceCategory Category => DatasourceCategory.VectorDB;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real POST /api/v1/collections)
            SupportsDropEntity = true,     // DELETE /api/v1/collections/{name}
            SupportsAddColumn = true,      // schemaless metadata → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created ChromaDB collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create ChromaDB collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                using var resp = _owner.MigrationHttp.DeleteAsync(
                    _owner.MigrationBaseUrl + "api/v1/collections/" + System.Uri.EscapeDataString(entityName))
                    .GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"Deleted ChromaDB collection '{entityName}'.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"ChromaDB collections are schemaless; field '{column?.FieldName}' is accepted as document metadata.");

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
