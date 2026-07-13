using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.SqlCompact
{
    /// <summary>
    /// Fluent registration for the SQL Server Compact data source driver.
    /// Only visible to hosts that reference this SqlCompactDatasourceCore project / NuGet.
    /// </summary>
    public static class SqlCompactDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the SQL Compact driver config (DataSourceType.SqlCompact,
        /// classHandler "SQLCompactDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddSqlCompactDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSqlCompactConfig);
    }
}
