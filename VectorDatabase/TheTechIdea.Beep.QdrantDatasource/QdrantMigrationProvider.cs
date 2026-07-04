using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.QdrantDatasource
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.Qdrant"/>.
    /// Drives the Qdrant REST API via the owning <see cref="QdrantDataSource"/>'s HTTP client:
    /// create collection (delegates to the datasource), DELETE collection, PUT/DELETE payload index.
    /// Qdrant payload is schemaless, so column add is a no-op. Truncate/rename/alter/FK unsupported.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Qdrant, DatasourceCategory.VectorDB)]
    public class QdrantMigrationProvider : ISchemaMigrationProvider
    {
        private readonly QdrantDataSource _owner;

        public QdrantMigrationProvider(IDataSource owner)
        {
            _owner = owner as QdrantDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a QdrantDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.Qdrant;
        public DatasourceCategory Category => DatasourceCategory.VectorDB;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // PUT /collections/{name} (vector config)
            SupportsDropEntity = true,     // DELETE /collections/{name}
            SupportsCreateIndex = true,    // PUT /collections/{name}/index (payload field index)
            SupportsDropIndex = true,      // DELETE /collections/{name}/index/{field}
            SupportsAddColumn = true,      // schemaless payload → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/AlterColumn/DropColumn/RenameColumn/AddFK/DropFK: unsupported
        };

        private HttpClient Http => _owner.MigrationHttp;
        private string Base => _owner.MigrationBaseUrl;

        private IErrorsInfo Delete(string relativeUrl)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                using var resp = Http.DeleteAsync(Base + relativeUrl).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"DELETE {relativeUrl} ok.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        private IErrorsInfo PutJson(string relativeUrl, string json)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = Http.PutAsync(Base + relativeUrl, content).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"PUT {relativeUrl} ok.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        private static string Coll(string name) => "collections/" + System.Uri.EscapeDataString(name);

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            // Delegates to the datasource's real collection-creation (vector config incl.).
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created Qdrant collection '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail($"Failed to create Qdrant collection '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName) => Delete(Coll(entityName));

        public IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string, object> options = null)
        {
            if (columns == null || columns.Length == 0)
                return SchemaMigrationResults.Fail("At least one payload field is required to create an index.");
            // Qdrant indexes one payload field at a time; index on the first column.
            var field = columns[0];
            var body = new { field_name = field, field_schema = "keyword" };
            return PutJson($"{Coll(entityName)}/index?wait=true", System.Text.Json.JsonSerializer.Serialize(body));
        }

        public IErrorsInfo DropIndex(string entityName, string indexName)
            => Delete($"{Coll(entityName)}/index/{System.Uri.EscapeDataString(indexName)}?wait=true");

        // Qdrant payload is schemaless — adding a field needs no structural change.
        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"Qdrant payload is schemaless; field '{column?.FieldName}' is accepted implicitly.");

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
        public IErrorsInfo AddForeignKey(string entityName, string[] columnNames, string referencedEntityName, string[] referencedColumnNames, string onDeleteBehavior, string onUpdateBehavior, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(AddForeignKey), DataSourceType);
        public IErrorsInfo DropForeignKey(string entityName, string constraintName)
            => SchemaMigrationResults.Unsupported(nameof(DropForeignKey), DataSourceType);
    }
}
