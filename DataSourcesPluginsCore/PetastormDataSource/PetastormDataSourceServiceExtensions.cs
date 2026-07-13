using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Petastorm
{
    /// <summary>
    /// Fluent registration for the Petastorm data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class PetastormDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Petastorm driver config (classHandler "PetastormDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddPetastorm(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreatePetastormConfig);
    }
}
