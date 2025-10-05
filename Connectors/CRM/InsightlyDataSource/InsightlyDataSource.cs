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

namespace TheTechIdea.Beep.Connectors.InsightlyDataSource
{
    /// <summary>
    /// Insightly CRM Data Source implementation using Insightly REST API
    /// </summary>
    public class InsightlyDataSource : WebAPIDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for Insightly connection
        /// </summary>
        public class InsightlyConfig
        {
            public string ApiKey { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = "https://api.insightly.com/v3.1";
        }

        /// <summary>
        /// Insightly entity metadata
        /// </summary>
        public class InsightlyEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        #region Private Fields

        private readonly InsightlyConfig _config;
        private HttpClient? _httpClient;
        private ConnectionState _connectionState = ConnectionState.Closed;
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, InsightlyEntity> _entityCache = new();

        #endregion

        #region Constructor

        public InsightlyDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new InsightlyConfig();

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
                Logger.WriteLog($"Connecting to Insightly: {_config.BaseUrl}");

                // Set authorization header with API key
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_config.ApiKey}:")));

                // Test connection by getting contacts
                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/Contacts");
                if (testResponse.IsSuccessStatusCode)
                {
                    ConnectionStatus = ConnectionState.Open;
                    Logger.WriteLog("Successfully connected to Insightly");
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
                Logger.WriteLog($"Insightly connection error: {ex.Message}");
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
                Logger.WriteLog("Disconnected from Insightly");
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
                    throw new InvalidOperationException("Not connected to Insightly");

                // Get available entities from Insightly
                var entities = GetInsightlyEntitiesAsync();
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
                    throw new InvalidOperationException("Not connected to Insightly");

                if (!refresh && Entities.Any())
                    return Task.FromResult(Entities);

                var insightlyEntities = GetInsightlyEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in insightlyEntities)
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
                    throw new InvalidOperationException("Not connected to Insightly");

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
                    throw new InvalidOperationException("Not connected to Insightly");

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
                    throw new InvalidOperationException("Not connected to Insightly");

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
                    throw new InvalidOperationException("Not connected to Insightly");

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

        private void ParseConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
                return;

            // Parse connection string format: ApiKey=xxx;BaseUrl=xxx
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

        private List<InsightlyEntity> GetInsightlyEntitiesAsync()
        {
            if (_entityCache.Any())
                return _entityCache.Values.ToList();

            // Common Insightly entities
            var entities = new List<InsightlyEntity>
            {
                new InsightlyEntity
                {
                    EntityName = "Contacts",
                    DisplayName = "Contacts",
                    ApiEndpoint = "Contacts",
                    Fields = new Dictionary<string, string>
                    {
                        ["CONTACT_ID"] = "Int32",
                        ["FIRST_NAME"] = "String",
                        ["LAST_NAME"] = "String",
                        ["SALUTATION"] = "String",
                        ["DATE_CREATED_UTC"] = "DateTime",
                        ["DATE_UPDATED_UTC"] = "DateTime",
                        ["EMAIL_ADDRESS"] = "String",
                        ["PHONE"] = "String",
                        ["MOBILE"] = "String",
                        ["ORGANISATION_ID"] = "Int32"
                    }
                },
                new InsightlyEntity
                {
                    EntityName = "Organisations",
                    DisplayName = "Organisations",
                    ApiEndpoint = "Organisations",
                    Fields = new Dictionary<string, string>
                    {
                        ["ORGANISATION_ID"] = "Int32",
                        ["ORGANISATION_NAME"] = "String",
                        ["DATE_CREATED_UTC"] = "DateTime",
                        ["DATE_UPDATED_UTC"] = "DateTime",
                        ["PHONE"] = "String",
                        ["FAX"] = "String",
                        ["WEBSITE"] = "String",
                        ["ADDRESS_BILLING_STREET"] = "String",
                        ["ADDRESS_BILLING_CITY"] = "String",
                        ["ADDRESS_BILLING_STATE"] = "String",
                        ["ADDRESS_BILLING_COUNTRY"] = "String"
                    }
                },
                new InsightlyEntity
                {
                    EntityName = "Opportunities",
                    DisplayName = "Opportunities",
                    ApiEndpoint = "Opportunities",
                    Fields = new Dictionary<string, string>
                    {
                        ["OPPORTUNITY_ID"] = "Int32",
                        ["OPPORTUNITY_NAME"] = "String",
                        ["OPPORTUNITY_DETAILS"] = "String",
                        ["PROBABILITY"] = "Decimal",
                        ["BID_AMOUNT"] = "Decimal",
                        ["BID_CURRENCY"] = "String",
                        ["DATE_CREATED_UTC"] = "DateTime",
                        ["DATE_UPDATED_UTC"] = "DateTime",
                        ["ORGANISATION_ID"] = "Int32",
                        ["CONTACT_ID"] = "Int32"
                    }
                },
                new InsightlyEntity
                {
                    EntityName = "Leads",
                    DisplayName = "Leads",
                    ApiEndpoint = "Leads",
                    Fields = new Dictionary<string, string>
                    {
                        ["LEAD_ID"] = "Int32",
                        ["FIRST_NAME"] = "String",
                        ["LAST_NAME"] = "String",
                        ["ORGANISATION_NAME"] = "String",
                        ["PHONE_NUMBER"] = "String",
                        ["EMAIL_ADDRESS"] = "String",
                        ["DATE_CREATED_UTC"] = "DateTime",
                        ["DATE_UPDATED_UTC"] = "DateTime",
                        ["LEAD_STATUS_ID"] = "Int32",
                        ["LEAD_SOURCE_ID"] = "Int32"
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

        public new object GetEntity(string entityname, List<AppFilter> filter)
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
            // Insightly doesn't support arbitrary SQL queries
            // This would need to be implemented using Insightly's filter API
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunQuery not supported. Use GetEntity with filters instead.";
            return ErrorObject;
        }

        public new IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = "RunScript not supported for Insightly";
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
