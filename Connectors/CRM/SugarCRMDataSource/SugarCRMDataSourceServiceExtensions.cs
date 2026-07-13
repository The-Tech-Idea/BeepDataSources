using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.SugarCRM
{
    /// <summary>
    /// Fluent registration for the SugarCRM data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SugarCRMDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the SugarCRM driver config (classHandler "SugarCRMDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSugarCRM(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSugarCRMConfig);
    }
}
