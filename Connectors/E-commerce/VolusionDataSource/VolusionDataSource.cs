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
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Volusion
{
    // Optional: enable discovery if your enum has Volusion
    // [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Volusion)]
    public class VolusionDataSource : WebAPIDataSource
    {
        // Logical entity -> (endpoint template, root property, required filters)
        // Root "data" is common for list endpoints; single-object lookups use root=null.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                // Catalog
                ["Products"] = ("products", "data", Array.Empty<string>()),
                ["Products.ById"] = ("products/{id}", null, new[] { "id" }),
                ["ProductImages"] = ("products/{product_id}/images", "data", new[] { "product_id" }),
                ["Categories"] = ("categories", "data", Array.Empty<string>()),
                ["Vendors"] = ("vendors", "data", Array.Empty<string>()),

                // Customers
                ["Customers"] = ("customers", "data", Array.Empty<string>()),

                // Orders
                ["Orders"] = ("orders", "data", Array.Empty<string>()),
                ["Orders.ById"] = ("orders/{id}", null, new[] { "id" }),
                ["OrderItems"] = ("orders/{order_id}/items", "data", new[] { "order_id" }),
                ["Shipments"] = ("shipments", "data", Array.Empty<string>()),

                // Promotions
                ["Coupons"] = ("coupons", "data", Array.Empty<string>()),
            };

        public VolusionDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist (configured externally)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Expose the fixed list
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
                throw new InvalidOperationException($"Unknown Volusion entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Volusion entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Single-object lookups (root == null) don’t paginate
            if (string.IsNullOrWhiteSpace(m.root))
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

            // Prefer page/per_page; if caller already supplied offset/limit, honor those instead.
            int page = Math.Max(1, pageNumber);
            int size = Math.Max(1, pageSize <= 0 ? 50 : pageSize);
            if (!q.ContainsKey("offset") && !q.ContainsKey("limit"))
            {
                q["page"] = page.ToString();
                q["per_page"] = size.ToString();
            }
            else
            {
                // Convert requested page to offset if only limit is given
                if (!q.ContainsKey("offset") && q.TryGetValue("limit", out var limStr) && int.TryParse(limStr, out var lim) && lim > 0)
                    q["offset"] = ((page - 1) * lim).ToString();
            }

            string endpoint = ResolveEndpoint(m.endpoint, q);

            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            // Try to infer total from headers or body; otherwise best-effort “so far”
            int count = items.Count;
            int totalRecords = InferTotalFromHeaders(resp) ?? InferTotalFromBody(resp) ?? ((page - 1) * size + count);
            int totalPages = size > 0 ? Math.Max(1, (int)Math.Ceiling(totalRecords / (double)size)) : page;

            // If no grand total and exactly filled page, assume there may be a next page.
            bool hasNext = totalRecords == ((page - 1) * size + count) ? (count == size) : (page < totalPages);

            return new PagedResult
            {
                Data = items,
                PageNumber = page,
                PageSize = size,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = hasNext
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
                throw new ArgumentException($"Volusion entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Replace common placeholders
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
            }
            if (template.Contains("{product_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("product_id", out var pid) || string.IsNullOrWhiteSpace(pid))
                    throw new ArgumentException("Missing required 'product_id' filter for this endpoint.");
                template = template.Replace("{product_id}", Uri.EscapeDataString(pid));
            }
            if (template.Contains("{order_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("order_id", out var oid) || string.IsNullOrWhiteSpace(oid))
                    throw new ArgumentException("Missing required 'order_id' filter for this endpoint.");
                template = template.Replace("{order_id}", Uri.EscapeDataString(oid));
            }
            return template;
        }

        private async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await base.GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Robust extractor: if root==null use whole body; else try root, falling back to common keys "data"/"items"/"results".
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                {
                    // fallback common keys
                    if (doc.RootElement.TryGetProperty("data", out var d)) node = d;
                    else if (doc.RootElement.TryGetProperty("items", out var i)) node = i;
                    else if (doc.RootElement.TryGetProperty("results", out var r)) node = r;
                    else return list;
                }
            }

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

        private static int? InferTotalFromHeaders(HttpResponseMessage resp)
        {
            try
            {
                if (resp.Headers.TryGetValues("X-Total-Count", out var vals))
                    if (int.TryParse(vals.FirstOrDefault(), out var n)) return n;
            }
            catch { /* ignore */ }
            return null;
        }

        private static int? InferTotalFromBody(HttpResponseMessage resp)
        {
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                // common places
                if (doc.RootElement.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number && t.TryGetInt32(out var n)) return n;
                if (doc.RootElement.TryGetProperty("meta", out var meta) && meta.ValueKind == JsonValueKind.Object)
                {
                    if (meta.TryGetProperty("total", out var mt) && mt.ValueKind == JsonValueKind.Number && mt.TryGetInt32(out var mn)) return mn;
                    if (meta.TryGetProperty("pagination", out var pg) && pg.ValueKind == JsonValueKind.Object &&
                        pg.TryGetProperty("total", out var pgt) && pgt.ValueKind == JsonValueKind.Number && pgt.TryGetInt32(out var pgn)) return pgn;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }
}
