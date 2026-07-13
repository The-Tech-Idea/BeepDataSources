using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Sqlite
{
    /// <summary>
    /// Fluent registration for the SQLite data source driver.
    /// Only visible to hosts that reference this SqliteDatasourceCore project / NuGet.
    /// </summary>
    public static class SqliteDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the SQLite driver config (DataSourceType.SqlLite,
        /// classHandler "SQLiteDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddSqliteDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSQLiteConfig);
    }
}
