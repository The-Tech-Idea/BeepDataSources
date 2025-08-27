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

namespace TheTechIdea.Beep.Connectors.CopperDataSource
{
    /// <summary>
    /// Copper CRM Data Source implementation using Copper REST API
    /// </summary>
    public class CopperDataSource : IDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Copper connection
        /// </summary>
        public class CopperConfig
        {
            public string ApiKey { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://api.copper.com/developer_api/v1";
        }

        /// <summary>
        /// Copper entity metadata
        /// </summary>
        public class CopperEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        #region Private Fields

        private readonly CopperConfig _config;
        private HttpClient? _httpClient;
        private readonly IDMEEditor _dmeEditor;
        private readonly IErrorsInfo _errorsInfo;
        private readonly IJsonLoader _jsonLoader;
        private readonly IDMLogger _logger;
        private readonly IUtil _util;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, CopperEntity> _entityCache = new();

        #endregion

        #region Constructor

        public CopperDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            _dmeEditor = dmeEditor;
            _errorsInfo = per;
            _jsonLoader = new JsonLoader();
            _logger = new DMLogger();
            _util = new Util();
            _config = new CopperConfig();

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
        public string DatasourceType { get; set; } = "Copper";
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
                _logger.WriteLog($"Connecting to Copper: {_config.BaseUrl}");

                // Set authorization header with API key
                _httpClient.DefaultRequestHeaders.Add("X-PW-AccessToken", _config.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("X-PW-Application", "developer_api");
                _httpClient.DefaultRequestHeaders.Add("X-PW-UserEmail", _config.Email);
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting people
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/people/fetch_by_email");
                if (testResponse.IsSuccessStatusCode || testResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _connectionState = ConnectionState.Open;
                    DatasourceConnection = _httpClient;
                    _logger.WriteLog("Successfully connected to Copper");
                    return true;
                }

                var errorContent = await testResponse.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Copper", $"Connection test failed: {testResponse.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Connection failed: {ex.Message}", ex);
                _logger.WriteLog($"Copper connection error: {ex.Message}");
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
                _logger.WriteLog("Disconnected from Copper");
                return true;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Disconnect failed: {ex.Message}", ex);
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
                    throw new InvalidOperationException("Not connected to Copper");

                // Get available entities from Copper
                var entities = await GetCopperEntitiesAsync();
                EntitiesNames = entities.Select(e => e.EntityName).ToList();
                return EntitiesNames;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Failed to get entities: {ex.Message}", ex);
                return new List<string>();
            }
        }

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Copper");

                if (!refresh && Entities.Any())
                    return Entities;

                var copperEntities = await GetCopperEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in copperEntities)
                {
                    var structure = new EntityStructure
                    {
                        EntityName = entity.EntityName,
                        DisplayName = entity.DisplayName,
                        SchemaName = "Copper",
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
                _errorsInfo.AddError("Copper", $"Failed to get entity structures: {ex.Message}", ex);
                return new List<EntityStructure>();
            }
        }

        public async Task<object?> GetEntityAsync(string entityName, List<AppFilter>? filter = null)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Copper");

                var queryParams = BuildQueryParameters(filter);
                var url = $"{_config.BaseUrl}/{entityName.ToLower()}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Copper", $"Failed to get {entityName}: {response.StatusCode} - {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Failed to get entity data: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> InsertEntityAsync(string entityName, object entityData)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Copper");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}/{entityName.ToLower()}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Copper", $"Failed to insert {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Failed to insert entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, object entityData, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Copper");

                var jsonData = JsonSerializer.Serialize(entityData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_config.BaseUrl}/{entityName.ToLower()}/{entityId}", content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Copper", $"Failed to update {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Failed to update entity: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, string entityId)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Copper");

                var response = await _httpClient.DeleteAsync($"{_config.BaseUrl}/{entityName.ToLower()}/{entityId}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _errorsInfo.AddError("Copper", $"Failed to delete {entityName}: {response.StatusCode} - {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _errorsInfo.AddError("Copper", $"Failed to delete entity: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: Email=xxx;ApiKey=xxx;BaseUrl=xxx
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
                        case "email":
                            _config.Email = value;
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

        private async Task<List<CopperEntity>> GetCopperEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Copper entities
            var entities = new List<CopperEntity>
            {
                new CopperEntity
                {
                    EntityName = "People",
                    DisplayName = "People",
                    ApiEndpoint = "people",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["first_name"] = "String",
                        ["last_name"] = "String",
                        ["email"] = "String",
                        ["phone_numbers"] = "String",
                        ["title"] = "String",
                        ["company_id"] = "Int32",
                        ["company_name"] = "String",
                        ["date_created"] = "Int32",
                        ["date_modified"] = "Int32"
                    }
                },
                new CopperEntity
                {
                    EntityName = "Companies",
                    DisplayName = "Companies",
                    ApiEndpoint = "companies",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["email_domain"] = "String",
                        ["phone_numbers"] = "String",
                        ["address"] = "String",
                        ["website"] = "String",
                        ["tags"] = "String",
                        ["date_created"] = "Int32",
                        ["date_modified"] = "Int32"
                    }
                },
                new CopperEntity
                {
                    EntityName = "Opportunities",
                    DisplayName = "Opportunities",
                    ApiEndpoint = "opportunities",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["primary_contact_id"] = "Int32",
                        ["company_id"] = "Int32",
                        ["company_name"] = "String",
                        ["status"] = "String",
                        ["pipeline_id"] = "Int32",
                        ["pipeline_stage_id"] = "Int32",
                        ["monetary_value"] = "Int32",
                        ["date_created"] = "Int32",
                        ["date_modified"] = "Int32"
                    }
                },
                new CopperEntity
                {
                    EntityName = "Leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "Int32",
                        ["name"] = "String",
                        ["first_name"] = "String",
                        ["last_name"] = "String",
                        ["email"] = "String",
                        ["phone_numbers"] = "String",
                        ["company_name"] = "String",
                        ["status"] = "String",
                        ["source_id"] = "Int32",
                        ["date_created"] = "Int32",
                        ["date_modified"] = "Int32"
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
            // Copper doesn't support arbitrary SQL queries
            // This would need to be implemented using Copper's filter API
            _errorsInfo.AddError("Copper", "RunQuery not supported. Use GetEntity with filters instead.");
            return null;
        }

        public object RunScript(ETLScriptDet dDLScripts)
        {
            _errorsInfo.AddError("Copper", "RunScript not supported for Copper");
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
