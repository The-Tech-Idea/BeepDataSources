using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.WooCommerce
{
    /// <summary>
    /// Fluent registration for the WooCommerce data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class WooCommerceDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the WooCommerce driver config (classHandler "WooCommerceDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddWooCommerce(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateWooCommerceConfig);
    }
}
