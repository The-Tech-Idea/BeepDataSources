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
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Communication.Slack
{
    public class SlackDataSource : WebAPIDataSource
    {
        // Fixed Slack entities (your list)
        private static readonly List<string> KnownEntities = new()
        {
            "channels", "messages", "users", "files", "reactions", "teams", "groups",
            "im", "mpim", "bots", "apps", "auth", "conversations", "pins",
            "reminders", "search", "stars", "team", "usergroups"
        };

        // Map entity -> (endpoint, root path, default query parameters)
        // root supports dotted paths (e.g., "messages.matches")
        private static readonly Dictionary<string, (string endpoint, string root, IReadOnlyDictionary<string, string> defaults)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                // Core �conversations.*�
                ["conversations"] = ("conversations.list", "channels", Empty()),
                ["channels"] = ("conversations.list", "channels", Empty()),
                ["groups"] = ("conversations.list", "channels", Dict(("types", "private_channel"))),
                ["im"] = ("conversations.list", "channels", Dict(("types", "im"))),
                ["mpim"] = ("conversations.list", "channels", Dict(("types", "mpim"))),

                // History (requires channel in filters)
                ["messages"] = ("conversations.history", "messages", Empty()),

                // Users / files
                ["users"] = ("users.list", "members", Empty()),
                ["files"] = ("files.list", "files", Empty()),

                // Reactions / stars / pins
                ["reactions"] = ("reactions.list", "items", Empty()),   // may require user
                ["stars"] = ("stars.list", "items", Empty()),
                ["pins"] = ("pins.list", "items", Empty()),        // requires channel in filters

                // Reminders
                ["reminders"] = ("reminders.list", "reminders", Empty()),

                // Search
                ["search"] = ("search.messages", "messages.matches", Empty()),

                // Auth/team info (object payloads; we�ll wrap into single-element list)
                ["auth"] = ("auth.test", null, Empty()),
                ["team"] = ("team.info", "team", Empty()),         // root object �team�
                ["teams"] = ("team.billableInfo.list", "billable_info", Empty()), // may vary per plan

                // Bots / apps (availability depends on workspace & scopes)
                ["bots"] = ("bots.list", "bots", Empty()),
                ["apps"] = ("apps.permissions.scopes.list", "scopes", Empty()),

                // User groups
                ["usergroups"] = ("usergroups.list", "usergroups", Empty()),
            };

        public SlackDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            if (Dataconnection is not WebAPIDataConnection)
            {
                Dataconnection = new WebAPIDataConnection
                {
                    Logger = Logger,
                    ErrorObject = ErrorObject,
                    DMEEditor = DMEEditor
                };
            }

            if (Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
            {
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep the same signature as your IDataSource
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // --------------------- OVERRIDES (same signatures) ---------------------

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Slack entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            q["limit"] = Math.Max(1, pageSize).ToString();

            string cursor = q.TryGetValue("cursor", out var cur) ? cur : null;

            // walk cursors to the requested page
            for (int i = 1; i < Math.Max(1, pageNumber); i++)
            {
                var step = CallSlack(m.endpoint, MergeDefaults(m.defaults, q, cursor), default).ConfigureAwait(false).GetAwaiter().GetResult();
                cursor = GetNextCursor(step);
                if (string.IsNullOrEmpty(cursor)) break;
            }

            var finalResp = CallSlack(m.endpoint, MergeDefaults(m.defaults, q, cursor), default).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);
            var nextCur = GetNextCursor(finalResp);

            // We can�t know true totals; provide best-effort
            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = nextCur == null ? pageNumber : pageNumber + 1, // estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = !string.IsNullOrEmpty(nextCur)
            };
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Slack entity '{EntityName}'.");

            // Slack endpoints often need specific params (e.g., channel for history/pins)
            RequireParamsIfNeeded(EntityName, Filter);

            var q = MergeDefaults(m.defaults, FiltersToQuery(Filter), cursor: null);

            using var resp = await CallSlack(m.endpoint, q, default).ConfigureAwait(false);
            if (resp == null) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // -------------------------- helpers --------------------------

        private static IReadOnlyDictionary<string, string> Empty() => new Dictionary<string, string>();
        private static IReadOnlyDictionary<string, string> Dict(params (string key, string value)[] items)
            => items?.Length > 0
                ? new Dictionary<string, string>(items.ToDictionary(t => t.key, t => t.value), StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>();

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                // Slack expects strings; leave values as provided
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static Dictionary<string, string> MergeDefaults(IReadOnlyDictionary<string, string> defaults, Dictionary<string, string> user, string cursor)
        {
            var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (defaults != null)
                foreach (var kv in defaults) m[kv.Key] = kv.Value;

            if (user != null)
                foreach (var kv in user) m[kv.Key] = kv.Value;

            if (!string.IsNullOrWhiteSpace(cursor))
                m["cursor"] = cursor;

            return m;
        }

        private static void RequireParamsIfNeeded(string entity, List<AppFilter> filters)
        {
            bool Has(string name) => filters?.Any(f => string.Equals(f?.FieldName, name, StringComparison.OrdinalIgnoreCase)) == true;

            switch (entity.ToLowerInvariant())
            {
                case "messages":
                case "pins":
                    if (!Has("channel"))
                        throw new ArgumentException($"Slack entity '{entity}' requires a 'channel' parameter in filters.");
                    break;
                default:
                    break;
            }
        }

        // Calls Slack endpoint via base HTTP helper (auth/headers handled in base)
        private async Task<HttpResponseMessage> CallSlack(string endpoint, Dictionary<string, string> query, CancellationToken ct)
        {
            // Slack API uses GET for most list endpoints; base.GetAsync attaches Authorization header from ConnectionProps
            return await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);
        }

        // Get Slack�s next cursor (if present)
        private static string GetNextCursor(HttpResponseMessage resp)
        {
            if (resp == null) return null;
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("response_metadata", out var md) &&
                    md.ValueKind == JsonValueKind.Object &&
                    md.TryGetProperty("next_cursor", out var cur) &&
                    cur.ValueKind == JsonValueKind.String)
                {
                    var s = cur.GetString();
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        // Extract array/object(s) from Slack JSON using a dotted root path (or wrap whole object)
        private static List<object> ExtractArray(HttpResponseMessage resp, string rootPath)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            // Slack always returns { "ok": bool, ... }
            if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.ValueKind == JsonValueKind.False)
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
                // wrap single object (e.g., auth.test or team.info)
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }
    }
}
