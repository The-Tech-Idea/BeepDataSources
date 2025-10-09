using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Tableau.Models;

namespace TheTechIdea.Beep.Connectors.Tableau
{
    /// <summary>
    /// Tableau data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau)]
    public class TableauDataSource : WebAPIDataSource
    {
        /// <summary>
        /// Initializes a new instance of the TableauDataSource class
        /// </summary>
        public TableauDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            DatasourceType = DataSourceType.Tableau;
            Category = DatasourceCategory.Connector;

            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }
        }

        /// <summary>
        /// Asynchronously retrieves an entity based on the provided name and filters.
        /// </summary>
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                string endpoint = GetEndpointForEntity(EntityName);
                if (string.IsNullOrEmpty(endpoint))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown Tableau entity: {EntityName}";
                    return Array.Empty<object>();
                }

                // Build the API URL - Tableau uses different base URLs depending on the operation
                string baseUrl = "https://YOUR_TABLEAU_SERVER/api/3.21"; // This should be configured
                string url = $"{baseUrl}{endpoint}";

                // Make the request using base class method (handles authentication automatically)
                var response = await GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Process the response based on entity type
                    return ProcessApiResponse(EntityName, json);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Tableau API error: {response?.StatusCode.ToString() ?? "Unknown error"}";
                    return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return Array.Empty<object>();
            }
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "sites" => "/sites",
                "users" => "/users",
                "groups" => "/groups",
                "projects" => "/projects",
                "workbooks" => "/workbooks",
                "views" => "/views",
                "datasources" => "/datasources",
                "flows" => "/flows",
                _ => null
            };
        }

        private IEnumerable<object> ProcessApiResponse(string entityName, string jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return Array.Empty<object>();

            try
            {
                // For now, return empty lists - detailed API parsing can be implemented later
                return entityName.ToLower() switch
                {
                    "sites" => new List<TableauSite>(),
                    "users" => new List<TableauUser>(),
                    "groups" => new List<TableauGroup>(),
                    "projects" => new List<TableauProject>(),
                    "workbooks" => new List<TableauWorkbook>(),
                    "views" => new List<TableauView>(),
                    "datasources" => new List<TableauDataSource>(),
                    _ => Array.Empty<object>()
                };
            }
            catch
            {
                // If deserialization fails, return empty
                return Array.Empty<object>();
            }
        }
    }
}