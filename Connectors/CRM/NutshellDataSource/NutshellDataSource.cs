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
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Nutshell.Models;

namespace TheTechIdea.Beep.Connectors.NutshellDataSource
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Nutshell)]
    public class NutshellDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Nutshell API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["contacts"] = "Contacts",
            ["accounts"] = "Accounts",
            ["leads"] = "Leads",
            ["opportunities"] = "Opportunities"
        };

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
        private string _connectionString = string.Empty;
        private readonly Dictionary<string, NutshellEntity> _entityCache = new();

        #endregion

        #region Constructor

        public NutshellDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            Category = DatasourceCategory.Connector;
            _config = new NutshellConfig();

            // Ensure WebAPI connection props exist
            if (Dataconnection != null && Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();

            // Initialize connection properties
            if (Dataconnection?.ConnectionProp != null)
            {
                _connectionString = Dataconnection.ConnectionProp.ConnectionString;
                ParseConnectionString();
            }
        }

        #endregion

        #region Nutshell-Specific Methods

        public async Task<bool> ConnectAsync()
        {
            try
            {
                Logger.WriteLog($"Connecting to Nutshell: {_config.BaseUrl}");

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
                        ConnectionStatus = ConnectionState.Open;
                        Logger.WriteLog("Successfully connected to Nutshell");
                        return true;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection test failed: {response.StatusCode} - {errorContent}";
                return false;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Connection failed: {ex.Message}";
                Logger.WriteLog($"Nutshell connection error: {ex.Message}");
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
                Logger.WriteLog("Disconnected from Nutshell");
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
                    throw new InvalidOperationException("Not connected to Nutshell");

                // Get available entities from Nutshell
                var entities = GetNutshellEntitiesAsync();
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
                    throw new InvalidOperationException("Not connected to Nutshell");

                if (!refresh && Entities.Any())
                    return Task.FromResult(Entities);

                var nutshellEntities = GetNutshellEntitiesAsync();
                Entities = new List<EntityStructure>();

                foreach (var entity in nutshellEntities)
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
                    throw new InvalidOperationException("Not connected to Nutshell");

                var request = new
                {
                    method = $"get{EntityEndpoints[entityName]}",
                    @params = BuildQueryParameters(filter),
                    id = Guid.NewGuid().ToString()
                };

                var jsonData = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.BaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = ParseResponse(responseContent, entityName);
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

        private List<NutshellEntity> GetNutshellEntitiesAsync()
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

        #region Response Parsing

        private IEnumerable<object>? ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName switch
                {
                    "contacts" => ExtractArray<Contact>(jsonContent),
                    "accounts" => ExtractArray<Account>(jsonContent),
                    "leads" => ExtractArray<Lead>(jsonContent),
                    "opportunities" => ExtractArray<Opportunity>(jsonContent),
                    _ => JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(jsonContent)?.Result.EnumerateArray().Select(x => (object)x) ?? new List<object>()
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return JsonSerializer.Deserialize<NutshellApiResponse<JsonElement>>(jsonContent)?.Result.EnumerateArray().Select(x => (object)x) ?? new List<object>();
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : NutshellEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<NutshellApiResponse<List<T>>>(jsonContent, options);
                if (apiResponse?.Result != null)
                {
                    foreach (var item in apiResponse.Result)
                    {
                        item.Attach<T>((IDataSource)this);
                    }
                }
                return apiResponse?.Result ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Nutshell, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "NutshellDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Contact>")]
        public async Task<IEnumerable<Contact>> GetContacts(AppFilter filter)
        {
            var result = await GetEntityAsync("contacts", new List<AppFilter> { filter });
            return result.Cast<Contact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Nutshell, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "NutshellDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Account>")]
        public async Task<IEnumerable<Account>> GetAccounts(AppFilter filter)
        {
            var result = await GetEntityAsync("accounts", new List<AppFilter> { filter });
            return result.Cast<Account>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Nutshell, PointType = EnumPointType.Function, ObjectType = "Leads", ClassName = "NutshellDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Lead>")]
        public async Task<IEnumerable<Lead>> GetLeads(AppFilter filter)
        {
            var result = await GetEntityAsync("leads", new List<AppFilter> { filter });
            return result.Cast<Lead>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Nutshell, PointType = EnumPointType.Function, ObjectType = "Opportunities", ClassName = "NutshellDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Opportunity>")]
        public async Task<IEnumerable<Opportunity>> GetOpportunities(AppFilter filter)
        {
            var result = await GetEntityAsync("opportunities", new List<AppFilter> { filter });
            return result.Cast<Opportunity>();
        }

        #endregion

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
            // Nutshell doesn't support arbitrary SQL queries
            // This would need to be implemented using Nutshell's filter API
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "RunQuery not supported. Use GetEntity with filters instead.";
            return ErrorObject;
        }

        public new IErrorsInfo RunScript(ETLScriptDet dDLScripts)
        {
            ErrorObject.Flag = Errors.Warning;
            ErrorObject.Message = "RunScript not supported for Nutshell";
            return ErrorObject;
        }

        public new void Dispose()
        {
            Task.Run(() => DisconnectAsync()).GetAwaiter().GetResult();
            _entityCache.Clear();
        }
    }
}
