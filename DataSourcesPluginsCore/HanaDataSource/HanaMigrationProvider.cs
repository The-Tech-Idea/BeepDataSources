using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Colocated Tier-1 provider for SAP HANA. Full 12/12 DDL — HANA is a full-featured enterprise
    /// RDBMS with ALTER TABLE ADD/DROP/ALTER COLUMN, RENAME COLUMN, indexes and transactional DDL
    /// all natively supported. This class did not exist; the 13 other RDBMS drivers each declare one
    /// (see CLAUDE.md's "Schema migration providers" section) and HANA was the exception.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Hana, DatasourceCategory.RDBMS)]
    public class HanaMigrationProvider : RdbmsSqlMigrationProvider
    {
        public HanaMigrationProvider(IDataSource owner) : base(owner) { }
    }
}
