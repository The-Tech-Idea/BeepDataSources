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
using TheTechIdea.Beep.Connectors.Ecommerce.Magento.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Magento
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento)]
    public class MagentoDataSource : WebAPIDataSource
    {
        // Supported Magento entities -> Magento endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                // Products
                ["products"] = ("products", null, Array.Empty<string>()),
                ["product"] = ("products/{sku}", null, new[] { "sku" }),

                // Categories
                ["categories"] = ("categories", null, Array.Empty<string>()),
                ["category"] = ("categories/{id}", null, new[] { "id" }),

                // Orders
                ["orders"] = ("orders", null, Array.Empty<string>()),
                ["order"] = ("orders/{id}", null, new[] { "id" }),

                // Customers
                ["customers"] = ("customers", null, Array.Empty<string>()),
                ["customer"] = ("customers/{id}", null, new[] { "id" }),

                // Inventory
                ["inventory"] = ("inventory/source-items", null, Array.Empty<string>()),
                ["inventory_item"] = ("inventory/source-items/{sku}", null, new[] { "sku" }),

                // Cart/Quote
                ["carts"] = ("carts", null, Array.Empty<string>()),
                ["cart"] = ("carts/{cartId}", null, new[] { "cartId" }),

                // Reviews
                ["reviews"] = ("reviews", null, Array.Empty<string>()),
                ["review"] = ("reviews/{id}", null, new[] { "id" }),

                // Store/Config
                ["store_configs"] = ("store/storeConfigs", null, Array.Empty<string>()),
                ["store_config"] = ("store/storeConfigs/{id}", null, new[] { "id" }),

                // Attributes
                ["attributes"] = ("products/attributes", null, Array.Empty<string>()),
                ["attribute"] = ("products/attributes/{attributeCode}", null, new[] { "attributeCode" }),

                // Tax Rules
                ["tax_rules"] = ("taxRules", null, Array.Empty<string>()),
                ["tax_rule"] = ("taxRules/{ruleId}", null, new[] { "ruleId" })
            };

        public MagentoDataSource(
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
            try
            {
                if (!Map.TryGetValue(EntityName, out var mapping))
                {
                    Logger.WriteLog($"Unknown Magento entity: {EntityName}");
                    return new List<object>();
                }

                var (endpoint, root, requiredFilters) = mapping;
                var q = FiltersToQuery(Filter);
                RequireFilters(EntityName, q, requiredFilters);

                var resolvedEndpoint = ResolveEndpoint(endpoint, q);

                using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
                if (resp is null || !resp.IsSuccessStatusCode)
                {
                    Logger.WriteLog($"API request failed for {EntityName}: {resp?.StatusCode}");
                    return new List<object>();
                }

                var jsonContent = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseResponse(jsonContent, EntityName);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error in GetEntityAsync for {EntityName}: {ex.Message}");
                return new List<object>();
            }
        }

        #region Response Parsing

        private IEnumerable<object> ParseResponse(string jsonContent, string entityName)
        {
            try
            {
                return entityName.ToLower() switch
                {
                    "products" => ExtractArray<Product>(jsonContent),
                    "product" => new List<object> { ExtractSingle<Product>(jsonContent) },
                    "categories" => ExtractArray<Category>(jsonContent),
                    "category" => new List<object> { ExtractSingle<Category>(jsonContent) },
                    "orders" => ExtractArray<Order>(jsonContent),
                    "order" => new List<object> { ExtractSingle<Order>(jsonContent) },
                    "customers" => ExtractArray<Customer>(jsonContent),
                    "customer" => new List<object> { ExtractSingle<Customer>(jsonContent) },
                    "inventory" => ExtractArray<InventoryItem>(jsonContent),
                    "inventory_item" => new List<object> { ExtractSingle<InventoryItem>(jsonContent) },
                    "carts" => ExtractArray<Cart>(jsonContent),
                    "cart" => new List<object> { ExtractSingle<Cart>(jsonContent) },
                    "reviews" => ExtractArray<Review>(jsonContent),
                    "review" => new List<object> { ExtractSingle<Review>(jsonContent) },
                    "store_configs" => ExtractArray<StoreConfig>(jsonContent),
                    "store_config" => new List<object> { ExtractSingle<StoreConfig>(jsonContent) },
                    "attributes" => ExtractArray<Models.Attribute>(jsonContent),
                    "attribute" => new List<object> { ExtractSingle<Models.Attribute>(jsonContent) },
                    "tax_rules" => ExtractArray<TaxRule>(jsonContent),
                    "tax_rule" => new List<object> { ExtractSingle<TaxRule>(jsonContent) },
                    _ => JsonSerializer.Deserialize<MagentoApiResponse<JsonElement>>(jsonContent)?.Items.EnumerateArray().Select(x => (object)x).ToList() ?? new List<object>()
                };
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error parsing response for {entityName}: {ex.Message}");
                return new List<object>();
            }
        }

        private List<T> ExtractArray<T>(string jsonContent) where T : MagentoEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResponse = JsonSerializer.Deserialize<MagentoApiResponse<List<T>>>(jsonContent, options);
                if (apiResponse?.Items != null)
                {
                    foreach (var item in apiResponse.Items)
                    {
                        item.Attach<T>((IDataSource)this);
                    }
                }
                return apiResponse?.Items ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting array of {typeof(T).Name}: {ex.Message}");
                return new List<T>();
            }
        }

        private T ExtractSingle<T>(string jsonContent) where T : MagentoEntityBase
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var item = JsonSerializer.Deserialize<T>(jsonContent, options);
                if (item != null)
                {
                    item.Attach<T>((IDataSource)this);
                }
                return item;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error extracting single {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        #endregion

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Product", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Product>")]
        public async Task<IEnumerable<Product>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("products", new List<AppFilter> { filter });
            return result.Cast<Product>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Category", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Category>")]
        public async Task<IEnumerable<Category>> GetCategories(AppFilter filter)
        {
            var result = await GetEntityAsync("categories", new List<AppFilter> { filter });
            return result.Cast<Category>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Order", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Order>")]
        public async Task<IEnumerable<Order>> GetOrders(AppFilter filter)
        {
            var result = await GetEntityAsync("orders", new List<AppFilter> { filter });
            return result.Cast<Order>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Customer", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Customer>")]
        public async Task<IEnumerable<Customer>> GetCustomers(AppFilter filter)
        {
            var result = await GetEntityAsync("customers", new List<AppFilter> { filter });
            return result.Cast<Customer>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="InventoryItem", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> GetInventory(AppFilter filter)
        {
            var result = await GetEntityAsync("inventory", new List<AppFilter> { filter });
            return result.Cast<InventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Cart", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Cart>")]
        public async Task<IEnumerable<Cart>> GetCarts(AppFilter filter)
        {
            var result = await GetEntityAsync("carts", new List<AppFilter> { filter });
            return result.Cast<Cart>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Review", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Review>")]
        public async Task<IEnumerable<Review>> GetReviews(AppFilter filter)
        {
            var result = await GetEntityAsync("reviews", new List<AppFilter> { filter });
            return result.Cast<Review>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="StoreConfig", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<StoreConfig>")]
        public async Task<IEnumerable<StoreConfig>> GetStoreConfigs(AppFilter filter)
        {
            var result = await GetEntityAsync("store_configs", new List<AppFilter> { filter });
            return result.Cast<StoreConfig>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Attribute", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<Models.Attribute>")]
        public async Task<IEnumerable<Models.Attribute>> GetAttributes(AppFilter filter)
        {
            var result = await GetEntityAsync("attributes", new List<AppFilter> { filter });
            return result.Cast<Models.Attribute>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="TaxRule", ClassName = "MagentoDataSource", Showin = ShowinType.Both, misc = "IEnumerable<TaxRule>")]
        public async Task<IEnumerable<TaxRule>> GetTaxRules(AppFilter filter)
        {
            var result = await GetEntityAsync("tax_rules", new List<AppFilter> { filter });
            return result.Cast<TaxRule>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Product", Name = "CreateProduct", Caption = "Create Magento Product", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Product>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Product", Name = "UpdateProduct", Caption = "Update Magento Product", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Product>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Category", Name = "CreateCategory", Caption = "Create Magento Category", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Category>")]
        public async Task<IEnumerable<Category>> CreateCategoryAsync(Category category)
        {
            try
            {
                var result = await PostAsync("categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<Category>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Category> { createdCategory }.Select(c => c.Attach<Category>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<Category>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Category", Name = "UpdateCategory", Caption = "Update Magento Category", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Category>")]
        public async Task<IEnumerable<Category>> UpdateCategoryAsync(Category category)
        {
            try
            {
                var result = await PutAsync($"categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<Category>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Category> { updatedCategory }.Select(c => c.Attach<Category>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<Category>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Order", Name = "CreateOrder", Caption = "Create Magento Order", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Order>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Order", Name = "UpdateOrder", Caption = "Update Magento Order", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Order>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Customer", Name = "CreateCustomer", Caption = "Create Magento Customer", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Customer>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Customer", Name = "UpdateCustomer", Caption = "Update Magento Customer", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Customer>")]
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="InventoryItem", Name = "CreateInventoryItem", Caption = "Create Magento Inventory Item", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "magento.png", misc = "ReturnType: IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> CreateInventoryItemAsync(InventoryItem inventoryItem)
        {
            try
            {
                var result = await PostAsync("inventory", inventoryItem);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdInventoryItem = JsonSerializer.Deserialize<InventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<InventoryItem> { createdInventoryItem }.Select(i => i.Attach<InventoryItem>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating inventory item: {ex.Message}");
            }
            return new List<InventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="InventoryItem", Name = "UpdateInventoryItem", Caption = "Update Magento Inventory Item", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 20, iconimage = "magento.png", misc = "ReturnType: IEnumerable<InventoryItem>")]
        public async Task<IEnumerable<InventoryItem>> UpdateInventoryItemAsync(InventoryItem inventoryItem)
        {
            try
            {
                var result = await PutAsync($"inventory/{inventoryItem.Id}", inventoryItem);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedInventoryItem = JsonSerializer.Deserialize<InventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<InventoryItem> { updatedInventoryItem }.Select(i => i.Attach<InventoryItem>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating inventory item: {ex.Message}");
            }
            return new List<InventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Cart", Name = "CreateCart", Caption = "Create Magento Cart", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 21, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Cart>")]
        public async Task<IEnumerable<Cart>> CreateCartAsync(Cart cart)
        {
            try
            {
                var result = await PostAsync("carts", cart);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCart = JsonSerializer.Deserialize<Cart>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Cart> { createdCart }.Select(c => c.Attach<Cart>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating cart: {ex.Message}");
            }
            return new List<Cart>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Cart", Name = "UpdateCart", Caption = "Update Magento Cart", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 22, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Cart>")]
        public async Task<IEnumerable<Cart>> UpdateCartAsync(Cart cart)
        {
            try
            {
                var result = await PutAsync($"carts/{cart.Id}", cart);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCart = JsonSerializer.Deserialize<Cart>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Cart> { updatedCart }.Select(c => c.Attach<Cart>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating cart: {ex.Message}");
            }
            return new List<Cart>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Review", Name = "CreateReview", Caption = "Create Magento Review", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 23, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Review>")]
        public async Task<IEnumerable<Review>> CreateReviewAsync(Review review)
        {
            try
            {
                var result = await PostAsync("reviews", review);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdReview = JsonSerializer.Deserialize<Review>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Review> { createdReview }.Select(r => r.Attach<Review>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating review: {ex.Message}");
            }
            return new List<Review>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Review", Name = "UpdateReview", Caption = "Update Magento Review", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 24, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Review>")]
        public async Task<IEnumerable<Review>> UpdateReviewAsync(Review review)
        {
            try
            {
                var result = await PutAsync($"reviews/{review.Id}", review);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedReview = JsonSerializer.Deserialize<Review>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Review> { updatedReview }.Select(r => r.Attach<Review>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating review: {ex.Message}");
            }
            return new List<Review>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="StoreConfig", Name = "CreateStoreConfig", Caption = "Create Magento Store Config", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 25, iconimage = "magento.png", misc = "ReturnType: IEnumerable<StoreConfig>")]
        public async Task<IEnumerable<StoreConfig>> CreateStoreConfigAsync(StoreConfig storeConfig)
        {
            try
            {
                var result = await PostAsync("store_configs", storeConfig);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdStoreConfig = JsonSerializer.Deserialize<StoreConfig>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<StoreConfig> { createdStoreConfig }.Select(s => s.Attach<StoreConfig>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating store config: {ex.Message}");
            }
            return new List<StoreConfig>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="StoreConfig", Name = "UpdateStoreConfig", Caption = "Update Magento Store Config", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 26, iconimage = "magento.png", misc = "ReturnType: IEnumerable<StoreConfig>")]
        public async Task<IEnumerable<StoreConfig>> UpdateStoreConfigAsync(StoreConfig storeConfig)
        {
            try
            {
                var result = await PutAsync($"store_configs/{storeConfig.Id}", storeConfig);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedStoreConfig = JsonSerializer.Deserialize<StoreConfig>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<StoreConfig> { updatedStoreConfig }.Select(s => s.Attach<StoreConfig>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating store config: {ex.Message}");
            }
            return new List<StoreConfig>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Attribute", Name = "CreateAttribute", Caption = "Create Magento Attribute", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 27, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Models.Attribute>")]
        public async Task<IEnumerable<Models.Attribute>> CreateAttributeAsync(Models.Attribute attribute)
        {
            try
            {
                var result = await PostAsync("attributes", attribute);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdAttribute = JsonSerializer.Deserialize<Models.Attribute>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Models.Attribute> { createdAttribute }.Select(a => a.Attach<Models.Attribute>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating attribute: {ex.Message}");
            }
            return new List<Models.Attribute>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="Attribute", Name = "UpdateAttribute", Caption = "Update Magento Attribute", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 28, iconimage = "magento.png", misc = "ReturnType: IEnumerable<Models.Attribute>")]
        public async Task<IEnumerable<Models.Attribute>> UpdateAttributeAsync(Models.Attribute attribute)
        {
            try
            {
                var result = await PutAsync($"attributes/{attribute.Id}", attribute);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedAttribute = JsonSerializer.Deserialize<Models.Attribute>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Models.Attribute> { updatedAttribute }.Select(a => a.Attach<Models.Attribute>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating attribute: {ex.Message}");
            }
            return new List<Models.Attribute>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="TaxRule", Name = "CreateTaxRule", Caption = "Create Magento Tax Rule", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 29, iconimage = "magento.png", misc = "ReturnType: IEnumerable<TaxRule>")]
        public async Task<IEnumerable<TaxRule>> CreateTaxRuleAsync(TaxRule taxRule)
        {
            try
            {
                var result = await PostAsync("tax_rules", taxRule);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTaxRule = JsonSerializer.Deserialize<TaxRule>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<TaxRule> { createdTaxRule }.Select(t => t.Attach<TaxRule>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating tax rule: {ex.Message}");
            }
            return new List<TaxRule>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Magento, PointType = EnumPointType.Function, ObjectType ="TaxRule", Name = "UpdateTaxRule", Caption = "Update Magento Tax Rule", ClassType ="MagentoDataSource", Showin = ShowinType.Both, Order = 30, iconimage = "magento.png", misc = "ReturnType: IEnumerable<TaxRule>")]
        public async Task<IEnumerable<TaxRule>> UpdateTaxRuleAsync(TaxRule taxRule)
        {
            try
            {
                var result = await PutAsync($"tax_rules/{taxRule.Id}", taxRule);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTaxRule = JsonSerializer.Deserialize<TaxRule>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<TaxRule> { updatedTaxRule }.Select(t => t.Attach<TaxRule>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating tax rule: {ex.Message}");
            }
            return new List<TaxRule>();
        }

        #endregion

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Magento entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, requiredFilters);

            // Magento uses page-based pagination
            q["pageSize"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Magento max is typically 100
            q["currentPage"] = Math.Max(1, pageNumber).ToString();

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = GetAsync(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            if (resp is null || !resp.IsSuccessStatusCode)
                return new PagedResult { Data = Array.Empty<object>() };

            var jsonContent = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ParseResponse(jsonContent, EntityName);

            return new PagedResult
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = items.Count(), // Magento may provide total count in response
                TotalPages = 1,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count() >= pageSize
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
                throw new ArgumentException($"Magento entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "id", "sku", "cartId", "attributeCode", "ruleId" })
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

        #region Configuration Classes

        private class MagentoApiResponse<T>
        {
            [System.Text.Json.Serialization.JsonPropertyName("items")]
            public T? Items { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("search_criteria")]
            public object? SearchCriteria { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("total_count")]
            public int? TotalCount { get; set; }
        }

        #endregion
    }
}