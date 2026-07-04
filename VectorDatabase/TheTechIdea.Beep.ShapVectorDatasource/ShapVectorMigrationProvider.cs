using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.ShapVectorDatasource
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.ShapVector"/>.
    /// Drives the ShapVector REST API via the owning <see cref="SharpVectorDatasource"/>'s HTTP client:
    /// collection create (delegated to the datasource, which reads dimension/metric from the entity's
    /// "dimension"/"metric" fields with sensible defaults) and DELETE /collections/{name} for drop.
    /// Vector collections index themselves; there is no separate index endpoint.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.ShapVector, DatasourceCategory.VectorDB)]
    public class ShapVectorMigrationProvider : ISchemaMigrationProvider
    {
        private readonly SharpVectorDatasource _owner;

        public ShapVectorMigrationProvider(IDataSource owner)
        {
            _owner = owner as SharpVectorDatasource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a SharpVectorDatasource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.ShapVector;
        public DatasourceCategory Category => DatasourceCategory.VectorDB;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real POST /collections)
            SupportsDropEntity = true,     // DELETE /collections/{name}
            SupportsAddColumn = true,      // schemaless payload → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
            // (Vector collections index themselves; per-vector metadata fields are data-level.)
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // Real: the datasource reads dimension/metric from entity fields and POSTs /collections.
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created ShapVector collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create ShapVector collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                using var resp = _owner.MigrationHttp.DeleteAsync(
                    _owner.MigrationBaseUrl + "collections/" + System.Uri.EscapeDataString(entityName))
                    .GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"Deleted ShapVector collection '{entityName}'.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"ShapVector collections are schemaless; field '{column?.FieldName}' is accepted as vector metadata.");

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
