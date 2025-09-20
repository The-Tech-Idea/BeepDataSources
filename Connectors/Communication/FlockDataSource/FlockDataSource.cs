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

namespace TheTechIdea.Beep.Connectors.Communication.Flock
{
    /// <summary>
    /// Flock API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Flock API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock)]
    public class FlockDataSource : WebAPIDataSource
    {
        // Supported Flock entities -> Flock endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("users.list", "users", Array.Empty<string>()),
                ["user"] = ("users.getInfo", "user", new[] { "userId" }),
                ["user_presence"] = ("users.getPresence", "presence", new[] { "userId" }),
                ["groups"] = ("groups.list", "groups", Array.Empty<string>()),
                ["group"] = ("groups.getInfo", "group", new[] { "groupId" }),
                ["group_members"] = ("groups.getMembers", "members", new[] { "groupId" }),
                ["channels"] = ("channels.list", "channels", Array.Empty<string>()),
                ["channel"] = ("channels.getInfo", "channel", new[] { "channelId" }),
                ["channel_members"] = ("channels.getMembers", "members", new[] { "channelId" }),
                ["messages"] = ("channels.getMessages", "messages", new[] { "channelId" }),
                ["message"] = ("chat.getMessage", "message", new[] { "messageId" }),
                ["message_reactions"] = ("chat.getReactions", "reactions", new[] { "messageId" }),
                ["message_replies"] = ("chat.getThreadMessages", "messages", new[] { "messageId" }),
                ["files"] = ("channels.getFiles", "files", new[] { "channelId" }),
                ["file"] = ("chat.getFile", "file", new[] { "fileId" }),
                ["contacts"] = ("contacts.list", "contacts", Array.Empty<string>()),
                ["contact"] = ("contacts.getInfo", "contact", new[] { "contactId" }),
                ["apps"] = ("apps.list", "apps", Array.Empty<string>()),
                ["app"] = ("apps.getInfo", "app", new[] { "appId" }),
                ["webhooks"] = ("webhooks.list", "webhooks", Array.Empty<string>()),
                ["webhook"] = ("webhooks.getInfo", "webhook", new[] { "webhookId" }),
                ["tokens"] = ("tokens.list", "tokens", Array.Empty<string>()),
                ["token"] = ("tokens.getInfo", "token", new[] { "tokenId" })
            };

        public FlockDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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
                throw new InvalidOperationException($"Unknown Flock entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown Flock entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Flock API supports pagination with limit and offset
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Flock max is usually 100
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
                    throw new ArgumentException($"Flock entity '{entity}' requires '{req}' parameter in filters.");
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