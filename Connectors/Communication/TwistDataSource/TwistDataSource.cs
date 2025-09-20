using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Communication.Twist
{
    /// <summary>
    /// Twist API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Twist API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Twist)]
    public class TwistDataSource : WebAPIDataSource
    {
        // Supported Twist entities -> Twist endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["workspaces"] = ("api/v3/workspaces/get", "workspaces", Array.Empty<string>()),
                ["workspace"] = ("api/v3/workspaces/getone", "workspace", new[] { "id" }),
                ["workspace_users"] = ("api/v3/workspaces/get_users", "users", new[] { "id" }),
                ["workspace_groups"] = ("api/v3/workspaces/get_groups", "groups", new[] { "id" }),
                ["channels"] = ("api/v3/channels/get", "channels", Array.Empty<string>()),
                ["channel"] = ("api/v3/channels/getone", "channel", new[] { "id" }),
                ["channel_users"] = ("api/v3/channels/get_users", "users", new[] { "id" }),
                ["threads"] = ("api/v3/threads/get", "threads", Array.Empty<string>()),
                ["thread"] = ("api/v3/threads/getone", "thread", new[] { "id" }),
                ["messages"] = ("api/v3/messages/get", "messages", Array.Empty<string>()),
                ["message"] = ("api/v3/messages/getone", "message", new[] { "id" }),
                ["message_reactions"] = ("api/v3/messages/get_reactions", "reactions", new[] { "id" }),
                ["comments"] = ("api/v3/comments/get", "comments", Array.Empty<string>()),
                ["comment"] = ("api/v3/comments/getone", "comment", new[] { "id" }),
                ["users"] = ("api/v3/users/get", "users", Array.Empty<string>()),
                ["user"] = ("api/v3/users/getone", "user", new[] { "id" }),
                ["user_workspaces"] = ("api/v3/users/get_workspaces", "workspaces", new[] { "id" }),
                ["groups"] = ("api/v3/groups/get", "groups", Array.Empty<string>()),
                ["group"] = ("api/v3/groups/getone", "group", new[] { "id" }),
                ["group_users"] = ("api/v3/groups/get_users", "users", new[] { "id" }),
                ["integrations"] = ("api/v3/integrations/get", "integrations", Array.Empty<string>()),
                ["integration"] = ("api/v3/integrations/getone", "integration", new[] { "id" }),
                ["attachments"] = ("api/v3/attachments/get", "attachments", Array.Empty<string>()),
                ["attachment"] = ("api/v3/attachments/getone", "attachment", new[] { "id" }),
                ["notifications"] = ("api/v3/notifications/get", "notifications", Array.Empty<string>()),
                ["notification"] = ("api/v3/notifications/getone", "notification", new[] { "id" })
            };

        public TwistDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection != null && Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep your interface exactly
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // ---------------- overrides (same signatures) ----------------

        // sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Twist entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            using var resp = await GetAsync(m.endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Twist entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Twist API supports pagination with limit and offset
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Twist max is usually 100
            q["offset"] = ((pageNumber - 1) * pageSize).ToString();

            var resp = GetAsync(m.endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            // Basic pagination estimate
            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = pageNumber, // Can't determine total pages without count
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more if we got full page
            };
        }

        // -------------------------- helpers --------------------------

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
            foreach (var req in required)
            {
                if (!q.ContainsKey(req) || string.IsNullOrWhiteSpace(q[req]))
                    throw new ArgumentException($"Twist entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                foreach (var part in rootPath.Split('.'))
                {
                    if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node))
                        return list; // path not found
                }
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
                // wrap single object
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }
    }
}