using System;using System;

using System.Collections.Generic;using System.Collections.Generic;

using System.Linq;using System.Data;

using System.Net.Http;using System.Linq;

using System.Text.Json;using System.Net.Http;

using System.Threading;using System.Net.Http.Json;

using System.Threading.Tasks;using System.Text.Json;

using TheTechIdea.Beep.ConfigUtil;using System.Threading.Tasks;

using TheTechIdea.Beep.DataBase;using Microsoft.Extensions.Http;

using TheTechIdea.Beep.Editor;using TheTechIdea.Beep.DataBase;

using TheTechIdea.Beep.Logger;using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Report;using TheTechIdea.Beep.Logger;

using TheTechIdea.Beep.Utilities;using TheTechIdea.Beep.Utilities;

using TheTechIdea.Beep.Vis;using TheTechIdea.Beep.Vis;

using TheTechIdea.Beep.WebAPI;using TheTechIdea.Beep.Workflow;

using TheTechIdea.Logger;

namespace TheTechIdea.Beep.Connectors.Pipedriveusing TheTechIdea.Util;

{

    /// <summary>namespace TheTechIdea.Beep.Connectors.PipedriveDataSource

    /// Pipedrive data source implementation using WebAPIDataSource as base class{

    /// </summary>    /// <summary>

    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pipedrive)]    /// Pipedrive CRM Data Source implementation using Pipedrive REST API

    public class PipedriveDataSource : WebAPIDataSource    /// </summary>

    {    public class PipedriveDataSource : IDataSource

        // -------- Fixed, known entities (Pipedrive API v1) --------    {

        private static readonly List<string> KnownEntities = new()        #region Configuration Classes

        {

            "deals",        /// <summary>

            "persons",        /// Configuration for Pipedrive connection

            "organizations",        /// </summary>

            "activities",        public class PipedriveConfig

            "users",        {

            "pipelines",            public string ApiToken { get; set; } = string.Empty;

            "stages",            public string BaseUrl { get; set; } = "https://api.pipedrive.com/v1";

            "products",        }

            "notes",

            "files",        /// <summary>

            "leads",        /// Pipedrive entity metadata

            "projects",        /// </summary>

            "filters",        public class PipedriveEntity

            "webhooks"        {

        };            public string EntityName { get; set; } = string.Empty;

            public string DisplayName { get; set; } = string.Empty;

        // entity -> (endpoint template, root path, required filter keys)            public string ApiEndpoint { get; set; } = string.Empty;

        // Pipedrive uses /v1/{entity} endpoints            public Dictionary<string, string> Fields { get; set; } = new();

        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map        }

            = new(StringComparer.OrdinalIgnoreCase)

            {        /// <summary>

                ["deals"] = ("/v1/deals", "data", Array.Empty<string>()),        /// Pipedrive API response wrapper

                ["persons"] = ("/v1/persons", "data", Array.Empty<string>()),        /// </summary>

                ["organizations"] = ("/v1/organizations", "data", Array.Empty<string>()),        public class PipedriveApiResponse<T>

                ["activities"] = ("/v1/activities", "data", Array.Empty<string>()),        {

                ["users"] = ("/v1/users", "data", Array.Empty<string>()),            public bool Success { get; set; }

                ["pipelines"] = ("/v1/pipelines", "data", Array.Empty<string>()),            public T? Data { get; set; }

                ["stages"] = ("/v1/stages", "data", Array.Empty<string>()),            public string? Error { get; set; }

                ["products"] = ("/v1/products", "data", Array.Empty<string>()),            public int? ErrorCode { get; set; }

                ["notes"] = ("/v1/notes", "data", Array.Empty<string>()),        }

                ["files"] = ("/v1/files", "data", Array.Empty<string>()),

                ["leads"] = ("/v1/leads", "data", Array.Empty<string>()),        #endregion

                ["projects"] = ("/v1/projects", "data", Array.Empty<string>()),

                ["filters"] = ("/v1/filters", "data", Array.Empty<string>()),        #region Private Fields

                ["webhooks"] = ("/v1/webhooks", "data", Array.Empty<string>()),

            };        private readonly PipedriveConfig _config;

        private HttpClient? _httpClient;

        public PipedriveDataSource(        private readonly IDMEEditor _dmeEditor;

            string datasourcename,        private readonly IErrorsInfo _errorsInfo;

            IDMLogger logger,        private readonly IJsonLoader _jsonLoader;

            IDMEEditor dmeEditor,        private readonly IDMLogger _logger;

            DataSourceType databasetype,        private readonly IUtil _util;

            IErrorsInfo errorObject)        private ConnectionState _connectionState = ConnectionState.Closed;

            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)        private string _connectionString = string.Empty;

        {        private readonly Dictionary<string, PipedriveEntity> _entityCache = new();

            // Ensure WebAPI connection props exist (API Token configuration is provided externally)

            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)        #endregion

                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

        #region Constructor

            // Register fixed entities

            EntitiesNames = KnownEntities.ToList();        public PipedriveDataSource(string datasourcename, IDMEEditor dmeEditor, IDataConnection cn, IErrorsInfo per)

            Entities = EntitiesNames        {

                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })            DatasourceName = datasourcename;

                .ToList();            _dmeEditor = dmeEditor;

        }            _errorsInfo = per;

            _jsonLoader = new JsonLoader();

        // Return the fixed list (use 'new' if base is virtual; otherwise this hides the base)            _logger = new DMLogger();

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;            _util = new Util();

            _config = new PipedriveConfig();

        // -------------------- Overrides (same signatures) --------------------

            // Initialize connection properties

        // Sync            Dataconnection = cn;

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)            if (cn != null)

        {            {

            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();                _connectionString = cn.ConnectionString;

            return data ?? Array.Empty<object>();                ParseConnectionString();

        }            }



        // Async            // Initialize HTTP client

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)            var handler = new HttpClientHandler();

        {            _httpClient = new HttpClient(handler);

            if (!Map.TryGetValue(EntityName, out var m))            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BeepDataConnector/1.0");

                throw new InvalidOperationException($"Unknown Pipedrive entity '{EntityName}'.");        }



            var q = FiltersToQuery(Filter);        #endregion

            RequireFilters(EntityName, q, m.requiredFilters);

        #region IDataSource Implementation

            var endpoint = ResolveEndpoint(m.endpoint, q);

        public string DatasourceName { get; set; } = string.Empty;

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);        public string DatasourceType { get; set; } = "Pipedrive";

            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();        public DatasourceCategory Category { get; set; } = DatasourceCategory.CRM;

        public IDataConnection? Dataconnection { get; set; }

            return ExtractArray(resp, m.root);        public object? DatasourceConnection { get; set; }

        }        public ConnectionState ConnectionStatus => _connectionState;

        public bool InMemory { get; set; } = false;

        // Paged (Pipedrive uses start and limit parameters)        public List<string> EntitiesNames { get; set; } = new();

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)        public List<EntityStructure> Entities { get; set; } = new();

        {        public IDMLogger Logger => _logger;

            if (!Map.TryGetValue(EntityName, out var m))        public IErrorsInfo ErrorObject => _errorsInfo;

                throw new InvalidOperationException($"Unknown Pipedrive entity '{EntityName}'.");        public IUtil util => _util;

        public IJsonLoader jsonLoader => _jsonLoader;

            var q = FiltersToQuery(filter);        public IDMEEditor DMEEditor => _dmeEditor;

            RequireFilters(EntityName, q, m.requiredFilters);

        public async Task<bool> ConnectAsync()

            // Pipedrive pagination        {

            q["limit"] = Math.Max(1, Math.Min(pageSize, 500)).ToString(); // Pipedrive max is 500            try

            if (pageNumber > 1)            {

            {                _logger.WriteLog($"Connecting to Pipedrive: {_config.BaseUrl}");

                q["start"] = ((pageNumber - 1) * pageSize).ToString();

            }                // Test connection by getting user info

                var testResponse = await _httpClient.GetAsync($"{_config.BaseUrl}/users?api_token={_config.ApiToken}");

            var endpoint = ResolveEndpoint(m.endpoint, q);                if (testResponse.IsSuccessStatusCode)

                {

            var finalResp = CallPipedrive(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();                    var responseContent = await testResponse.Content.ReadAsStringAsync();

            var items = ExtractArray(finalResp, m.root);                    var apiResponse = JsonSerializer.Deserialize<PipedriveApiResponse<JsonElement>>(responseContent);



            return new PagedResult                    if (apiResponse?.Success == true)

            {                    {

                Data = items,                        _connectionState = ConnectionState.Open;

                PageNumber = Math.Max(1, pageNumber),                        DatasourceConnection = _httpClient;

                PageSize = pageSize,                        _logger.WriteLog("Successfully connected to Pipedrive");

                TotalRecords = items.Count, // Pipedrive provides additional_data.pagination.more_items_in_collection                        return true;

                TotalPages = pageNumber, // Conservative estimate                    }

                HasPreviousPage = pageNumber > 1,                }

                HasNextPage = items.Count == pageSize // Assume more data if we got a full page

            };                var errorContent = await testResponse.Content.ReadAsStringAsync();

        }                _errorsInfo.AddError("Pipedrive", $"Connection test failed: {testResponse.StatusCode} - {errorContent}");

                return false;

        // ---------------------------- helpers ----------------------------            }

            catch (Exception ex)

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)            {

        {                _errorsInfo.AddError("Pipedrive", $"Connection failed: {ex.Message}", ex);

            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);                _logger.WriteLog($"Pipedrive connection error: {ex.Message}");

            if (filters == null) return q;                return false;

            foreach (var f in filters)            }

            {        }

                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;

                q[f.FieldName] = f.FilterValue?.ToString() ?? string.Empty;        public async Task<bool> DisconnectAsync()

            }        {

            return q;            try

        }            {

                _httpClient?.Dispose();

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)                _httpClient = null;

        {                _connectionState = ConnectionState.Closed;

            if (required == null || required.Length == 0) return;                DatasourceConnection = null;

            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();                _entityCache.Clear();

            if (missing.Count > 0)                _logger.WriteLog("Disconnected from Pipedrive");

                throw new ArgumentException($"Pipedrive entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");                return true;

        }            }

            catch (Exception ex)

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)            {

        {                _errorsInfo.AddError("Pipedrive", $"Disconnect failed: {ex.Message}", ex);

            // Substitute any {parameter} placeholders from filters if present                return false;

            foreach (var kvp in q)            }

            {        }

                if (template.Contains($"{{{kvp.Key}}}", StringComparison.Ordinal))

                {        public async Task<bool> OpenconnectionAsync()

                    template = template.Replace($"{{{kvp.Key}}}", Uri.EscapeDataString(kvp.Value));        {

                }            return await ConnectAsync();

            }        }

            return template;

        }        public async Task<bool> CloseconnectionAsync()

        {

        private async Task<HttpResponseMessage> CallPipedrive(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)            return await DisconnectAsync();

            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);        }



        // Extracts "data" (array) into a List<object> (Dictionary<string,object> per item).        public async Task<List<string>> GetEntitiesNamesAsync()

        // If root is null, wraps whole payload as a single object.        {

        private static List<object> ExtractArray(HttpResponseMessage resp, string root)            try

        {            {

            var list = new List<object>();                if (_httpClient == null)

            if (resp == null) return list;                    throw new InvalidOperationException("Not connected to Pipedrive");



            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();                // Get available entities from Pipedrive

            using var doc = JsonDocument.Parse(json);                var entities = await GetPipedriveEntitiesAsync();

                EntitiesNames = entities.Select(e => e.EntityName).ToList();

            JsonElement node = doc.RootElement;                return EntitiesNames;

            }

            if (!string.IsNullOrWhiteSpace(root))            catch (Exception ex)

            {            {

                if (!node.TryGetProperty(root, out node))                _errorsInfo.AddError("Pipedrive", $"Failed to get entities: {ex.Message}", ex);

                    return list; // no "data" -> empty                return new List<string>();

            }            }

        }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public async Task<List<EntityStructure>> GetEntityStructuresAsync(bool refresh = false)

            if (node.ValueKind == JsonValueKind.Array)        {

            {            try

                list.Capacity = node.GetArrayLength();            {

                foreach (var el in node.EnumerateArray())                if (_httpClient == null)

                {                    throw new InvalidOperationException("Not connected to Pipedrive");

                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);

                    if (obj != null) list.Add(obj);                if (!refresh && Entities.Any())

                }                    return Entities;

            }

            else if (node.ValueKind == JsonValueKind.Object)                var pipedriveEntities = await GetPipedriveEntitiesAsync();

            {                Entities = new List<EntityStructure>();

                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);

                if (obj != null) list.Add(obj);                foreach (var entity in pipedriveEntities)

            }                {

                    var structure = new EntityStructure

            return list;                    {

        }                        EntityName = entity.EntityName,

    }                        DisplayName = entity.DisplayName,

}                        SchemaName = "Pipedrive",
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
