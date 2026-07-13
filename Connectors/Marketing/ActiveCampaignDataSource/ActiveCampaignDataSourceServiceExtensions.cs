using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.ActiveCampaign
{
    /// <summary>
    /// Fluent registration for the ActiveCampaign data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ActiveCampaignDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the ActiveCampaign driver config (classHandler "ActiveCampaignDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddActiveCampaign(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateActiveCampaignConfig);
    }
}
