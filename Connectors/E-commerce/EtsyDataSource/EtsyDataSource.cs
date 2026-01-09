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
using TheTechIdea.Beep.Connectors.Ecommerce.EtsyDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Etsy
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy)]
    public class EtsyDataSource : WebAPIDataSource
    {
        // Etsy API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Shop Management
            ["shops"] = ("/v3/application/shops", "results", new[] { "" }),
            ["shop"] = ("/v3/application/shops/{shop_id}", "", new[] { "shop_id" }),
            ["shop_sections"] = ("/v3/application/shops/{shop_id}/sections", "results", new[] { "shop_id" }),

            // Listings
            ["listings"] = ("/v3/application/shops/{shop_id}/listings", "results", new[] { "shop_id" }),
            ["listing"] = ("/v3/application/listings/{listing_id}", "", new[] { "listing_id" }),
            ["listing_images"] = ("/v3/application/listings/{listing_id}/images", "results", new[] { "listing_id" }),
            ["listing_variations"] = ("/v3/application/listings/{listing_id}/variations", "results", new[] { "listing_id" }),
            ["listing_inventory"] = ("/v3/application/listings/{listing_id}/inventory", "", new[] { "listing_id" }),

            // Orders & Transactions
            ["receipts"] = ("/v3/application/shops/{shop_id}/receipts", "results", new[] { "shop_id" }),
            ["receipt"] = ("/v3/application/shops/{shop_id}/receipts/{receipt_id}", "", new[] { "shop_id", "receipt_id" }),
            ["transactions"] = ("/v3/application/shops/{shop_id}/transactions", "results", new[] { "shop_id" }),

            // Reviews
            ["reviews"] = ("/v3/application/shops/{shop_id}/reviews", "results", new[] { "shop_id" }),

            // User Account
            ["user"] = ("/v3/application/users/{user_id}", "", new[] { "user_id" }),
            ["user_addresses"] = ("/v3/application/users/{user_id}/addresses", "results", new[] { "user_id" }),

            // Treasury
            ["treasury_listings"] = ("/v3/application/treasury/listings", "results", new[] { "" }),

            // Categories & Attributes
            ["buyer_taxonomy"] = ("/v3/application/buyer-taxonomy/nodes", "results", new[] { "" }),
            ["seller_taxonomy"] = ("/v3/application/seller-taxonomy/nodes", "results", new[] { "" }),
            ["shipping_profiles"] = ("/v3/application/shops/{shop_id}/shipping-profiles", "results", new[] { "shop_id" }),
            ["payment_templates"] = ("/v3/application/shops/{shop_id}/payment-templates", "results", new[] { "shop_id" })
        };

        public EtsyDataSource(
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
                throw new InvalidOperationException($"Unknown Etsy entity '{EntityName}'.");

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
                throw new ArgumentException($"Etsy entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "shop_id", "listing_id", "receipt_id", "user_id" })
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Listings", ClassName = "EtsyDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EtsyListing>")]
        public async Task<IEnumerable<EtsyListing>> GetListings(AppFilter filter)
        {
            var result = await GetEntityAsync("listings", new List<AppFilter> { filter });
            return result.Cast<EtsyListing>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Receipts", ClassName = "EtsyDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EtsyReceipt>")]
        public async Task<IEnumerable<EtsyReceipt>> GetReceipts(AppFilter filter)
        {
            var result = await GetEntityAsync("receipts", new List<AppFilter> { filter });
            return result.Cast<EtsyReceipt>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Users", ClassName = "EtsyDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EtsyUser>")]
        public async Task<IEnumerable<EtsyUser>> GetUsers(AppFilter filter)
        {
            var result = await GetEntityAsync("users", new List<AppFilter> { filter });
            return result.Cast<EtsyUser>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Shops", ClassName = "EtsyDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EtsyShop>")]
        public async Task<IEnumerable<EtsyShop>> GetShops(AppFilter filter)
        {
            var result = await GetEntityAsync("shops", new List<AppFilter> { filter });
            return result.Cast<EtsyShop>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Transactions", ClassName = "EtsyDataSource", Showin = ShowinType.Both, misc = "IEnumerable<EtsyTransaction>")]
        public async Task<IEnumerable<EtsyTransaction>> GetTransactions(AppFilter filter)
        {
            var result = await GetEntityAsync("transactions", new List<AppFilter> { filter });
            return result.Cast<EtsyTransaction>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Listings", Name = "CreateListing", Caption = "Create Etsy Listing", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 6, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyListing>")]
        public async Task<IEnumerable<EtsyListing>> CreateListingAsync(EtsyListing listing)
        {
            try
            {
                var result = await PostAsync("listings", listing);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdListing = JsonSerializer.Deserialize<EtsyListing>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyListing> { createdListing }.Select(l => l.Attach<EtsyListing>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating listing: {ex.Message}");
            }
            return new List<EtsyListing>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Listings", Name = "UpdateListing", Caption = "Update Etsy Listing", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 7, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyListing>")]
        public async Task<IEnumerable<EtsyListing>> UpdateListingAsync(EtsyListing listing)
        {
            try
            {
                var result = await PutAsync($"listings/{listing.Id}", listing);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedListing = JsonSerializer.Deserialize<EtsyListing>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyListing> { updatedListing }.Select(l => l.Attach<EtsyListing>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating listing: {ex.Message}");
            }
            return new List<EtsyListing>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Receipts", Name = "CreateReceipt", Caption = "Create Etsy Receipt", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 8, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyReceipt>")]
        public async Task<IEnumerable<EtsyReceipt>> CreateReceiptAsync(EtsyReceipt receipt)
        {
            try
            {
                var result = await PostAsync("receipts", receipt);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdReceipt = JsonSerializer.Deserialize<EtsyReceipt>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyReceipt> { createdReceipt }.Select(r => r.Attach<EtsyReceipt>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating receipt: {ex.Message}");
            }
            return new List<EtsyReceipt>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Receipts", Name = "UpdateReceipt", Caption = "Update Etsy Receipt", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 9, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyReceipt>")]
        public async Task<IEnumerable<EtsyReceipt>> UpdateReceiptAsync(EtsyReceipt receipt)
        {
            try
            {
                var result = await PutAsync($"receipts/{receipt.Id}", receipt);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedReceipt = JsonSerializer.Deserialize<EtsyReceipt>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyReceipt> { updatedReceipt }.Select(r => r.Attach<EtsyReceipt>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating receipt: {ex.Message}");
            }
            return new List<EtsyReceipt>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Users", Name = "CreateUser", Caption = "Create Etsy User", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyUser>")]
        public async Task<IEnumerable<EtsyUser>> CreateUserAsync(EtsyUser user)
        {
            try
            {
                var result = await PostAsync("users", user);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdUser = JsonSerializer.Deserialize<EtsyUser>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyUser> { createdUser }.Select(u => u.Attach<EtsyUser>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating user: {ex.Message}");
            }
            return new List<EtsyUser>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Users", Name = "UpdateUser", Caption = "Update Etsy User", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyUser>")]
        public async Task<IEnumerable<EtsyUser>> UpdateUserAsync(EtsyUser user)
        {
            try
            {
                var result = await PutAsync($"users/{user.Id}", user);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedUser = JsonSerializer.Deserialize<EtsyUser>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyUser> { updatedUser }.Select(u => u.Attach<EtsyUser>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating user: {ex.Message}");
            }
            return new List<EtsyUser>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Shops", Name = "CreateShop", Caption = "Create Etsy Shop", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 12, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyShop>")]
        public async Task<IEnumerable<EtsyShop>> CreateShopAsync(EtsyShop shop)
        {
            try
            {
                var result = await PostAsync("shops", shop);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdShop = JsonSerializer.Deserialize<EtsyShop>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyShop> { createdShop }.Select(s => s.Attach<EtsyShop>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating shop: {ex.Message}");
            }
            return new List<EtsyShop>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Shops", Name = "UpdateShop", Caption = "Update Etsy Shop", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 13, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyShop>")]
        public async Task<IEnumerable<EtsyShop>> UpdateShopAsync(EtsyShop shop)
        {
            try
            {
                var result = await PutAsync($"shops/{shop.Id}", shop);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedShop = JsonSerializer.Deserialize<EtsyShop>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyShop> { updatedShop }.Select(s => s.Attach<EtsyShop>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating shop: {ex.Message}");
            }
            return new List<EtsyShop>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Transactions", Name = "CreateTransaction", Caption = "Create Etsy Transaction", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 14, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyTransaction>")]
        public async Task<IEnumerable<EtsyTransaction>> CreateTransactionAsync(EtsyTransaction transaction)
        {
            try
            {
                var result = await PostAsync("transactions", transaction);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTransaction = JsonSerializer.Deserialize<EtsyTransaction>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyTransaction> { createdTransaction }.Select(t => t.Attach<EtsyTransaction>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating transaction: {ex.Message}");
            }
            return new List<EtsyTransaction>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Etsy, PointType = EnumPointType.Function, ObjectType = "Transactions", Name = "UpdateTransaction", Caption = "Update Etsy Transaction", ClassType = "EtsyDataSource", Showin = ShowinType.Both, Order = 15, iconimage = "etsy.png", misc = "ReturnType: IEnumerable<EtsyTransaction>")]
        public async Task<IEnumerable<EtsyTransaction>> UpdateTransactionAsync(EtsyTransaction transaction)
        {
            try
            {
                var result = await PutAsync($"transactions/{transaction.Id}", transaction);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTransaction = JsonSerializer.Deserialize<EtsyTransaction>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<EtsyTransaction> { updatedTransaction }.Select(t => t.Attach<EtsyTransaction>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating transaction: {ex.Message}");
            }
            return new List<EtsyTransaction>();
        }

        #endregion
    }
}