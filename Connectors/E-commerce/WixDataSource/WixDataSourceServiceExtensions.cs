using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Wix
{
    /// <summary>
    /// Fluent registration for the Wix data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class WixDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Wix driver config (classHandler "WixDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddWix(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateWixConfig);
    }
}
