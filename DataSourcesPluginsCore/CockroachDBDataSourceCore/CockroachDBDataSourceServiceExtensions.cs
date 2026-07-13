using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.CockroachDB
{
    /// <summary>
    /// Fluent registration for the CockroachDB data source driver.
    /// Only visible to hosts that reference this CockroachDBDataSourceCore project / NuGet.
    /// </summary>
    public static class CockroachDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the CockroachDB driver config (DataSourceType.Cockroach,
        /// classHandler "CockroachDBDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddCockroachDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateCockroachConfig);
    }
}
