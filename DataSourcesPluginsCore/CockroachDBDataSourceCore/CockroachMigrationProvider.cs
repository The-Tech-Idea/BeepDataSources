using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    [SchemaMigrationProvider(DataSourceType.Cockroach, DatasourceCategory.RDBMS)]
    public class CockroachMigrationProvider : RdbmsSqlMigrationProvider
    {
        public CockroachMigrationProvider(IDataSource owner) : base(owner) { }
        // Full 12/12 standard SQL DDL — CockroachDB is PostgreSQL-compatible.
    }
}