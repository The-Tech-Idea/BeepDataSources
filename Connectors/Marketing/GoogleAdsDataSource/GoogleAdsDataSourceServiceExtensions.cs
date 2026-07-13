using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.GoogleAds
{
    /// <summary>
    /// Fluent registration for the GoogleAds data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class GoogleAdsDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the GoogleAds driver config (classHandler "GoogleAdsDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddGoogleAds(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateGoogleAdsConfig);
    }
}
