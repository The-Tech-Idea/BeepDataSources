using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Vertica
{
    /// <summary>
    /// Fluent registration for the Vertica data source driver.
    /// Only visible to hosts that reference this VerticaDataSource project / NuGet.
    /// </summary>
    public static class VerticaDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Vertica driver config (DataSourceType.Vertica,
        /// classHandler "VerticaDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddVerticaDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateVerticaConfig);
    }
}
