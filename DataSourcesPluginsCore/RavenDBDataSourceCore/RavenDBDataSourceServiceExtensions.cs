using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.RavenDB
{
    /// <summary>
    /// Fluent registration for the RavenDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class RavenDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the RavenDB driver config (classHandler "RavenDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddRavenDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateRavenDBConfig);
    }
}
