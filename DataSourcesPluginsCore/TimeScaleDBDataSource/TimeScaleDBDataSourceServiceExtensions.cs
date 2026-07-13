using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.TimeScaleDB
{
    /// <summary>
    /// Fluent registration for the TimeScaleDB data source driver.
    /// Only visible to hosts that reference this TimeScaleDBDataSource project / NuGet.
    /// </summary>
    public static class TimeScaleDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the TimeScaleDB driver config (DataSourceType.TimeScale,
        /// classHandler "TimeScaleDBDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddTimeScaleDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateTimeScaleConfig);
    }
}
