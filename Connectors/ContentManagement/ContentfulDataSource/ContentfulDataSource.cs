using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Contentful.Models;

namespace TheTechIdea.Beep.Connectors.Contentful
{
    /// <summary>
    /// Contentful data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful)]
    public class ContentfulDataSource : WebAPIDataSource
    {
        // Contentful API endpoints
        private const string CDA_BASE_URL = "https://cdn.contentful.com";
        private const string CMA_BASE_URL = "https://api.contentful.com";

        // Entity endpoints mapping for Contentful APIs
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Content Delivery API (CDA) - Read operations
            ["entries"] = "spaces/{spaceId}/environments/{environmentId}/entries",
            ["entries.get"] = "spaces/{spaceId}/environments/{environmentId}/entries/{entryId}",
            ["contenttypes"] = "spaces/{spaceId}/environments/{environmentId}/content_types",
            ["contenttypes.get"] = "spaces/{spaceId}/environments/{environmentId}/content_types/{contentTypeId}",
            ["assets"] = "spaces/{spaceId}/environments/{environmentId}/assets",
            ["assets.get"] = "spaces/{spaceId}/environments/{environmentId}/assets/{assetId}",

            // Content Management API (CMA) - Write operations
            ["entries.create"] = "spaces/{spaceId}/environments/{environmentId}/entries",
            ["entries.update"] = "spaces/{spaceId}/environments/{environmentId}/entries/{entryId}",
            ["entries.delete"] = "spaces/{spaceId}/environments/{environmentId}/entries/{entryId}",
            ["assets.create"] = "spaces/{spaceId}/environments/{environmentId}/assets",
            ["assets.update"] = "spaces/{spaceId}/environments/{environmentId}/assets/{assetId}",
            ["assets.delete"] = "spaces/{spaceId}/environments/{environmentId}/assets/{assetId}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["entries.get"] = new[] { "entryId" },
            ["contenttypes.get"] = new[] { "contentTypeId" },
            ["assets.get"] = new[] { "assetId" },
            ["entries.update"] = new[] { "entryId" },
            ["entries.delete"] = new[] { "entryId" },
            ["assets.update"] = new[] { "assetId" },
            ["assets.delete"] = new[] { "assetId" }
        };

        // Contentful-specific properties
        private string _spaceId;
        private string _environmentId;
        private string _accessToken;
        private string _managementToken;

        public ContentfulDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();

