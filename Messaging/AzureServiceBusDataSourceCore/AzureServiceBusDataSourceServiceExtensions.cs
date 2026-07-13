using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.AzureServiceBus
{
    /// <summary>
    /// Fluent registration for the AzureServiceBus data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class AzureServiceBusDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the AzureServiceBus driver config (classHandler "AzureServiceBusDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddAzureServiceBus(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateAzureServiceBusConfig);
    }
}
