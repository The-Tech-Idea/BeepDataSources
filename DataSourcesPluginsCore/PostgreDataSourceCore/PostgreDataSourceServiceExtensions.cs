using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataBase.Postgre
{
    /// <summary>
    /// Fluent registration for the PostgreSQL data source driver.
    /// Only visible to hosts that reference this PostgreDataSourceCore project / NuGet.
    /// </summary>
    public static class PostgreDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the PostgreSQL driver config (DataSourceType.Postgre,
        /// classHandler "PostgreDataSource") with ConfigEditor.DataDriversClasses.
        /// Idempotent — deduped by classHandler.
        /// </summary>
        public static IBeepService AddPostgreDatabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreatePostgreConfig);
    }
}
