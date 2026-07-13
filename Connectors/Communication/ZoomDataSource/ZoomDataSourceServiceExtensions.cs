using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Zoom
{
    /// <summary>
    /// Fluent registration for the Zoom data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ZoomDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Zoom driver config (classHandler "ZoomDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddZoom(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateZoomConfig);
    }
}
