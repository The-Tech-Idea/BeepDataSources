using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.DuckDB
{
    /// <summary>
    /// Fluent registration for the DuckDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class DuckDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the DuckDB driver config (classHandler "DuckDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddDuckDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateDuckDBConfig);
    }
}
