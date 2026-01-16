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

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
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

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Products", ClassName = "OpenCartDataSource", Showin = ShowinType.Both, misc = "IEnumerable<OpenCartProduct>")]
        public async Task<IEnumerable<OpenCartProduct>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("products", new List<AppFilter> { filter });
            return result.Cast<OpenCartProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Orders", ClassName = "OpenCartDataSource", Showin = ShowinType.Both, misc = "IEnumerable<OpenCartOrder>")]
        public async Task<IEnumerable<OpenCartOrder>> GetOrders(AppFilter filter)
        {
            var result = await GetEntityAsync("orders", new List<AppFilter> { filter });
            return result.Cast<OpenCartOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Customers", ClassName = "OpenCartDataSource", Showin = ShowinType.Both, misc = "IEnumerable<OpenCartCustomer>")]
        public async Task<IEnumerable<OpenCartCustomer>> GetCustomers(AppFilter filter)
        {
            var result = await GetEntityAsync("customers", new List<AppFilter> { filter });
            return result.Cast<OpenCartCustomer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Categories", ClassName = "OpenCartDataSource", Showin = ShowinType.Both, misc = "IEnumerable<OpenCartCategory>")]
        public async Task<IEnumerable<OpenCartCategory>> GetCategories(AppFilter filter)
        {
            var result = await GetEntityAsync("categories", new List<AppFilter> { filter });
            return result.Cast<OpenCartCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Manufacturers", ClassName = "OpenCartDataSource", Showin = ShowinType.Both, misc = "IEnumerable<OpenCartManufacturer>")]
        public async Task<IEnumerable<OpenCartManufacturer>> GetManufacturers(AppFilter filter)
        {
            var result = await GetEntityAsync("manufacturers", new List<AppFilter> { filter });
            return result.Cast<OpenCartManufacturer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Products", Name = "CreateProduct", Caption = "Create OpenCart Product", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 6, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartProduct>")]
        public async Task<IEnumerable<OpenCartProduct>> CreateProductAsync(OpenCartProduct product)
        {
            try
            {
                var result = await PostAsync("products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<OpenCartProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartProduct> { createdProduct }.Select(p => p.Attach<OpenCartProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<OpenCartProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Products", Name = "UpdateProduct", Caption = "Update OpenCart Product", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 7, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartProduct>")]
        public async Task<IEnumerable<OpenCartProduct>> UpdateProductAsync(OpenCartProduct product)
        {
            try
            {
                var result = await PutAsync($"products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<OpenCartProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartProduct> { updatedProduct }.Select(p => p.Attach<OpenCartProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<OpenCartProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "CreateOrder", Caption = "Create OpenCart Order", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 8, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartOrder>")]
        public async Task<IEnumerable<OpenCartOrder>> CreateOrderAsync(OpenCartOrder order)
        {
            try
            {
                var result = await PostAsync("orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<OpenCartOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartOrder> { createdOrder }.Select(o => o.Attach<OpenCartOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<OpenCartOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "UpdateOrder", Caption = "Update OpenCart Order", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 9, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartOrder>")]
        public async Task<IEnumerable<OpenCartOrder>> UpdateOrderAsync(OpenCartOrder order)
        {
            try
            {
                var result = await PutAsync($"orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<OpenCartOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartOrder> { updatedOrder }.Select(o => o.Attach<OpenCartOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<OpenCartOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Customers", Name = "CreateCustomer", Caption = "Create OpenCart Customer", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartCustomer>")]
        public async Task<IEnumerable<OpenCartCustomer>> CreateCustomerAsync(OpenCartCustomer customer)
        {
            try
            {
                var result = await PostAsync("customers", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCustomer = JsonSerializer.Deserialize<OpenCartCustomer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartCustomer> { createdCustomer }.Select(c => c.Attach<OpenCartCustomer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating customer: {ex.Message}");
            }
            return new List<OpenCartCustomer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Customers", Name = "UpdateCustomer", Caption = "Update OpenCart Customer", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartCustomer>")]
        public async Task<IEnumerable<OpenCartCustomer>> UpdateCustomerAsync(OpenCartCustomer customer)
        {
            try
            {
                var result = await PutAsync($"customers/{customer.Id}", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCustomer = JsonSerializer.Deserialize<OpenCartCustomer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartCustomer> { updatedCustomer }.Select(c => c.Attach<OpenCartCustomer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating customer: {ex.Message}");
            }
            return new List<OpenCartCustomer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Categories", Name = "CreateCategory", Caption = "Create OpenCart Category", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartCategory>")]
        public async Task<IEnumerable<OpenCartCategory>> CreateCategoryAsync(OpenCartCategory category)
        {
            try
            {
                var result = await PostAsync("categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<OpenCartCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartCategory> { createdCategory }.Select(c => c.Attach<OpenCartCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<OpenCartCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Categories", Name = "UpdateCategory", Caption = "Update OpenCart Category", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartCategory>")]
        public async Task<IEnumerable<OpenCartCategory>> UpdateCategoryAsync(OpenCartCategory category)
        {
            try
            {
                var result = await PutAsync($"categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<OpenCartCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartCategory> { updatedCategory }.Select(c => c.Attach<OpenCartCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<OpenCartCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Manufacturers", Name = "CreateManufacturer", Caption = "Create OpenCart Manufacturer", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartManufacturer>")]
        public async Task<IEnumerable<OpenCartManufacturer>> CreateManufacturerAsync(OpenCartManufacturer manufacturer)
        {
            try
            {
                var result = await PostAsync("manufacturers", manufacturer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdManufacturer = JsonSerializer.Deserialize<OpenCartManufacturer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartManufacturer> { createdManufacturer }.Select(m => m.Attach<OpenCartManufacturer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating manufacturer: {ex.Message}");
            }
            return new List<OpenCartManufacturer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OpenCart, PointType = EnumPointType.Function, ObjectType ="Manufacturers", Name = "UpdateManufacturer", Caption = "Update OpenCart Manufacturer", ClassType ="OpenCartDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "opencart.png", misc = "ReturnType: IEnumerable<OpenCartManufacturer>")]
        public async Task<IEnumerable<OpenCartManufacturer>> UpdateManufacturerAsync(OpenCartManufacturer manufacturer)
        {
            try
            {
                var result = await PutAsync($"manufacturers/{manufacturer.Id}", manufacturer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedManufacturer = JsonSerializer.Deserialize<OpenCartManufacturer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<OpenCartManufacturer> { updatedManufacturer }.Select(m => m.Attach<OpenCartManufacturer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating manufacturer: {ex.Message}");
            }
            return new List<OpenCartManufacturer>();
        }

        #endregion
    }
}