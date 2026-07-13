using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Discord
{
    /// <summary>
    /// Fluent registration for the Discord data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class DiscordDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Discord driver config (classHandler "DiscordDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddDiscord(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateDiscordConfig);
    }
}
