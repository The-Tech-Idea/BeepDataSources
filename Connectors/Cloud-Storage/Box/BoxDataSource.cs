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

namespace TheTechIdea.Beep.Connectors.Box
{
    /// <summary>
    /// Box data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, users, groups, and metadata operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box)]
    public class BoxDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Box API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://api.box.com/2.0/folders/{folder_id}/items",
            ["file_info"] = "https://api.box.com/2.0/files/{file_id}",
            ["file_content"] = "https://api.box.com/2.0/files/{file_id}/content",
            ["file_versions"] = "https://api.box.com/2.0/files/{file_id}/versions",
            // Folder operations
            ["folders"] = "https://api.box.com/2.0/folders/{folder_id}",
            ["folder_items"] = "https://api.box.com/2.0/folders/{folder_id}/items",
            // User operations
            ["users"] = "https://api.box.com/2.0/users",
            ["user_info"] = "https://api.box.com/2.0/users/{user_id}",
            ["current_user"] = "https://api.box.com/2.0/users/me",
            // Group operations
            ["groups"] = "https://api.box.com/2.0/groups",
            ["group_info"] = "https://api.box.com/2.0/groups/{group_id}",
            // Shared link operations
            ["shared_links"] = "https://api.box.com/2.0/files/{file_id}#shared_link",
            // Metadata operations
            ["metadata"] = "https://api.box.com/2.0/files/{file_id}/metadata/global/properties",
            // Webhook operations
            ["webhooks"] = "https://api.box.com/2.0/webhooks",
            ["webhook_info"] = "https://api.box.com/2.0/webhooks/{webhook_id}",
            // Search operations
            ["search"] = "https://api.box.com/2.0/search"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require folder_id or file_id
            ["files"] = new[] { "folder_id" },
            ["file_info"] = new[] { "file_id" },
            ["file_content"] = new[] { "file_id" },
            ["file_versions"] = new[] { "file_id" },
            // Folder operations require folder_id
            ["folders"] = new[] { "folder_id" },
            ["folder_items"] = new[] { "folder_id" },
            // User operations may require user_id for specific user
            ["users"] = Array.Empty<string>(),
            ["user_info"] = new[] { "user_id" },
            ["current_user"] = Array.Empty<string>(),
            // Group operations may require group_id for specific group
            ["groups"] = Array.Empty<string>(),
            ["group_info"] = new[] { "group_id" },
            // Shared link operations require file_id
            ["shared_links"] = new[] { "file_id" },
            // Metadata operations require file_id
            ["metadata"] = new[] { "file_id" },
            // Webhook operations may require webhook_id for specific webhook
            ["webhooks"] = Array.Empty<string>(),
            ["webhook_info"] = new[] { "webhook_id" },
            // Search operations don't require filters (query can be optional)
            ["search"] = Array.Empty<string>()
        };

        public BoxDataSource(
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
                throw new InvalidOperationException($"Unknown Box entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "entries");
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
                throw new ArgumentException($"Box entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle folder_id
            if (result.Contains("{folder_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("folder_id", out var folderId) || string.IsNullOrWhiteSpace(folderId))
                    folderId = "0"; // Default to root folder
                result = result.Replace("{folder_id}", Uri.EscapeDataString(folderId));
            }

            // Handle file_id
            if (result.Contains("{file_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("file_id", out var fileId) || string.IsNullOrWhiteSpace(fileId))
                    throw new ArgumentException("Missing required 'file_id' filter for this endpoint.");
                result = result.Replace("{file_id}", Uri.EscapeDataString(fileId));
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

            // Handle webhook_id
            if (result.Contains("{webhook_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("webhook_id", out var webhookId) || string.IsNullOrWhiteSpace(webhookId))
                    throw new ArgumentException("Missing required 'webhook_id' filter for this endpoint.");
                result = result.Replace("{webhook_id}", Uri.EscapeDataString(webhookId));
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
