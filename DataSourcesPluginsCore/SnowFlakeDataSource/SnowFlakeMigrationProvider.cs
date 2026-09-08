using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Cloud.Snowflake
{
    /// <summary>
    /// Colocated Tier-1 provider for Snowflake. This class did not exist.
    /// </summary>
    /// <remarks>
    /// Two genuine capability gaps, not the base's optimistic 12/12 default:
    ///
    /// Snowflake has no user-managed index concept at all — it relies on automatic micro-partition
    /// pruning and clustering keys instead of the B-tree/hash indexes every other engine here
    /// declares. <c>CREATE INDEX</c>/<c>DROP INDEX</c> are not Snowflake syntax; declaring them
    /// supported the way <see cref="RdbmsSqlMigrationProvider"/>'s default does would let
    /// <c>MigrationManager</c> attempt DDL Snowflake rejects outright, rather than refusing up front
    /// the way an honest capability declaration is supposed to.
    ///
    /// DDL statements in Snowflake auto-commit — there is no wrapping a <c>CREATE</c>/<c>ALTER</c> in
    /// a transaction the way <see cref="SchemaMigrationCapabilities.SupportsTransactionalDdl"/>
    /// promises for the engines that declare it.
    /// </remarks>
    [SchemaMigrationProvider(DataSourceType.SnowFlake, DatasourceCategory.RDBMS)]
    public class SnowFlakeMigrationProvider : RdbmsSqlMigrationProvider
    {
        public SnowFlakeMigrationProvider(IDataSource owner) : base(owner) { }

        public override SchemaMigrationCapabilities Capabilities => new()
        {
            SupportsCreateEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            SupportsRenameEntity = true,
            SupportsAddColumn = true,
            SupportsAlterColumn = true,
            SupportsDropColumn = true,
            SupportsRenameColumn = true,
            SupportsCreateIndex = false,          // Snowflake: no user-managed indexes; automatic micro-partition pruning instead
            SupportsDropIndex = false,
            SupportsAddForeignKey = true,          // Syntactically supported; Snowflake does not enforce it (informational only)
            SupportsDropForeignKey = true,
            SupportsTransactionalDdl = false       // Snowflake: DDL auto-commits, cannot be wrapped in a transaction
        };
    }
}
