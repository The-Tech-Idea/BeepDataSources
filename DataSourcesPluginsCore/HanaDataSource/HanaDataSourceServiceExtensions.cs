using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Hana
{
    /// <summary>
    /// Fluent registration for the SAP HANA data source driver.
    /// Only visible to hosts that reference this HanaDataSource project / NuGet.
    /// </summary>
    public static class HanaDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the HANA driver config (DataSourceType.Hana,
        /// classHandler "HanaDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddHanaDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateHanaConfig);
    }
}
