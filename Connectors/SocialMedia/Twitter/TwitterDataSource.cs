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

namespace TheTechIdea.Beep.Connectors.Twitter
{
    /// <summary>
    /// Twitter data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Twitter)]
    public class TwitterDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (Twitter API v2) --------
        private static readonly List<string> KnownEntities = new()
        {
            // Tweets
            "tweets.search",       // GET tweets/search/recent   (requires query)
            "tweets.by_id",        // GET tweets?ids=...
            // Users
            "users.by_username",   // GET users/by?usernames=...
            "users.by_id",         // GET users?ids=...
            "users.tweets",        // GET users/{id}/tweets      (requires id)
            "users.followers",     // GET users/{id}/followers   (requires id)
            "users.following",     // GET users/{id}/following   (requires id)
            // Lists
            "lists.by_user",       // GET users/{id}/owned_lists (requires id)
            "lists.tweets",        // GET lists/{id}/tweets      (requires id)
            // Spaces
            "spaces.search"        // GET spaces/search          (requires query)
        };

        // entity -> (endpoint template, root path, required filter keys)
        // endpoint supports {id} substitution taken from filters.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["tweets.search"] = ("tweets/search/recent", "data", new[] { "query" }),
                ["tweets.by_id"] = ("tweets", "data", new[] { "ids" }),
                ["users.by_username"] = ("users/by", "data", new[] { "usernames" }),
                ["users.by_id"] = ("users", "data", new[] { "ids" }),
                ["users.tweets"] = ("users/{id}/tweets", "data", new[] { "id" }),
                ["users.followers"] = ("users/{id}/followers", "data", new[] { "id" }),
                ["users.following"] = ("users/{id}/following", "data", new[] { "id" }),
                ["lists.by_user"] = ("users/{id}/owned_lists", "data", new[] { "id" }),
                ["lists.tweets"] = ("lists/{id}/tweets", "data", new[] { "id" }),
                ["spaces.search"] = ("spaces/search", "data", new[] { "query" }),
            };

        public TwitterDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (URL/Auth configured outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'override' if base is virtual; otherwise this hides the base)
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
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Twitter entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Twitter uses cursor-based via meta.next_token / pagination_token)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Twitter entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);
            q["max_results"] = Math.Max(10, Math.Min(pageSize, 100)).ToString();

            string endpoint = ResolveEndpoint(m.endpoint, q);
            string cursor = q.TryGetValue("pagination_token", out var tok) ? tok : null;

            for (int i = 1; i < Math.Max(1, pageNumber); i++)
            {
                var step = CallTwitter(endpoint, MergeCursor(q, cursor)).ConfigureAwait(false).GetAwaiter().GetResult();
                cursor = GetNextToken(step);
                if (string.IsNullOrEmpty(cursor)) break;
            }

            var finalResp = CallTwitter(endpoint, MergeCursor(q, cursor)).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);
            var nextTok = GetNextToken(finalResp);

            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = nextTok == null ? pageNumber : pageNumber + 1, // estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = !string.IsNullOrEmpty(nextTok)
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
                throw new ArgumentException($"Twitter entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
                // Do not remove 'id' from query; leaving it is harmless (Twitter ignores unknowns for most endpoints).
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallTwitter(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        private static Dictionary<string, string> MergeCursor(Dictionary<string, string> q, string cursor)
        {
            var m = new Dictionary<string, string>(q, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(cursor)) m["pagination_token"] = cursor;
            return m;
        }

        private static string GetNextToken(HttpResponseMessage resp)
        {
            if (resp == null) return null;
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("meta", out var meta) &&
                    meta.ValueKind == JsonValueKind.Object &&
                    meta.TryGetProperty("next_token", out var tok) &&
                    tok.ValueKind == JsonValueKind.String)
                {
                    var s = tok.GetString();
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        // Extracts "data" (array or object) into a List<object> (Dictionary<string,object> per item).
        // If root is null, wraps whole payload as a single object.
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
                    return list; // no "data" -> empty
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
