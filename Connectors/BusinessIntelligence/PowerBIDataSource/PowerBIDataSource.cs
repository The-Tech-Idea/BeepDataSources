using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Connectors.BusinessIntelligence.PowerBI.Models;

namespace TheTechIdea.Beep.Connectors.BusinessIntelligence.PowerBI
{
    /// <summary>
    /// Power BI data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI)]
    public class PowerBIDataSource : WebAPIDataSource
    {
        private const string BASE_URL = "https://api.powerbi.com/v1.0/myorg";
        private string _accessToken;

        public PowerBIDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            DataSourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = DMEEditor;
            DatasourceType = DataSourceType.PowerBI;
            Category = DatasourceCategory.Connector;

            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }
            else
            {
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.Where(c => c.ConnectionName == datasourcename).FirstOrDefault();
            }

            // Register entities
            EntitiesNames = new List<string>
            {
                "Workspaces",
                "Datasets",
                "Reports",
                "Dashboards",
                "Dataflows",
                "Apps",
                "Tables",
                "ActivityEvents",
                "Gateways"
            };
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Authentication", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: bool")]
        public override async Task<bool> Authenticate()
        {
            try
            {
                Logger?.LogInfo($"Authenticating with Power BI for {DataSourceName}");

                if (string.IsNullOrEmpty(_accessToken))
                {
                    Logger?.LogError("Access token is required for Power BI authentication");
                    return false;
                }

                // Test authentication by getting user profile
                var userProfile = await GetUserProfile();
                if (userProfile != null)
                {
                    Logger?.LogInfo("Power BI authentication successful");
                    return true;
                }
                else
                {
                    Logger?.LogError("Power BI authentication failed - could not retrieve user profile");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Power BI authentication error: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Workspaces", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIWorkspace>")]
        public async Task<List<PowerBIWorkspace>> GetWorkspaces()
        {
            try
            {
                Logger?.LogInfo("Getting Power BI workspaces");

                var response = await GetAsync($"{BASE_URL}/groups");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIWorkspace>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} workspaces");
                    return apiResponse?.Value ?? new List<PowerBIWorkspace>();
                }
                else
                {
                    Logger?.LogError($"Failed to get workspaces: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIWorkspace>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting workspaces: {ex.Message}");
                return new List<PowerBIWorkspace>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Datasets", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIDataset>")]
        public async Task<List<PowerBIDataset>> GetDatasets(string workspaceId = null)
        {
            try
            {
                Logger?.LogInfo($"Getting Power BI datasets{(workspaceId != null ? $" for workspace {workspaceId}" : "")}");

                string url = workspaceId != null
                    ? $"{BASE_URL}/groups/{workspaceId}/datasets"
                    : $"{BASE_URL}/datasets";

                var response = await GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDataset>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} datasets");
                    return apiResponse?.Value ?? new List<PowerBIDataset>();
                }
                else
                {
                    Logger?.LogError($"Failed to get datasets: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIDataset>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting datasets: {ex.Message}");
                return new List<PowerBIDataset>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Reports", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIReport>")]
        public async Task<List<PowerBIReport>> GetReports(string workspaceId = null)
        {
            try
            {
                Logger?.LogInfo($"Getting Power BI reports{(workspaceId != null ? $" for workspace {workspaceId}" : "")}");

                string url = workspaceId != null
                    ? $"{BASE_URL}/groups/{workspaceId}/reports"
                    : $"{BASE_URL}/reports";

                var response = await GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIReport>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} reports");
                    return apiResponse?.Value ?? new List<PowerBIReport>();
                }
                else
                {
                    Logger?.LogError($"Failed to get reports: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIReport>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting reports: {ex.Message}");
                return new List<PowerBIReport>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Dashboards", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIDashboard>")]
        public async Task<List<PowerBIDashboard>> GetDashboards(string workspaceId = null)
        {
            try
            {
                Logger?.LogInfo($"Getting Power BI dashboards{(workspaceId != null ? $" for workspace {workspaceId}" : "")}");

                string url = workspaceId != null
                    ? $"{BASE_URL}/groups/{workspaceId}/dashboards"
                    : $"{BASE_URL}/dashboards";

                var response = await GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDashboard>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} dashboards");
                    return apiResponse?.Value ?? new List<PowerBIDashboard>();
                }
                else
                {
                    Logger?.LogError($"Failed to get dashboards: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIDashboard>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting dashboards: {ex.Message}");
                return new List<PowerBIDashboard>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Dataflows", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIDataflow>")]
        public async Task<List<PowerBIDataflow>> GetDataflows(string workspaceId)
        {
            try
            {
                Logger?.LogInfo($"Getting Power BI dataflows for workspace {workspaceId}");

                var response = await GetAsync($"{BASE_URL}/groups/{workspaceId}/dataflows");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDataflow>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} dataflows");
                    return apiResponse?.Value ?? new List<PowerBIDataflow>();
                }
                else
                {
                    Logger?.LogError($"Failed to get dataflows: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIDataflow>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting dataflows: {ex.Message}");
                return new List<PowerBIDataflow>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Apps", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIApp>")]
        public async Task<List<PowerBIApp>> GetApps()
        {
            try
            {
                Logger?.LogInfo("Getting Power BI apps");

                var response = await GetAsync($"{BASE_URL}/apps");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIApp>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} apps");
                    return apiResponse?.Value ?? new List<PowerBIApp>();
                }
                else
                {
                    Logger?.LogError($"Failed to get apps: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIApp>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting apps: {ex.Message}");
                return new List<PowerBIApp>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Tables", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBITable>")]
        public async Task<List<PowerBITable>> GetDatasetTables(string workspaceId, string datasetId)
        {
            try
            {
                Logger?.LogInfo($"Getting tables for dataset {datasetId} in workspace {workspaceId}");

                var response = await GetAsync($"{BASE_URL}/groups/{workspaceId}/datasets/{datasetId}/tables");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBITable>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} tables");
                    return apiResponse?.Value ?? new List<PowerBITable>();
                }
                else
                {
                    Logger?.LogError($"Failed to get dataset tables: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBITable>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting dataset tables: {ex.Message}");
                return new List<PowerBITable>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "RefreshHistory", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIRefreshHistory>")]
        public async Task<List<PowerBIRefreshHistory>> GetDatasetRefreshHistory(string workspaceId, string datasetId, int top = 10)
        {
            try
            {
                Logger?.LogInfo($"Getting refresh history for dataset {datasetId} in workspace {workspaceId}");

                var response = await GetAsync($"{BASE_URL}/groups/{workspaceId}/datasets/{datasetId}/refreshes?$top={top}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIRefreshHistory>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} refresh history records");
                    return apiResponse?.Value ?? new List<PowerBIRefreshHistory>();
                }
                else
                {
                    Logger?.LogError($"Failed to get refresh history: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIRefreshHistory>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting refresh history: {ex.Message}");
                return new List<PowerBIRefreshHistory>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Dataset", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: bool")]
        public async Task<bool> RefreshDataset(string workspaceId, string datasetId)
        {
            try
            {
                Logger?.LogInfo($"Refreshing dataset {datasetId} in workspace {workspaceId}");

                var response = await PostAsync($"{BASE_URL}/groups/{workspaceId}/datasets/{datasetId}/refreshes", null);
                if (response.IsSuccessStatusCode)
                {
                    Logger?.LogInfo("Dataset refresh initiated successfully");
                    return true;
                }
                else
                {
                    Logger?.LogError($"Failed to refresh dataset: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error refreshing dataset: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "ActivityEvents", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIActivityEvent>")]
        public async Task<List<PowerBIActivityEvent>> GetActivityEvents(DateTime startDateTime, DateTime endDateTime, int top = 100)
        {
            try
            {
                Logger?.LogInfo($"Getting activity events from {startDateTime} to {endDateTime}");

                string startDate = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                string endDate = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                var response = await GetAsync($"{BASE_URL}/admin/activityevents?startDateTime='{startDate}'&endDateTime='{endDate}'&$top={top}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIActivityEvent>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} activity events");
                    return apiResponse?.Value ?? new List<PowerBIActivityEvent>();
                }
                else
                {
                    Logger?.LogError($"Failed to get activity events: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIActivityEvent>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting activity events: {ex.Message}");
                return new List<PowerBIActivityEvent>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.PowerBI, PointType = EnumPointType.Function, ObjectType = "Gateways", ClassName = "PowerBIDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<PowerBIGateway>")]
        public async Task<List<PowerBIGateway>> GetGateways()
        {
            try
            {
                Logger?.LogInfo("Getting Power BI gateways");

                var response = await GetAsync($"{BASE_URL}/gateways");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIGateway>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Logger?.LogInfo($"Retrieved {apiResponse?.Value?.Count ?? 0} gateways");
                    return apiResponse?.Value ?? new List<PowerBIGateway>();
                }
                else
                {
                    Logger?.LogError($"Failed to get gateways: {response.StatusCode} - {response.ReasonPhrase}");
                    return new List<PowerBIGateway>();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error getting gateways: {ex.Message}");
                return new List<PowerBIGateway>();
            }
        }

        private async Task<dynamic> GetUserProfile()
        {
            try
            {
                var response = await GetAsync($"{BASE_URL}/users/me");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<dynamic>(content);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public override ConnectionProperties GetConnectionProperties()
        {
            return Dataconnection.ConnectionProp;
        }

        public override string GetConnectionString()
        {
            return $"PowerBI;AccessToken={_accessToken}";
        }

        public override IErrorsInfo UpdateData()
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo UpdateEntities(string entityname, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            throw new NotImplementedException();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                string endpoint = GetEndpointForEntity(EntityName);
                if (string.IsNullOrEmpty(endpoint))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown Power BI entity: {EntityName}";
                    return Array.Empty<object>();
                }

                string url = $"{BASE_URL}{endpoint}";
                var response = await GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return ProcessApiResponse(EntityName, json);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Power BI API error: {response?.StatusCode.ToString() ?? "Unknown error"}";
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

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName switch
            {
                "Workspaces" => "/groups",
                "Datasets" => "/datasets",
                "Reports" => "/reports",
                "Dashboards" => "/dashboards",
                "Dataflows" => "/dataflows",
                "Apps" => "/apps",
                "Tables" => "/datasets", // Tables require workspace and dataset IDs
                "ActivityEvents" => "/admin/activityevents",
                "Gateways" => "/gateways",
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
                
                // Power BI API returns data in a "value" array
                return entityName switch
                {
                    "Workspaces" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIWorkspace>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Datasets" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDataset>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Reports" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIReport>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Dashboards" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDashboard>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Dataflows" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIDataflow>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Apps" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIApp>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Tables" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBITable>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "ActivityEvents" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIActivityEvent>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    "Gateways" => JsonSerializer.Deserialize<PowerBIApiResponse<PowerBIGateway>>(jsonResponse, options)?.Value?.Cast<object>() ?? Array.Empty<object>(),
                    _ => Array.Empty<object>()
                };
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        public override List<EntityStructure> GetEntityStructure(string EntityName, bool refresh = false)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo CreateEntityAs(EntityStructure entity)
        {
            throw new NotImplementedException();
        }

        public override List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo RunQuery(string qrystr)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo BeginTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo EndTransaction(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public override IErrorsInfo Commit(PassedArgs args)
        {
            throw new NotImplementedException();
        }

        public override List<ChildRelation> GetChildTablesList(string tablename, string SchemaName)
        {
            throw new NotImplementedException();
        }

        public override List<string> GetDatabaseList()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetSchemaList()
        {
            throw new NotImplementedException();
        }

        public override bool IfConnectionExist()
        {
            return !string.IsNullOrEmpty(_accessToken);
        }

        public override bool CreateConnection()
        {
            try
            {
                if (Dataconnection.ConnectionProp != null)
                {
                    _accessToken = Dataconnection.ConnectionProp.Password;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating Power BI connection: {ex.Message}");
                return false;
            }
        }

        public override bool Closeconnection()
        {
            _accessToken = null;
            return true;
        }

        protected override HttpClient CreateHttpClient()
        {
            var client = base.CreateHttpClient();
            if (!string.IsNullOrEmpty(_accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            return client;
        }
    }
}