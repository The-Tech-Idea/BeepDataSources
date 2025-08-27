using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace TheTechIdea.Beep.Connectors.ZohoDataSource
{
    /// <summary>
    /// Zoho CRM Data Source implementation using Zoho CRM API v2
    /// </summary>
    public class ZohoDataSource : IDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Zoho CRM connection
        /// </summary>
        public class ZohoConfig
        {
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string RedirectUri { get; set; } = "http://localhost:8080";
            public string RefreshToken { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string ApiDomain { get; set; } = "www.zohoapis.com"; // or zohoapis.eu, zohoapis.com.au, etc.
            public string DataCenter { get; set; } = "us"; // us, eu, au, in, cn
            public DateTime TokenExpiry { get; set; } = DateTime.MinValue;
        }

        /// <summary>
        /// Zoho CRM entity metadata
        /// </summary>
        public class ZohoEntity
        {
            public string ModuleName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiName { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        /// <summary>
        /// Zoho API response wrapper
        /// </summary>
        public class ZohoApiResponse<T>
        {
            public List<T> Data { get; set; } = new();
            public ZohoApiInfo Info { get; set; } = new();
        }

        /// <summary>
        /// Zoho API response info
        /// </summary>
        public class ZohoApiInfo
        {
            public int Count { get; set; }
            public bool MoreRecords { get; set; }
            public int Page { get; set; }
            public int PerPage { get; set; }
        }

        #endregion

        #region Private Fields

        private readonly ZohoConfig _config;
        private HttpClient? _httpClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, ZohoEntity> _entityCache = new();

        #endregion

        #region Constructor

        public ZohoDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new ZohoConfig();

            // Initialize connection properties
            Dataconnection = cn;
            if (cn != null)
            {
                _connectionString = cn.ConnectionString;
                ParseConnectionString();
            }

            // Initialize HTTP client
            var handler = new HttpClientHandler();
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeepDataConnector/1.0");
        }

        #endregion

        #region IDataSource Implementation

        public string DatasourceName { get; set; } = string.Empty;
        public string DatasourceType { get; set; } = "Zoho";
        public DatasourceCategory Category { get; set; } = DatasourceCategory.CRM;
        public IDataConnection? Dataconnection { get; set; }
        public object? DatasourceConnection { get; set; }
        public ConnectionState ConnectionStatus => _connectionState;
        public bool InMemory { get; set; } = false;
        public List<string> EntitiesNames { get; set; } = new();
        public List<EntityStructure> Entities { get; set; } = new();
        public IDMLogger Logger => _logger;
        public IErrorsInfo ErrorObject => _errorsInfo;
        public IUtil util => _util;
        public IJsonLoader jsonLoader => _jsonLoader;
        public IDMEEditor DMEEditor => _dmeEditor;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _logger.WriteLog("Connecting to Zoho CRM");

                // Refresh access token if needed
                if (string.IsNullOrEmpty(_config.AccessToken) || DateTime.Now >= _config.TokenExpiry)
                {
                    if (!await RefreshAccessTokenAsync())
                    {
                        _errorsInfo.AddError("Zoho", "Failed to obtain access token");
                        return false;
                    }
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", _config.AccessToken);

                // Test connection by getting user info
                var testResponse = await _httpClient.GetAsync($"https://{_config.ApiDomain}/crm/v2/users?type=CurrentUser");
                if (testResponse.IsSuccessStatusCode)
                {
                    _connectionState = ConnectionState.Open;
                    DatasourceConnection = _httpClient;
                    _logger.WriteLog("Successfully connected to Zoho CRM");
                    return true;
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Connection test failed: {testResponse.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"Zoho CRM connection error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient?.Dispose();
                _httpClient = null;
                _connectionState = ConnectionState.Closed;
                DatasourceConnection = null;
                _entityCache.Clear();
                _logger.WriteLog("Disconnected from Zoho CRM");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Disconnect failed: {ex.Message}", ex);
                return false;
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

        public async Task<List<string>> GetEntitiesNamesAsync()
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                // Get available modules from Zoho CRM
                var entities = await GetZohoEntitiesAsync();
                EntitiesNames = entities.Select(e => e.ModuleName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                if (!refresh && Entities.Any())
                    return Entities;

                var zohoEntities = await GetZohoEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in zohoEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.ModuleName,
                        DisplayName = entity.DisplayName,
                        SchemaName = "ZohoCRM",
                        Fields = new List<EntityField>()
                    };

                    foreach (var field in entity.Fields)
                    {
                        structure.Fields.Add(new EntityField
                        {
                            fieldname = field.Key,
                            fieldtype = field.Value,
                            FieldDisplayName = field.Key
                        });
                    }

                    Entities.Add(structure);
                }

                return Entities;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var queryParams = BuildQueryParameters(filter);
                var url = $"https://{_config.ApiDomain}/crm/v2/{entityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Failed to get {entityName}: {response.StatusCode} - {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to get entity data: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var jsonData = JsonSerializer.Serialize(new { data = new[] { entityData } });
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"https://{_config.ApiDomain}/crm/v2/{entityName}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to insert entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var jsonData = JsonSerializer.Serialize(new { data = new[] { entityData } });
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"https://{_config.ApiDomain}/crm/v2/{entityName}/{entityId}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Failed to update {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var response = await _httpClient.DeleteAsync($"https://{_config.ApiDomain}/crm/v2/{entityName}/{entityId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: ClientId=xxx;ClientSecret=xxx;RefreshToken=xxx;DataCenter=xxx
            var parts = _connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
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
                        case "datacenter":
                            _config.DataCenter = value;
                            SetApiDomain(value);
                            break;
                        case "accesstoken":
                            _config.AccessToken = value;
                            break;
                    }
                }
            }
        }

        private void SetApiDomain(string dataCenter)
        {
            _config.ApiDomain = dataCenter switch
            {
                "us" => "www.zohoapis.com",
                "eu" => "www.zohoapis.eu",
                "au" => "www.zohoapis.com.au",
                "in" => "www.zohoapis.in",
                "cn" => "www.zohoapis.cn",
                _ => "www.zohoapis.com"
            };
        }

        private async Task<bool> RefreshAccessTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.RefreshToken))
                {
                    _errorsInfo.AddError("Zoho", "Refresh token is required");
                    return false;
                }

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.zoho.com/oauth/v2/token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["refresh_token"] = _config.RefreshToken,
                        ["client_id"] = _config.ClientId,
                        ["client_secret"] = _config.ClientSecret,
                        ["grant_type"] = "refresh_token"
                    })
                };

                var response = await _httpClient.SendAsync(tokenRequest);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _config.AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
                    var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
                    _config.TokenExpiry = DateTime.Now.AddSeconds(expiresIn - 60); // Refresh 1 minute early

                    _logger.WriteLog("Successfully refreshed Zoho access token");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Zoho", $"Token refresh failed: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Zoho", $"Token refresh error: {ex.Message}", ex);
                return false;
            }
        }

        private async Task<List<ZohoEntity>> GetZohoEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Zoho CRM modules
            var entities = new List<ZohoEntity>
            {
                new ZohoEntity
                {
                    ModuleName = "Leads",
                    DisplayName = "Leads",
                    ApiName = "Leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["First_Name"] = "String",
                        ["Last_Name"] = "String",
                        ["Email"] = "String",
                        ["Phone"] = "String",
                        ["Company"] = "String",
                        ["Lead_Source"] = "String",
                        ["Lead_Status"] = "String"
                    }
                },
                new ZohoEntity
                {
                    ModuleName = "Contacts",
                    DisplayName = "Contacts",
                    ApiName = "Contacts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["First_Name"] = "String",
                        ["Last_Name"] = "String",
                        ["Email"] = "String",
                        ["Phone"] = "String",
                        ["Mobile"] = "String",
                        ["Account_Name"] = "String"
                    }
                },
                new ZohoEntity
                {
                    ModuleName = "Accounts",
                    DisplayName = "Accounts",
                    ApiName = "Accounts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["Account_Name"] = "String",
                        ["Website"] = "String",
                        ["Phone"] = "String",
                        ["Billing_City"] = "String",
                        ["Billing_Country"] = "String"
                    }
                },
                new ZohoEntity
                {
                    ModuleName = "Deals",
                    DisplayName = "Deals",
                    ApiName = "Deals",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["Deal_Name"] = "String",
                        ["Account_Name"] = "String",
                        ["Contact_Name"] = "String",
                        ["Amount"] = "Decimal",
                        ["Stage"] = "String",
                        ["Closing_Date"] = "DateTime"
                    }
                }
            };

            foreach (var entity in entities)
            {
                _entityCache[entity.ModuleName] = entity;
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
                    queryParts.Add($"({filter.FieldName}:equals:{filter.FilterValue})");
                }
            }

            return queryParts.Any() ? $"?criteria=({string.Join(" and ", queryParts)})" : string.Empty;
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

        public bool DeleteEntity(string entityname, string entityid)
        {
            return Task.Run(() => DeleteEntityAsync(entityname, entityid)).GetAwaiter().GetResult();
        }

        public object GetEntity(string entityname, List<AppFilter> filter)
        {
            return Task.Run(() => GetEntityAsync(entityname, filter)).GetAwaiter().GetResult();
        }

        public List<EntityStructure> GetEntityStructures(bool refresh = false)
        {
            return Task.Run(() => GetEntityStructuresAsync(refresh)).GetAwaiter().GetResult();
        }

        public List<string> GetEntitesList()
        {
            return Task.Run(() => GetEntitiesNamesAsync()).GetAwaiter().GetResult();
        }

        public bool Openconnection()
        {
            return Task.Run(() => OpenconnectionAsync()).GetAwaiter().GetResult();
        }

        public bool Closeconnection()
        {
            return Task.Run(() => CloseconnectionAsync()).GetAwaiter().GetResult();
        }

        public bool CreateEntityAs(string entityname, object entitydata)
        {
            return CreateEntityAsAsync(entityname, entitydata);
        }

        public object RunQuery(string qrystr)
        {
            // Zoho CRM doesn't support arbitrary SQL queries
            // This would need to be implemented using Zoho's custom query API
            _errorsInfo.AddError("Zoho", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("Zoho", "RunScript not supported for Zoho CRM");
            return null;
        }

        public void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _entityCache.Clear();
        }

        #endregion
    }
}
