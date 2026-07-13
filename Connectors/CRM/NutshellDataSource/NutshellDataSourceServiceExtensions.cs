using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Nutshell
{
    /// <summary>
    /// Fluent registration for the Nutshell data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class NutshellDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Nutshell driver config (classHandler "NutshellDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddNutshell(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateNutshellConfig);
    }
}
