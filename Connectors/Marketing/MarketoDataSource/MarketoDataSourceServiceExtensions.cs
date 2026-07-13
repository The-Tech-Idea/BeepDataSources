using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Marketo
{
    /// <summary>
    /// Fluent registration for the Marketo data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MarketoDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Marketo driver config (classHandler "MarketoDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMarketo(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMarketoConfig);
    }
}
