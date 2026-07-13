using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Hadoop
{
    /// <summary>
    /// Fluent registration for the Hadoop data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class HadoopDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Hadoop driver config (classHandler "HadoopDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddHadoop(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateHadoopConfig);
    }
}
