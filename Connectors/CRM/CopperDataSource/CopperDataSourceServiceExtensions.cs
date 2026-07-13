using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Copper
{
    /// <summary>
    /// Fluent registration for the Copper data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class CopperDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Copper driver config (classHandler "CopperDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddCopper(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateCopperConfig);
    }
}
