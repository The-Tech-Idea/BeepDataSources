using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Qdrant
{
    /// <summary>
    /// Fluent registration for the Qdrant data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class QdrantDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Qdrant driver config (classHandler "QdrantDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddQdrant(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateQdrantConfig);
    }
}
