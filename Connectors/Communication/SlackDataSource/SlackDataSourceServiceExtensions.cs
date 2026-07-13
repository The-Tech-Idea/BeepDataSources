using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Slack
{
    /// <summary>
    /// Fluent registration for the Slack data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SlackDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Slack driver config (classHandler "SlackDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSlack(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSlackConfig);
    }
}
