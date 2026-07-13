using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.RealIM
{
    /// <summary>
    /// Fluent registration for the RealIM data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class RealMDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the RealIM driver config (classHandler "RealMDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddRealIM(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateRealIMConfig);
    }
}
