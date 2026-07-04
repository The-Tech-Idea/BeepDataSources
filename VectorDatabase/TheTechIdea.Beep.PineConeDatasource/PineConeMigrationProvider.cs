using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.PineConeDatasource
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.PineCone"/>.
    /// PineCone's "entity" is an index. Provider delegates create to the datasource (real
    /// <c>POST /indexes</c>) and drives <c>DELETE /indexes/{name}</c> for drop via the internal
    /// HTTP accessors. PineCone vectors are schemaless JSON payloads.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.PineCone, DatasourceCategory.VectorDB)]
    public class PineConeMigrationProvider : ISchemaMigrationProvider
    {
        private readonly PineConeDatasource _owner;

        public PineConeMigrationProvider(IDataSource owner)
        {
            _owner = owner as PineConeDatasource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a PineConeDatasource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.PineCone;
        public DatasourceCategory Category => DatasourceCategory.VectorDB;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real CreateIndex POST /indexes)
            SupportsDropEntity = true,     // DELETE /indexes/{name}
            SupportsAddColumn = true,      // schemaless vector metadata → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created PineCone index '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create PineCone index '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                using var req = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Delete,
                    _owner.MigrationBaseUrl + "indexes/" + System.Uri.EscapeDataString(entityName));
                using var resp = _owner.MigrationHttp.SendAsync(req).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"Dropped PineCone index '{entityName}'.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"PineCone vectors are schemaless; field '{column?.FieldName}' is accepted as vector metadata.");

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