using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.BigCommerce
{
    /// <summary>
    /// Fluent registration for the BigCommerce data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class BigCommerceDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the BigCommerce driver config (classHandler "BigCommerceDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddBigCommerce(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateBigCommerceConfig);
    }
}
