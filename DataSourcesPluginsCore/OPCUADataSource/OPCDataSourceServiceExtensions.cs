using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.OPC
{
    /// <summary>
    /// Fluent registration for the OPC data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class OPCDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the OPC driver config (classHandler "OPCDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddOPC(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateOPCConfig);
    }
}
