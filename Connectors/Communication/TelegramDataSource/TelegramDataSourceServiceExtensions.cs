using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Telegram
{
    /// <summary>
    /// Fluent registration for the Telegram data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class TelegramDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Telegram driver config (classHandler "TelegramDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddTelegram(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateTelegramConfig);
    }
}
