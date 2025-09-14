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

namespace TheTechIdea.Beep.Connectors.Instagram
{
    /// <summary>
    /// Instagram Data Source built on WebAPIDataSource (Graph API / Basic Display).
    /// Configure WebAPIConnectionProperties externally (Url like https://graph.facebook.com/v18.0/ and OAuth2/Bearer).
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Instagram)]
    public class InstagramDataSource : WebAPIDataSource
    {
        // Fixed, supported entities (Graph-like)
        private static readonly List<string> KnownEntities = new()
        {
            // Users / Accounts
            "me",                 // GET me?fields=...
            "me.accounts",        // GET me/accounts?fields=...
            "me.media",           // GET me/media?fields=...
            "me.stories",         // GET me/stories?fields=...
            "users.by_id",        // GET {id}?fields=...
            "users.media",        // GET {id}/media?fields=...
            "users.stories",      // GET {id}/stories?fields=...

            // Media / Comments / Insights
            "media.by_id",        // GET {id}?fields=...
            "media.children",     // GET {id}/children?fields=...
            "media.comments",     // GET {id}/comments?fields=...
            "comments.replies",   // GET {id}/replies?fields=...
            "media.insights"      // GET {id}/insights?metric=...&period=...  (Graph)
        };

        // entity -> (endpoint template, root property, required filters)
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["me"] = ("me", null, Array.Empty<string>()),
                ["me.accounts"] = ("me/accounts", "data", Array.Empty<string>()),
                ["me.media"] = ("me/media", "data", Array.Empty<string>()),
                ["me.stories"] = ("me/stories", "data", Array.Empty<string>()),

                ["users.by_id"] = ("{id}", null, new[] { "id" }),
                ["users.media"] = ("{id}/media", "data", new[] { "id" }),
                ["users.stories"] = ("{id}/stories", "data", new[] { "id" }),

                ["media.by_id"] = ("{id}", null, new[] { "id" }),
                ["media.children"] = ("{id}/children", "data", new[] { "id" }),
                ["media.comments"] = ("{id}/comments", "data", new[] { "id" }),
                ["comments.replies"] = ("{id}/replies", "data", new[] { "id" }),

                // insights require metric; period optional depending on metric
                ["media.insights"] = ("{id}/insights", "data", new[] { "id", "metric" })
            };

        public InstagramDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props (Url/Auth) exist (configure outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep the exact IDataSource signatures

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Instagram entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Support field selection (fields=...), paging (limit, after), metrics, etc. via AppFilter passthrough.
            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (cursor-based via paging.cursors.after)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Instagram entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Graph-style pagination: limit + after
            int limit = Math.Max(1, Math.Min(pageSize <= 0 ? 25 : pageSize, 100));
            q["limit"] = limit.ToString();

            string endpoint = ResolveEndpoint(m.endpoint, q);
            string after = q.TryGetValue("after", out var a) ? a : null;

            // step cursors up to requested page
            for (int i = 1; i < Math.Max(1, pageNumber); i++)
            {
                var stepResp = GetAsync(endpoint, MergeAfter(q, after)).ConfigureAwait(false).GetAwaiter().GetResult();
                after = GetNextAfter(stepResp);
                if (string.IsNullOrEmpty(after)) break;
            }

            var finalResp = GetAsync(endpoint, MergeAfter(q, after)).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);
            var nextAfter = GetNextAfter(finalResp);

            // We don’t get a grand total; best-effort counters
            int pageIdx = Math.Max(1, pageNumber);
            int pageCount = items.Count;
            int totalSoFar = (pageIdx - 1) * limit + pageCount;

            return new PagedResult
            {
                Data = items,
                PageNumber = pageIdx,
                PageSize = limit,
                TotalRecords = totalSoFar,
                TotalPages = string.IsNullOrEmpty(nextAfter) ? pageIdx : pageIdx + 1,
                HasPreviousPage = pageIdx > 1,
                HasNextPage = !string.IsNullOrEmpty(nextAfter)
            };
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
                throw new ArgumentException($"Instagram entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
                // keep 'id' in query; harmless
            }
            return template;
        }

        private static Dictionary<string, string> MergeAfter(Dictionary<string, string> q, string after)
        {
            var m = new Dictionary<string, string>(q, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(after)) m["after"] = after;
            else m.Remove("after");
            return m;
        }

        private async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await base.GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            // Graph responses often use { "data": [...] } plus "paging"
            JsonElement node = doc.RootElement;
            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list;
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

        private static string GetNextAfter(HttpResponseMessage resp)
        {
            if (resp == null) return null;
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("paging", out var paging) &&
                    paging.ValueKind == JsonValueKind.Object &&
                    paging.TryGetProperty("cursors", out var cursors) &&
                    cursors.ValueKind == JsonValueKind.Object &&
                    cursors.TryGetProperty("after", out var after) &&
                    after.ValueKind == JsonValueKind.String)
                {
                    var s = after.GetString();
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }
}
