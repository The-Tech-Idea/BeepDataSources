using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    [SchemaMigrationProvider(DataSourceType.Oracle, DatasourceCategory.RDBMS)]
    public class OracleMigrationProvider : RdbmsSqlMigrationProvider
    {
        public OracleMigrationProvider(IDataSource owner) : base(owner) { }
        // Full 12/12 standard SQL DDL — Oracle supports all operations natively.
    }
}