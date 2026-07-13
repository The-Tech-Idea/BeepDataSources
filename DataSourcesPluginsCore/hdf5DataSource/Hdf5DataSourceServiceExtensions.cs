using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Hdf5DataSource
{
    /// <summary>
    /// Fluent registration for the Hdf5DataSource data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class Hdf5DataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Hdf5DataSource driver config (classHandler "Hdf5DataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddHdf5DataSource(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateHdf5DataSourceConfig);
    }
}
