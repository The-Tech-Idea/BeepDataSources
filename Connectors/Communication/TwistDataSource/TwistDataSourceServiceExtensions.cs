using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Twist
{
    /// <summary>
    /// Fluent registration for the Twist data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class TwistDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Twist driver config (classHandler "TwistDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddTwist(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateTwistConfig);
    }
}
