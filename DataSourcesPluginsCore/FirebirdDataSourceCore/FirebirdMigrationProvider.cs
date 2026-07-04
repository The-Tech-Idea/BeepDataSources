using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Colocated Tier-1 provider for Firebird (networked + embedded). Full 12/12 DDL.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.FireBird, DatasourceCategory.RDBMS)]
    public class FirebirdMigrationProvider : RdbmsSqlMigrationProvider
    {
        public FirebirdMigrationProvider(IDataSource owner) : base(owner) { }
    }
}