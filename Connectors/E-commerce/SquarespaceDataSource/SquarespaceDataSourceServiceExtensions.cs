using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Squarespace
{
    /// <summary>
    /// Fluent registration for the Squarespace data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SquarespaceDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Squarespace driver config (classHandler "SquarespaceDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSquarespace(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSquarespaceConfig);
    }
}
