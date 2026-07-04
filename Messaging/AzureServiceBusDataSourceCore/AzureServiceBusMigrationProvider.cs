using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.AzureServiceBus
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for
    /// <see cref="DataSourceType.AzureServiceBus"/>. Drives the Azure SDK via the owning
    /// <see cref="AzureServiceBusDataSource"/>'s <c>ServiceBusAdministrationClient</c>:
    /// create queue (delegated, which uses <c>CreateQueueAsync</c>) and drop queue/topic
    /// (delegated). ASB is FIFO/Standard queues + topics + subscriptions; column/index ops don't apply.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.AzureServiceBus, DatasourceCategory.MessageQueue)]
    public class AzureServiceBusMigrationProvider : ISchemaMigrationProvider
    {
        private readonly AzureServiceBusDataSource _owner;

        public AzureServiceBusMigrationProvider(IDataSource owner)
        {
            _owner = owner as AzureServiceBusDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected an AzureServiceBusDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.AzureServiceBus;
        public DatasourceCategory Category => DatasourceCategory.MessageQueue;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            SupportsCreateEntity = true,   // delegates to datasource (real CreateQueue)
            SupportsDropEntity = true,     // delegates to datasource (real DeleteQueue/DeleteTopic)
            SupportsTransactionalDdl = false
            // ASB has no column/index/FK semantics.
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Created Service Bus entity '{entity?.EntityName}'.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message ?? $"Failed to create Service Bus entity '{entity?.EntityName}'.");
        }

        public IErrorsInfo DropEntity(string entityName)
        {
            var r = _owner.DeleteEntity(entityName, null);
            return r != null && r.Flag == Errors.Ok
                ? SchemaMigrationResults.Ok($"Dropped Service Bus entity '{entityName}'.")
                : SchemaMigrationResults.Fail(r?.Message ?? $"Failed to drop Service Bus entity '{entityName}'.");
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