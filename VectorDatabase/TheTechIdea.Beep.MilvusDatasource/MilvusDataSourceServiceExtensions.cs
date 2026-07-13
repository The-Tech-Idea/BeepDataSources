using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Milvus
{
    /// <summary>
    /// Fluent registration for the Milvus data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MilvusDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Milvus driver config (classHandler "MilvusDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMilvus(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMilvusConfig);
    }
}
