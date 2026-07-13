using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.SnowFlake
{
    /// <summary>
    /// Fluent registration for the SnowFlake data source driver.
    /// Only visible to hosts that reference this SnowFlakeDataSource project / NuGet.
    /// </summary>
    public static class SnowFlakeDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the SnowFlake driver config (DataSourceType.SnowFlake,
        /// classHandler "SnowFlakeDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddSnowFlakeDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSnowFlakeConfig);
    }
}
