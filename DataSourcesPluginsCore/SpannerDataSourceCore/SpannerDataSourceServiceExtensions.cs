using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Spanner
{
    /// <summary>
    /// Fluent registration for the Google Spanner data source driver.
    /// Only visible to hosts that reference this SpannerDataSourceCore project / NuGet.
    /// </summary>
    public static class SpannerDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Spanner driver config (DataSourceType.Spanner,
        /// classHandler "SpannerDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddSpannerDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSpannerConfig);
    }
}
