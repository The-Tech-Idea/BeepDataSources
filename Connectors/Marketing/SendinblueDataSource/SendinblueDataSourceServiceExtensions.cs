using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Sendinblue
{
    /// <summary>
    /// Fluent registration for the Sendinblue data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SendinblueDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Sendinblue driver config (classHandler "SendinblueDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSendinblue(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSendinblueConfig);
    }
}
