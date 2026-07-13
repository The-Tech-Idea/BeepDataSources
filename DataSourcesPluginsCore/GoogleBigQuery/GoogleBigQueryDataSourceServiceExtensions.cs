using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.GoogleBigQuery
{
    /// <summary>
    /// Fluent registration for the GoogleBigQuery data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class GoogleBigQueryDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the GoogleBigQuery driver config (classHandler "GoogleBigQueryDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddGoogleBigQuery(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateGoogleBigQueryConfig);
    }
}
