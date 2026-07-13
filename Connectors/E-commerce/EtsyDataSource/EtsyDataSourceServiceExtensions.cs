using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Etsy
{
    /// <summary>
    /// Fluent registration for the Etsy data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class EtsyDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Etsy driver config (classHandler "EtsyDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddEtsy(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateEtsyConfig);
    }
}
