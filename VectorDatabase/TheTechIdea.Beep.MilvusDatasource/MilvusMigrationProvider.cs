using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.MilvusDatasource
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.Milvus"/>.
    /// Drives the Milvus REST API via the owning <see cref="MilvusDataSource"/>'s HTTP client:
    /// collection create (delegated to the datasource, which now POSTs for real, reading
    /// dimension from the entity's "dimension" field) and <c>POST /v1/vector/collections/drop</c>
    /// for drop. Milvus collections index their own embeddings.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Milvus, DatasourceCategory.VectorDB)]
    public class MilvusMigrationProvider : ISchemaMigrationProvider
    {
        private readonly MilvusDataSource _owner;

        public MilvusMigrationProvider(IDataSource owner)
        {
            _owner = owner as MilvusDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a MilvusDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.Milvus;
        public DatasourceCategory Category => DatasourceCategory.VectorDB;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real POST /v1/vector/collections/create)
            SupportsDropEntity = true,     // POST /v1/vector/collections/drop
            SupportsAddColumn = true,      // schemaless payload → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created Milvus collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create Milvus collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var body = new { collectionName = entityName };
                using var content = new System.Net.Http.StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8, "application/json");
                using var resp = _owner.MigrationHttp
                    .PostAsync(_owner.MigrationBaseUrl + "v1/vector/collections/drop", content)
                    .GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"Dropped Milvus collection '{entityName}'.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"Milvus collections are schemaless; field '{column?.FieldName}' is accepted as payload.");

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