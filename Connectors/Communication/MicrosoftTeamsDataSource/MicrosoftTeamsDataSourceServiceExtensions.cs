using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.MicrosoftTeams
{
    /// <summary>
    /// Fluent registration for the MicrosoftTeams data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MicrosoftTeamsDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the MicrosoftTeams driver config (classHandler "MicrosoftTeamsDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMicrosoftTeams(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMicrosoftTeamsConfig);
    }
}
