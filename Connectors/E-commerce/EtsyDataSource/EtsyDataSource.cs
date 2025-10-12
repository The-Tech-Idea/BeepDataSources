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

        #endregion
    }
}