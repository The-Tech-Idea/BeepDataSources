using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.SqlServer
{
    /// <summary>
    /// Fluent registration for the SQL Server data source driver.
    /// Only visible to hosts that reference this SqlServerDataSourceCore project / NuGet.
    /// </summary>
    public static class SqlServerDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the SQL Server driver config (DataSourceType.SqlServer,
        /// classHandler "SQLServerDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddSqlServerDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSqlServerConfig);
    }
}
