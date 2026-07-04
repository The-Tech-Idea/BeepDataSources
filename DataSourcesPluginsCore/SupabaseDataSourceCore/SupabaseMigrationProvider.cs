using System.Collections.Generic;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace SupabaseDataSourceCore
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.Supabase"/>.
    /// Supabase schema management (table create/drop/alter) lives in the Supabase SQL editor
    /// or Admin API — PostgREST is read/write only. The provider delegates CreateEntity to the
    /// datasource (which checks existence via the PostgREST HEAD endpoint) and reports the rest
    /// honestly as <c>Unsupported</c> rather than fake-successing.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Supabase, DatasourceCategory.WEBAPI)]
    public class SupabaseMigrationProvider : ISchemaMigrationProvider
    {
        private readonly SupabaseDataSource _owner;

        public SupabaseMigrationProvider(IDataSource owner)
        {
            _owner = owner as SupabaseDataSource
                     ?? throw new System.ArgumentNullException(nameof(owner), "Expected a SupabaseDataSource instance.");
        }

        public DataSourceType DataSourceType => DataSourceType.Supabase;
        public DatasourceCategory Category => DatasourceCategory.WEBAPI;

        public SchemaMigrationCapabilities Capabilities { get; } = new SchemaMigrationCapabilities
        {
            // Supabase schema management is outside PostgREST; the provider is honest about it.
            SupportsCreateEntity = true,   // delegates (checks existence; reports missing tables)
            SupportsAddColumn = false,
            SupportsDropEntity = false,
            SupportsTransactionalDdl = false
        };

        public IErrorsInfo CreateEntity(EntityStructure entity)
        {
            var ok = _owner.CreateEntityAs(entity);
            return ok
                ? SchemaMigrationResults.Ok($"Supabase table '{entity?.EntityName}' exists.")
                : SchemaMigrationResults.Fail(_owner.ErrorObject?.Message
                    ?? $"Supabase table '{entity?.EntityName}' not found — create it in the Supabase SQL editor first.");
        }

        public IErrorsInfo AddColumn(string entityName, EntityField column)
            => SchemaMigrationResults.Unsupported(nameof(AddColumn), DataSourceType);
        public IErrorsInfo DropEntity(string entityName)
            => SchemaMigrationResults.Unsupported(nameof(DropEntity), DataSourceType);
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