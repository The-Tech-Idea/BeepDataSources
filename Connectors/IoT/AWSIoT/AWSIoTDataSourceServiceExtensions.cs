using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.AWSIoT
{
    /// <summary>
    /// Fluent registration for the AWSIoT data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class AWSIoTDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the AWSIoT driver config (classHandler "AWSIoTDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddAWSIoT(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateAWSIoTConfig);
    }
}
