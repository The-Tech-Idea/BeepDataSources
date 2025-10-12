using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Connectors.Ecommerce.WooCommerce.Models;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Ecommerce.WooCommerce
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce)]
    public class WooCommerceDataSource : WebAPIDataSource
    {


        // Map entity -> (endpoint template, root array/property, required filter keys)
        // Endpoints are relative to base URL, e.g., https://example.com/wp-json/wc/v3/
        private static readonly Dictionary<string, (string endpoint, string? root, string[] required)>
            Map = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Products"] = ("products", null, Array.Empty<string>()),
                ["Orders"] = ("orders", null, Array.Empty<string>()),
                ["Customers"] = ("customers", null, Array.Empty<string>()),
                ["Coupons"] = ("coupons", null, Array.Empty<string>()),
                ["Categories"] = ("products/categories", null, Array.Empty<string>()),
                ["Reviews"] = ("products/reviews", null, Array.Empty<string>()),
                ["Taxes"] = ("taxes", null, Array.Empty<string>()),
                ["TaxClasses"] = ("tax/classes", null, Array.Empty<string>()),
                ["ShippingZones"] = ("shipping/zones", null, Array.Empty<string>()),
                ["ShippingMethods"] = ("shipping_methods", null, Array.Empty<string>()),
                ["Attributes"] = ("products/attributes", null, Array.Empty<string>()),

                // Requires product id in filters: id={productId}
                ["ProductVariations"] = ("products/{id}/variations", null, new[] { "id" }),
            };

        public WooCommerceDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (configured outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = WooEntityRegistry.Names.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();

        }

        // Return the fixed list (use 'override' if base is virtual; otherwise 'new' hides the base)
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
                throw new InvalidOperationException($"Unknown WooCommerce entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.required);

            var endpoint = ResolveEndpoint(m.endpoint, q);
            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (WooCommerce supports page & per_page; totals via X-WP-Total headers)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown WooCommerce entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            q["page"] = Math.Max(1, pageNumber).ToString();
            q["per_page"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();

            var endpoint = ResolveEndpoint(m.endpoint, q);
            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            // Derive totals from headers when available
            int totalRecords = items.Count;
            int totalPages = 1;
            if (resp != null &&
                resp.Headers.TryGetValues("X-WP-Total", out var totals) &&
                int.TryParse(totals.FirstOrDefault(), out var t))
            {
                totalRecords = t;
                totalPages = (int)Math.Ceiling((double)totalRecords / Math.Max(1, pageSize));
            }

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
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
                // WooCommerce uses query params; pass through as strings
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"WooCommerce entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
                // keep 'id' in query (harmless); WooCommerce ignores unknowns for most endpoints
            }
            return template;
        }

        private async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await base.GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // COMMAND ATTRIBUTE METHODS - Strongly typed WooCommerce operations
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooProduct", Name = "GetProducts", Caption = "Get WooCommerce Products", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 1, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooProduct>")]
        public async Task<IEnumerable<WooProduct>> GetProducts(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Products", filters ?? new List<AppFilter>());
            return result.Cast<WooProduct>().Select(p => p.Attach<WooProduct>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooOrder", Name = "GetOrders", Caption = "Get WooCommerce Orders", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 2, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooOrder>")]
        public async Task<IEnumerable<WooOrder>> GetOrders(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Orders", filters ?? new List<AppFilter>());
            return result.Cast<WooOrder>().Select(o => o.Attach<WooOrder>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCustomer", Name = "GetCustomers", Caption = "Get WooCommerce Customers", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 3, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCustomer>")]
        public async Task<IEnumerable<WooCustomer>> GetCustomers(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Customers", filters ?? new List<AppFilter>());
            return result.Cast<WooCustomer>().Select(c => c.Attach<WooCustomer>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCoupon", Name = "GetCoupons", Caption = "Get WooCommerce Coupons", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 4, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCoupon>")]
        public async Task<IEnumerable<WooCoupon>> GetCoupons(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Coupons", filters ?? new List<AppFilter>());
            return result.Cast<WooCoupon>().Select(c => c.Attach<WooCoupon>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCategory", Name = "GetCategories", Caption = "Get WooCommerce Categories", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 5, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCategory>")]
        public async Task<IEnumerable<WooCategory>> GetCategories(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Categories", filters ?? new List<AppFilter>());
            return result.Cast<WooCategory>().Select(c => c.Attach<WooCategory>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooReview", Name = "GetReviews", Caption = "Get WooCommerce Reviews", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 6, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooReview>")]
        public async Task<IEnumerable<WooReview>> GetReviews(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Reviews", filters ?? new List<AppFilter>());
            return result.Cast<WooReview>().Select(r => r.Attach<WooReview>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTax", Name = "GetTaxes", Caption = "Get WooCommerce Taxes", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 7, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTax>")]
        public async Task<IEnumerable<WooTax>> GetTaxes(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Taxes", filters ?? new List<AppFilter>());
            return result.Cast<WooTax>().Select(t => t.Attach<WooTax>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTaxClass", Name = "GetTaxClasses", Caption = "Get WooCommerce Tax Classes", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 8, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTaxClass>")]
        public async Task<IEnumerable<WooTaxClass>> GetTaxClasses(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("TaxClasses", filters ?? new List<AppFilter>());
            return result.Cast<WooTaxClass>().Select(t => t.Attach<WooTaxClass>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingZone", Name = "GetShippingZones", Caption = "Get WooCommerce Shipping Zones", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 9, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingZone>")]
        public async Task<IEnumerable<WooShippingZone>> GetShippingZones(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("ShippingZones", filters ?? new List<AppFilter>());
            return result.Cast<WooShippingZone>().Select(s => s.Attach<WooShippingZone>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingMethod", Name = "GetShippingMethods", Caption = "Get WooCommerce Shipping Methods", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingMethod>")]
        public async Task<IEnumerable<WooShippingMethod>> GetShippingMethods(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("ShippingMethods", filters ?? new List<AppFilter>());
            return result.Cast<WooShippingMethod>().Select(s => s.Attach<WooShippingMethod>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooAttribute", Name = "GetAttributes", Caption = "Get WooCommerce Attributes", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooAttribute>")]
        public async Task<IEnumerable<WooAttribute>> GetAttributes(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("Attributes", filters ?? new List<AppFilter>());
            return result.Cast<WooAttribute>().Select(a => a.Attach<WooAttribute>(this));
        }

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
                    return list; // no root -> empty
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
                // Wrap single object (some endpoints return an object if id specified)
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }
    }
}
