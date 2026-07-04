using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.AmazonSQS
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.AmazonSQS"/>.
    /// Drives the AWS SDK via the owning <see cref="AmazonSQSDataSource"/>'s <c>AmazonSQSClient</c>:
    /// create queue (delegated, which uses <c>CreateQueueRequest</c>) and drop queue (delegated via
    /// <c>DeleteQueueRequest</c>). SQS is FIFO/Standard queues; column/index ops don't apply.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.AmazonSQS, DatasourceCategory.MessageQueue)]
    public class AmazonSQSMigrationProvider : ISchemaMigrationProvider
    {
        private readonly AmazonSQSDataSource _owner;

        public AmazonSQSMigrationProvider(IDataSource owner)
        {
            _owner = owner as AmazonSQSDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected an AmazonSQSDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.AmazonSQS;
        public DatasourceCategory Category => DatasourceCategory.MessageQueue;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real CreateQueue)
            SupportsDropEntity = true,     // delegates to datasource (real DeleteQueue)
            SupportsTransactionalDdl = false
            // SQS has no column/index/FK semantics.
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created SQS queue '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create SQS queue '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped SQS queue '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop SQS queue '{entityName}'.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Unsupported(nameof(AddColumn), DataSourceType);
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