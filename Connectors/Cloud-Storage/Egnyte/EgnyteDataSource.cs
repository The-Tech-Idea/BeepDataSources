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

namespace TheTechIdea.Beep.Connectors.Egnyte
{
    /// <summary>
    /// Egnyte data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, users, and metadata operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte)]
    public class EgnyteDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Egnyte API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://apidemo.egnyte.com/pubapi/v1/fs/{path}",
            ["file_info"] = "https://apidemo.egnyte.com/pubapi/v1/fs/{path}",
            ["file_content"] = "https://apidemo.egnyte.com/pubapi/v1/fs-content/{path}",
            ["file_versions"] = "https://apidemo.egnyte.com/pubapi/v1/fs-versions/{path}",
            // Folder operations
            ["folders"] = "https://apidemo.egnyte.com/pubapi/v1/fs/{path}",
            ["folder_items"] = "https://apidemo.egnyte.com/pubapi/v1/fs/{path}",
            // User operations
            ["users"] = "https://apidemo.egnyte.com/pubapi/v1/users",
            ["user_info"] = "https://apidemo.egnyte.com/pubapi/v1/users/{user_id}",
            ["current_user"] = "https://apidemo.egnyte.com/pubapi/v1/userinfo",
            // Group operations
            ["groups"] = "https://apidemo.egnyte.com/pubapi/v1/groups",
            ["group_info"] = "https://apidemo.egnyte.com/pubapi/v1/groups/{group_id}",
            // Search operations
            ["search"] = "https://apidemo.egnyte.com/pubapi/v1/search",
            // Link operations
            ["links"] = "https://apidemo.egnyte.com/pubapi/v1/links",
            ["link_info"] = "https://apidemo.egnyte.com/pubapi/v1/links/{link_id}",
            // Audit operations
            ["audit"] = "https://apidemo.egnyte.com/pubapi/v1/audit",
            // Permission operations
            ["permissions"] = "https://apidemo.egnyte.com/pubapi/v1/perms/{path}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require path
            ["files"] = new[] { "path" },
            ["file_info"] = new[] { "path" },
            ["file_content"] = new[] { "path" },
            ["file_versions"] = new[] { "path" },
            // Folder operations require path
            ["folders"] = new[] { "path" },
            ["folder_items"] = new[] { "path" },
            // User operations may require user_id for specific user
            ["users"] = Array.Empty<string>(),
            ["user_info"] = new[] { "user_id" },
            ["current_user"] = Array.Empty<string>(),
            // Group operations may require group_id for specific group
            ["groups"] = Array.Empty<string>(),
            ["group_info"] = new[] { "group_id" },
            // Search operations don't require filters (query can be optional)
            ["search"] = Array.Empty<string>(),
            // Link operations may require link_id for specific link
            ["links"] = Array.Empty<string>(),
            ["link_info"] = new[] { "link_id" },
            // Audit operations don't require filters
            ["audit"] = Array.Empty<string>(),
            // Permission operations require path
            ["permissions"] = new[] { "path" }
        };

        public EgnyteDataSource(
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
                throw new InvalidOperationException($"Unknown Egnyte entity '{EntityName}'.");

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
                throw new ArgumentException($"Egnyte entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle path
            if (result.Contains("{path}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
                    path = "/Shared"; // Default to root shared folder
                result = result.Replace("{path}", Uri.EscapeDataString(path));
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

            // Handle link_id
            if (result.Contains("{link_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("link_id", out var linkId) || string.IsNullOrWhiteSpace(linkId))
                    throw new ArgumentException("Missing required 'link_id' filter for this endpoint.");
                result = result.Replace("{link_id}", Uri.EscapeDataString(linkId));
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
