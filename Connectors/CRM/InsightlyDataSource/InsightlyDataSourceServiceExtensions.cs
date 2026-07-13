using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Insightly
{
    /// <summary>
    /// Fluent registration for the Insightly data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class InsightlyDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Insightly driver config (classHandler "InsightlyDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddInsightly(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateInsightlyConfig);
    }
}
