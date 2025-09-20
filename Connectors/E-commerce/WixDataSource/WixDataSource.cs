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

        // Override GetEntity to handle Wix API specifics
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter>? Filter)
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
    }
}
