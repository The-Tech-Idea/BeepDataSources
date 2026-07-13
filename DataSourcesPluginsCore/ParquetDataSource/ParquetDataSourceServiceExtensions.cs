using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.ParquetDataSource
{
    /// <summary>
    /// Fluent registration for the ParquetDataSource data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ParquetDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the ParquetDataSource driver config (classHandler "ParquetDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddParquetDataSource(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateParquetDataSourceConfig);
    }
}
