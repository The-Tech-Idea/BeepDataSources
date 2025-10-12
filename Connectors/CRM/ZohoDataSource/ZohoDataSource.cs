using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Connectors.Zoho.Models;

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

            // Register entities
            EntitiesNames.Add("Leads");
            EntitiesNames.Add("Contacts");
            EntitiesNames.Add("Accounts");
            EntitiesNames.Add("Deals");
            EntitiesNames.Add("Campaigns");
            EntitiesNames.Add("Tasks");
            EntitiesNames.Add("Events");
            EntitiesNames.Add("Calls");
            EntitiesNames.Add("Notes");
            EntitiesNames.Add("Products");
            EntitiesNames.Add("Quotes");
            EntitiesNames.Add("Invoices");
            EntitiesNames.Add("Vendors");
            EntitiesNames.Add("Users");

            Entities.Add(new EntityStructure { EntityName = "Leads", DatasourceEntityName = "Leads" });
            Entities.Add(new EntityStructure { EntityName = "Contacts", DatasourceEntityName = "Contacts" });
            Entities.Add(new EntityStructure { EntityName = "Accounts", DatasourceEntityName = "Accounts" });
            Entities.Add(new EntityStructure { EntityName = "Deals", DatasourceEntityName = "Deals" });
            Entities.Add(new EntityStructure { EntityName = "Campaigns", DatasourceEntityName = "Campaigns" });
            Entities.Add(new EntityStructure { EntityName = "Tasks", DatasourceEntityName = "Tasks" });
            Entities.Add(new EntityStructure { EntityName = "Events", DatasourceEntityName = "Events" });
            Entities.Add(new EntityStructure { EntityName = "Calls", DatasourceEntityName = "Calls" });
            Entities.Add(new EntityStructure { EntityName = "Notes", DatasourceEntityName = "Notes" });
            Entities.Add(new EntityStructure { EntityName = "Products", DatasourceEntityName = "Products" });
            Entities.Add(new EntityStructure { EntityName = "Quotes", DatasourceEntityName = "Quotes" });
            Entities.Add(new EntityStructure { EntityName = "Invoices", DatasourceEntityName = "Invoices" });
            Entities.Add(new EntityStructure { EntityName = "Vendors", DatasourceEntityName = "Vendors" });
            Entities.Add(new EntityStructure { EntityName = "Users", DatasourceEntityName = "users" });
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

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (_httpClient == null)
                    throw new InvalidOperationException("Not connected to Zoho CRM");

                var queryParams = BuildQueryParameters(Filter);
                var url = $"{_config.BaseUrl}/{EntityName}{queryParams}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.WriteLog($"API request failed: {response.StatusCode} - {errorContent}");
                    return new List<object>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                return ParseResponse(jsonContent, EntityName) ?? new List<object>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityAsync for {EntityName}: {ex.Message}");
                return new List<object>();
            }
        }

        #region Response Parsing

        private IEnumerable<object>? ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName switch
                {
                    "Leads" => ExtractArray<ZohoLead>(jsonContent),
                    "Contacts" => ExtractArray<ZohoContact>(jsonContent),
                    "Accounts" => ExtractArray<ZohoAccount>(jsonContent),
                    "Deals" => ExtractArray<ZohoDeal>(jsonContent),
                    "Campaigns" => ExtractArray<ZohoCampaign>(jsonContent),
                    "Tasks" => ExtractArray<ZohoTask>(jsonContent),
                    "Events" => ExtractArray<ZohoEvent>(jsonContent),
                    "Calls" => ExtractArray<ZohoCall>(jsonContent),
                    "Notes" => ExtractArray<ZohoNote>(jsonContent),
                    "Products" => ExtractArray<ZohoProduct>(jsonContent),
                    "Quotes" => ExtractArray<ZohoQuote>(jsonContent),
                    "Invoices" => ExtractArray<ZohoInvoice>(jsonContent),
                    "Vendors" => ExtractArray<ZohoVendor>(jsonContent),
                    "Users" => ExtractArray<ZohoUser>(jsonContent),
                    _ => ExtractRecords(jsonContent)
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return ExtractRecords(jsonContent);
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : ZohoEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<ZohoApiResponse<List<T>>>(jsonContent, options);
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

        private static IEnumerable<object> ExtractRecords(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("data", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = new List<object>(arr.GetArrayLength());
                foreach (var el in arr.EnumerateArray())
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (dict != null) list.Add(dict);
                }
                return list;
            }
            return Array.Empty<object>();
        }

        #endregion

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

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Leads", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoLead>")]
        public async Task<IEnumerable<ZohoLead>> GetLeads(AppFilter filter)
        {
            var result = await GetEntityAsync("Leads", new List<AppFilter> { filter });
            return result.Cast<ZohoLead>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoContact>")]
        public async Task<IEnumerable<ZohoContact>> GetContacts(AppFilter filter)
        {
            var result = await GetEntityAsync("Contacts", new List<AppFilter> { filter });
            return result.Cast<ZohoContact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoAccount>")]
        public async Task<IEnumerable<ZohoAccount>> GetAccounts(AppFilter filter)
        {
            var result = await GetEntityAsync("Accounts", new List<AppFilter> { filter });
            return result.Cast<ZohoAccount>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Deals", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoDeal>")]
        public async Task<IEnumerable<ZohoDeal>> GetDeals(AppFilter filter)
        {
            var result = await GetEntityAsync("Deals", new List<AppFilter> { filter });
            return result.Cast<ZohoDeal>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Campaigns", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoCampaign>")]
        public async Task<IEnumerable<ZohoCampaign>> GetCampaigns(AppFilter filter)
        {
            var result = await GetEntityAsync("Campaigns", new List<AppFilter> { filter });
            return result.Cast<ZohoCampaign>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Tasks", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoTask>")]
        public async Task<IEnumerable<ZohoTask>> GetTasks(AppFilter filter)
        {
            var result = await GetEntityAsync("Tasks", new List<AppFilter> { filter });
            return result.Cast<ZohoTask>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Events", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoEvent>")]
        public async Task<IEnumerable<ZohoEvent>> GetEvents(AppFilter filter)
        {
            var result = await GetEntityAsync("Events", new List<AppFilter> { filter });
            return result.Cast<ZohoEvent>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Calls", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoCall>")]
        public async Task<IEnumerable<ZohoCall>> GetCalls(AppFilter filter)
        {
            var result = await GetEntityAsync("Calls", new List<AppFilter> { filter });
            return result.Cast<ZohoCall>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Notes", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoNote>")]
        public async Task<IEnumerable<ZohoNote>> GetNotes(AppFilter filter)
        {
            var result = await GetEntityAsync("Notes", new List<AppFilter> { filter });
            return result.Cast<ZohoNote>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Products", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoProduct>")]
        public async Task<IEnumerable<ZohoProduct>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("Products", new List<AppFilter> { filter });
            return result.Cast<ZohoProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Quotes", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoQuote>")]
        public async Task<IEnumerable<ZohoQuote>> GetQuotes(AppFilter filter)
        {
            var result = await GetEntityAsync("Quotes", new List<AppFilter> { filter });
            return result.Cast<ZohoQuote>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Invoices", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoInvoice>")]
        public async Task<IEnumerable<ZohoInvoice>> GetInvoices(AppFilter filter)
        {
            var result = await GetEntityAsync("Invoices", new List<AppFilter> { filter });
            return result.Cast<ZohoInvoice>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Vendors", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoVendor>")]
        public async Task<IEnumerable<ZohoVendor>> GetVendors(AppFilter filter)
        {
            var result = await GetEntityAsync("Vendors", new List<AppFilter> { filter });
            return result.Cast<ZohoVendor>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zoho, PointType = EnumPointType.Function, ObjectType = "Users", ClassName = "ZohoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<ZohoUser>")]
        public async Task<IEnumerable<ZohoUser>> GetUsers(AppFilter filter)
        {
            var result = await GetEntityAsync("Users", new List<AppFilter> { filter });
            return result.Cast<ZohoUser>();
        }

        #endregion

        #region Configuration Classes

        private class ZohoApiResponse<T>
        {
            [JsonPropertyName("data")]
            public T? Data { get; set; }

            [JsonPropertyName("info")]
            public ZohoResponseInfo? Info { get; set; }
        }

        private class ZohoResponseInfo
        {
            [JsonPropertyName("per_page")]
            public int? PerPage { get; set; }

            [JsonPropertyName("count")]
            public int? Count { get; set; }

            [JsonPropertyName("page")]
            public int? Page { get; set; }

            [JsonPropertyName("more_records")]
            public bool? MoreRecords { get; set; }
        }

        #endregion
    }
}

