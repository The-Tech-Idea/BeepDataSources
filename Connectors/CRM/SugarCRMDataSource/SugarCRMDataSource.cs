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

namespace TheTechIdea.Beep.Connectors.SugarCRMDataSource
{
    /// <summary>
    /// SugarCRM Data Source implementation using SugarCRM REST API v11
    /// </summary>
    public class SugarCRMDataSource : IDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for SugarCRM connection
        /// </summary>
        public class SugarConfig
        {
            public string BaseUrl { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ClientId { get; set; } = string.Empty;
            public string ClientSecret { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public DateTime TokenExpiry { get; set; } = DateTime.MinValue;
            public string Platform { get; set; } = "base"; // base, mobile, portal
        }

        /// <summary>
        /// SugarCRM module metadata
        /// </summary>
        public class SugarModule
        {
            public string ModuleName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        /// <summary>
        /// SugarCRM API response wrapper
        /// </summary>
        public class SugarApiResponse<T>
        {
            public List<T> Records { get; set; } = new();
            public int NextOffset { get; set; } = -1;
    }

        #endregion

        #region Private Fields

        private readonly SugarConfig _config;
        private HttpClient? _httpClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, SugarModule> _moduleCache = new();

        #endregion

        #region Constructor

        public SugarCRMDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new SugarConfig();

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
        public string DatasourceType { get; set; } = "SugarCRM";
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
                _logger.WriteLog($"Connecting to SugarCRM: {_config.BaseUrl}");

                // Authenticate and get access token
                if (!await AuthenticateAsync())
                {
                    _errorsInfo.AddError("SugarCRM", "Authentication failed");
                    return false;
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user info
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/rest/v11/me");
                if (testResponse.IsSuccessStatusCode)
                {
                    _connectionState = ConnectionState.Open;
                    DatasourceConnection = _httpClient;
                    _logger.WriteLog("Successfully connected to SugarCRM");
                    return true;
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Connection test failed: {testResponse.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"SugarCRM connection error: {ex.Message}");
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
                _moduleCache.Clear();
                _logger.WriteLog("Disconnected from SugarCRM");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Disconnect failed: {ex.Message}", ex);
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
                    throw new InvalidOperationException("Not connected to SugarCRM");

                // Get available modules from SugarCRM
                var modules = await GetSugarModulesAsync();
                EntitiesNames = modules.Select(m => m.ModuleName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to SugarCRM");

                if (!refresh && Entities.Any())
                    return Entities;

                var sugarModules = await GetSugarModulesAsync();
                Entities = new List<EntityStructure>();

                foreach (var module in sugarModules)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = module.ModuleName,
                        DisplayName = module.DisplayName,
                        SchemaName = "SugarCRM",
                        Fields = new List<EntityField>()
                    };

                    foreach (var field in module.Fields)
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
                _errorsInfo.AddError("SugarCRM", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to SugarCRM");

                var queryParams = BuildQueryParameters(filter);
                var url = $"{_config.BaseUrl}/rest/v11/{entityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Failed to get {entityName}: {response.StatusCode} - {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Failed to get entity data: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to SugarCRM");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/rest/v11/{entityName}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Failed to insert entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to SugarCRM");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_config.BaseUrl}/rest/v11/{entityName}/{entityId}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Failed to update {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to SugarCRM");

                var response = await _httpClient.DeleteAsync($"{_config.BaseUrl}/rest/v11/{entityName}/{entityId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: BaseUrl=xxx;Username=xxx;Password=xxx;ClientId=xxx;ClientSecret=xxx;Platform=xxx
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
                        case "baseurl":
                            _config.BaseUrl = value;
                            break;
                        case "username":
                            _config.Username = value;
                            break;
                        case "password":
                            _config.Password = value;
                            break;
                        case "clientid":
                            _config.ClientId = value;
                            break;
                        case "clientsecret":
                            _config.ClientSecret = value;
                            break;
                        case "platform":
                            _config.Platform = value;
                            break;
                        case "accesstoken":
                            _config.AccessToken = value;
                            break;
                        case "refreshtoken":
                            _config.RefreshToken = value;
                            break;
                    }
                }
            }
        }

