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

namespace TheTechIdea.Beep.Connectors.NutshellDataSource
{
    /// <summary>
    /// Nutshell CRM Data Source implementation using Nutshell REST API
    /// </summary>
    public class NutshellDataSource : IDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Nutshell connection
        /// </summary>
        public class NutshellConfig
        {
            public string Username { get; set; } = string.Empty;
            public string ApiKey { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://app.nutshell.com/api/v1/json";
        }

        /// <summary>
        /// Nutshell entity metadata
        /// </summary>
        public class NutshellEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        /// <summary>
        /// Nutshell API response wrapper
        /// </summary>
        public class NutshellApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Result { get; set; }
            public string? Error { get; set; }
        }

        #endregion

        #region Private Fields

        private readonly NutshellConfig _config;
        private HttpClient? _httpClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, NutshellEntity> _entityCache = new();

        #endregion

        #region Constructor

        public NutshellDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new NutshellConfig();

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
        public string DatasourceType { get; set; } = "Nutshell";
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
                _logger.WriteLog($"Connecting to Nutshell: {_config.BaseUrl}");

                // Test connection by getting user info
                var testRequest = new
                {
                    method = "getUser",
                    @params = new { },
                    id = "test"
                };

                var jsonData = JsonSerializer.Serialize(testRequest);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var testResponse = JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(responseContent);

                    if (testResponse?.Success == true)
                    {
                        _connectionState = ConnectionState.Open;
                        DatasourceConnection = _httpClient;
                        _logger.WriteLog("Successfully connected to Nutshell");
                        return true;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Nutshell", $"Connection test failed: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"Nutshell connection error: {ex.Message}");
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
                _logger.WriteLog("Disconnected from Nutshell");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Disconnect failed: {ex.Message}", ex);
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
                    throw new InvalidOperationException("Not connected to Nutshell");

                // Get available entities from Nutshell
                var entities = await GetNutshellEntitiesAsync();
                EntitiesNames = entities.Select(e => e.EntityName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Nutshell");

                if (!refresh && Entities.Any())
                    return Entities;

                var nutshellEntities = await GetNutshellEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in nutshellEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.EntityName,
                        DisplayName = entity.DisplayName,
                        SchemaName = "Nutshell",
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
                _errorsInfo.AddError("Nutshell", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Nutshell");

                var request = new
                {
                    method = $"get{entityName}",
                    @params = BuildQueryParameters(filter),
                    id = Guid.NewGuid().ToString()
                };

                var jsonData = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(responseContent);

                    if (apiResponse?.Success == true && apiResponse.Result.HasValue)
                    {
                        return apiResponse.Result.Value;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Nutshell", $"Failed to get {entityName}: {response.StatusCode} - {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Failed to get entity data: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Nutshell");

                var request = new
                {
                    method = $"new{entityName}",
                    @params = entityData,
                    id = Guid.NewGuid().ToString()
                };

                var jsonData = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Nutshell", $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Failed to insert entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Nutshell");

                var request = new
                {
                    method = $"update{entityName}",
                    @params = new { id = int.Parse(entityId), data = entityData },
                    id = Guid.NewGuid().ToString()
                };

                var jsonData = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Nutshell", $"Failed to update {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Nutshell");

                var request = new
                {
                    method = $"delete{entityName}",
                    @params = new { id = int.Parse(entityId) },
                    id = Guid.NewGuid().ToString()
                };

                var jsonData = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Nutshell", $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Nutshell", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: Username=xxx;ApiKey=xxx;BaseUrl=xxx
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
                        case "username":
                            _config.Username = value;
                            break;
                        case "apikey":
                            _config.ApiKey = value;
                            break;
                        case "baseurl":
                            _config.BaseUrl = value;
                            break;
                    }
                }
            }
        }

        private async Task<List<NutshellEntity>> GetNutshellEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Nutshell entities
            var entities = new List<NutshellEntity>
            {
                new NutshellEntity
                {
                    EntityName = "Contacts",
                    DisplayName = "Contacts",
                    ApiEndpoint = "Contacts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["firstName"] = "String",
                        ["lastName"] = "String",
                        ["email"] = "String",
                        ["phone"] = "String",
                        ["jobTitle"] = "String",
                        ["account"] = "String",
                        ["createdTime"] = "DateTime",
                        ["modifiedTime"] = "DateTime"
                    }
                },
                new NutshellEntity
                {
                    EntityName = "Accounts",
                    DisplayName = "Accounts",
                    ApiEndpoint = "Accounts",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["description"] = "String",
                        ["industry"] = "String",
                        ["phone"] = "String",
                        ["email"] = "String",
                        ["website"] = "String",
                        ["createdTime"] = "DateTime",
                        ["modifiedTime"] = "DateTime"
                    }
                },
                new NutshellEntity
                {
                    EntityName = "Leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "Leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["firstName"] = "String",
                        ["lastName"] = "String",
                        ["email"] = "String",
                        ["company"] = "String",
                        ["jobTitle"] = "String",
                        ["phone"] = "String",
                        ["status"] = "String",
                        ["createdTime"] = "DateTime",
                        ["modifiedTime"] = "DateTime"
                    }
                },
                new NutshellEntity
                {
                    EntityName = "Opportunities",
                    DisplayName = "Opportunities",
                    ApiEndpoint = "Opportunities",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["account"] = "String",
                        ["contact"] = "String",
                        ["value"] = "Decimal",
                        ["currency"] = "String",
                        ["status"] = "String",
                        ["createdTime"] = "DateTime",
                        ["modifiedTime"] = "DateTime"
                    }
                }
            };

            foreach (var entity in entities)
            {
                _entityCache[entity.EntityName] = entity;
            }

            return entities;
        }

        private object BuildQueryParameters(List<AppFilter>? filters)
        {
            if (filters == null || !filters.Any())
                return new { };

            var queryParams = new Dictionary<string, object>();

            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.FilterValue))
                {
                    queryParams[filter.FieldName] = filter.FilterValue;
                }
            }

            return queryParams;
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
            // Nutshell doesn't support arbitrary SQL queries
            // This would need to be implemented using Nutshell's filter API
            _errorsInfo.AddError("Nutshell", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("Nutshell", "RunScript not supported for Nutshell");
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
