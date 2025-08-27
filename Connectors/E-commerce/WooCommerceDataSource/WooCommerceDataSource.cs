using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BeepDM.DataManagementModelsStandard;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.Ecommerce.WooCommerce
{
    public class WooCommerceConfig
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string StoreUrl { get; set; }
        public string BaseUrl => $"{StoreUrl}/wp-json/wc/v3";
    }

    public class WooCommerceDataSource : IDataSource
    {
        private readonly ILogger<WooCommerceDataSource> _logger;
        private HttpClient _httpClient;
        private WooCommerceConfig _config;
        private bool _isConnected;

        public string DataSourceName => "WooCommerce";
        public string DataSourceType => "E-commerce";
        public string Version => "1.0.0";
        public string Description => "WooCommerce E-commerce Platform Data Source";

        public WooCommerceDataSource(ILogger<WooCommerceDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("ConsumerKey") || !parameters.ContainsKey("ConsumerSecret") || !parameters.ContainsKey("StoreUrl"))
                {
                    throw new ArgumentException("ConsumerKey, ConsumerSecret, and StoreUrl are required parameters");
                }

                _config = new WooCommerceConfig
                {
                    ConsumerKey = parameters["ConsumerKey"].ToString(),
                    ConsumerSecret = parameters["ConsumerSecret"].ToString(),
                    StoreUrl = parameters["StoreUrl"].ToString()
                };

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(_config.BaseUrl)
                };

                // Set authentication header for WooCommerce REST API
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ConsumerKey}:{_config.ConsumerSecret}"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting system status
                var response = await _httpClient.GetAsync("/system_status");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to WooCommerce API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to WooCommerce API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to WooCommerce API");
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from WooCommerce API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from WooCommerce API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WooCommerce API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
                var queryParams = BuildQueryParameters(parameters);

                HttpResponseMessage response;
                if (!string.IsNullOrEmpty(queryParams))
                {
                    response = await _httpClient.GetAsync($"{endpoint}?{queryParams}");
                }
                else
                {
                    response = await _httpClient.GetAsync(endpoint);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"WooCommerce API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from WooCommerce");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WooCommerce API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);

                foreach (DataRow row in data.Rows)
                {
                    var jsonData = ConvertDataRowToJson(row, entityName);
                    var content = JsonContent.Create(jsonData);

                    HttpResponseMessage response;
                    if (row.RowState == DataRowState.Added)
                    {
                        response = await _httpClient.PostAsync(endpoint, content);
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        var id = row["id"]?.ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogError("Cannot update entity without ID");
                            continue;
                        }
                        response = await _httpClient.PutAsync($"{endpoint}/{id}", content);
                    }
                    else if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["id"]?.ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogError("Cannot delete entity without ID");
                            continue;
                        }
                        response = await _httpClient.DeleteAsync($"{endpoint}/{id}?force=true");
                    }
                    else
                    {
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Failed to update {entityName}: {response.StatusCode} - {response.ReasonPhrase}. Details: {errorContent}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in WooCommerce");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WooCommerce API");
            }

            if (!parameters.ContainsKey("id"))
            {
                throw new ArgumentException("id parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
                var id = parameters["id"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}?force=true");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with id {id}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from WooCommerce");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "products",
                "orders",
                "customers",
                "categories",
                "coupons",
                "reviews",
                "variations",
                "attributes",
                "tags",
                "shipping_zones",
                "tax_rates",
                "webhooks",
                "reports",
                "settings",
                "system_status"
            };
        }

        public async Task<DataTable> GetEntityMetadataAsync(string entityName)
        {
            var metadata = new DataTable("Metadata");
            metadata.Columns.Add("ColumnName", typeof(string));
            metadata.Columns.Add("DataType", typeof(string));
            metadata.Columns.Add("IsNullable", typeof(bool));
            metadata.Columns.Add("Description", typeof(string));

            // Add common metadata fields based on entity type
            switch (entityName.ToLower())
            {
                case "products":
                    metadata.Rows.Add("id", "integer", false, "Unique identifier for the product");
                    metadata.Rows.Add("name", "string", false, "Product name");
                    metadata.Rows.Add("slug", "string", false, "Product slug");
                    metadata.Rows.Add("permalink", "string", false, "Product permalink");
                    metadata.Rows.Add("date_created", "datetime", false, "Date the product was created");
                    metadata.Rows.Add("date_modified", "datetime", false, "Date the product was last modified");
                    metadata.Rows.Add("type", "string", false, "Product type (simple, grouped, external, variable)");
                    metadata.Rows.Add("status", "string", false, "Product status (draft, pending, private, publish)");
                    metadata.Rows.Add("featured", "boolean", false, "Whether the product is featured");
                    metadata.Rows.Add("catalog_visibility", "string", false, "Catalog visibility (visible, catalog, search, hidden)");
                    metadata.Rows.Add("description", "string", true, "Product description");
                    metadata.Rows.Add("short_description", "string", true, "Product short description");
                    metadata.Rows.Add("sku", "string", true, "Product SKU");
                    metadata.Rows.Add("price", "decimal", true, "Product price");
                    metadata.Rows.Add("regular_price", "decimal", true, "Product regular price");
                    metadata.Rows.Add("sale_price", "decimal", true, "Product sale price");
                    metadata.Rows.Add("date_on_sale_from", "datetime", true, "Date the sale starts");
                    metadata.Rows.Add("date_on_sale_to", "datetime", true, "Date the sale ends");
                    metadata.Rows.Add("on_sale", "boolean", false, "Whether the product is on sale");
                    metadata.Rows.Add("purchasable", "boolean", false, "Whether the product is purchasable");
                    metadata.Rows.Add("total_sales", "integer", false, "Total sales count");
                    metadata.Rows.Add("virtual", "boolean", false, "Whether the product is virtual");
                    metadata.Rows.Add("downloadable", "boolean", false, "Whether the product is downloadable");
                    metadata.Rows.Add("downloads", "array", true, "Product downloads");
                    metadata.Rows.Add("download_limit", "integer", true, "Download limit");
                    metadata.Rows.Add("download_expiry", "integer", true, "Download expiry");
                    metadata.Rows.Add("external_url", "string", true, "External product URL");
                    metadata.Rows.Add("button_text", "string", true, "External product button text");
                    metadata.Rows.Add("tax_status", "string", false, "Tax status (taxable, shipping, none)");
                    metadata.Rows.Add("tax_class", "string", true, "Tax class");
                    metadata.Rows.Add("manage_stock", "boolean", false, "Whether to manage stock");
                    metadata.Rows.Add("stock_quantity", "integer", true, "Stock quantity");
                    metadata.Rows.Add("stock_status", "string", false, "Stock status (instock, outofstock, onbackorder)");
                    metadata.Rows.Add("backorders", "string", false, "Backorders allowed (no, notify, yes)");
                    metadata.Rows.Add("backordered", "boolean", false, "Whether the product is backordered");
                    metadata.Rows.Add("sold_individually", "boolean", false, "Whether the product is sold individually");
                    metadata.Rows.Add("weight", "string", true, "Product weight");
                    metadata.Rows.Add("dimensions", "object", true, "Product dimensions");
                    metadata.Rows.Add("shipping_required", "boolean", false, "Whether shipping is required");
                    metadata.Rows.Add("shipping_taxable", "boolean", false, "Whether shipping is taxable");
                    metadata.Rows.Add("shipping_class", "string", true, "Shipping class slug");
                    metadata.Rows.Add("shipping_class_id", "integer", false, "Shipping class ID");
                    metadata.Rows.Add("reviews_allowed", "boolean", false, "Whether reviews are allowed");
                    metadata.Rows.Add("average_rating", "string", false, "Average rating");
                    metadata.Rows.Add("rating_count", "integer", false, "Rating count");
                    metadata.Rows.Add("related_ids", "array", false, "Related product IDs");
                    metadata.Rows.Add("upsell_ids", "array", false, "Upsell product IDs");
                    metadata.Rows.Add("cross_sell_ids", "array", false, "Cross-sell product IDs");
                    metadata.Rows.Add("parent_id", "integer", false, "Parent product ID");
                    metadata.Rows.Add("purchase_note", "string", true, "Purchase note");
                    metadata.Rows.Add("categories", "array", false, "Product categories");
                    metadata.Rows.Add("tags", "array", false, "Product tags");
                    metadata.Rows.Add("images", "array", false, "Product images");
                    metadata.Rows.Add("attributes", "array", true, "Product attributes");
                    metadata.Rows.Add("default_attributes", "array", true, "Default attributes");
                    metadata.Rows.Add("variations", "array", true, "Product variations");
                    metadata.Rows.Add("grouped_products", "array", true, "Grouped product IDs");
                    metadata.Rows.Add("menu_order", "integer", false, "Menu order");
                    metadata.Rows.Add("meta_data", "array", true, "Product meta data");
                    break;

                case "orders":
                    metadata.Rows.Add("id", "integer", false, "Unique identifier for the order");
                    metadata.Rows.Add("parent_id", "integer", false, "Parent order ID");
                    metadata.Rows.Add("number", "string", false, "Order number");
                    metadata.Rows.Add("order_key", "string", false, "Order key");
                    metadata.Rows.Add("created_via", "string", false, "How the order was created");
                    metadata.Rows.Add("version", "string", false, "Order version");
                    metadata.Rows.Add("status", "string", false, "Order status");
                    metadata.Rows.Add("currency", "string", false, "Order currency");
                    metadata.Rows.Add("date_created", "datetime", false, "Date the order was created");
                    metadata.Rows.Add("date_modified", "datetime", false, "Date the order was modified");
                    metadata.Rows.Add("discount_total", "string", false, "Discount total");
                    metadata.Rows.Add("discount_tax", "string", false, "Discount tax");
                    metadata.Rows.Add("shipping_total", "string", false, "Shipping total");
                    metadata.Rows.Add("shipping_tax", "string", false, "Shipping tax");
                    metadata.Rows.Add("cart_tax", "string", false, "Cart tax");
                    metadata.Rows.Add("total", "string", false, "Order total");
                    metadata.Rows.Add("total_tax", "string", false, "Order total tax");
                    metadata.Rows.Add("prices_include_tax", "boolean", false, "Whether prices include tax");
                    metadata.Rows.Add("customer_id", "integer", false, "Customer ID");
                    metadata.Rows.Add("customer_ip_address", "string", true, "Customer IP address");
                    metadata.Rows.Add("customer_user_agent", "string", true, "Customer user agent");
                    metadata.Rows.Add("customer_note", "string", true, "Customer note");
                    metadata.Rows.Add("billing", "object", false, "Billing address");
                    metadata.Rows.Add("shipping", "object", false, "Shipping address");
                    metadata.Rows.Add("payment_method", "string", false, "Payment method");
                    metadata.Rows.Add("payment_method_title", "string", false, "Payment method title");
                    metadata.Rows.Add("transaction_id", "string", true, "Transaction ID");
                    metadata.Rows.Add("date_paid", "datetime", true, "Date the order was paid");
                    metadata.Rows.Add("date_completed", "datetime", true, "Date the order was completed");
                    metadata.Rows.Add("cart_hash", "string", true, "Cart hash");
                    metadata.Rows.Add("meta_data", "array", true, "Order meta data");
                    metadata.Rows.Add("line_items", "array", false, "Order line items");
                    metadata.Rows.Add("tax_lines", "array", false, "Order tax lines");
                    metadata.Rows.Add("shipping_lines", "array", false, "Order shipping lines");
                    metadata.Rows.Add("fee_lines", "array", false, "Order fee lines");
                    metadata.Rows.Add("coupon_lines", "array", false, "Order coupon lines");
                    metadata.Rows.Add("refunds", "array", false, "Order refunds");
                    break;

                case "customers":
                    metadata.Rows.Add("id", "integer", false, "Unique identifier for the customer");
                    metadata.Rows.Add("date_created", "datetime", false, "Date the customer was created");
                    metadata.Rows.Add("date_modified", "datetime", false, "Date the customer was modified");
                    metadata.Rows.Add("email", "string", false, "Customer email");
                    metadata.Rows.Add("first_name", "string", true, "Customer first name");
                    metadata.Rows.Add("last_name", "string", true, "Customer last name");
                    metadata.Rows.Add("role", "string", false, "Customer role");
                    metadata.Rows.Add("username", "string", true, "Customer username");
                    metadata.Rows.Add("billing", "object", false, "Billing address");
                    metadata.Rows.Add("shipping", "object", false, "Shipping address");
                    metadata.Rows.Add("is_paying_customer", "boolean", false, "Whether the customer has made a purchase");
                    metadata.Rows.Add("orders_count", "integer", false, "Number of orders");
                    metadata.Rows.Add("total_spent", "string", false, "Total spent");
                    metadata.Rows.Add("avatar_url", "string", false, "Avatar URL");
                    metadata.Rows.Add("meta_data", "array", true, "Customer meta data");
                    break;

                default:
                    metadata.Rows.Add("id", "integer", false, "Unique identifier");
                    metadata.Rows.Add("name", "string", true, "Name");
                    metadata.Rows.Add("slug", "string", true, "Slug");
                    metadata.Rows.Add("description", "string", true, "Description");
                    metadata.Rows.Add("date_created", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("date_modified", "datetime", true, "Last update timestamp");
                    break;
            }

            return metadata;
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!_isConnected)
            {
                return false;
            }

            try
            {
                var response = await _httpClient.GetAsync("/system_status");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetConnectionInfoAsync()
        {
            return new Dictionary<string, object>
            {
                ["DataSourceName"] = DataSourceName,
                ["DataSourceType"] = DataSourceType,
                ["Version"] = Version,
                ["IsConnected"] = _isConnected,
                ["StoreUrl"] = _config?.StoreUrl ?? "Not configured",
                ["BaseUrl"] = _config?.BaseUrl ?? "Not configured"
            };
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "products" => "/products",
                "orders" => "/orders",
                "customers" => "/customers",
                "categories" => "/products/categories",
                "coupons" => "/coupons",
                "reviews" => "/products/reviews",
                "variations" => "/products/variations",
                "attributes" => "/products/attributes",
                "tags" => "/products/tags",
                "shipping_zones" => "/shipping/zones",
                "tax_rates" => "/taxes",
                "webhooks" => "/webhooks",
                "reports" => "/reports",
                "settings" => "/settings",
                "system_status" => "/system_status",
                _ => $"/{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }

            var queryParams = new List<string>();
            foreach (var param in parameters)
            {
                if (param.Value != null)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value.ToString())}");
                }
            }

            return string.Join("&", queryParams);
        }

        private DataTable ParseJsonToDataTable(string jsonString, string entityName)
        {
            var dataTable = new DataTable(entityName);

            try
            {
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                // Handle WooCommerce's response structure
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Array response (multiple items)
                    foreach (var item in root.EnumerateArray())
                    {
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (var property in item.EnumerateObject())
                            {
                                dataTable.Columns.Add(property.Name, typeof(string));
                            }
                        }

                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            row[property.Name] = property.Value.ToString();
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    // Single object response
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var property in root.EnumerateObject())
                        {
                            dataTable.Columns.Add(property.Name, typeof(string));
                        }
                    }

                    var row = dataTable.NewRow();
                    foreach (var property in root.EnumerateObject())
                    {
                        row[property.Name] = property.Value.ToString();
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from WooCommerce");
            }

            return dataTable;
        }

        private object ConvertDataRowToJson(DataRow row, string entityName)
        {
            var jsonObject = new Dictionary<string, object>();

            foreach (DataColumn column in row.Table.Columns)
            {
                if (row[column] != DBNull.Value)
                {
                    jsonObject[column.ColumnName] = row[column];
                }
            }

            return jsonObject;
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
