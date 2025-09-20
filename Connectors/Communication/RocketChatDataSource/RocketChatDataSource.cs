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

namespace TheTechIdea.Beep.Connectors.Communication.RocketChat
{
    /// <summary>
    /// Rocket.Chat API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Rocket.Chat API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat)]
    public class RocketChatDataSource : WebAPIDataSource
    {
        // Supported Rocket.Chat entities -> Rocket.Chat endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("api/v1/users.list", "users", Array.Empty<string>()),
                ["user"] = ("api/v1/users.info", "user", new[] { "userId" }),
                ["channels"] = ("api/v1/channels.list", "channels", Array.Empty<string>()),
                ["channel"] = ("api/v1/channels.info", "channel", new[] { "roomId" }),
                ["channel_members"] = ("api/v1/channels.members", "members", new[] { "roomId" }),
                ["channel_messages"] = ("api/v1/channels.messages", "messages", new[] { "roomId" }),
                ["groups"] = ("api/v1/groups.list", "groups", Array.Empty<string>()),
                ["group"] = ("api/v1/groups.info", "group", new[] { "roomId" }),
                ["group_members"] = ("api/v1/groups.members", "members", new[] { "roomId" }),
                ["group_messages"] = ("api/v1/groups.messages", "messages", new[] { "roomId" }),
                ["im_list"] = ("api/v1/im.list", "ims", Array.Empty<string>()),
                ["im_messages"] = ("api/v1/im.messages", "messages", new[] { "roomId" }),
                ["im_history"] = ("api/v1/im.history", "messages", new[] { "roomId" }),
                ["rooms"] = ("api/v1/rooms.get", "rooms", Array.Empty<string>()),
                ["room"] = ("api/v1/rooms.info", "room", new[] { "roomId" }),
                ["subscriptions"] = ("api/v1/subscriptions.get", "subscriptions", Array.Empty<string>()),
                ["roles"] = ("api/v1/roles.list", "roles", Array.Empty<string>()),
                ["permissions"] = ("api/v1/permissions.list", "permissions", Array.Empty<string>()),
                ["settings"] = ("api/v1/settings", "settings", Array.Empty<string>()),
                ["statistics"] = ("api/v1/statistics", "statistics", Array.Empty<string>()),
                ["integrations"] = ("api/v1/integrations.list", "integrations", Array.Empty<string>()),
                ["webhooks"] = ("api/v1/integrations.list", "integrations", Array.Empty<string>())
            };

        public RocketChatDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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
                throw new InvalidOperationException($"Unknown Rocket.Chat entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown Rocket.Chat entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Rocket.Chat API supports pagination with count and offset
            q["count"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Rocket.Chat max is usually 100
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
                    throw new ArgumentException($"Rocket.Chat entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            // Rocket.Chat API always returns { success: bool, ... }
            if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.False)
            {
                // Optionally, pick up "error" for diagnostics; here we just return empty.
                return list;
            }

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