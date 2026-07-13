using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Freshsales
{
    /// <summary>
    /// Fluent registration for the Freshsales data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class FreshsalesDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Freshsales driver config (classHandler "FreshsalesDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddFreshsales(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateFreshsalesConfig);
    }
}
