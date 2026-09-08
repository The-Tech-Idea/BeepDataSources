using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Cloud.Presto
{
    /// <summary>
    /// Colocated Tier-1 provider for Presto/Trino. This class did not exist.
    /// </summary>
    /// <remarks>
    /// Conservative by design, matching <see cref="PrestoDataSource"/>'s own stance in
    /// <c>DisableFKConstraints</c>/<c>EnableFKConstraints</c>: "Presto is primarily a query engine,
    /// not a transactional database." Presto/Trino sit in front of many different backing catalogs
    /// (Hive, Iceberg, MySQL, memory, ...), and DDL support genuinely varies by which connector a
    /// given deployment uses — a capability this class cannot know at compile time. Declaring
    /// everything the base's optimistic default does would be guessing; declaring only what is true
    /// for Presto/Trino's own SQL layer regardless of connector is the honest middle ground:
    ///
    /// No user-managed indexes (index support, where it exists at all, is connector-specific and not
    /// part of the ANSI-ish surface this driver targets). No enforced foreign keys — Presto/Trino do
    /// not enforce referential integrity even where a connector accepts the DDL. No transactional
    /// DDL — Presto has no cross-statement DDL transaction concept. <c>ALTER COLUMN ... SET DATA
    /// TYPE</c> is left unsupported: type changes are the one column operation without broad,
    /// connector-independent support.
    /// </remarks>
    [SchemaMigrationProvider(DataSourceType.Presto, DatasourceCategory.RDBMS)]
    public class PrestoMigrationProvider : RdbmsSqlMigrationProvider
    {
        public PrestoMigrationProvider(IDataSource owner) : base(owner) { }

        public override SchemaMigrationCapabilities Capabilities => new()
        {
            SupportsCreateEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            SupportsRenameEntity = true,
            SupportsAddColumn = true,
            SupportsAlterColumn = false,           // type changes are not reliably supported across connectors
            SupportsDropColumn = true,
            SupportsRenameColumn = true,
            SupportsCreateIndex = false,           // connector-specific at best; no ANSI-layer index support
            SupportsDropIndex = false,
            SupportsAddForeignKey = false,          // not enforced; matches PrestoDataSource's own FK-toggle stance
            SupportsDropForeignKey = false,
            SupportsTransactionalDdl = false        // no cross-statement DDL transaction concept
        };
    }
}
