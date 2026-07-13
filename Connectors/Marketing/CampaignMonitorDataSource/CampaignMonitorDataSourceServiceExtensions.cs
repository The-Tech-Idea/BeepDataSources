using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.CampaignMonitor
{
    /// <summary>
    /// Fluent registration for the CampaignMonitor data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class CampaignMonitorDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the CampaignMonitor driver config (classHandler "CampaignMonitorDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddCampaignMonitor(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateCampaignMonitorConfig);
    }
}
