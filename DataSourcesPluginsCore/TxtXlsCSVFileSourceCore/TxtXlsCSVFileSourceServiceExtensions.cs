using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.TxtXlsCSVFileSource
{
    /// <summary>
    /// Fluent registration for the TxtXlsCSVFileSource data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class TxtXlsCSVFileSourceServiceExtensions
    {
        /// <summary>
        /// Registers the TxtXlsCSVFileSource driver config (classHandler "TxtXlsCSVFileSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddTxtXlsCSVFileSource(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateTxtXlsCSVFileSourceConfig);
    }
}
