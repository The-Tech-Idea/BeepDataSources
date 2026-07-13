using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Chanty
{
    /// <summary>
    /// Fluent registration for the Chanty data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ChantyDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Chanty driver config (classHandler "ChantyDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddChanty(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateChantyConfig);
    }
}
