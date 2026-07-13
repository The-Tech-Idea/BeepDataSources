using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.InfluxDB
{
    /// <summary>
    /// Fluent registration for the InfluxDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class InfluxDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the InfluxDB driver config (classHandler "InfluxDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddInfluxDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateInfluxDBConfig);
    }
}
