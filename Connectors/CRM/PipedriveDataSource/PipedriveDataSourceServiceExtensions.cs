using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Pipedrive
{
    /// <summary>
    /// Fluent registration for the Pipedrive data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class PipedriveDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Pipedrive driver config (classHandler "PipedriveDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddPipedrive(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreatePipedriveConfig);
    }
}
