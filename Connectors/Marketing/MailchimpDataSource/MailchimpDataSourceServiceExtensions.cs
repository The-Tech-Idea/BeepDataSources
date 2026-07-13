using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Mailchimp
{
    /// <summary>
    /// Fluent registration for the Mailchimp data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MailchimpDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Mailchimp driver config (classHandler "MailchimpDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMailchimp(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMailchimpConfig);
    }
}
