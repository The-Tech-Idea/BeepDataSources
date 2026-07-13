using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Oracle
{
    /// <summary>
    /// Fluent registration for the Oracle data source driver.
    /// Only visible to hosts that reference this OracleDataSourceCore project / NuGet.
    /// </summary>
    public static class OracleDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Oracle driver config (DataSourceType.Oracle,
        /// classHandler "OracleDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddOracleDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateOracleConfig);
    }
}
