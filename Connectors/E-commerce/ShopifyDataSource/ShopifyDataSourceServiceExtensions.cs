using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Shopify
{
    /// <summary>
    /// Fluent registration for the Shopify data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ShopifyDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Shopify driver config (classHandler "ShopifyDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddShopify(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateShopifyConfig);
    }
}
