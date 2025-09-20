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
using TheTechIdea.Beep.Connectors.Ecommerce.OpenCartDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.OpenCart
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart)]
    public class OpenCartDataSource : WebAPIDataSource
    {
        // OpenCart API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Store Information
            ["store"] = ("", "", new[] { "" }),

            // Categories
            ["categories"] = ("categories", "data", new[] { "" }),
            ["category"] = ("categories/{category_id}", "", new[] { "category_id" }),

            // Products
            ["products"] = ("products", "data", new[] { "" }),
            ["product"] = ("products/{product_id}", "", new[] { "product_id" }),
            ["product_images"] = ("products/{product_id}/images", "data", new[] { "product_id" }),
            ["product_options"] = ("products/{product_id}/options", "data", new[] { "product_id" }),
            ["product_option_values"] = ("products/{product_id}/options/{option_id}/values", "data", new[] { "product_id", "option_id" }),

            // Orders
            ["orders"] = ("orders", "data", new[] { "" }),
            ["order"] = ("orders/{order_id}", "", new[] { "order_id" }),
            ["order_products"] = ("orders/{order_id}/products", "data", new[] { "order_id" }),
            ["order_totals"] = ("orders/{order_id}/totals", "data", new[] { "order_id" }),
            ["order_histories"] = ("orders/{order_id}/histories", "data", new[] { "order_id" }),

            // Customers
            ["customers"] = ("customers", "data", new[] { "" }),
            ["customer"] = ("customers/{customer_id}", "", new[] { "customer_id" }),
            ["customer_groups"] = ("customers/groups", "data", new[] { "" }),
            ["customer_addresses"] = ("customers/{customer_id}/addresses", "data", new[] { "customer_id" }),

            // Manufacturers
            ["manufacturers"] = ("manufacturers", "data", new[] { "" }),
            ["manufacturer"] = ("manufacturers/{manufacturer_id}", "", new[] { "manufacturer_id" }),

            // Attributes & Options
            ["attributes"] = ("attributes", "data", new[] { "" }),
            ["attribute_groups"] = ("attributes/groups", "data", new[] { "" }),
            ["options"] = ("options", "data", new[] { "" }),
            ["option_values"] = ("options/{option_id}/values", "data", new[] { "option_id" }),

            // Coupons & Vouchers
            ["coupons"] = ("coupons", "data", new[] { "" }),
            ["vouchers"] = ("vouchers", "data", new[] { "" }),

            // Reviews
            ["reviews"] = ("reviews", "data", new[] { "" }),
            ["product_reviews"] = ("products/{product_id}/reviews", "data", new[] { "product_id" }),

            // Returns
            ["returns"] = ("returns", "data", new[] { "" }),
            ["return"] = ("returns/{return_id}", "", new[] { "return_id" }),

            // Affiliates
            ["affiliates"] = ("affiliates", "data", new[] { "" }),
            ["affiliate"] = ("affiliates/{affiliate_id}", "", new[] { "affiliate_id" }),

            // Marketing
            ["marketing"] = ("marketing", "data", new[] { "" }),

            // Zones & Geo Zones
            ["zones"] = ("zones", "data", new[] { "" }),
            ["geo_zones"] = ("geo_zones", "data", new[] { "" }),
            ["geo_zone_zones"] = ("geo_zones/{geo_zone_id}/zones", "data", new[] { "geo_zone_id" }),

            // Languages & Currencies
            ["languages"] = ("languages", "data", new[] { "" }),
            ["currencies"] = ("currencies", "data", new[] { "" }),

            // Stock Status
            ["stock_statuses"] = ("stock_statuses", "data", new[] { "" }),

            // Order Statuses
            ["order_statuses"] = ("order_statuses", "data", new[] { "" }),

            // Tax Classes & Rates
            ["tax_classes"] = ("tax_classes", "data", new[] { "" }),
            ["tax_rates"] = ("tax_rates", "data", new[] { "" }),

            // Weight Classes
            ["weight_classes"] = ("weight_classes", "data", new[] { "" }),

            // Length Classes
            ["length_classes"] = ("length_classes", "data", new[] { "" }),

            // Information Pages
            ["informations"] = ("informations", "data", new[] { "" }),
            ["information"] = ("informations/{information_id}", "", new[] { "information_id" })
        };

        public OpenCartDataSource(
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
                throw new InvalidOperationException($"Unknown OpenCart entity '{EntityName}'.");

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
                throw new ArgumentException($"OpenCart entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "category_id", "product_id", "order_id", "customer_id", "manufacturer_id", "option_id", "return_id", "affiliate_id", "geo_zone_id", "information_id" })
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