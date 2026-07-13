using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Services;

namespace TheTechIdea.Beep.DataSources.Salesforce
{
    /// <summary>
    /// Fluent registration for the Salesforce data source driver.
    /// Only visible to hosts that reference this project / NuGet.
    /// </summary>
    public static class SalesforceDataSourceServiceExtensions
    {
        /// <summary>
        /// Registers the Salesforce driver config (classHandler "SalesforceDataSource")
        /// with ConfigEditor.DataDriversClasses. Idempotent - deduped by classHandler.
        /// </summary>
        public static IBeepService AddSalesforce(this IBeepService beep)
            => beep.AddDataSourceDriver(ConnectionHelper.CreateSalesforceConfig);
    }
}
