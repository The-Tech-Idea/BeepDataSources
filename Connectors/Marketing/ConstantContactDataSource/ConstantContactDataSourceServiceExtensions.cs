using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.ConstantContact
{
    /// <summary>
    /// Fluent registration for the ConstantContact data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class ConstantContactDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the ConstantContact driver config (classHandler "ConstantContactDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddConstantContact(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateConstantContactConfig);
    }
}
