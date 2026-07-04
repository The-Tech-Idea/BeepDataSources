using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Colocated Tier-1 <see cref="ISchemaMigrationProvider"/> for <see cref="DataSourceType.SqlLite"/>.
    /// Extends <see cref="RdbmsSqlMigrationProvider"/> with honest SQLite-specific capabilities:
    /// ALTER COLUMN and DROP COLUMN use table-rebuild patterns (not supported natively),
    /// DROP FOREIGN KEY requires a table rebuild, and DDL operations are not fully
    /// transactional in SQLite (table rebuilds bypass the transaction engine).
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.SqlLite, DatasourceCategory.RDBMS)]
    public class SQLiteMigrationProvider : RdbmsSqlMigrationProvider
    {
        public SQLiteMigrationProvider(IDataSource owner) : base(owner) { }

        public new SchemaMigrationCapabilities Capabilities => new()
        {
            SupportsCreateEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            SupportsRenameEntity = true,
            SupportsAddColumn = true,
            SupportsAlterColumn = false,          // SQLite: table-rebuild required, no native ALTER COLUMN
            SupportsDropColumn = false,           // SQLite: table-rebuild required (3.35+ has DROP COLUMN, but we stay conservative)
            SupportsRenameColumn = true,          // SQLite 3.25+ supports RENAME COLUMN
            SupportsCreateIndex = true,
            SupportsDropIndex = true,
            SupportsAddForeignKey = true,         // Needs PRAGMA foreign_keys = ON
            SupportsDropForeignKey = false,       // SQLite: table-rebuild required, no native DROP FOREIGN KEY
            SupportsTransactionalDdl = false      // SQLite: table-rebuild DDL bypasses transactions
        };
    }
}