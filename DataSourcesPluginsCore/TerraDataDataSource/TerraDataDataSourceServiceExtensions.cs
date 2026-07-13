using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.TerraData
{
    /// <summary>
    /// Fluent registration for the TerraData data source driver.
    /// Only visible to hosts that reference this TerraDataDataSource project / NuGet.
    /// </summary>
    public static class TerraDataDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the TerraData driver config (DataSourceType.TerraData,
        /// classHandler "TerraDataDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddTerraDataDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateTerraDataConfig);
    }
}
