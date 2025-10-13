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

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Products", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceProduct>")]
        public async Task<IEnumerable<SquarespaceProduct>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("products", new List<AppFilter> { filter });
            return result.Cast<SquarespaceProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Orders", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceOrder>")]
        public async Task<IEnumerable<SquarespaceOrder>> GetOrders(AppFilter filter)
        {
            var result = await GetEntityAsync("orders", new List<AppFilter> { filter });
            return result.Cast<SquarespaceOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Profiles", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceProfile>")]
        public async Task<IEnumerable<SquarespaceProfile>> GetProfiles(AppFilter filter)
        {
            var result = await GetEntityAsync("profiles", new List<AppFilter> { filter });
            return result.Cast<SquarespaceProfile>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Pages", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespacePage>")]
        public async Task<IEnumerable<SquarespacePage>> GetPages(AppFilter filter)
        {
            var result = await GetEntityAsync("pages", new List<AppFilter> { filter });
            return result.Cast<SquarespacePage>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Blogs", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceBlog>")]
        public async Task<IEnumerable<SquarespaceBlog>> GetBlogs(AppFilter filter)
        {
            var result = await GetEntityAsync("blogs", new List<AppFilter> { filter });
            return result.Cast<SquarespaceBlog>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Events", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceEvent>")]
        public async Task<IEnumerable<SquarespaceEvent>> GetEvents(AppFilter filter)
        {
            var result = await GetEntityAsync("events", new List<AppFilter> { filter });
            return result.Cast<SquarespaceEvent>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Galleries", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceGallery>")]
        public async Task<IEnumerable<SquarespaceGallery>> GetGalleries(AppFilter filter)
        {
            var result = await GetEntityAsync("galleries", new List<AppFilter> { filter });
            return result.Cast<SquarespaceGallery>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Categories", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceCategory>")]
        public async Task<IEnumerable<SquarespaceCategory>> GetCategories(AppFilter filter)
        {
            var result = await GetEntityAsync("categories", new List<AppFilter> { filter });
            return result.Cast<SquarespaceCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Inventory", ClassName = "SquarespaceDataSource", Showin = ShowinType.Both, misc = "IEnumerable<SquarespaceInventory>")]
        public async Task<IEnumerable<SquarespaceInventory>> GetInventory(AppFilter filter)
        {
            var result = await GetEntityAsync("inventory", new List<AppFilter> { filter });
            return result.Cast<SquarespaceInventory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Products", Name = "CreateProduct", Caption = "Create Squarespace Product", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceProduct>")]
        public async Task<IEnumerable<SquarespaceProduct>> CreateProductAsync(SquarespaceProduct product)
        {
            try
            {
                var result = await PostAsync("products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<SquarespaceProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceProduct> { createdProduct }.Select(p => p.Attach<SquarespaceProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<SquarespaceProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Products", Name = "UpdateProduct", Caption = "Update Squarespace Product", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceProduct>")]
        public async Task<IEnumerable<SquarespaceProduct>> UpdateProductAsync(SquarespaceProduct product)
        {
            try
            {
                var result = await PutAsync($"products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<SquarespaceProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceProduct> { updatedProduct }.Select(p => p.Attach<SquarespaceProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<SquarespaceProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Orders", Name = "CreateOrder", Caption = "Create Squarespace Order", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceOrder>")]
        public async Task<IEnumerable<SquarespaceOrder>> CreateOrderAsync(SquarespaceOrder order)
        {
            try
            {
                var result = await PostAsync("orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<SquarespaceOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceOrder> { createdOrder }.Select(o => o.Attach<SquarespaceOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<SquarespaceOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Orders", Name = "UpdateOrder", Caption = "Update Squarespace Order", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceOrder>")]
        public async Task<IEnumerable<SquarespaceOrder>> UpdateOrderAsync(SquarespaceOrder order)
        {
            try
            {
                var result = await PutAsync($"orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<SquarespaceOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceOrder> { updatedOrder }.Select(o => o.Attach<SquarespaceOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<SquarespaceOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Profiles", Name = "CreateProfile", Caption = "Create Squarespace Profile", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceProfile>")]
        public async Task<IEnumerable<SquarespaceProfile>> CreateProfileAsync(SquarespaceProfile profile)
        {
            try
            {
                var result = await PostAsync("profiles", profile);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProfile = JsonSerializer.Deserialize<SquarespaceProfile>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceProfile> { createdProfile }.Select(p => p.Attach<SquarespaceProfile>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating profile: {ex.Message}");
            }
            return new List<SquarespaceProfile>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Profiles", Name = "UpdateProfile", Caption = "Update Squarespace Profile", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceProfile>")]
        public async Task<IEnumerable<SquarespaceProfile>> UpdateProfileAsync(SquarespaceProfile profile)
        {
            try
            {
                var result = await PutAsync($"profiles/{profile.Id}", profile);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProfile = JsonSerializer.Deserialize<SquarespaceProfile>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceProfile> { updatedProfile }.Select(p => p.Attach<SquarespaceProfile>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating profile: {ex.Message}");
            }
            return new List<SquarespaceProfile>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Pages", Name = "CreatePage", Caption = "Create Squarespace Page", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 16, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespacePage>")]
        public async Task<IEnumerable<SquarespacePage>> CreatePageAsync(SquarespacePage page)
        {
            try
            {
                var result = await PostAsync("pages", page);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdPage = JsonSerializer.Deserialize<SquarespacePage>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespacePage> { createdPage }.Select(p => p.Attach<SquarespacePage>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating page: {ex.Message}");
            }
            return new List<SquarespacePage>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Pages", Name = "UpdatePage", Caption = "Update Squarespace Page", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 17, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespacePage>")]
        public async Task<IEnumerable<SquarespacePage>> UpdatePageAsync(SquarespacePage page)
        {
            try
            {
                var result = await PutAsync($"pages/{page.Id}", page);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedPage = JsonSerializer.Deserialize<SquarespacePage>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespacePage> { updatedPage }.Select(p => p.Attach<SquarespacePage>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating page: {ex.Message}");
            }
            return new List<SquarespacePage>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Blogs", Name = "CreateBlog", Caption = "Create Squarespace Blog", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 18, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceBlog>")]
        public async Task<IEnumerable<SquarespaceBlog>> CreateBlogAsync(SquarespaceBlog blog)
        {
            try
            {
                var result = await PostAsync("blogs", blog);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdBlog = JsonSerializer.Deserialize<SquarespaceBlog>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceBlog> { createdBlog }.Select(b => b.Attach<SquarespaceBlog>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating blog: {ex.Message}");
            }
            return new List<SquarespaceBlog>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Blogs", Name = "UpdateBlog", Caption = "Update Squarespace Blog", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 19, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceBlog>")]
        public async Task<IEnumerable<SquarespaceBlog>> UpdateBlogAsync(SquarespaceBlog blog)
        {
            try
            {
                var result = await PutAsync($"blogs/{blog.Id}", blog);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedBlog = JsonSerializer.Deserialize<SquarespaceBlog>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceBlog> { updatedBlog }.Select(b => b.Attach<SquarespaceBlog>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating blog: {ex.Message}");
            }
            return new List<SquarespaceBlog>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Events", Name = "CreateEvent", Caption = "Create Squarespace Event", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 20, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceEvent>")]
        public async Task<IEnumerable<SquarespaceEvent>> CreateEventAsync(SquarespaceEvent @event)
        {
            try
            {
                var result = await PostAsync("events", @event);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdEvent = JsonSerializer.Deserialize<SquarespaceEvent>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceEvent> { createdEvent }.Select(e => e.Attach<SquarespaceEvent>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating event: {ex.Message}");
            }
            return new List<SquarespaceEvent>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Events", Name = "UpdateEvent", Caption = "Update Squarespace Event", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 21, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceEvent>")]
        public async Task<IEnumerable<SquarespaceEvent>> UpdateEventAsync(SquarespaceEvent @event)
        {
            try
            {
                var result = await PutAsync($"events/{@event.Id}", @event);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedEvent = JsonSerializer.Deserialize<SquarespaceEvent>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceEvent> { updatedEvent }.Select(e => e.Attach<SquarespaceEvent>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating event: {ex.Message}");
            }
            return new List<SquarespaceEvent>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Galleries", Name = "CreateGallery", Caption = "Create Squarespace Gallery", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 22, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceGallery>")]
        public async Task<IEnumerable<SquarespaceGallery>> CreateGalleryAsync(SquarespaceGallery gallery)
        {
            try
            {
                var result = await PostAsync("galleries", gallery);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdGallery = JsonSerializer.Deserialize<SquarespaceGallery>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceGallery> { createdGallery }.Select(g => g.Attach<SquarespaceGallery>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating gallery: {ex.Message}");
            }
            return new List<SquarespaceGallery>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Galleries", Name = "UpdateGallery", Caption = "Update Squarespace Gallery", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 23, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceGallery>")]
        public async Task<IEnumerable<SquarespaceGallery>> UpdateGalleryAsync(SquarespaceGallery gallery)
        {
            try
            {
                var result = await PutAsync($"galleries/{gallery.Id}", gallery);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedGallery = JsonSerializer.Deserialize<SquarespaceGallery>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceGallery> { updatedGallery }.Select(g => g.Attach<SquarespaceGallery>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating gallery: {ex.Message}");
            }
            return new List<SquarespaceGallery>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Categories", Name = "CreateCategory", Caption = "Create Squarespace Category", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 24, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceCategory>")]
        public async Task<IEnumerable<SquarespaceCategory>> CreateCategoryAsync(SquarespaceCategory category)
        {
            try
            {
                var result = await PostAsync("categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<SquarespaceCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceCategory> { createdCategory }.Select(c => c.Attach<SquarespaceCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<SquarespaceCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Categories", Name = "UpdateCategory", Caption = "Update Squarespace Category", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 25, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceCategory>")]
        public async Task<IEnumerable<SquarespaceCategory>> UpdateCategoryAsync(SquarespaceCategory category)
        {
            try
            {
                var result = await PutAsync($"categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<SquarespaceCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceCategory> { updatedCategory }.Select(c => c.Attach<SquarespaceCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<SquarespaceCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Inventory", Name = "CreateInventory", Caption = "Create Squarespace Inventory", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 26, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceInventory>")]
        public async Task<IEnumerable<SquarespaceInventory>> CreateInventoryAsync(SquarespaceInventory inventory)
        {
            try
            {
                var result = await PostAsync("inventory", inventory);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdInventory = JsonSerializer.Deserialize<SquarespaceInventory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceInventory> { createdInventory }.Select(i => i.Attach<SquarespaceInventory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating inventory: {ex.Message}");
            }
            return new List<SquarespaceInventory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Squarespace, PointType = EnumPointType.Function, ObjectType = "Inventory", Name = "UpdateInventory", Caption = "Update Squarespace Inventory", ClassType = "SquarespaceDataSource", Showin = ShowinType.Both, Order = 27, iconimage = "squarespace.png", misc = "ReturnType: IEnumerable<SquarespaceInventory>")]
        public async Task<IEnumerable<SquarespaceInventory>> UpdateInventoryAsync(SquarespaceInventory inventory)
        {
            try
            {
                var result = await PutAsync($"inventory/{inventory.Id}", inventory);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedInventory = JsonSerializer.Deserialize<SquarespaceInventory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<SquarespaceInventory> { updatedInventory }.Select(i => i.Attach<SquarespaceInventory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating inventory: {ex.Message}");
            }
            return new List<SquarespaceInventory>();
        }

        #endregion
    }
}