using System;
using System.Collections.Generic;
using System.Linq;
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

namespace TheTechIdea.Beep.Connectors.Ecommerce.Shopify
{
    using TheTechIdea.Beep.Connectors.Ecommerce.Shopify.Models;
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify)]
    public class ShopifyDataSource : WebAPIDataSource
    {
        // Supported Shopify entities -> Shopify endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["products"] = ("products", null, Array.Empty<string>()),
                ["product"] = ("products/{id}", null, new[] { "id" }),
                ["variants"] = ("products/{product_id}/variants", null, new[] { "product_id" }),
                ["variant"] = ("variants/{id}", null, new[] { "id" }),
                ["orders"] = ("orders", null, Array.Empty<string>()),
                ["order"] = ("orders/{id}", null, new[] { "id" }),
                ["customers"] = ("customers", null, Array.Empty<string>()),
                ["customer"] = ("customers/{id}", null, new[] { "id" }),
                ["inventory_items"] = ("inventory_items", null, Array.Empty<string>()),
                ["inventory_item"] = ("inventory_items/{id}", null, new[] { "id" }),
                ["inventory_levels"] = ("inventory_levels", null, Array.Empty<string>()),
                ["locations"] = ("locations", null, Array.Empty<string>()),
                ["location"] = ("locations/{id}", null, new[] { "id" }),
                ["custom_collections"] = ("custom_collections", null, Array.Empty<string>()),
                ["custom_collection"] = ("custom_collections/{id}", null, new[] { "id" }),
                ["smart_collections"] = ("smart_collections", null, Array.Empty<string>()),
                ["smart_collection"] = ("smart_collections/{id}", null, new[] { "id" }),
                ["collects"] = ("collects", null, Array.Empty<string>()),
                ["webhooks"] = ("webhooks", null, Array.Empty<string>()),
                ["webhook"] = ("webhooks/{id}", null, new[] { "id" })
            };

        public ShopifyDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
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

        // -------------------- Overrides (same signatures as base) --------------------

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
                throw new InvalidOperationException($"Unknown Shopify entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root);
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Shopify entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, requiredFilters);

            // Shopify uses cursor-based pagination
            q["limit"] = Math.Max(1, Math.Min(pageSize, 250)).ToString(); // Shopify max is 250

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = GetAsync(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            if (resp is null || !resp.IsSuccessStatusCode)
                return new PagedResult { Data = Array.Empty<object>() };

            var items = ExtractArray(resp, root);

            return new PagedResult
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = items.Count, // Shopify doesn't provide total count
                TotalPages = 1,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count >= pageSize
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
                throw new ArgumentException($"Shopify entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "id", "product_id" })
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

        // COMMAND ATTRIBUTE METHODS - Strongly typed Shopify operations
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Product", Name = "GetProducts", Caption = "Get Shopify Products", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 1, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Product>")]
        public async Task<IEnumerable<Product>> GetProducts(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("products", filters ?? new List<AppFilter>());
            return result.Cast<Product>().Select(p => p.Attach<Product>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Order", Name = "GetOrders", Caption = "Get Shopify Orders", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 2, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Order>")]
        public async Task<IEnumerable<Order>> GetOrders(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("orders", filters ?? new List<AppFilter>());
            return result.Cast<Order>().Select(o => o.Attach<Order>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Customer", Name = "GetCustomers", Caption = "Get Shopify Customers", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 3, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Customer>")]
        public async Task<IEnumerable<Customer>> GetCustomers(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("customers", filters ?? new List<AppFilter>());
            return result.Cast<Customer>().Select(c => c.Attach<Customer>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "InventoryItem", Name = "GetInventoryItems", Caption = "Get Shopify Inventory Items", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 4, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> GetInventoryItems(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("inventory_items", filters ?? new List<AppFilter>());
            return result.Cast<InventoryItem>().Select(i => i.Attach(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Location", Name = "GetLocations", Caption = "Get Shopify Locations", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 5, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Location>")]
        public async Task<IEnumerable<Location>> GetLocations(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("locations", filters ?? new List<AppFilter>());
            return result.Cast<Location>().Select(l => l.Attach(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "CustomCollection", Name = "GetCustomCollections", Caption = "Get Shopify Custom Collections", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 6, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<CustomCollection>")]
        public async Task<IEnumerable<CustomCollection>> GetCustomCollections(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("custom_collections", filters ?? new List<AppFilter>());
            return result.Cast<CustomCollection>().Select(c => c.Attach<CustomCollection>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "SmartCollection", Name = "GetSmartCollections", Caption = "Get Shopify Smart Collections", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 7, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<SmartCollection>")]
        public async Task<IEnumerable<SmartCollection>> GetSmartCollections(List<AppFilter>? filters = null)
        {
            var result = await GetEntityAsync("smart_collections", filters ?? new List<AppFilter>());
            return result.Cast<SmartCollection>().Select(s => s.Attach<SmartCollection>(this));
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Product", Name = "CreateProduct", Caption = "Create Shopify Product", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 8, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Product>")]
        public async Task<IEnumerable<Product>> CreateProductAsync(Product product)
        {
            try
            {
                var result = await PostAsync("products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Product> { createdProduct }.Select(p => p.Attach<Product>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<Product>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Product", Name = "UpdateProduct", Caption = "Update Shopify Product", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 9, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Product>")]
        public async Task<IEnumerable<Product>> UpdateProductAsync(Product product)
        {
            try
            {
                var result = await PutAsync($"products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Product> { updatedProduct }.Select(p => p.Attach<Product>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<Product>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Order", Name = "CreateOrder", Caption = "Create Shopify Order", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Order>")]
        public async Task<IEnumerable<Order>> CreateOrderAsync(Order order)
        {
            try
            {
                var result = await PostAsync("orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Order> { createdOrder }.Select(o => o.Attach<Order>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<Order>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Order", Name = "UpdateOrder", Caption = "Update Shopify Order", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Order>")]
        public async Task<IEnumerable<Order>> UpdateOrderAsync(Order order)
        {
            try
            {
                var result = await PutAsync($"orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<Order>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Order> { updatedOrder }.Select(o => o.Attach<Order>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<Order>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Customer", Name = "CreateCustomer", Caption = "Create Shopify Customer", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Customer>")]
        public async Task<IEnumerable<Customer>> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var result = await PostAsync("customers", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCustomer = JsonSerializer.Deserialize<Customer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Customer> { createdCustomer }.Select(c => c.Attach<Customer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating customer: {ex.Message}");
            }
            return new List<Customer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Customer", Name = "UpdateCustomer", Caption = "Update Shopify Customer", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Customer>")]
        public async Task<IEnumerable<Customer>> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var result = await PutAsync($"customers/{customer.Id}", customer);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCustomer = JsonSerializer.Deserialize<Customer>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Customer> { updatedCustomer }.Select(c => c.Attach<Customer>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating customer: {ex.Message}");
            }
            return new List<Customer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "InventoryItem", Name = "CreateInventoryItem", Caption = "Create Shopify Inventory Item", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> CreateInventoryItemAsync(InventoryItem inventoryItem)
        {
            try
            {
                var result = await PostAsync("inventory_items", inventoryItem);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdInventoryItem = JsonSerializer.Deserialize<InventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<InventoryItem> { createdInventoryItem }.Select(i => i.Attach(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating inventory item: {ex.Message}");
            }
            return new List<InventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "InventoryItem", Name = "UpdateInventoryItem", Caption = "Update Shopify Inventory Item", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> UpdateInventoryItemAsync(InventoryItem inventoryItem)
        {
            try
            {
                var result = await PutAsync($"inventory_items/{inventoryItem.Id}", inventoryItem);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedInventoryItem = JsonSerializer.Deserialize<InventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<InventoryItem> { updatedInventoryItem }.Select(i => i.Attach(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating inventory item: {ex.Message}");
            }
            return new List<InventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Location", Name = "CreateLocation", Caption = "Create Shopify Location", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Location>")]
        public async Task<IEnumerable<Location>> CreateLocationAsync(Location location)
        {
            try
            {
                var result = await PostAsync("locations", location);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdLocation = JsonSerializer.Deserialize<Location>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Location> { createdLocation }.Select(l => l.Attach(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating location: {ex.Message}");
            }
            return new List<Location>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "Location", Name = "UpdateLocation", Caption = "Update Shopify Location", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<Location>")]
        public async Task<IEnumerable<Location>> UpdateLocationAsync(Location location)
        {
            try
            {
                var result = await PutAsync($"locations/{location.Id}", location);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedLocation = JsonSerializer.Deserialize<Location>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Location> { updatedLocation }.Select(l => l.Attach(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating location: {ex.Message}");
            }
            return new List<Location>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "CustomCollection", Name = "CreateCustomCollection", Caption = "Create Shopify Custom Collection", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<CustomCollection>")]
        public async Task<IEnumerable<CustomCollection>> CreateCustomCollectionAsync(CustomCollection customCollection)
        {
            try
            {
                var result = await PostAsync("custom_collections", customCollection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCustomCollection = JsonSerializer.Deserialize<CustomCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<CustomCollection> { createdCustomCollection }.Select(c => c.Attach<CustomCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating custom collection: {ex.Message}");
            }
            return new List<CustomCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "CustomCollection", Name = "UpdateCustomCollection", Caption = "Update Shopify Custom Collection", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<CustomCollection>")]
        public async Task<IEnumerable<CustomCollection>> UpdateCustomCollectionAsync(CustomCollection customCollection)
        {
            try
            {
                var result = await PutAsync($"custom_collections/{customCollection.Id}", customCollection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCustomCollection = JsonSerializer.Deserialize<CustomCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<CustomCollection> { updatedCustomCollection }.Select(c => c.Attach<CustomCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating custom collection: {ex.Message}");
            }
            return new List<CustomCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "SmartCollection", Name = "CreateSmartCollection", Caption = "Create Shopify Smart Collection", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 20, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<SmartCollection>")]
        public async Task<IEnumerable<SmartCollection>> CreateSmartCollectionAsync(SmartCollection smartCollection)
        {
            try
            {
                var result = await PostAsync("smart_collections", smartCollection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdSmartCollection = JsonSerializer.Deserialize<SmartCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SmartCollection> { createdSmartCollection }.Select(s => s.Attach<SmartCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating smart collection: {ex.Message}");
            }
            return new List<SmartCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify, PointType = EnumPointType.Function, ObjectType = "SmartCollection", Name = "UpdateSmartCollection", Caption = "Update Shopify Smart Collection", ClassType = "ShopifyDataSource", Showin = ShowinType.Both, Order = 21, iconimage = "shopify.png", misc = "ReturnType: IEnumerable<SmartCollection>")]
        public async Task<IEnumerable<SmartCollection>> UpdateSmartCollectionAsync(SmartCollection smartCollection)
        {
            try
            {
                var result = await PutAsync($"smart_collections/{smartCollection.Id}", smartCollection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedSmartCollection = JsonSerializer.Deserialize<SmartCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SmartCollection> { updatedSmartCollection }.Select(s => s.Attach<SmartCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating smart collection: {ex.Message}");
            }
            return new List<SmartCollection>();
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
