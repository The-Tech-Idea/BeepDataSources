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

            // Register entities
            EntitiesNames = new List<string> { "sites", "users", "groups", "projects", "workbooks", "views", "datasources" };
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
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
                _ => null
            };
        }

        private IEnumerable<object> ProcessApiResponse(string entityName, string jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return Array.Empty<object>();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return entityName.ToLower() switch
                {
                    "sites" => new List<TableauSite> { JsonSerializer.Deserialize<TableauSite>(jsonResponse, options) ?? new TableauSite() },
                    "users" => JsonSerializer.Deserialize<TableauUsers>(jsonResponse, options)?.User ?? new List<TableauUser>(),
                    "groups" => JsonSerializer.Deserialize<TableauGroups>(jsonResponse, options)?.Group ?? new List<TableauGroup>(),
                    "projects" => JsonSerializer.Deserialize<TableauProjects>(jsonResponse, options)?.Project ?? new List<TableauProject>(),
                    "workbooks" => JsonSerializer.Deserialize<TableauWorkbooks>(jsonResponse, options)?.Workbook ?? new List<TableauWorkbook>(),
                    "views" => JsonSerializer.Deserialize<TableauViews>(jsonResponse, options)?.View ?? new List<TableauView>(),
                    "datasources" => JsonSerializer.Deserialize<TableauDatasources>(jsonResponse, options)?.Datasource != null ? JsonSerializer.Deserialize<TableauDatasources>(jsonResponse, options).Datasource : new List<Models.TableauDataSource>(),
                    _ => Array.Empty<object>()
                };
            }
            catch
            {
                // If deserialization fails, return empty
                return Array.Empty<object>();
            }
        }

        // CommandAttribute methods for framework integration
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Sites", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauSite>")]
        public IEnumerable<TableauSite> GetSites(List<AppFilter> filter) => GetEntity("sites", filter).Cast<TableauSite>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Users", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauUser>")]
        public IEnumerable<TableauUser> GetUsers(List<AppFilter> filter) => GetEntity("users", filter).Cast<TableauUser>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Groups", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauGroup>")]
        public IEnumerable<TableauGroup> GetGroups(List<AppFilter> filter) => GetEntity("groups", filter).Cast<TableauGroup>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Projects", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauProject>")]
        public IEnumerable<TableauProject> GetProjects(List<AppFilter> filter) => GetEntity("projects", filter).Cast<TableauProject>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Workbooks", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauWorkbook>")]
        public IEnumerable<TableauWorkbook> GetWorkbooks(List<AppFilter> filter) => GetEntity("workbooks", filter).Cast<TableauWorkbook>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "Views", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauView>")]
        public IEnumerable<TableauView> GetViews(List<AppFilter> filter) => GetEntity("views", filter).Cast<TableauView>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Tableau, PointType = EnumPointType.Function, ObjectType = "DataSources", ClassName = "TableauDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<TableauDataSource>")]
        public IEnumerable<TableauDataSource> GetDataSources(List<AppFilter> filter) => GetEntity("datasources", filter).Cast<TableauDataSource>();
    }
}