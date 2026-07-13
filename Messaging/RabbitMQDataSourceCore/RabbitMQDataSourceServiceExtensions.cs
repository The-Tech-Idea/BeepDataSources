using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.RabbitMQ
{
    /// <summary>
    /// Fluent registration for the RabbitMQ data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class RabbitMQDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the RabbitMQ driver config (classHandler "RabbitMQDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddRabbitMQ(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateRabbitMQConfig);
    }
}
