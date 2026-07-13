using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Drip
{
    /// <summary>
    /// Fluent registration for the Drip data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class DripDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Drip driver config (classHandler "DripDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddDrip(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateDripConfig);
    }
}
