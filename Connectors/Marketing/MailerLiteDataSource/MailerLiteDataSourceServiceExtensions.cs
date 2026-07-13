using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.MailerLite
{
    /// <summary>
    /// Fluent registration for the MailerLite data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MailerLiteDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the MailerLite driver config (classHandler "MailerLiteDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMailerLite(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMailerLiteConfig);
    }
}
