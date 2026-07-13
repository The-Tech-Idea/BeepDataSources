using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Kafka
{
    /// <summary>
    /// Fluent registration for the Kafka data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class KafkaDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Kafka driver config (classHandler "KafkaDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddKafka(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateKafkaConfig);
    }
}
