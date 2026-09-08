using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Colocated Tier-1 provider for SQL Server Compact Edition (SQL CE 4.0). This class did not
    /// exist, matching the missing <see cref="SQLCompactDataSource"/> class it depends on — see that
    /// class's remarks for why (no ADO.NET provider for this discontinued engine on .NET Core+).
    /// </summary>
    /// <remarks>
    /// SQL CE's ALTER TABLE support is a reduced subset of SQL Server's: it has no
    /// <c>sp_rename</c>-equivalent and no <c>ALTER TABLE ... RENAME</c> at all — a table or column
    /// cannot be renamed once created, only dropped and recreated — and it does not support changing
    /// a column's data type via <c>ALTER COLUMN</c>. DDL in SQL CE is not transactional in the way
    /// full SQL Server's is (its transaction model is a single-connection, non-distributed subset),
    /// so <c>SupportsTransactionalDdl</c> is declared conservatively false rather than guessed true.
    /// </remarks>
    [SchemaMigrationProvider(DataSourceType.SqlCompact, DatasourceCategory.RDBMS)]
    public class SqlCompactMigrationProvider : RdbmsSqlMigrationProvider
    {
        public SqlCompactMigrationProvider(IDataSource owner) : base(owner) { }

        public override SchemaMigrationCapabilities Capabilities => new()
        {
            SupportsCreateEntity = true,
            SupportsDropEntity = true,
            SupportsTruncateEntity = true,
            SupportsRenameEntity = false,          // SQL CE: no ALTER TABLE RENAME / sp_rename equivalent
            SupportsAddColumn = true,
            SupportsAlterColumn = false,           // SQL CE: cannot change a column's data type once created
            SupportsDropColumn = true,
            SupportsRenameColumn = false,          // SQL CE: no column rename support
            SupportsCreateIndex = true,
            SupportsDropIndex = true,
            SupportsAddForeignKey = true,
            SupportsDropForeignKey = true,
            SupportsTransactionalDdl = false        // SQL CE's transaction model does not reliably cover DDL
        };
    }
}
