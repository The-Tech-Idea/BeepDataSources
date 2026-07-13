using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Volusion
{
    /// <summary>
    /// Fluent registration for the Volusion data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class VolusionDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Volusion driver config (classHandler "VolusionDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddVolusion(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateVolusionConfig);
    }
}
