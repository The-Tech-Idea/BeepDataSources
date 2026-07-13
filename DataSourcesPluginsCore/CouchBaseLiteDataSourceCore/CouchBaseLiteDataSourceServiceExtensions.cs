using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.CouchbaseLite
{
    /// <summary>
    /// Fluent registration for the CouchbaseLite data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class CouchBaseLiteDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the CouchbaseLite driver config (classHandler "CouchBaseLiteDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddCouchbaseLite(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateCouchbaseLiteConfig);
    }
}
