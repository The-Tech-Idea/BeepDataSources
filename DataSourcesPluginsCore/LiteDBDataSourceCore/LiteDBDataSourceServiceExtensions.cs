using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.LiteDBDataSource
{
    /// <summary>
    /// Fluent registration for the LiteDBDataSource data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class LiteDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the LiteDBDataSource driver config (classHandler "LiteDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddLiteDBDataSource(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateLiteDBDataSourceConfig);
    }
}
