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
using TheTechIdea.Beep.Connectors.Ecommerce.WixDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.WixDataSource
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix)]
    public class WixDataSource : WebAPIDataSource
    {
        // Wix API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Core Commerce
            ["products"] = ("stores/v1/products", "products", new[] { "" }),
            ["product_variants"] = ("stores/v1/products/{product_id}/variants", "variants", new[] { "product_id" }),
            ["collections"] = ("stores/v1/collections", "collections", new[] { "" }),
            ["orders"] = ("stores/v1/orders", "orders", new[] { "" }),
            ["order_line_items"] = ("stores/v1/orders/{order_id}/lineItems", "lineItems", new[] { "order_id" }),
            ["inventory"] = ("stores/v1/inventoryItems", "inventoryItems", new[] { "" }),
            ["abandoned_carts"] = ("stores/v1/abandonedCarts", "abandonedCarts", new[] { "" }),

            // Content Management
            ["sites"] = ("sites/v1/sites", "sites", new[] { "" }),
            ["pages"] = ("sites/v1/pages", "pages", new[] { "" }),
            ["blogs"] = ("blogs/v1/blogs", "blogs", new[] { "" }),
            ["blog_posts"] = ("blogs/v1/posts", "posts", new[] { "" }),
            ["events"] = ("events/v1/events", "events", new[] { "" }),

            // Marketing
            ["contacts"] = ("contacts/v1/contacts", "contacts", new[] { "" }),
            ["campaigns"] = ("marketing/v1/campaigns", "campaigns", new[] { "" }),
            ["email_subscribers"] = ("marketing/v1/email-subscribers", "subscribers", new[] { "" }),

            // Bookings
            ["bookings_services"] = ("bookings/v1/services", "services", new[] { "" }),
            ["bookings_sessions"] = ("bookings/v1/sessions", "sessions", new[] { "" }),
            ["bookings_appointments"] = ("bookings/v1/appointments", "appointments", new[] { "" }),

            // Store Settings
            ["store_info"] = ("stores/v1/store/info", "", new[] { "" }),
            ["currencies"] = ("stores/v1/currencies", "currencies", new[] { "" }),
            ["taxes"] = ("stores/v1/taxes", "taxes", new[] { "" }),
            ["shipping_rates"] = ("stores/v1/shipping/rates", "rates", new[] { "" }),

            // Analytics
            ["analytics_sales"] = ("analytics/v1/sales", "data", new[] { "" }),
            ["analytics_traffic"] = ("analytics/v1/traffic", "data", new[] { "" }),

            // Forms
            ["forms"] = ("forms/v1/forms", "forms", new[] { "" }),
            ["form_submissions"] = ("forms/v1/submissions", "submissions", new[] { "" }),

            // Media
            ["media_files"] = ("media/v1/files", "files", new[] { "" }),
            ["media_folders"] = ("media/v1/folders", "folders", new[] { "" })
        };

        public WixDataSource(
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

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Helper method to resolve endpoints with parameters
        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "product_id", "order_id" })
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

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Wix entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

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

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Wix entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter ?? new List<AppFilter>());
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root);
        }

        // Helper method to get entity type for deserialization
        public override Type GetEntityType(string entityName) => entityName switch
        {
            "products" => typeof(WixProduct),
            "product_variants" => typeof(WixProductVariant),
            "collections" => typeof(WixCollection),
            "orders" => typeof(WixOrder),
            "order_line_items" => typeof(WixOrderLineItem),
            "inventory" => typeof(WixInventoryItem),
            "abandoned_carts" => typeof(WixAbandonedCart),
            "sites" => typeof(WixSite),
            "pages" => typeof(WixPage),
            "blogs" => typeof(WixBlog),
            "blog_posts" => typeof(WixBlogPost),
            "events" => typeof(WixEvent),
            "contacts" => typeof(WixContact),
            "campaigns" => typeof(WixCampaign),
            "email_subscribers" => typeof(WixEmailSubscriber),
            "bookings_services" => typeof(WixBookingService),
            "bookings_sessions" => typeof(WixBookingSession),
            "bookings_appointments" => typeof(WixBookingAppointment),
            "store_info" => typeof(WixStoreInfo),
            "currencies" => typeof(WixCurrency),
            "taxes" => typeof(WixTax),
            "shipping_rates" => typeof(WixShippingRate),
            "analytics_traffic" => typeof(WixAnalyticsTraffic),
            "forms" => typeof(WixForm),
            "form_submissions" => typeof(WixFormSubmission),
            "media_files" => typeof(WixMediaFile),
            "media_folders" => typeof(WixMediaFolder),
            _ => typeof(Dictionary<string, object>)
        };

        // Helper method to get entity list type for deserialization
        private Type GetEntityListType(string entityName) => entityName switch
        {
            "products" => typeof(List<WixProduct>),
            "product_variants" => typeof(List<WixProductVariant>),
            "collections" => typeof(List<WixCollection>),
            "orders" => typeof(List<WixOrder>),
            "order_line_items" => typeof(List<WixOrderLineItem>),
            "inventory" => typeof(List<WixInventoryItem>),
            "abandoned_carts" => typeof(List<WixAbandonedCart>),
            "sites" => typeof(List<WixSite>),
            "pages" => typeof(List<WixPage>),
            "blogs" => typeof(List<WixBlog>),
            "blog_posts" => typeof(List<WixBlogPost>),
            "events" => typeof(List<WixEvent>),
            "contacts" => typeof(List<WixContact>),
            "campaigns" => typeof(List<WixCampaign>),
            "email_subscribers" => typeof(List<WixEmailSubscriber>),
            "bookings_services" => typeof(List<WixBookingService>),
            "bookings_sessions" => typeof(List<WixBookingSession>),
            "bookings_appointments" => typeof(List<WixBookingAppointment>),
            "currencies" => typeof(List<WixCurrency>),
            "taxes" => typeof(List<WixTax>),
            "shipping_rates" => typeof(List<WixShippingRate>),
            "analytics_traffic" => typeof(List<WixAnalyticsTraffic>),
            "forms" => typeof(List<WixForm>),
            "form_submissions" => typeof(List<WixFormSubmission>),
            "media_files" => typeof(List<WixMediaFile>),
            "media_folders" => typeof(List<WixMediaFolder>),
            _ => typeof(List<Dictionary<string, object>>)
        };

        // Properties for Wix authentication
        private new string? ApiKey => (Dataconnection?.ConnectionProp as WebAPIConnectionProperties)?.ApiKey;
        private string? BaseURL => (Dataconnection?.ConnectionProp as WebAPIConnectionProperties)?.Url;

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

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Products", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixProduct>")]
        public async Task<IEnumerable<WixProduct>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("products", new List<AppFilter> { filter });
            return result.Cast<WixProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Orders", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixOrder>")]
        public async Task<IEnumerable<WixOrder>> GetOrders(AppFilter filter)
        {
            var result = await GetEntityAsync("orders", new List<AppFilter> { filter });
            return result.Cast<WixOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Collections", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixCollection>")]
        public async Task<IEnumerable<WixCollection>> GetCollections(AppFilter filter)
        {
            var result = await GetEntityAsync("collections", new List<AppFilter> { filter });
            return result.Cast<WixCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Contacts", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixContact>")]
        public async Task<IEnumerable<WixContact>> GetContacts(AppFilter filter)
        {
            var result = await GetEntityAsync("contacts", new List<AppFilter> { filter });
            return result.Cast<WixContact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Inventory", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixInventoryItem>")]
        public async Task<IEnumerable<WixInventoryItem>> GetInventory(AppFilter filter)
        {
            var result = await GetEntityAsync("inventory", new List<AppFilter> { filter });
            return result.Cast<WixInventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Coupons", ClassName = "WixDataSource", Showin = ShowinType.Both, misc = "IEnumerable<WixCoupon>")]
        public async Task<IEnumerable<WixCoupon>> GetCoupons(AppFilter filter)
        {
            var result = await GetEntityAsync("coupons", new List<AppFilter> { filter });
            return result.Cast<WixCoupon>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Products", Name = "CreateProduct", Caption = "Create Wix Product", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixProduct>")]
        public async Task<IEnumerable<WixProduct>> CreateProductAsync(WixProduct product)
        {
            try
            {
                var result = await PostAsync("stores/v1/products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<WixProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixProduct> { createdProduct }.Select(p => p.Attach<WixProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<WixProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Products", Name = "UpdateProduct", Caption = "Update Wix Product", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixProduct>")]
        public async Task<IEnumerable<WixProduct>> UpdateProductAsync(WixProduct product)
        {
            try
            {
                var result = await PutAsync($"stores/v1/products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<WixProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixProduct> { updatedProduct }.Select(p => p.Attach<WixProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<WixProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "CreateOrder", Caption = "Create Wix Order", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixOrder>")]
        public async Task<IEnumerable<WixOrder>> CreateOrderAsync(WixOrder order)
        {
            try
            {
                var result = await PostAsync("stores/v1/orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<WixOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixOrder> { createdOrder }.Select(o => o.Attach<WixOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<WixOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "UpdateOrder", Caption = "Update Wix Order", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixOrder>")]
        public async Task<IEnumerable<WixOrder>> UpdateOrderAsync(WixOrder order)
        {
            try
            {
                var result = await PutAsync($"stores/v1/orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<WixOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixOrder> { updatedOrder }.Select(o => o.Attach<WixOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<WixOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Collections", Name = "CreateCollection", Caption = "Create Wix Collection", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixCollection>")]
        public async Task<IEnumerable<WixCollection>> CreateCollectionAsync(WixCollection collection)
        {
            try
            {
                var result = await PostAsync("stores/v1/collections", collection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCollection = JsonSerializer.Deserialize<WixCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixCollection> { createdCollection }.Select(c => c.Attach<WixCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating collection: {ex.Message}");
            }
            return new List<WixCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Collections", Name = "UpdateCollection", Caption = "Update Wix Collection", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixCollection>")]
        public async Task<IEnumerable<WixCollection>> UpdateCollectionAsync(WixCollection collection)
        {
            try
            {
                var result = await PutAsync($"stores/v1/collections/{collection.Id}", collection);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCollection = JsonSerializer.Deserialize<WixCollection>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixCollection> { updatedCollection }.Select(c => c.Attach<WixCollection>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating collection: {ex.Message}");
            }
            return new List<WixCollection>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Contacts", Name = "CreateContact", Caption = "Create Wix Contact", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixContact>")]
        public async Task<IEnumerable<WixContact>> CreateContactAsync(WixContact contact)
        {
            try
            {
                var result = await PostAsync("contacts/v1/contacts", contact);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdContact = JsonSerializer.Deserialize<WixContact>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixContact> { createdContact }.Select(c => c.Attach<WixContact>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating contact: {ex.Message}");
            }
            return new List<WixContact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Contacts", Name = "UpdateContact", Caption = "Update Wix Contact", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixContact>")]
        public async Task<IEnumerable<WixContact>> UpdateContactAsync(WixContact contact)
        {
            try
            {
                var result = await PutAsync($"contacts/v1/contacts/{contact.Id}", contact);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedContact = JsonSerializer.Deserialize<WixContact>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixContact> { updatedContact }.Select(c => c.Attach<WixContact>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating contact: {ex.Message}");
            }
            return new List<WixContact>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Inventory", Name = "CreateInventory", Caption = "Create Wix Inventory Item", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixInventoryItem>")]
        public async Task<IEnumerable<WixInventoryItem>> CreateInventoryAsync(WixInventoryItem inventory)
        {
            try
            {
                var result = await PostAsync("stores/v1/inventoryItems", inventory);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdInventory = JsonSerializer.Deserialize<WixInventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixInventoryItem> { createdInventory }.Select(i => i.Attach<WixInventoryItem>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating inventory: {ex.Message}");
            }
            return new List<WixInventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Inventory", Name = "UpdateInventory", Caption = "Update Wix Inventory Item", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixInventoryItem>")]
        public async Task<IEnumerable<WixInventoryItem>> UpdateInventoryAsync(WixInventoryItem inventory)
        {
            try
            {
                var result = await PutAsync($"stores/v1/inventoryItems/{inventory.Id}", inventory);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedInventory = JsonSerializer.Deserialize<WixInventoryItem>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixInventoryItem> { updatedInventory }.Select(i => i.Attach<WixInventoryItem>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating inventory: {ex.Message}");
            }
            return new List<WixInventoryItem>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Coupons", Name = "CreateCoupon", Caption = "Create Wix Coupon", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 20, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixCoupon>")]
        public async Task<IEnumerable<WixCoupon>> CreateCouponAsync(WixCoupon coupon)
        {
            try
            {
                var result = await PostAsync("stores/v1/coupons", coupon);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCoupon = JsonSerializer.Deserialize<WixCoupon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixCoupon> { createdCoupon }.Select(c => c.Attach<WixCoupon>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating coupon: {ex.Message}");
            }
            return new List<WixCoupon>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Wix, PointType = EnumPointType.Function, ObjectType ="Coupons", Name = "UpdateCoupon", Caption = "Update Wix Coupon", ClassType ="WixDataSource", Showin = ShowinType.Both, Order = 21, iconimage = "wix.png", misc = "ReturnType: IEnumerable<WixCoupon>")]
        public async Task<IEnumerable<WixCoupon>> UpdateCouponAsync(WixCoupon coupon)
        {
            try
            {
                var result = await PutAsync($"stores/v1/coupons/{coupon.Id}", coupon);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCoupon = JsonSerializer.Deserialize<WixCoupon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<WixCoupon> { updatedCoupon }.Select(c => c.Attach<WixCoupon>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating coupon: {ex.Message}");
            }
            return new List<WixCoupon>();
        }

        #endregion
    }
}