            // Initialize Contentful-specific properties from connection
            InitializeContentfulProperties();
        }

        private void InitializeContentfulProperties()
        {
            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                _spaceId = webApiProps.Headers?.FirstOrDefault(h => h.Key.Equals("X-Contentful-Space-Id", StringComparison.OrdinalIgnoreCase)).Value
                          ?? webApiProps.Parameters?.FirstOrDefault(p => p.Key.Equals("spaceId", StringComparison.OrdinalIgnoreCase)).Value;

                _environmentId = webApiProps.Parameters?.FirstOrDefault(p => p.Key.Equals("environmentId", StringComparison.OrdinalIgnoreCase)).Value ?? "master";

                _accessToken = webApiProps.Headers?.FirstOrDefault(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).Value?
                    .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

                _managementToken = webApiProps.Parameters?.FirstOrDefault(p => p.Key.Equals("managementToken", StringComparison.OrdinalIgnoreCase)).Value;
            }
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // Override to add Contentful-specific headers
        protected override HttpClient CreateHttpClient()
        {
            var client = base.CreateHttpClient();

            if (!string.IsNullOrEmpty(_accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            client.DefaultRequestHeaders.Add("X-Contentful-Space-Id", _spaceId);

            return client;
        }

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Contentful entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL - use CDA for read operations, CMA for write operations
            var baseUrl = IsReadOperation(EntityName) ? CDA_BASE_URL : CMA_BASE_URL;
            var url = $"{baseUrl}/{endpoint}";
            url = ReplacePlaceholders(url, q);

            // Add query parameters
            var queryParams = BuildQueryParameters(q);
            if (!string.IsNullOrEmpty(queryParams))
                url += "?" + queryParams;

            // Make the request
            var response = await GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity
            return EntityName switch
            {
                "entries" => ParseEntries(json),
                "entries.get" => ParseEntry(json),
                "contenttypes" => ParseContentTypes(json),
                "contenttypes.get" => ParseContentType(json),
                "assets" => ParseAssets(json),
                "assets.get" => ParseAsset(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        private bool IsReadOperation(string entityName)
        {
            return !entityName.Contains(".create") && !entityName.Contains(".update") && !entityName.Contains(".delete");
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseEntityResponse(string entityName, string json)
        {
            return entityName.ToLower() switch
            {
                "entries" => ParseEntries(json),
                "entries.get" => ParseEntry(json),
                "contenttypes" => ParseContentTypes(json),
                "contenttypes.get" => ParseContentType(json),
                "assets" => ParseAssets(json),
                "assets.get" => ParseAsset(json),
                _ => Array.Empty<object>()
            };
        }

        private IEnumerable<object> ParseEntries(string json)
        {
            var response = JsonSerializer.Deserialize<ContentfulApiResponse<ContentfulEntry>>(json);
            return response?.Items ?? new List<ContentfulEntry>();
        }

        private IEnumerable<object> ParseEntry(string json)
        {
            var entry = JsonSerializer.Deserialize<ContentfulEntry>(json);
            return entry != null ? new[] { entry } : Array.Empty<ContentfulEntry>();
        }

        private IEnumerable<object> ParseContentTypes(string json)
        {
            var response = JsonSerializer.Deserialize<ContentfulApiResponse<ContentfulContentType>>(json);
            return response?.Items ?? new List<ContentfulContentType>();
        }

        private IEnumerable<object> ParseContentType(string json)
        {
            var contentType = JsonSerializer.Deserialize<ContentfulContentType>(json);
            return contentType != null ? new[] { contentType } : Array.Empty<ContentfulContentType>();
        }

        private IEnumerable<object> ParseAssets(string json)
        {
            var response = JsonSerializer.Deserialize<ContentfulApiResponse<ContentfulAsset>>(json);
            return response?.Items ?? new List<ContentfulAsset>();
        }

        private IEnumerable<object> ParseAsset(string json)
        {
            var asset = JsonSerializer.Deserialize<ContentfulAsset>(json);
            return asset != null ? new[] { asset } : Array.Empty<ContentfulAsset>();
        }

        // Helper methods
        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Contentful entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Replace Contentful-specific placeholders
            url = url.Replace("{spaceId}", _spaceId ?? throw new InvalidOperationException("Space ID is required"));
            url = url.Replace("{environmentId}", _environmentId ?? "master");

            // Replace entity-specific placeholders
            if (url.Contains("{entryId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("entryId", out var entryId) || string.IsNullOrWhiteSpace(entryId))
                    throw new ArgumentException("Missing required 'entryId' filter for this endpoint.");
                url = url.Replace("{entryId}", Uri.EscapeDataString(entryId));
            }

            if (url.Contains("{contentTypeId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("contentTypeId", out var contentTypeId) || string.IsNullOrWhiteSpace(contentTypeId))
                    throw new ArgumentException("Missing required 'contentTypeId' filter for this endpoint.");
                url = url.Replace("{contentTypeId}", Uri.EscapeDataString(contentTypeId));
            }

            if (url.Contains("{assetId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("assetId", out var assetId) || string.IsNullOrWhiteSpace(assetId))
                    throw new ArgumentException("Missing required 'assetId' filter for this endpoint.");
                url = url.Replace("{assetId}", Uri.EscapeDataString(assetId));
            }

            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) &&
                    !kvp.Key.Contains("{") && !kvp.Key.Contains("}") &&
                    !kvp.Key.Equals("entryId", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Key.Equals("contentTypeId", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Key.Equals("assetId", StringComparison.OrdinalIgnoreCase))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "ContentfulEntry",
            PointType = EnumPointType.Function,
            Name = "GetEntries",
            Caption = "Get Contentful Entries",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulEntry>"
        )]
        public IEnumerable<ContentfulEntry> GetEntries()
        {
            return GetEntity("entries", null).Cast<ContentfulEntry>();
        }

        [CommandAttribute(
            ObjectType = "ContentfulEntry",
            PointType = EnumPointType.Function,
            Name = "GetEntry",
            Caption = "Get Contentful Entry by ID",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulEntry>"
        )]
        public IEnumerable<ContentfulEntry> GetEntry(string entryId)
        {
            return GetEntity("entries.get", new List<AppFilter> { new AppFilter { FieldName = "entryId", FilterValue = entryId } }).Cast<ContentfulEntry>();
        }

        [CommandAttribute(
            ObjectType = "ContentfulContentType",
            PointType = EnumPointType.Function,
            Name = "GetContentTypes",
            Caption = "Get Contentful Content Types",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulContentType>"
        )]
        public IEnumerable<ContentfulContentType> GetContentTypes()
        {
            return GetEntity("contenttypes", null).Cast<ContentfulContentType>();
        }

        [CommandAttribute(
            ObjectType = "ContentfulContentType",
            PointType = EnumPointType.Function,
            Name = "GetContentType",
            Caption = "Get Contentful Content Type by ID",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulContentType>"
        )]
        public IEnumerable<ContentfulContentType> GetContentType(string contentTypeId)
        {
            return GetEntity("contenttypes.get", new List<AppFilter> { new AppFilter { FieldName = "contentTypeId", FilterValue = contentTypeId } }).Cast<ContentfulContentType>();
        }

        [CommandAttribute(
            ObjectType = "ContentfulAsset",
            PointType = EnumPointType.Function,
            Name = "GetAssets",
            Caption = "Get Contentful Assets",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulAsset>"
        )]
        public IEnumerable<ContentfulAsset> GetAssets()
        {
            return GetEntity("assets", null).Cast<ContentfulAsset>();
        }

        [CommandAttribute(
            ObjectType = "ContentfulAsset",
            PointType = EnumPointType.Function,
            Name = "GetAsset",
            Caption = "Get Contentful Asset by ID",
            ClassName = "ContentfulDataSource",
            misc = "ReturnType: IEnumerable<ContentfulAsset>"
        )]
        public IEnumerable<ContentfulAsset> GetAsset(string assetId)
        {
            return GetEntity("assets.get", new List<AppFilter> { new AppFilter { FieldName = "assetId", FilterValue = assetId } }).Cast<ContentfulAsset>();
        }

        // POST methods for creating entities (CMA)
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulEntry", Name = "CreateEntry", Caption = "Create Contentful Entry", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "contentful.png", misc = "ReturnType: IEnumerable<ContentfulEntry>")]
        public async Task<IEnumerable<ContentfulEntry>> CreateEntryAsync(ContentfulEntry entry)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/entries";
                var result = await PostAsync(url, entry);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdEntry = JsonSerializer.Deserialize<ContentfulEntry>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<ContentfulEntry> { createdEntry }.Select(e => e.Attach<ContentfulEntry>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating entry: {ex.Message}");
            }
            return new List<ContentfulEntry>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulAsset", Name = "CreateAsset", Caption = "Create Contentful Asset", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "contentful.png", misc = "ReturnType: IEnumerable<ContentfulAsset>")]
        public async Task<IEnumerable<ContentfulAsset>> CreateAssetAsync(ContentfulAsset asset)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/assets";
                var result = await PostAsync(url, asset);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdAsset = JsonSerializer.Deserialize<ContentfulAsset>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<ContentfulAsset> { createdAsset }.Select(a => a.Attach<ContentfulAsset>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating asset: {ex.Message}");
            }
            return new List<ContentfulAsset>();
        }

        // PUT methods for updating entities (CMA)
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulEntry", Name = "UpdateEntry", Caption = "Update Contentful Entry", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "contentful.png", misc = "ReturnType: IEnumerable<ContentfulEntry>")]
        public async Task<IEnumerable<ContentfulEntry>> UpdateEntryAsync(string entryId, ContentfulEntry entry)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/entries/{entryId}";
                var result = await PutAsync(url, entry);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedEntry = JsonSerializer.Deserialize<ContentfulEntry>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<ContentfulEntry> { updatedEntry }.Select(e => e.Attach<ContentfulEntry>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating entry: {ex.Message}");
            }
            return new List<ContentfulEntry>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulAsset", Name = "UpdateAsset", Caption = "Update Contentful Asset", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "contentful.png", misc = "ReturnType: IEnumerable<ContentfulAsset>")]
        public async Task<IEnumerable<ContentfulAsset>> UpdateAssetAsync(string assetId, ContentfulAsset asset)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/assets/{assetId}";
                var result = await PutAsync(url, asset);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedAsset = JsonSerializer.Deserialize<ContentfulAsset>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<ContentfulAsset> { updatedAsset }.Select(a => a.Attach<ContentfulAsset>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating asset: {ex.Message}");
            }
            return new List<ContentfulAsset>();
        }

        // DELETE methods for deleting entities (CMA)
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulEntry", Name = "DeleteEntry", Caption = "Delete Contentful Entry", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "contentful.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteEntryAsync(string entryId)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/entries/{entryId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting entry: {ex.Message}");
                return false;
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Contentful, PointType = EnumPointType.Function, ObjectType = "ContentfulAsset", Name = "DeleteAsset", Caption = "Delete Contentful Asset", ClassType = "ContentfulDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "contentful.png", misc = "ReturnType: bool")]
        public async Task<bool> DeleteAssetAsync(string assetId)
        {
            try
            {
                var url = $"{CMA_BASE_URL}/spaces/{_spaceId}/environments/{_environmentId}/assets/{assetId}";
                var result = await DeleteAsync(url);
                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error deleting asset: {ex.Message}");
                return false;
            }
        }
    }
}