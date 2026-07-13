using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Firebird
{
    /// <summary>
    /// Fluent registration for the Firebird data source driver.
    /// Only visible to hosts that reference this FirebirdDataSourceCore project / NuGet.
    /// </summary>
    public static class FirebirdDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Firebird driver config (DataSourceType.FireBird,
        /// classHandler "FireBirdDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddFirebirdDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateFirebirdConfig);
    }
}
