using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.RocketChat
{
    /// <summary>
    /// Fluent registration for the RocketChat data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class RocketChatDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the RocketChat driver config (classHandler "RocketChatDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddRocketChat(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateRocketChatConfig);
    }
}
