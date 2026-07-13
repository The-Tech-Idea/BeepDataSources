using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Asana
{
    /// <summary>
    /// Fluent registration for the Asana data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class AsanaDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Asana driver config (classHandler "AsanaDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddAsana(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateAsanaConfig);
    }
}
