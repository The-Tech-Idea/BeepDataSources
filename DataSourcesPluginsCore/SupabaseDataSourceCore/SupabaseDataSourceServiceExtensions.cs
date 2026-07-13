using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Supabase
{
    /// <summary>
    /// Fluent registration for the Supabase data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SupabaseDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Supabase driver config (classHandler "SupabaseDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSupabase(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSupabaseConfig);
    }
}
