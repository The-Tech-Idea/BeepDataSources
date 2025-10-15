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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooProduct", Name = "CreateProduct", Caption = "Create WooCommerce Product", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooProduct>")]
        public async Task<IEnumerable<WooProduct>> CreateProductAsync(WooProduct product)
        {
            try
            {
                var result = await PostAsync("Products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<WooProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooProduct> { createdProduct }.Select(p => p.Attach<WooProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<WooProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooProduct", Name = "UpdateProduct", Caption = "Update WooCommerce Product", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooProduct>")]
        public async Task<IEnumerable<WooProduct>> UpdateProductAsync(WooProduct product)
        {
            try
            {
                var result = await PutAsync($"Products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<WooProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooProduct> { updatedProduct }.Select(p => p.Attach<WooProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<WooProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooOrder", Name = "CreateOrder", Caption = "Create WooCommerce Order", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooOrder>")]
        public async Task<IEnumerable<WooOrder>> CreateOrderAsync(WooOrder order)
        {
            try
            {
                var result = await PostAsync("Orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<WooOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooOrder> { createdOrder }.Select(o => o.Attach<WooOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<WooOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooOrder", Name = "UpdateOrder", Caption = "Update WooCommerce Order", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooOrder>")]
        public async Task<IEnumerable<WooOrder>> UpdateOrderAsync(WooOrder order)
        {
            try
            {
                var result = await PutAsync($"Orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<WooOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooOrder> { updatedOrder }.Select(o => o.Attach<WooOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<WooOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCustomer", Name = "CreateCustomer", Caption = "Create WooCommerce Customer", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCustomer>")]
        public async Task<IEnumerable<WooCustomer>> CreateCustomerAsync(WooCustomer customer)
        {
            try
            {
                var result = await PostAsync("Customers", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCustomer = JsonSerializer.Deserialize<WooCustomer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCustomer> { createdCustomer }.Select(c => c.Attach<WooCustomer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating customer: {ex.Message}");
            }
            return new List<WooCustomer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCustomer", Name = "UpdateCustomer", Caption = "Update WooCommerce Customer", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCustomer>")]
        public async Task<IEnumerable<WooCustomer>> UpdateCustomerAsync(WooCustomer customer)
        {
            try
            {
                var result = await PutAsync($"Customers/{customer.Id}", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCustomer = JsonSerializer.Deserialize<WooCustomer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCustomer> { updatedCustomer }.Select(c => c.Attach<WooCustomer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating customer: {ex.Message}");
            }
            return new List<WooCustomer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCoupon", Name = "CreateCoupon", Caption = "Create WooCommerce Coupon", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCoupon>")]
        public async Task<IEnumerable<WooCoupon>> CreateCouponAsync(WooCoupon coupon)
        {
            try
            {
                var result = await PostAsync("Coupons", coupon);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCoupon = JsonSerializer.Deserialize<WooCoupon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCoupon> { createdCoupon }.Select(c => c.Attach<WooCoupon>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating coupon: {ex.Message}");
            }
            return new List<WooCoupon>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCoupon", Name = "UpdateCoupon", Caption = "Update WooCommerce Coupon", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCoupon>")]
        public async Task<IEnumerable<WooCoupon>> UpdateCouponAsync(WooCoupon coupon)
        {
            try
            {
                var result = await PutAsync($"Coupons/{coupon.Id}", coupon);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCoupon = JsonSerializer.Deserialize<WooCoupon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCoupon> { updatedCoupon }.Select(c => c.Attach<WooCoupon>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating coupon: {ex.Message}");
            }
            return new List<WooCoupon>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCategory", Name = "CreateCategory", Caption = "Create WooCommerce Category", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 20, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCategory>")]
        public async Task<IEnumerable<WooCategory>> CreateCategoryAsync(WooCategory category)
        {
            try
            {
                var result = await PostAsync("Categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<WooCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCategory> { createdCategory }.Select(c => c.Attach<WooCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<WooCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooCategory", Name = "UpdateCategory", Caption = "Update WooCommerce Category", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 21, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooCategory>")]
        public async Task<IEnumerable<WooCategory>> UpdateCategoryAsync(WooCategory category)
        {
            try
            {
                var result = await PutAsync($"Categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<WooCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooCategory> { updatedCategory }.Select(c => c.Attach<WooCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<WooCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooReview", Name = "CreateReview", Caption = "Create WooCommerce Review", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 22, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooReview>")]
        public async Task<IEnumerable<WooReview>> CreateReviewAsync(WooReview review)
        {
            try
            {
                var result = await PostAsync("Reviews", review);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdReview = JsonSerializer.Deserialize<WooReview>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooReview> { createdReview }.Select(r => r.Attach<WooReview>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating review: {ex.Message}");
            }
            return new List<WooReview>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooReview", Name = "UpdateReview", Caption = "Update WooCommerce Review", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 23, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooReview>")]
        public async Task<IEnumerable<WooReview>> UpdateReviewAsync(WooReview review)
        {
            try
            {
                var result = await PutAsync($"Reviews/{review.Id}", review);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedReview = JsonSerializer.Deserialize<WooReview>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooReview> { updatedReview }.Select(r => r.Attach<WooReview>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating review: {ex.Message}");
            }
            return new List<WooReview>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTax", Name = "CreateTax", Caption = "Create WooCommerce Tax", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 24, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTax>")]
        public async Task<IEnumerable<WooTax>> CreateTaxAsync(WooTax tax)
        {
            try
            {
                var result = await PostAsync("Taxes", tax);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTax = JsonSerializer.Deserialize<WooTax>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooTax> { createdTax }.Select(t => t.Attach<WooTax>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating tax: {ex.Message}");
            }
            return new List<WooTax>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTax", Name = "UpdateTax", Caption = "Update WooCommerce Tax", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 25, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTax>")]
        public async Task<IEnumerable<WooTax>> UpdateTaxAsync(WooTax tax)
        {
            try
            {
                var result = await PutAsync($"Taxes/{tax.Id}", tax);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTax = JsonSerializer.Deserialize<WooTax>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooTax> { updatedTax }.Select(t => t.Attach<WooTax>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating tax: {ex.Message}");
            }
            return new List<WooTax>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTaxClass", Name = "CreateTaxClass", Caption = "Create WooCommerce Tax Class", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 26, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTaxClass>")]
        public async Task<IEnumerable<WooTaxClass>> CreateTaxClassAsync(WooTaxClass taxClass)
        {
            try
            {
                var result = await PostAsync("TaxClasses", taxClass);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTaxClass = JsonSerializer.Deserialize<WooTaxClass>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooTaxClass> { createdTaxClass }.Select(t => t.Attach<WooTaxClass>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating tax class: {ex.Message}");
            }
            return new List<WooTaxClass>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooTaxClass", Name = "UpdateTaxClass", Caption = "Update WooCommerce Tax Class", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 27, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooTaxClass>")]
        public async Task<IEnumerable<WooTaxClass>> UpdateTaxClassAsync(WooTaxClass taxClass)
        {
            try
            {
                var result = await PutAsync($"TaxClasses/{taxClass.Slug}", taxClass);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTaxClass = JsonSerializer.Deserialize<WooTaxClass>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooTaxClass> { updatedTaxClass }.Select(t => t.Attach<WooTaxClass>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating tax class: {ex.Message}");
            }
            return new List<WooTaxClass>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingZone", Name = "CreateShippingZone", Caption = "Create WooCommerce Shipping Zone", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 28, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingZone>")]
        public async Task<IEnumerable<WooShippingZone>> CreateShippingZoneAsync(WooShippingZone shippingZone)
        {
            try
            {
                var result = await PostAsync("ShippingZones", shippingZone);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdShippingZone = JsonSerializer.Deserialize<WooShippingZone>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooShippingZone> { createdShippingZone }.Select(s => s.Attach<WooShippingZone>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating shipping zone: {ex.Message}");
            }
            return new List<WooShippingZone>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingZone", Name = "UpdateShippingZone", Caption = "Update WooCommerce Shipping Zone", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 29, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingZone>")]
        public async Task<IEnumerable<WooShippingZone>> UpdateShippingZoneAsync(WooShippingZone shippingZone)
        {
            try
            {
                var result = await PutAsync($"ShippingZones/{shippingZone.Id}", shippingZone);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedShippingZone = JsonSerializer.Deserialize<WooShippingZone>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooShippingZone> { updatedShippingZone }.Select(s => s.Attach<WooShippingZone>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating shipping zone: {ex.Message}");
            }
            return new List<WooShippingZone>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingMethod", Name = "CreateShippingMethod", Caption = "Create WooCommerce Shipping Method", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 30, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingMethod>")]
        public async Task<IEnumerable<WooShippingMethod>> CreateShippingMethodAsync(WooShippingMethod shippingMethod)
        {
            try
            {
                var result = await PostAsync("ShippingMethods", shippingMethod);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdShippingMethod = JsonSerializer.Deserialize<WooShippingMethod>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooShippingMethod> { createdShippingMethod }.Select(s => s.Attach<WooShippingMethod>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating shipping method: {ex.Message}");
            }
            return new List<WooShippingMethod>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooShippingMethod", Name = "UpdateShippingMethod", Caption = "Update WooCommerce Shipping Method", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 31, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooShippingMethod>")]
        public async Task<IEnumerable<WooShippingMethod>> UpdateShippingMethodAsync(WooShippingMethod shippingMethod)
        {
            try
            {
                var result = await PutAsync($"ShippingMethods/{shippingMethod.Id}", shippingMethod);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedShippingMethod = JsonSerializer.Deserialize<WooShippingMethod>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooShippingMethod> { updatedShippingMethod }.Select(s => s.Attach<WooShippingMethod>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating shipping method: {ex.Message}");
            }
            return new List<WooShippingMethod>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooAttribute", Name = "CreateAttribute", Caption = "Create WooCommerce Attribute", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 32, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooAttribute>")]
        public async Task<IEnumerable<WooAttribute>> CreateAttributeAsync(WooAttribute attribute)
        {
            try
            {
                var result = await PostAsync("Attributes", attribute);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdAttribute = JsonSerializer.Deserialize<WooAttribute>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooAttribute> { createdAttribute }.Select(a => a.Attach<WooAttribute>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating attribute: {ex.Message}");
            }
            return new List<WooAttribute>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.WooCommerce, PointType = EnumPointType.Function, ObjectType = "WooAttribute", Name = "UpdateAttribute", Caption = "Update WooCommerce Attribute", ClassType = "WooCommerceDataSource", Showin = ShowinType.Both, Order = 33, iconimage = "woocommerce.png", misc = "ReturnType: IEnumerable<WooAttribute>")]
        public async Task<IEnumerable<WooAttribute>> UpdateAttributeAsync(WooAttribute attribute)
        {
            try
            {
                var result = await PutAsync($"Attributes/{attribute.Id}", attribute);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedAttribute = JsonSerializer.Deserialize<WooAttribute>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WooAttribute> { updatedAttribute }.Select(a => a.Attach<WooAttribute>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating attribute: {ex.Message}");
            }
            return new List<WooAttribute>();
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
