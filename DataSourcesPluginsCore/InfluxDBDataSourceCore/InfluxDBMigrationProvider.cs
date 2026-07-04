using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace InfluxDBDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.InfluxDB"/>.
    /// Drives the InfluxDB SDK via the owning <see cref="InfluxDBDataSource"/>'s <c>BucketsApi</c>:
    /// create bucket (delegated) and delete bucket (delegated) for migration. InfluxDB is
    /// schemaless (tags/fields appear on first write), so column add is a no-op.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.InfluxDB, DatasourceCategory.NOSQL)]
    public class InfluxDBMigrationProvider : ISchemaMigrationProvider
    {
        private readonly InfluxDBDataSource _owner;

        public InfluxDBMigrationProvider(IDataSource owner)
        {
            _owner = owner as InfluxDBDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected an InfluxDBDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.InfluxDB;
        public DatasourceCategory Category => DatasourceCategory.NOSQL;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real CreateBucketAsync)
            SupportsDropEntity = true,     // delegates to datasource (real DeleteBucketAsync)
            SupportsAddColumn = true,      // schemaless tags/fields → no-op Ok
            SupportsTransactionalDdl = false
            // Truncate/Rename/Alter/DropCol/RenameCol/CreateIndex/DropIndex/AddFK/DropFK: unsupported
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created InfluxDB bucket '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create InfluxDB bucket '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            // Datasource now implements DeleteEntity via BucketsApi.FindBucketByNameAsync + DeleteBucketAsync.
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped InfluxDB bucket '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop InfluxDB bucket '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Ok($"InfluxDB is schemaless; tag/field '{column?.FieldName}' is accepted on first write.");

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