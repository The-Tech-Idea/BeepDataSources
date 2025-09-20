// File: BeepDM/Connectors/Ecommerce/BigCommerceDataSource/BigCommerceDataSource.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Ecommerce.BigCommerceDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Ecommerce.BigCommerceDataSource
{
    public class BigCommerceDataSource : WebAPIDataSource
    {
        // BigCommerce API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Core Commerce
            ["products"] = ("v3/catalog/products", "data", new[] { "" }),
            ["categories"] = ("v3/catalog/categories", "data", new[] { "" }),
            ["brands"] = ("v3/catalog/brands", "data", new[] { "" }),
            ["customers"] = ("v3/customers", "data", new[] { "" }),
            ["orders"] = ("v2/orders", "", new[] { "" }),
            ["order_products"] = ("v2/orders/{order_id}/products", "", new[] { "order_id" }),
            ["carts"] = ("v3/carts", "data", new[] { "" }),
            ["checkouts"] = ("v3/checkouts", "data", new[] { "" }),
            ["wishlists"] = ("v3/wishlists", "data", new[] { "" }),

            // Content
            ["pages"] = ("v3/content/pages", "data", new[] { "" }),
            ["blog_posts"] = ("v3/content/blog/posts", "data", new[] { "" }),
            ["blog_tags"] = ("v3/content/blog/tags", "data", new[] { "" }),

            // Marketing
            ["coupons"] = ("v3/marketing/coupons", "data", new[] { "" }),
            ["gift_certificates"] = ("v3/gift-certificates", "data", new[] { "" }),
            ["abandoned_carts"] = ("v3/marketing/abandoned-carts", "data", new[] { "" }),

            // Store Settings
            ["store"] = ("v2/store", "", new[] { "" }),
            ["currencies"] = ("v3/currencies", "data", new[] { "" }),
            ["tax_classes"] = ("v3/tax/classes", "data", new[] { "" }),
            ["shipping_zones"] = ("v2/shipping/zones", "", new[] { "" }),
            ["payment_methods"] = ("v2/payments/methods", "", new[] { "" }),

            // Analytics & Reports
            ["analytics"] = ("v3/analytics", "data", new[] { "" }),
            ["sales_reports"] = ("v2/store/sales_reports", "", new[] { "" }),

            // Inventory
            ["inventory"] = ("v3/inventory/items", "data", new[] { "" }),

            // Customer Groups & Segments
            ["customer_groups"] = ("v3/customer-groups", "data", new[] { "" }),
            ["price_lists"] = ("v3/pricing/price-lists", "data", new[] { "" }),

            // Reviews & Ratings
            ["product_reviews"] = ("v3/catalog/products/{product_id}/reviews", "data", new[] { "product_id" }),
            ["store_reviews"] = ("v3/store/reviews", "data", new[] { "" })
        };

        public BigCommerceDataSource(
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
        private string ResolveEndpoint(string entityName, List<AppFilter>? filters = null)
        {
            if (!Map.TryGetValue(entityName, out var mapping))
                throw new ArgumentException($"Entity '{entityName}' not found in BigCommerce API map");

            var endpoint = mapping.endpoint;

            // Replace path parameters from filters
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    if (filter.FieldName != null && filter.FieldValue != null)
                    {
                        endpoint = endpoint.Replace($"{{{filter.FieldName}}}", filter.FieldValue.ToString());
                    }
                }
            }

            return endpoint;
        }

        // Helper method to check required filters
        private void RequireFilters(string entityName, List<AppFilter>? filters)
        {
            if (!Map.TryGetValue(entityName, out var mapping))
                return;

            var missingFilters = mapping.requiredFilters
                .Where(rf => !string.IsNullOrEmpty(rf) &&
                           (filters == null || !filters.Any(f => f.FieldName == rf)))
                .ToList();

            if (missingFilters.Any())
            {
                throw new ArgumentException(
                    $"Missing required filters for {entityName}: {string.Join(", ", missingFilters)}");
            }
        }

        // Helper method to convert filters to query parameters
        private string FiltersToQuery(List<AppFilter>? filters)
        {
            if (filters == null || !filters.Any())
                return "";

            var queryParams = new List<string>();

            foreach (var filter in filters)
            {
                if (filter.FieldName != null && filter.FieldValue != null)
                {
                    // Skip filters that are used as path parameters
                    if (!Map.ContainsKey(filter.FieldName ?? "") ||
                        !Map[filter.FieldName ?? ""].endpoint.Contains($"{{{filter.FieldName}}}"))
                    {
                        queryParams.Add($"{filter.FieldName}={Uri.EscapeDataString(filter.FieldValue.ToString() ?? "")}");
                    }
                }
            }

            return queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
        }

        // Override GetEntity to handle BigCommerce API specifics
        public override async Task<object?> GetEntityAsync(string EntityName, List<AppFilter>? filter)
        {
            try
            {
                RequireFilters(EntityName, filter);
                var endpoint = ResolveEndpoint(EntityName, filter);
                var query = FiltersToQuery(filter);

                var fullUrl = $"{BaseURL?.TrimEnd('/')}/{endpoint}{query}";

                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                // Add BigCommerce specific headers
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    request.Headers.Add("X-Auth-Token", AccessToken);
                }

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Handle BigCommerce response structure
                if (!string.IsNullOrEmpty(Map[EntityName].root))
                {
                    // Parse nested response
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty(Map[EntityName].root!, out var dataElement))
                    {
                        return JsonSerializer.Deserialize(
                            dataElement.GetRawText(),
                            GetEntityType(EntityName),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }

                // Direct response
                return JsonSerializer.Deserialize(
                    jsonResponse,
                    GetEntityType(EntityName),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting {EntityName}: {ex.Message}");
                throw;
            }
        }

        // Override GetEntitiesAsync for pagination support
        public override async Task<object?> GetEntitiesAsync(string EntityName, List<AppFilter>? filter)
        {
            try
            {
                RequireFilters(EntityName, filter);
                var endpoint = ResolveEndpoint(EntityName, filter);
                var query = FiltersToQuery(filter);

                // Add pagination parameters if not specified
                if (!query.Contains("limit="))
                {
                    query += string.IsNullOrEmpty(query) ? "?" : "&";
                    query += "limit=50"; // BigCommerce default page size
                }

                var fullUrl = $"{BaseURL?.TrimEnd('/')}/{endpoint}{query}";

                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                // Add BigCommerce specific headers
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    request.Headers.Add("X-Auth-Token", AccessToken);
                }

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await HttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Handle BigCommerce response structure
                if (!string.IsNullOrEmpty(Map[EntityName].root))
                {
                    // Parse nested response
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty(Map[EntityName].root!, out var dataElement))
                    {
                        return JsonSerializer.Deserialize(
                            dataElement.GetRawText(),
                            GetEntityListType(EntityName),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }

                // Direct response
                return JsonSerializer.Deserialize(
                    jsonResponse,
                    GetEntityListType(EntityName),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error getting {EntityName} entities: {ex.Message}");
                throw;
            }
        }

        // Helper method to get entity type for deserialization
        private Type GetEntityType(string entityName) => entityName switch
        {
            "products" => typeof(BigCommerceProduct),
            "categories" => typeof(BigCommerceCategory),
            "brands" => typeof(BigCommerceBrand),
            "customers" => typeof(BigCommerceCustomer),
            "orders" => typeof(BigCommerceOrder),
            "carts" => typeof(BigCommerceCart),
            "checkouts" => typeof(BigCommerceCheckout),
            "wishlists" => typeof(BigCommerceWishlist),
            "pages" => typeof(BigCommercePage),
            "blog_posts" => typeof(BigCommerceBlogPost),
            "coupons" => typeof(BigCommerceCoupon),
            "gift_certificates" => typeof(BigCommerceGiftCertificate),
            "store" => typeof(BigCommerceStore),
            "currencies" => typeof(BigCommerceCurrency),
            "tax_classes" => typeof(BigCommerceTaxClass),
            "shipping_zones" => typeof(BigCommerceShippingZone),
            "payment_methods" => typeof(BigCommercePaymentMethod),
            "inventory" => typeof(BigCommerceInventoryItem),
            "customer_groups" => typeof(BigCommerceCustomerGroup),
            "price_lists" => typeof(BigCommercePriceList),
            "product_reviews" => typeof(BigCommerceProductReview),
            "store_reviews" => typeof(BigCommerceStoreReview),
            _ => typeof(Dictionary<string, object>)
        };

        // Helper method to get entity list type for deserialization
        private Type GetEntityListType(string entityName) => entityName switch
        {
            "products" => typeof(List<BigCommerceProduct>),
            "categories" => typeof(List<BigCommerceCategory>),
            "brands" => typeof(List<BigCommerceBrand>),
            "customers" => typeof(List<BigCommerceCustomer>),
            "orders" => typeof(List<BigCommerceOrder>),
            "carts" => typeof(List<BigCommerceCart>),
            "checkouts" => typeof(List<BigCommerceCheckout>),
            "wishlists" => typeof(List<BigCommerceWishlist>),
            "pages" => typeof(List<BigCommercePage>),
            "blog_posts" => typeof(List<BigCommerceBlogPost>),
            "coupons" => typeof(List<BigCommerceCoupon>),
            "gift_certificates" => typeof(List<BigCommerceGiftCertificate>),
            "currencies" => typeof(List<BigCommerceCurrency>),
            "tax_classes" => typeof(List<BigCommerceTaxClass>),
            "shipping_zones" => typeof(List<BigCommerceShippingZone>),
            "payment_methods" => typeof(List<BigCommercePaymentMethod>),
            "inventory" => typeof(List<BigCommerceInventoryItem>),
            "customer_groups" => typeof(List<BigCommerceCustomerGroup>),
            "price_lists" => typeof(List<BigCommercePriceList>),
            "product_reviews" => typeof(List<BigCommerceProductReview>),
            "store_reviews" => typeof(List<BigCommerceStoreReview>),
            _ => typeof(List<Dictionary<string, object>>)
        };

        // Properties for BigCommerce authentication
        private string? AccessToken => (Dataconnection?.ConnectionProp as WebAPIConnectionProperties)?.ApiKey;
        private string? BaseURL => (Dataconnection?.ConnectionProp as WebAPIConnectionProperties)?.Url;
    }
}