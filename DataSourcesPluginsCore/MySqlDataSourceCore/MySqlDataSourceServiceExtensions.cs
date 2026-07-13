using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.MySql
{
    /// <summary>
    /// Fluent registration for the MySQL data source driver.
    /// Only visible to hosts that reference this MySqlDataSourceCore project / NuGet.
    /// </summary>
    public static class MySqlDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the MySQL driver config (DataSourceType.Mysql,
        /// classHandler "MySQLDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddMySqlDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMySqlConfig);
    }
}
