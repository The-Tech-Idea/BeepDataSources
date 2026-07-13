using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.HubSpot
{
    /// <summary>
    /// Fluent registration for the HubSpot data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class HubSpotDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the HubSpot driver config (classHandler "HubSpotDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddHubSpot(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateHubSpotConfig);
    }
}
