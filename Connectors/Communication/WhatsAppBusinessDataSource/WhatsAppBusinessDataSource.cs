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

namespace TheTechIdea.Beep.Connectors.Communication.WhatsAppBusiness
{
    /// <summary>
    /// WhatsApp Business (Cloud API via Graph). Reading inbound/outbound messages is webhook-based;
    /// this connector exposes read-friendly endpoints like phone_numbers, message_templates, media, business_profiles, subscribed_apps.
    /// Configure WebAPIConnectionProperties.Url externally, e.g. https://graph.facebook.com/v19.0/
    /// Auth (Bearer) is handled by the WebAPI base using your connection properties.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WhatsAppBusiness)]
    public class WhatsAppBusinessDataSource : WebAPIDataSource
    {
        // Supported logical entities -> (endpoint template, root array/object, required filters)
        // {waba_id} = Your WhatsApp Business Account ID, {media_id} = media id
        // Graph returns { "data": [...] } + paging for lists; single-object lookups return the object itself.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["waba.phone_numbers"] = ("{waba_id}/phone_numbers", "data", new[] { "waba_id" }),
                ["waba.message_templates"] = ("{waba_id}/message_templates", "data", new[] { "waba_id" }),
                ["waba.subscribed_apps"] = ("{waba_id}/subscribed_apps", "data", new[] { "waba_id" }),
                ["waba.business_profiles"] = ("{waba_id}/business_profiles", "data", new[] { "waba_id" }),

                // Single-object lookup
                ["media.by_id"] = ("{media_id}", null, new[] { "media_id" })
            };

        public WhatsAppBusinessDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep exact signature
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // ------------ Overrides (same signatures) ------------

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
                throw new InvalidOperationException($"Unknown WhatsApp entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);
            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Graph-style cursors: limit + after)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown WhatsApp entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Single-object lookups (media.by_id) don’t paginate
            bool isSingle = string.IsNullOrWhiteSpace(m.root);
            if (isSingle)
            {
                var r = GetAsync(ResolveEndpoint(m.endpoint, q), q).ConfigureAwait(false).GetAwaiter().GetResult();
                var one = ExtractArray(r, m.root);
                return new PagedResult
                {
                    Data = one,
                    PageNumber = 1,
                    PageSize = Math.Max(1, pageSize),
                    TotalRecords = one.Count,
                    TotalPages = 1,
                    HasNextPage = false,
                    HasPreviousPage = false
                };
            }

            int limit = Math.Max(1, Math.Min(pageSize <= 0 ? 25 : pageSize, 100));
            q["limit"] = limit.ToString();

            string endpoint = ResolveEndpoint(m.endpoint, q);
            string after = q.TryGetValue("after", out var a) ? a : null;

            int targetPage = Math.Max(1, pageNumber);
            for (int i = 1; i < targetPage; i++)
            {
                var step = GetAsync(endpoint, MergeAfter(q, after)).ConfigureAwait(false).GetAwaiter().GetResult();
                after = GetNextAfter(step); // null if last page
                if (string.IsNullOrEmpty(after)) break;
            }

            var finalResp = GetAsync(endpoint, MergeAfter(q, after)).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);
            var nextAfter = GetNextAfter(finalResp);

            int pageIdx = targetPage;
            int count = items.Count;
            int totalSoFar = (pageIdx - 1) * limit + count;

            return new PagedResult
            {
                Data = items,
                PageNumber = pageIdx,
                PageSize = limit,
                TotalRecords = totalSoFar,                     // Graph doesn't give a grand total here
                TotalPages = string.IsNullOrEmpty(nextAfter) ? pageIdx : pageIdx + 1,
                HasPreviousPage = pageIdx > 1,
                HasNextPage = !string.IsNullOrEmpty(nextAfter)
            };
        }

        // ---------------- helpers ----------------

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
                throw new ArgumentException($"WhatsApp entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            if (template.Contains("{waba_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("waba_id", out var w) || string.IsNullOrWhiteSpace(w))
                    throw new ArgumentException("Missing required 'waba_id' filter for this endpoint.");
                template = template.Replace("{waba_id}", Uri.EscapeDataString(w));
            }
            if (template.Contains("{media_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("media_id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'media_id' filter for this endpoint.");
                template = template.Replace("{media_id}", Uri.EscapeDataString(id));
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
