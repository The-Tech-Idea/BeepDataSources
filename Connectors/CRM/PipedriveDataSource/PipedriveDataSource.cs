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

namespace TheTechIdea.Beep.Connectors.PipedriveDataSource
{
    /// <summary>
    /// Pipedrive CRM Data Source implementation using Pipedrive REST API
    /// </summary>
    public class PipedriveDataSource : IDataSource
    {
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

        /// <summary>
        /// Pipedrive API response wrapper
        /// </summary>
        public class PipedriveApiResponse<T>
        {
            public bool Success { get; set; }
            public T? Data { get; set; }
            public string? Error { get; set; }
            public int? ErrorCode { get; set; }
        }

        #endregion

        #region Private Fields

        private readonly PipedriveConfig _config;
        private HttpClient? _httpClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, PipedriveEntity> _entityCache = new();

        #endregion

        #region Constructor

        public PipedriveDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new PipedriveConfig();

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
        public string DatasourceType { get; set; } = "Pipedrive";
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
                _logger.WriteLog($"Connecting to Pipedrive: {_config.BaseUrl}");

                // Test connection by getting user info
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/users?api_token={_config.ApiToken}");
                if (testResponse.IsSuccessStatusCode)
                {
                    var responseContent = await testResponse.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);

                    if (apiResponse?.Success == true)
                    {
                        _connectionState = ConnectionState.Open;
                        DatasourceConnection = _httpClient;
                        _logger.WriteLog("Successfully connected to Pipedrive");
                        return true;
                    }
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Pipedrive", $"Connection test failed: {testResponse.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"Pipedrive connection error: {ex.Message}");
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
                _logger.WriteLog("Disconnected from Pipedrive");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Disconnect failed: {ex.Message}", ex);
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
                    throw new InvalidOperationException("Not connected to Pipedrive");

                // Get available entities from Pipedrive
                var entities = await GetPipedriveEntitiesAsync();
                EntitiesNames = entities.Select(e => e.EntityName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                if (!refresh && Entities.Any())
                    return Entities;

                var pipedriveEntities = await GetPipedriveEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in pipedriveEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.EntityName,
                        DisplayName = entity.DisplayName,
                        SchemaName = "Pipedrive",
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
                _errorsInfo.AddError("Pipedrive", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var queryParams = BuildQueryParameters(filter);
                var url = $"{_config.BaseUrl}/{entityName.ToLower()}{queryParams}&api_token={_config.ApiToken}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);

                    if (apiResponse?.Success == true && apiResponse.Data.HasValue)
                    {
                        return apiResponse.Data.Value;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Pipedrive", $"Failed to get {entityName}: {response.StatusCode} - {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Failed to get entity data: {ex.Message}", ex);
                return null;
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

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/{entityName.ToLower()}?api_token={_config.ApiToken}", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Pipedrive", $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Failed to insert entity: {ex.Message}", ex);
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

                var response = await _httpClient.PutAsync($"{_config.BaseUrl}/{entityName.ToLower()}/{entityId}?api_token={_config.ApiToken}", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Pipedrive", $"Failed to update {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Pipedrive");

                var response = await _httpClient.DeleteAsync($"{_config.BaseUrl}/{entityName.ToLower()}/{entityId}?api_token={_config.ApiToken}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);

                    return apiResponse?.Success == true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Pipedrive", $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Pipedrive", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: ApiToken=xxx;BaseUrl=xxx
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

        private async Task<List<PipedriveEntity>> GetPipedriveEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Pipedrive entities
            var entities = new List<PipedriveEntity>
            {
                new PipedriveEntity
                {
                    EntityName = "Persons",
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
                        ["org_id"] = "Int32",
                        ["owner_id"] = "Int32",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "Organizations",
                    DisplayName = "Organizations",
                    ApiEndpoint = "organizations",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["address"] = "String",
                        ["address_country"] = "String",
                        ["email"] = "String",
                        ["phone"] = "String",
                        ["website"] = "String",
                        ["owner_id"] = "Int32",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "Deals",
                    DisplayName = "Deals",
                    ApiEndpoint = "deals",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["title"] = "String",
                        ["value"] = "Decimal",
                        ["currency"] = "String",
                        ["status"] = "String",
                        ["org_id"] = "Int32",
                        ["person_id"] = "Int32",
                        ["user_id"] = "Int32",
                        ["add_time"] = "DateTime",
                        ["update_time"] = "DateTime"
                    }
                },
                new PipedriveEntity
                {
                    EntityName = "Leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "String",
                        ["title"] = "String",
                        ["person_id"] = "Int32",
                        ["organization_id"] = "Int32",
                        ["owner_id"] = "Int32",
                        ["source_name"] = "String",
                        ["is_archived"] = "Boolean",
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
            // Pipedrive doesn't support arbitrary SQL queries
            // This would need to be implemented using Pipedrive's filter API
            _errorsInfo.AddError("Pipedrive", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("Pipedrive", "RunScript not supported for Pipedrive");
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
