using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Flock
{
    /// <summary>
    /// Fluent registration for the Flock data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class FlockDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Flock driver config (classHandler "FlockDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddFlock(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateFlockConfig);
    }
}
