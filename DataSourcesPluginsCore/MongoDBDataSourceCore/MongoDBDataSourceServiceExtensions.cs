using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.MongoDB
{
    /// <summary>
    /// Fluent registration for the MongoDB data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class MongoDBDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the MongoDB driver config (classHandler "MongoDBDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddMongoDB(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateMongoDBConfig);
    }
}
