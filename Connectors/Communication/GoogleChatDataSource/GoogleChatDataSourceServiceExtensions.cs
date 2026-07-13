using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.GoogleChat
{
    /// <summary>
    /// Fluent registration for the GoogleChat data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class GoogleChatDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the GoogleChat driver config (classHandler "GoogleChatDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddGoogleChat(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateGoogleChatConfig);
    }
}
