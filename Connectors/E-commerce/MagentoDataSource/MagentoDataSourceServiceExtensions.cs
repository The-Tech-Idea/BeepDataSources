using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Magento
{
    /// <summary>
    /// Fluent registration for the Magento data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MagentoDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Magento driver config (classHandler "MagentoDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMagento(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMagentoConfig);
    }
}
