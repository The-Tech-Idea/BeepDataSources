using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    [SchemaMigrationProvider(DataSourceType.SqlServer, DatasourceCategory.RDBMS)]
    public class SQLServerMigrationProvider : RdbmsSqlMigrationProvider
    {
        public SQLServerMigrationProvider(IDataSource owner) : base(owner) { }
        // Full 12/12 standard SQL DDL — SQL Server supports all operations natively (2016+).
    }
}