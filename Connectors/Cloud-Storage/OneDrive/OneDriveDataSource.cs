using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace TheTechIdea.Beep.Connectors.OneDrive
{
    /// <summary>
    /// OneDrive data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, and drive operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive)]
    public class OneDriveDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Microsoft Graph API (OneDrive)
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Drive operations
            ["drives"] = "https://graph.microsoft.com/v1.0/me/drives",
            ["drive_details"] = "https://graph.microsoft.com/v1.0/me/drive",
            // Root operations
            ["root"] = "https://graph.microsoft.com/v1.0/me/drive/root",
            ["root_children"] = "https://graph.microsoft.com/v1.0/me/drive/root/children",
            // Item operations
            ["items"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}",
            ["item_children"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}/children",
            ["item_content"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}/content",
            // Search operations
            ["search"] = "https://graph.microsoft.com/v1.0/me/drive/search(q='{query}')",
            // Recent files
            ["recent"] = "https://graph.microsoft.com/v1.0/me/drive/recent",
            // Shared with me
            ["shared"] = "https://graph.microsoft.com/v1.0/me/drive/sharedWithMe",
            // Special folders
            ["documents"] = "https://graph.microsoft.com/v1.0/me/drive/special/documents",
            ["photos"] = "https://graph.microsoft.com/v1.0/me/drive/special/photos",
            ["cameraroll"] = "https://graph.microsoft.com/v1.0/me/drive/special/cameraroll"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // Drive operations don't require filters
            ["drives"] = Array.Empty<string>(),
            ["drive_details"] = Array.Empty<string>(),
            // Root operations don't require filters
            ["root"] = Array.Empty<string>(),
            ["root_children"] = Array.Empty<string>(),
            // Item operations require item_id
            ["items"] = new[] { "item_id" },
            ["item_children"] = new[] { "item_id" },
            ["item_content"] = new[] { "item_id" },
            // Search operations require query
            ["search"] = new[] { "query" },
            // Recent and shared don't require filters
            ["recent"] = Array.Empty<string>(),
            ["shared"] = Array.Empty<string>(),
            // Special folders don't require filters
            ["documents"] = Array.Empty<string>(),
            ["photos"] = Array.Empty<string>(),
            ["cameraroll"] = Array.Empty<string>()
        };

        public OneDriveDataSource(
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
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

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
                throw new InvalidOperationException($"Unknown OneDrive entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "value");
        }

        // ---------------------------- helpers ----------------------------

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
                throw new ArgumentException($"OneDrive entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle item_id
            if (result.Contains("{item_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("item_id", out var itemId) || string.IsNullOrWhiteSpace(itemId))
                    throw new ArgumentException("Missing required 'item_id' filter for this endpoint.");
                result = result.Replace("{item_id}", Uri.EscapeDataString(itemId));
            }

            // Handle query for search
            if (result.Contains("{query}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("query", out var query) || string.IsNullOrWhiteSpace(query))
                    throw new ArgumentException("Missing required 'query' filter for this endpoint.");
                result = result.Replace("{query}", Uri.EscapeDataString(query));
            }

            return result;
        }

        // Extracts array from response into a List<object> (Dictionary<string,object> per item).
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list; // no root -> empty
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (node.ValueKind == JsonValueKind.Array)
            {
                list.Capacity = node.GetArrayLength();
                foreach (var el in node.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }
    }
}
