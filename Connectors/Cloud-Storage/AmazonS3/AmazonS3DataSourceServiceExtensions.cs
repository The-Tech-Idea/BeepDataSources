using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.AmazonS3
{
    /// <summary>
    /// Fluent registration for the AmazonS3 data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class AmazonS3DataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the AmazonS3 driver config (classHandler "AmazonS3DataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddAmazonS3(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateAmazonS3Config);
    }
}
