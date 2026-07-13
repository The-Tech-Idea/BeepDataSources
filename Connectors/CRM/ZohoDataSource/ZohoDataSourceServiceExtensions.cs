using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Zoho
{
    /// <summary>
    /// Fluent registration for the Zoho data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ZohoDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Zoho driver config (classHandler "ZohoDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddZoho(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateZohoConfig);
    }
}
