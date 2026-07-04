using TheTechIdea.Beep.Editor.SchemaMigration;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Cloud.Spanner
{
    /// <summary>
    /// Colocated Tier-1 provider for Google Cloud Spanner. Full 12/12 standard SQL DDL
    /// (Spanner is PostgreSQL-compatible). FK toggle is a no-op because Spanner enforces
    /// referential integrity at the database level.
    /// </summary>
    [SchemaMigrationProvider(DataSourceType.Spanner, DatasourceCategory.RDBMS)]
    public class SpannerMigrationProvider : RdbmsSqlMigrationProvider
    {
        public SpannerMigrationProvider(IDataSource owner) : base(owner) { }
    }
}