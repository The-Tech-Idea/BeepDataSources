using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.ConvertKit
{
    /// <summary>
    /// Fluent registration for the ConvertKit data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ConvertKitDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the ConvertKit driver config (classHandler "ConvertKitDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddConvertKit(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateConvertKitConfig);
    }
}
