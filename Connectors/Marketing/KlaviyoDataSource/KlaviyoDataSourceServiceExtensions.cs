using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Klaviyo
{
    /// <summary>
    /// Fluent registration for the Klaviyo data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class KlaviyoDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Klaviyo driver config (classHandler "KlaviyoDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddKlaviyo(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateKlaviyoConfig);
    }
}
