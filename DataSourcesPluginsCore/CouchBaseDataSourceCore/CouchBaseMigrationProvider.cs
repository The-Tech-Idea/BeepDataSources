using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace CouchBaseDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.Couchbase"/>.
    /// Drives the Couchbase REST API via the owning <see cref="CouchBaseDataSource"/>'s HTTP client:
    /// bucket create (delegated to the datasource, which now PUTs for real) and
    /// <c>DELETE /pools/default/buckets/{name}</c> for drop. Couchbase documents are schemaless JSON.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Couchbase, DatasourceCategory.NOSQL)]
    public class CouchBaseMigrationProvider : ISchemaMigrationProvider
    {
        private readonly CouchBaseDataSource _owner;

        public CouchBaseMigrationProvider(IDataSource owner)
        {
            _owner = owner as CouchBaseDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a CouchBaseDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.Couchbase;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real PUT /pools/default/buckets)
            SupportsDropEntity = true,     // DELETE /pools/default/buckets/{name}
            SupportsAddColumn = true,      // schemaless JSON → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created Couchbase bucket '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create Couchbase bucket '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            try
            {
                _owner.EnsureMigrationConnected();
                var req = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Delete,
                    _owner.MigrationBaseUrl + "pools/default/buckets/" + System.Uri.EscapeDataString(entityName));
                if (!string.IsNullOrEmpty(_owner.keyToken))
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _owner.keyToken);
                using var resp = _owner.MigrationHttp.SendAsync(req).GetAwaiter().GetResult();
                return resp.IsSuccessStatusCode
                    ? SchemaMigrationResults.Ok($"Dropped Couchbase bucket '{entityName}'.")
                    : SchemaMigrationResults.Fail($"{(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
            catch (System.Exception ex) { return SchemaMigrationResults.Fail(ex.Message, ex); }
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"Couchbase documents are schemaless; field '{column?.FieldName}' is accepted implicitly.");

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