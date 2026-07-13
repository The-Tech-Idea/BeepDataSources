using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Redis
{
    /// <summary>
    /// Fluent registration for the Redis data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class RedisDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Redis driver config (classHandler "RedisDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddRedis(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateRedisConfig);
    }
}
