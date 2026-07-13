using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.ChromaDB
{
    /// <summary>
    /// Fluent registration for the ChromaDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ChromaDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the ChromaDB driver config (classHandler "ChromaDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddChromaDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateChromaDBConfig);
    }
}
