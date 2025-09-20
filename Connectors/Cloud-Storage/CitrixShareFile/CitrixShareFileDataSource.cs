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

namespace TheTechIdea.Beep.Connectors.CitrixShareFile
{
    /// <summary>
    /// Citrix ShareFile data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, users, and collaboration operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile)]
    public class CitrixShareFileDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Citrix ShareFile API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://api.sharefile.com/sf/v3/Items({item_id})",
            ["file_download"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Download",
            ["file_upload"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Upload",
            ["file_versions"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Version",
            // Folder operations
            ["folders"] = "https://api.sharefile.com/sf/v3/Items({item_id})",
            ["folder_children"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Children",
            ["root_folder"] = "https://api.sharefile.com/sf/v3/Items",
            // User operations
            ["users"] = "https://api.sharefile.com/sf/v3/Users",
            ["user_info"] = "https://api.sharefile.com/sf/v3/Users({user_id})",
            ["current_user"] = "https://api.sharefile.com/sf/v3/Users/me",
            // Group operations
            ["groups"] = "https://api.sharefile.com/sf/v3/Groups",
            ["group_info"] = "https://api.sharefile.com/sf/v3/Groups({group_id})",
            // Share operations
            ["shares"] = "https://api.sharefile.com/sf/v3/Shares",
            ["share_info"] = "https://api.sharefile.com/sf/v3/Shares({share_id})",
            ["share_requests"] = "https://api.sharefile.com/sf/v3/ShareRequests",
            // Search operations
            ["search"] = "https://api.sharefile.com/sf/v3/Items/Search",
            // Favorite operations
            ["favorites"] = "https://api.sharefile.com/sf/v3/Favorites",
            // Connector operations
            ["connectors"] = "https://api.sharefile.com/sf/v3/ConnectorGroups",
            ["connector_info"] = "https://api.sharefile.com/sf/v3/ConnectorGroups({connector_id})",
            // Account operations
            ["account"] = "https://api.sharefile.com/sf/v3/Accounts",
            // Access control operations
            ["access_controls"] = "https://api.sharefile.com/sf/v3/Items({item_id})/AccessControls"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require item_id
            ["files"] = new[] { "item_id" },
            ["file_download"] = new[] { "item_id" },
            ["file_upload"] = new[] { "item_id" },
            ["file_versions"] = new[] { "item_id" },
            // Folder operations require item_id
            ["folders"] = new[] { "item_id" },
            ["folder_children"] = new[] { "item_id" },
            ["access_controls"] = new[] { "item_id" },
            // Root folder doesn't require filters
            ["root_folder"] = Array.Empty<string>(),
            // User operations may require user_id for specific user
            ["users"] = Array.Empty<string>(),
            ["user_info"] = new[] { "user_id" },
            ["current_user"] = Array.Empty<string>(),
            // Group operations may require group_id for specific group
            ["groups"] = Array.Empty<string>(),
            ["group_info"] = new[] { "group_id" },
            // Share operations may require share_id for specific share
            ["shares"] = Array.Empty<string>(),
            ["share_info"] = new[] { "share_id" },
            ["share_requests"] = Array.Empty<string>(),
            // Search operations don't require filters (query can be optional)
            ["search"] = Array.Empty<string>(),
            // Favorites don't require filters
            ["favorites"] = Array.Empty<string>(),
            // Connector operations may require connector_id for specific connector
            ["connectors"] = Array.Empty<string>(),
            ["connector_info"] = new[] { "connector_id" },
            // Account operations don't require filters
            ["account"] = Array.Empty<string>()
        };

        public CitrixShareFileDataSource(
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
                throw new InvalidOperationException($"Unknown Citrix ShareFile entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, null);
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
                throw new ArgumentException($"Citrix ShareFile entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

            // Handle user_id
            if (result.Contains("{user_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("user_id", out var userId) || string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("Missing required 'user_id' filter for this endpoint.");
                result = result.Replace("{user_id}", Uri.EscapeDataString(userId));
            }

            // Handle group_id
            if (result.Contains("{group_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("group_id", out var groupId) || string.IsNullOrWhiteSpace(groupId))
                    throw new ArgumentException("Missing required 'group_id' filter for this endpoint.");
                result = result.Replace("{group_id}", Uri.EscapeDataString(groupId));
            }

            // Handle share_id
            if (result.Contains("{share_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("share_id", out var shareId) || string.IsNullOrWhiteSpace(shareId))
                    throw new ArgumentException("Missing required 'share_id' filter for this endpoint.");
                result = result.Replace("{share_id}", Uri.EscapeDataString(shareId));
            }

            // Handle connector_id
            if (result.Contains("{connector_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("connector_id", out var connectorId) || string.IsNullOrWhiteSpace(connectorId))
                    throw new ArgumentException("Missing required 'connector_id' filter for this endpoint.");
                result = result.Replace("{connector_id}", Uri.EscapeDataString(connectorId));
            }

            return result;
        }

        // Extracts array from response into a List<object> (Dictionary<string,object> per item).
        private static List<object> ExtractArray(HttpResponseMessage resp, string? root)
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
