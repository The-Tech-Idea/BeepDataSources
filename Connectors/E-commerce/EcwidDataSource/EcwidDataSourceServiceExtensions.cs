using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Ecwid
{
    /// <summary>
    /// Fluent registration for the Ecwid data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class EcwidDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Ecwid driver config (classHandler "EcwidDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddEcwid(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateEcwidConfig);
    }
}
