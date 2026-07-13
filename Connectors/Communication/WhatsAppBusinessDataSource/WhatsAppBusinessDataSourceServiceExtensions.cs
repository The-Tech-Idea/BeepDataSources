using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.WhatsAppBusiness
{
    /// <summary>
    /// Fluent registration for the WhatsAppBusiness data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class WhatsAppBusinessDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the WhatsAppBusiness driver config (classHandler "WhatsAppBusinessDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddWhatsAppBusiness(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateWhatsAppBusinessConfig);
    }
}
