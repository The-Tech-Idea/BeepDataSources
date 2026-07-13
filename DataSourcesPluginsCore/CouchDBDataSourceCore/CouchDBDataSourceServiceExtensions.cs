using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.CouchDB
{
    /// <summary>
    /// Fluent registration for the CouchDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class CouchDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the CouchDB driver config (classHandler "CouchDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddCouchDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateCouchDBConfig);
    }
}
