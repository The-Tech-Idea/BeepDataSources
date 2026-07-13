using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.OpenCart
{
    /// <summary>
    /// Fluent registration for the OpenCart data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class OpenCartDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the OpenCart driver config (classHandler "OpenCartDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddOpenCart(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateOpenCartConfig);
    }
}
