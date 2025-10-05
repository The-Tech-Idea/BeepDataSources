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
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Connectors.ZohoDataSource
{
    /// <summary>
    /// Zoho CRM Data Source implementation using Zoho CRM REST API
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho)]
    public class ZohoDataSource : WebAPIDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Zoho connection
        /// </summary>
        public class ZohoConfig
        {
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://www.zohoapis.com/crm/v2";
            public string Domain { get; set; } = "com"; // com, eu, in, au, jp, cn
        }

        /// <summary>
        /// Zoho entity metadata
        /// </summary>
        public class ZohoEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        #region Private Fields

        private readonly ZohoConfig _config;
        private HttpClient? _httpClient;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, ZohoEntity> _entityCache = new();
        private DateTime _tokenExpiry = DateTime.MinValue;

        #endregion

        #region Constructor

        public ZohoDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new ZohoConfig();

            // Initialize connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

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
                Logger.WriteLog($"Connecting to Zoho CRM: {_config.BaseUrl}");

                // Refresh access token if needed
                if (string.IsNullOrEmpty(_config.AccessToken) || DateTime.Now >= _tokenExpiry)
                {
                    if (!await RefreshAccessTokenAsync())
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = "Failed to obtain access token";
                        return false;
                    }
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", _config.AccessToken);

                // Test connection by getting users
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/users?type=CurrentUser");
                if (testResponse.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger.WriteLog("Successfully connected to Zoho CRM");
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
                Logger.WriteLog($"Zoho CRM connection error: {ex.Message}");
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
                _config.AccessToken = string.Empty;
                Logger.WriteLog("Disconnected from Zoho CRM");
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
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                // Get available entities from Zoho CRM
                var entities = GetZohoEntitiesAsync();
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
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                if (!refresh && Entities.Any())
                    return Task.FromResult(Entities);

                var zohoEntities = GetZohoEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in zohoEntities)
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

        public new async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var queryParams = BuildQueryParameters(filter);
                var url = $"{_config.BaseUrl}/{entityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get {entityName}: {response.StatusCode} - {errorContent}";
                return null;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entity data: {ex.Message}";
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

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
                    throw new InvalidOperationException("Not connected to Zoho CRM");

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
                    throw new InvalidOperationException("Not connected to Zoho CRM");

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

        private async Task<bool> RefreshAccessTokenAsync()
        {
            try
            {
                using var tokenClient = new HttpClient();
                var tokenUrl = $"https://accounts.zoho.{_config.Domain}/oauth/v2/token";

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("refresh_token", _config.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _config.ClientId),
                    new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                });

                var response = await tokenClient.PostAsync(tokenUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _config.AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;

                    // Set token expiry (typically 1 hour)
                    _tokenExpiry = DateTime.Now.AddHours(1);

                    Logger.WriteLog("Successfully refreshed Zoho CRM access token");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Token refresh failed: {response.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Token refresh error: {ex.Message}";
                return false;
            }
        }

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
                        case "clientid":
                            _config.ClientId = value;
                            break;
                        case "clientsecret":
                            _config.ClientSecret = value;
                            break;
                        case "refreshtoken":
                            _config.RefreshToken = value;
                            break;
                        case "domain":
                            _config.Domain = value;
                            break;
                        case "baseurl":
                            _config.BaseUrl = value;
                            break;
                    }
                }
            }
        }

        private List<ZohoEntity> GetZohoEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Zoho CRM entities
            var entities = new List<ZohoEntity>
            {
                new ZohoEntity
                {
                    EntityName = "Leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "Leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["First_Name"] = "String",
                        ["Last_Name"] = "String",
                        ["Email"] = "String",
                        ["Phone"] = "String",
                        ["Company"] = "String",
                        ["Lead_Source"] = "String",
                        ["Lead_Status"] = "String",
                        ["Created_Time"] = "DateTime",
                        ["Modified_Time"] = "DateTime"
                    }
                },
                new ZohoEntity
                {
                    EntityName = "Contacts",
                    DisplayName = "Contacts",
                    ApiEndpoint = "Contacts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["First_Name"] = "String",
                        ["Last_Name"] = "String",
                        ["Email"] = "String",
                        ["Phone"] = "String",
                        ["Mobile"] = "String",
                        ["Account_Name"] = "String",
                        ["Created_Time"] = "DateTime",
                        ["Modified_Time"] = "DateTime"
                    }
                },
                new ZohoEntity
                {
                    EntityName = "Accounts",
                    DisplayName = "Accounts",
                    ApiEndpoint = "Accounts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["Account_Name"] = "String",
                        ["Website"] = "String",
                        ["Phone"] = "String",
                        ["Billing_Street"] = "String",
                        ["Billing_City"] = "String",
                        ["Billing_State"] = "String",
                        ["Billing_Country"] = "String",
                        ["Created_Time"] = "DateTime",
                        ["Modified_Time"] = "DateTime"
                    }
                },
                new ZohoEntity
                {
                    EntityName = "Deals",
                    DisplayName = "Deals",
                    ApiEndpoint = "Deals",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["Deal_Name"] = "String",
                        ["Stage"] = "String",
                        ["Amount"] = "Decimal",
                        ["Closing_Date"] = "DateTime",
                        ["Account_Name"] = "String",
                        ["Contact_Name"] = "String",
                        ["Created_Time"] = "DateTime",
                        ["Modified_Time"] = "DateTime"
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

        #region Standard Interface Methods

        public bool CreateEntityAsAsync(string entityname, object entitydata)
        {
            return Task.Run(() => InsertEntityAsync(entityname, entitydata)).GetAwaiter().GetResult();
        }

        public bool UpdateEntity(string entityname, object entitydata, string entityid)
        {
            return Task.Run(() => UpdateEntityAsync(entityname, entitydata, entityid)).GetAwaiter().GetResult();
        }

        public bool DeleteEntity(string entityname, string entitydata, string entityid)
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
            // Zoho CRM doesn't support arbitrary SQL queries
            // This would need to be implemented using Zoho's filter API
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunQuery not supported. Use GetEntity with filters instead.";
            return ErrorObject;
        }

        public new IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunScript not supported for Zoho CRM";
            return ErrorObject;
        }

        public new void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _entityCache.Clear();
        }

        #endregion
    }
}

