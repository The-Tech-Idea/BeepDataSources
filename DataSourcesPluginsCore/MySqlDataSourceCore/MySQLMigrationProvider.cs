using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    [SchemaMigrationProvider(DataSourceType.Mysql, DatasourceCategory.RDBMS)]
    public class MySQLMigrationProvider : RdbmsSqlMigrationProvider
    {
        public MySQLMigrationProvider(IDataSource owner) : base(owner) { }
        // Full 12/12 standard SQL DDL — MySQL/MariaDB supports all operations natively.
    }
}