        private async Task<bool> AuthenticateAsync()
        {
            try
            {
                // If we already have a valid token, use it
                if (!string.IsNullOrEmpty(_config.AccessToken) && DateTime.Now < _config.TokenExpiry)
                {
                    return true;
                }

                // Use OAuth2 password grant flow
                var authData = new
                {
                    grant_type = "password",
                    client_id = _config.ClientId,
                    client_secret = _config.ClientSecret,
                    username = _config.Username,
                    password = _config.Password,
                    platform = _config.Platform
                };

                var jsonData = JsonSerializer.Serialize(authData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/rest/v11/oauth2/token", content);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _config.AccessToken = tokenResponse.GetProperty("access_token").GetString() ?? string.Empty;
                    _config.RefreshToken = tokenResponse.GetProperty("refresh_token").GetString() ?? string.Empty;

                    // Set token expiry (default to 1 hour if not specified)
                    var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expiresProp)
                        ? expiresProp.GetInt32()
                        : 3600;
                    _config.TokenExpiry = DateTime.Now.AddSeconds(expiresIn - 60); // Refresh 1 minute early

                    _logger.WriteLog("Successfully authenticated with SugarCRM");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("SugarCRM", $"Authentication failed: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("SugarCRM", $"Authentication error: {ex.Message}", ex);
                return false;
            }
        }

        private async Task<List<SugarModule>> GetSugarModulesAsync()
        {
            if (_moduleCache.Any())
                return _moduleCache.Values.ToList();

            // Common SugarCRM modules
            var modules = new List<SugarModule>
            {
                new SugarModule
                {
                    ModuleName = "Leads",
                    DisplayName = "Leads",
                    TableName = "leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["first_name"] = "String",
                        ["last_name"] = "String",
                        ["account_name"] = "String",
                        ["email1"] = "String",
                        ["phone_work"] = "String",
                        ["phone_mobile"] = "String",
                        ["lead_source"] = "String",
                        ["status"] = "String",
                        ["date_entered"] = "DateTime"
                    }
                },
                new SugarModule
                {
                    ModuleName = "Contacts",
                    DisplayName = "Contacts",
                    TableName = "contacts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["first_name"] = "String",
                        ["last_name"] = "String",
                        ["account_name"] = "String",
                        ["email1"] = "String",
                        ["phone_work"] = "String",
                        ["phone_mobile"] = "String",
                        ["title"] = "String",
                        ["date_entered"] = "DateTime"
                    }
                },
                new SugarModule
                {
                    ModuleName = "Accounts",
                    DisplayName = "Accounts",
                    TableName = "accounts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["name"] = "String",
                        ["website"] = "String",
                        ["phone_office"] = "String",
                        ["email1"] = "String",
                        ["billing_address_city"] = "String",
                        ["billing_address_country"] = "String",
                        ["industry"] = "String",
                        ["date_entered"] = "DateTime"
                    }
                },
                new SugarModule
                {
                    ModuleName = "Opportunities",
                    DisplayName = "Opportunities",
                    TableName = "opportunities",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["name"] = "String",
                        ["account_name"] = "String",
                        ["amount"] = "Decimal",
                        ["date_closed"] = "Date",
                        ["sales_stage"] = "String",
                        ["probability"] = "Decimal",
                        ["date_entered"] = "DateTime"
                    }
                }
            };

            foreach (var module in modules)
            {
                _moduleCache[module.ModuleName] = module;
            }

            return modules;
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
                    queryParts.Add($"{filter.FieldName}={filter.FilterValue}");
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
            // SugarCRM doesn't support arbitrary SQL queries
            // This would need to be implemented using SugarCRM's filter API
            _errorsInfo.AddError("SugarCRM", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("SugarCRM", "RunScript not supported for SugarCRM");
            return null;
        }

        public void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _moduleCache.Clear();
        }

        #endregion
    }
}
