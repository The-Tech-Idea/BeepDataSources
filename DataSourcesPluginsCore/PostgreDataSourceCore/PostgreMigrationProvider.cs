using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    [SchemaMigrationProvider(DataSourceType.Postgre, DatasourceCategory.RDBMS)]
    public class PostgreMigrationProvider : RdbmsSqlMigrationProvider
    {
        public PostgreMigrationProvider(IDataSource owner) : base(owner) { }
        // Full 12/12 standard SQL DDL — PostgreSQL supports all operations natively.
    }
}