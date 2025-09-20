using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Ecommerce.SquarespaceDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Squarespace
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace)]
    public class SquarespaceDataSource : WebAPIDataSource
    {
        // Squarespace API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Core Commerce
            ["products"] = ("1.0/commerce/products", "products", new[] { "" }),
            ["product_variants"] = ("1.0/commerce/products/{product_id}/variants", "variants", new[] { "product_id" }),
            ["orders"] = ("1.0/commerce/orders", "result", new[] { "" }),
            ["order_fulfillments"] = ("1.0/commerce/orders/{order_id}/fulfillments", "fulfillments", new[] { "order_id" }),
            ["inventory"] = ("1.0/commerce/inventory", "inventory", new[] { "" }),
            ["profiles"] = ("1.0/commerce/profiles", "profiles", new[] { "" }),

            // Content Management
            ["pages"] = ("1.0/sites/pages", "pages", new[] { "" }),
            ["blogs"] = ("1.0/sites/blogs", "blogs", new[] { "" }),
            ["blog_posts"] = ("1.0/sites/blogs/{blog_id}/posts", "posts", new[] { "blog_id" }),
            ["events"] = ("1.0/sites/events", "events", new[] { "" }),
            ["galleries"] = ("1.0/sites/galleries", "galleries", new[] { "" }),
            ["gallery_images"] = ("1.0/sites/galleries/{gallery_id}/images", "images", new[] { "gallery_id" }),

            // Store Settings
            ["store"] = ("1.0/commerce/store", "", new[] { "" }),
            ["shipping"] = ("1.0/commerce/shipping", "shippingOptions", new[] { "" }),
            ["taxes"] = ("1.0/commerce/taxes", "taxes", new[] { "" }),

            // Forms & Donations
            ["forms"] = ("1.0/sites/forms", "forms", new[] { "" }),
            ["form_submissions"] = ("1.0/sites/forms/{form_id}/submissions", "submissions", new[] { "form_id" }),
            ["donations"] = ("1.0/commerce/donations", "donations", new[] { "" }),

            // Analytics
            ["website_traffic"] = ("1.0/analytics/traffic", "traffic", new[] { "" }),
            ["website_orders"] = ("1.0/analytics/orders", "orders", new[] { "" }),

            // Categories & Navigation
            ["categories"] = ("1.0/commerce/categories", "categories", new[] { "" }),
            ["navigation"] = ("1.0/sites/navigation", "navigation", new[] { "" })
        };

        public SquarespaceDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure we're on WebAPI connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities from Map
            EntitiesNames = Map.Keys.ToList();
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
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Squarespace entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter ?? new List<AppFilter>());
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root);
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
                throw new ArgumentException($"Squarespace entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "product_id", "order_id", "blog_id", "gallery_id", "form_id" })
            {
                if (result.Contains($"{{{param}}}", StringComparison.Ordinal))
                {
                    if (!q.TryGetValue(param, out var value) || string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException($"Missing required '{param}' filter for this endpoint.");
                    result = result.Replace($"{{{param}}}", Uri.EscapeDataString(value));
                }
            }
            return result;
        }

        // Extracts array from response
        private static List<object> ExtractArray(HttpResponseMessage resp, string? root)
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
    }
}