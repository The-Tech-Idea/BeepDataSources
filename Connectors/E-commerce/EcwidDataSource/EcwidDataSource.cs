// File: BeepDM/Connectors/Ecommerce/EcwidDataSource/EcwidDataSource.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Ecommerce.EcwidDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.EcwidDataSource
{
    /// <summary>
    /// Ecwid data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid)]
    public class EcwidDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Ecwid API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Products
            ["products"] = "products",
            ["productvariations"] = "products/{productId}/variations",
            ["productimages"] = "products/{productId}/images",
            ["productattributes"] = "products/{productId}/attributes",
            ["productoptions"] = "products/{productId}/options",
            ["productfiles"] = "products/{productId}/files",
            ["productwholesaleprices"] = "products/{productId}/wholesalePrices",
            ["productrelated"] = "products/{productId}/related",
            ["productupselled"] = "products/{productId}/upsell",
            ["productcrossselled"] = "products/{productId}/crosssell",
            ["productinventory"] = "products/{productId}/inventory",
            ["productreviews"] = "productreviews",

            // Categories
            ["categories"] = "categories",
            ["categoryproducts"] = "categories/{categoryId}/products",

            // Orders
            ["orders"] = "orders",
            ["orderdetails"] = "orders/{orderNumber}",
            ["orderitems"] = "orders/{orderNumber}/items",

            // Customers
            ["customers"] = "customers",
            ["customerorders"] = "customers/{customerId}/orders",
            ["customeraddresses"] = "customers/{customerId}/addresses",

            // Store
            ["stores"] = "profile",
            ["storestats"] = "stats",

            // Types & Classes
            ["producttypes"] = "producttypes",
            ["productclasses"] = "classes",

            // Marketing
            ["coupons"] = "discount_coupons",
            ["favorites"] = "favorites",
            ["abandonedcarts"] = "carts",

            // Settings
            ["shippingmethods"] = "shipping/methods",
            ["taxes"] = "taxes",
            ["paymentmethods"] = "payment/methods"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["productvariations"] = new[] { "productId" },
            ["productimages"] = new[] { "productId" },
            ["productattributes"] = new[] { "productId" },
            ["productoptions"] = new[] { "productId" },
            ["productfiles"] = new[] { "productId" },
            ["productwholesaleprices"] = new[] { "productId" },
            ["productrelated"] = new[] { "productId" },
            ["productupselled"] = new[] { "productId" },
            ["productcrossselled"] = new[] { "productId" },
            ["productinventory"] = new[] { "productId" },
            ["productreviews"] = new[] { "productId" },
            ["categoryproducts"] = new[] { "categoryId" },
            ["orderdetails"] = new[] { "orderNumber" },
            ["orderitems"] = new[] { "orderNumber" },
            ["customerorders"] = new[] { "customerId" },
            ["customeraddresses"] = new[] { "customerId" }
        };

        public EcwidDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
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
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Ecwid entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, GetRootElement(EntityName));
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Ecwid entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Add pagination parameters
            q["offset"] = ((pageNumber - 1) * pageSize).ToString();
            q["limit"] = pageSize.ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallEcwid(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, GetRootElement(EntityName));

            return new PagedResult
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = items.Count, // Ecwid doesn't provide total count in response
                TotalPages = 1, // Estimate based on current page
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // If we got full page, there might be more
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
                throw new ArgumentException($"Ecwid entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute placeholders from filters if present
            var result = template;

            // Handle {productId}
            if (result.Contains("{productId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("productId", out var productId) || string.IsNullOrWhiteSpace(productId))
                    throw new ArgumentException("Missing required 'productId' filter for this endpoint.");
                result = result.Replace("{productId}", Uri.EscapeDataString(productId));
            }

            // Handle {categoryId}
            if (result.Contains("{categoryId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("categoryId", out var categoryId) || string.IsNullOrWhiteSpace(categoryId))
                    throw new ArgumentException("Missing required 'categoryId' filter for this endpoint.");
                result = result.Replace("{categoryId}", Uri.EscapeDataString(categoryId));
            }

            // Handle {orderNumber}
            if (result.Contains("{orderNumber}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("orderNumber", out var orderNumber) || string.IsNullOrWhiteSpace(orderNumber))
                    throw new ArgumentException("Missing required 'orderNumber' filter for this endpoint.");
                result = result.Replace("{orderNumber}", Uri.EscapeDataString(orderNumber));
            }

            // Handle {customerId}
            if (result.Contains("{customerId}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("customerId", out var customerId) || string.IsNullOrWhiteSpace(customerId))
                    throw new ArgumentException("Missing required 'customerId' filter for this endpoint.");
                result = result.Replace("{customerId}", Uri.EscapeDataString(customerId));
            }

            return result;
        }

        private async Task<HttpResponseMessage> CallEcwid(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        private static string GetRootElement(string entityName)
        {
            return entityName.ToLower() switch
            {
                "stores" or "orderdetails" or "productinventory" or "storestats" => "",
                "productvariations" => "variations",
                "productimages" => "images",
                "productattributes" => "attributes",
                "productoptions" => "options",
                "productfiles" => "files",
                "productwholesaleprices" => "wholesalePrices",
                "productrelated" => "relatedProducts",
                "productupselled" => "upsellProducts",
                "productcrossselled" => "crosssellProducts",
                "customeraddresses" => "addresses",
                "shippingmethods" => "methods",
                "paymentmethods" => "methods",
                _ => "items"
            };
        }

        // Extracts root element (array or object) into a List<object> (Dictionary<string,object> per item).
        // If root is null/empty, wraps whole payload as a single object.
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
                    return list; // no root element -> empty
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Products", ClassName = "EcwidDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EcwidProduct>")]
        public async Task<IEnumerable<EcwidProduct>> GetProducts(AppFilter filter)
        {
            var result = await GetEntityAsync("products", new List<AppFilter> { filter });
            return result.Cast<EcwidProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Orders", ClassName = "EcwidDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EcwidOrder>")]
        public async Task<IEnumerable<EcwidOrder>> GetOrders(AppFilter filter)
        {
            var result = await GetEntityAsync("orders", new List<AppFilter> { filter });
            return result.Cast<EcwidOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Categories", ClassName = "EcwidDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EcwidCategory>")]
        public async Task<IEnumerable<EcwidCategory>> GetCategories(AppFilter filter)
        {
            var result = await GetEntityAsync("categories", new List<AppFilter> { filter });
            return result.Cast<EcwidCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Products", Name = "CreateProduct", Caption = "Create Ecwid Product", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 4, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidProduct>")]
        public async Task<IEnumerable<EcwidProduct>> CreateProductAsync(EcwidProduct product)
        {
            try
            {
                var result = await PostAsync("products", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdProduct = JsonSerializer.Deserialize<EcwidProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidProduct> { createdProduct }.Select(p => p.Attach<EcwidProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating product: {ex.Message}");
            }
            return new List<EcwidProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Products", Name = "UpdateProduct", Caption = "Update Ecwid Product", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 5, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidProduct>")]
        public async Task<IEnumerable<EcwidProduct>> UpdateProductAsync(EcwidProduct product)
        {
            try
            {
                var result = await PutAsync($"products/{product.Id}", product);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedProduct = JsonSerializer.Deserialize<EcwidProduct>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidProduct> { updatedProduct }.Select(p => p.Attach<EcwidProduct>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating product: {ex.Message}");
            }
            return new List<EcwidProduct>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "CreateOrder", Caption = "Create Ecwid Order", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 6, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidOrder>")]
        public async Task<IEnumerable<EcwidOrder>> CreateOrderAsync(EcwidOrder order)
        {
            try
            {
                var result = await PostAsync("orders", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdOrder = JsonSerializer.Deserialize<EcwidOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidOrder> { createdOrder }.Select(o => o.Attach<EcwidOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating order: {ex.Message}");
            }
            return new List<EcwidOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Orders", Name = "UpdateOrder", Caption = "Update Ecwid Order", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 7, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidOrder>")]
        public async Task<IEnumerable<EcwidOrder>> UpdateOrderAsync(EcwidOrder order)
        {
            try
            {
                var result = await PutAsync($"orders/{order.Id}", order);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedOrder = JsonSerializer.Deserialize<EcwidOrder>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidOrder> { updatedOrder }.Select(o => o.Attach<EcwidOrder>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating order: {ex.Message}");
            }
            return new List<EcwidOrder>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Categories", Name = "CreateCategory", Caption = "Create Ecwid Category", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 8, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidCategory>")]
        public async Task<IEnumerable<EcwidCategory>> CreateCategoryAsync(EcwidCategory category)
        {
            try
            {
                var result = await PostAsync("categories", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdCategory = JsonSerializer.Deserialize<EcwidCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidCategory> { createdCategory }.Select(c => c.Attach<EcwidCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating category: {ex.Message}");
            }
            return new List<EcwidCategory>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Ecwid, PointType = EnumPointType.Function, ObjectType ="Categories", Name = "UpdateCategory", Caption = "Update Ecwid Category", ClassType ="EcwidDataSource", Showin = ShowinType.Both, Order = 9, iconimage = "ecwid.png", misc = "ReturnType: IEnumerable<EcwidCategory>")]
        public async Task<IEnumerable<EcwidCategory>> UpdateCategoryAsync(EcwidCategory category)
        {
            try
            {
                var result = await PutAsync($"categories/{category.Id}", category);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedCategory = JsonSerializer.Deserialize<EcwidCategory>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EcwidCategory> { updatedCategory }.Select(c => c.Attach<EcwidCategory>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating category: {ex.Message}");
            }
            return new List<EcwidCategory>();
        }

        #endregion
    }
}