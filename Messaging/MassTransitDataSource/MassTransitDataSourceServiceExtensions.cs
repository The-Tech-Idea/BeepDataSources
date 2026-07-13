using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.MassTransit
{
    /// <summary>
    /// Fluent registration for the MassTransit data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MassTransitDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the MassTransit driver config (classHandler "MassTransitDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMassTransit(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMassTransitConfig);
    }
}
