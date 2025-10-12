using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Connectors.PipedriveDataSource
{
    using TheTechIdea.Beep.Connectors.Pipedrive.Models;
    /// <summary>
    /// Pipedrive CRM Data Source implementation using Pipedrive REST API
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pipedrive)]
    public class PipedriveDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Pipedrive API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["persons"] = "persons",
            ["organizations"] = "organizations", 
            ["deals"] = "deals",
            ["activities"] = "activities",
            ["users"] = "users",
            ["pipelines"] = "pipelines",
            ["stages"] = "stages",
            ["products"] = "products"
        };

        #region Configuration Classes

        /// <summary>
        /// Configuration for Pipedrive connection
        /// </summary>
        public class PipedriveConfig
        {
            public string ApiToken { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://api.pipedrive.com/v1";
        }

        /// <summary>
        /// Pipedrive entity metadata
        /// </summary>
        public class PipedriveEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        #region Private Fields

        private readonly PipedriveConfig _config;
        private HttpClient? _httpClient;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, PipedriveEntity> _entityCache = new();

        #endregion

        #region Constructor

        public PipedriveDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new PipedriveConfig();

            // Initialize connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();

            // Initialize HTTP client
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeepDataConnector/1.0");
        }

        #endregion

        #region IDataSource Implementation

        public async Task<bool> ConnectAsync()
        {
            try
            {
                Logger.WriteLog($"Connecting to Pipedrive: {_config.BaseUrl}");

                // Set authorization header with API token
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiToken);

                // Test connection by getting users
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/users");
                if (testResponse.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger.WriteLog("Successfully connected to Pipedrive");
                    return true;
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection test failed: {testResponse.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection failed: {ex.Message}";
                Logger.WriteLog($"Pipedrive connection error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                ConnectionStatus = ConnectionState.Closed;
                _entityCache.Clear();
                Logger.WriteLog("Disconnected from Pipedrive");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Disconnect failed: {ex.Message}";
                return Task.FromResult(false);
            }
        }

        public async Task<bool> OpenconnectionAsync()
        {
            return await ConnectAsync();
        }

        public async Task<bool> CloseconnectionAsync()
        {
            return await DisconnectAsync();
        }

        public Task<List<string>> GetEntitiesNamesAsync()
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                // Get available entities from Pipedrive
                var entities = GetPipedriveEntitiesAsync();
                EntitiesNames = entities.Select(e => e.EntityName).ToList();
                return Task.FromResult(EntitiesNames);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entities: {ex.Message}";
                return Task.FromResult(new List<string>());
            }
        }

        public Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                if (!refresh && Entities.Any())
                    return Task.FromResult(Entities);

                var pipedriveEntities = GetPipedriveEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in pipedriveEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.EntityName,
                        Caption = entity.DisplayName,
                        Fields = new List<EntityField>()
                    };

                    foreach (var field in entity.Fields)
                    {
                        structure.Fields.Add(new EntityField
                        {
                            fieldname = field.Key,
                            fieldtype = field.Value
                        });
                    }

                    Entities.Add(structure);
                }

                return Task.FromResult(Entities);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entity structures: {ex.Message}";
                return Task.FromResult(new List<EntityStructure>());
            }
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var queryParams = BuildQueryParameters(filter);
                var url = $"{_config.BaseUrl}/{entityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = ParseResponse(content, entityName);
                    return result ?? new List<object>();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get {entityName}: {response.StatusCode} - {errorContent}";
                return new List<object>();
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entity data: {ex.Message}";
                return new List<object>();
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/{entityName}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to insert entity: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_config.BaseUrl}/{entityName}/{entityId}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to update {entityName}: {response.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to update entity: {ex.Message}";
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var response = await _httpClient.DeleteAsync($"{_config.BaseUrl}/{entityName}/{entityId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to delete entity: {ex.Message}";
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return;

            var parameters = connectionString.Split(';');
            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "apitoken":
                            _config.ApiToken = value;
                            break;
                        case "baseurl":
                            _config.BaseUrl = value;
                            break;
                    }
                }
            }
        }

        private List<PipedriveEntity> GetPipedriveEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Pipedrive entities
            var entities = new List<PipedriveEntity>
            {
                new PipedriveEntity
                {
                    EntityName = "persons",
                    DisplayName = "Persons",
                    ApiEndpoint = "persons",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["first_name"] = "String",
                        ["last_name"] = "String",
                        ["email"] = "String",
                        ["phone"] = "String",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime",
                        ["org_id"] = "Int32"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "organizations",
                    DisplayName = "Organizations",
                    ApiEndpoint = "organizations",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["address"] = "String",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime",
                        ["owner_id"] = "Int32"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "deals",
                    DisplayName = "Deals",
                    ApiEndpoint = "deals",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["title"] = "String",
                        ["value"] = "Decimal",
                        ["currency"] = "String",
                        ["status"] = "String",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime",
                        ["org_id"] = "Int32",
                        ["person_id"] = "Int32"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["title"] = "String",
                        ["person_id"] = "Int32",
                        ["organization_id"] = "Int32",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime"
                    }
                }
            };

            foreach (var entity in entities)
            {
                _entityCache[entity.EntityName] = entity;
            }

            return entities;
        }

        private string BuildQueryParameters(List<AppFilter>? filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var queryParts = new List<string>();

            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FilterValue))
                {
                    queryParts.Add($"{filter.FieldName}={Uri.EscapeDataString(filter.FilterValue)}");
                }
            }

            return queryParts.Any() ? $"?{string.Join("&", queryParts)}" : string.Empty;
        }

        #endregion

        #region Response Parsing

        private IEnumerable<object>? ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName switch
                {
                    "deals" => ExtractArray<Deal>(jsonContent),
                    "persons" => ExtractArray<Person>(jsonContent),
                    "organizations" => ExtractArray<Organization>(jsonContent),
                    "activities" => ExtractArray<Activity>(jsonContent),
                    "users" => ExtractArray<User>(jsonContent),
                    "pipelines" => ExtractArray<Pipeline>(jsonContent),
                    "stages" => ExtractArray<Stage>(jsonContent),
                    "products" => ExtractArray<Product>(jsonContent),
                    _ => JsonSerializer.Deserialize<PipedriveResponse<object>>(jsonContent)?.Data ?? new List<object>()
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return JsonSerializer.Deserialize<PipedriveResponse<object>>(jsonContent)?.Data ?? new List<object>();
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : PipedriveEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<PipedriveResponse<T>>(jsonContent, options);
                if (apiResponse?.Data != null)
                {
                    foreach (var item in apiResponse.Data)
                    {
                        item.Attach<T>((IDataSource)this);
                    }
                }
                return apiResponse?.Data ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region Command Methods

        public bool CreateEntityAsAsync(string entityname, object entitydata)
        {
            return Task.Run(() => InsertEntityAsync(entityname, entitydata)).GetAwaiter().GetResult();
        }

        public bool UpdateEntity(string entityname, object entitydata, string entityid)
        {
            return Task.Run(() => UpdateEntityAsync(entityname, entitydata, entityid)).GetAwaiter().GetResult();
        }

        public bool DeleteEntity(string entityname, string entityid)
        {
            return Task.Run(() => DeleteEntityAsync(entityname, entityid)).GetAwaiter().GetResult();
        }

        public new object? GetEntity(string entityname, List<AppFilter> filter)
        {
            return Task.Run(() => GetEntityAsync(entityname, filter)).GetAwaiter().GetResult();
        }

        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            return Task.Run(() => GetEntityStructuresAsync(refresh)).GetAwaiter().GetResult();
        }

        public new List<string> GetEntitesList()
        {
            return Task.Run(() => GetEntitiesNamesAsync()).GetAwaiter().GetResult();
        }

        public new bool Openconnection()
        {
            return Task.Run(() => OpenconnectionAsync()).GetAwaiter().GetResult();
        }

        public new bool Closeconnection()
        {
            return Task.Run(() => CloseconnectionAsync()).GetAwaiter().GetResult();
        }

        public bool CreateEntityAs(string entityname, object entitydata)
        {
            return CreateEntityAsAsync(entityname, entitydata);
        }

        public new IErrorsInfo RunQuery(string qrystr)
        {
            // Pipedrive doesn't support arbitrary SQL queries
            // This would need to be implemented using Pipedrive's filter API
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunQuery not supported. Use GetEntity with filters instead.";
            return ErrorObject;
        }

        public new IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunScript not supported for Pipedrive";
            return ErrorObject;
        }

        public new void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _entityCache.Clear();
        }

        [CommandAttribute(
            Name = "GetDeals",
            Caption = "Get Pipedrive Deals",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Deal",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Deal>"
        )]
        public async Task<IEnumerable<Deal>> GetDeals(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("deals", filters ?? new List<AppFilter>());
            return result.Cast<Deal>().Select(d => d.Attach<Deal>(this));
        }

        [CommandAttribute(
            Name = "GetPersons",
            Caption = "Get Pipedrive Persons",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Person",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Person>"
        )]
        public async Task<IEnumerable<Person>> GetPersons(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("persons", filters ?? new List<AppFilter>());
            return result.Cast<Person>().Select(p => p.Attach<Person>(this));
        }

        [CommandAttribute(
            Name = "GetOrganizations",
            Caption = "Get Pipedrive Organizations",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Organization",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Organization>"
        )]
        public async Task<IEnumerable<Organization>> GetOrganizations(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("organizations", filters ?? new List<AppFilter>());
            return result.Cast<Organization>().Select(o => o.Attach<Organization>(this));
        }

        [CommandAttribute(
            Name = "GetActivities",
            Caption = "Get Pipedrive Activities",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Activity",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Activity>"
        )]
        public async Task<IEnumerable<Activity>> GetActivities(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("activities", filters ?? new List<AppFilter>());
            return result.Cast<Activity>().Select(a => a.Attach<Activity>(this));
        }

        [CommandAttribute(
            Name = "GetUsers",
            Caption = "Get Pipedrive Users",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "User",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<User>"
        )]
        public async Task<IEnumerable<User>> GetUsers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("users", filters ?? new List<AppFilter>());
            return result.Cast<User>().Select(u => u.Attach<User>(this));
        }

        [CommandAttribute(
            Name = "GetPipelines",
            Caption = "Get Pipedrive Pipelines",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Pipeline",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Pipeline>"
        )]
        public async Task<IEnumerable<Pipeline>> GetPipelines(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("pipelines", filters ?? new List<AppFilter>());
            return result.Cast<Pipeline>().Select(p => p.Attach<Pipeline>(this));
        }

        [CommandAttribute(
            Name = "GetStages",
            Caption = "Get Pipedrive Stages",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Stage",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Stage>"
        )]
        public async Task<IEnumerable<Stage>> GetStages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("stages", filters ?? new List<AppFilter>());
            return result.Cast<Stage>().Select(s => s.Attach<Stage>(this));
        }

        [CommandAttribute(
            Name = "GetProducts",
            Caption = "Get Pipedrive Products",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Pipedrive,
            PointType = EnumPointType.Function,
            ObjectType = "Product",
            ClassType = "PipedriveDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "pipedrive.png",
            misc = "ReturnType: IEnumerable<Product>"
        )]
        public async Task<IEnumerable<Product>> GetProducts(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("products", filters ?? new List<AppFilter>());
            return result.Cast<Product>().Select(p => p.Attach<Product>(this));
        }

        #endregion
    }
}
