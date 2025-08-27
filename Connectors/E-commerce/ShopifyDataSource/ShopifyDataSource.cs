using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BeepDM.DataManagementModelsStandard;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.Ecommerce.Shopify
{
    public class ShopifyConfig
    {
        public string ApiKey { get; set; }
        public string StoreUrl { get; set; }
        public string BaseUrl => $"https://{StoreUrl}/admin/api/2023-10";
    }

    public class ShopifyDataSource : IDataSource
    {
        private readonly ILogger<ShopifyDataSource> _logger;
        private HttpClient _httpClient;
        private ShopifyConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Shopify";
        public string DataSourceType => "E-commerce";
        public string Version => "1.0.0";
        public string Description => "Shopify E-commerce Platform Data Source";

        public ShopifyDataSource(ILogger<ShopifyDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("ApiKey") || !parameters.ContainsKey("StoreUrl"))
                {
                    throw new ArgumentException("ApiKey and StoreUrl are required parameters");
                }

                _config = new ShopifyConfig
                {
                    ApiKey = parameters["ApiKey"].ToString(),
                    StoreUrl = parameters["StoreUrl"].ToString()
                };

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(_config.BaseUrl)
                };

                // Set authentication header
                _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", _config.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting shop info
                var response = await _httpClient.GetAsync("/shop.json");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Shopify API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Shopify API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Shopify API");
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
                _logger.LogInformation("Disconnected from Shopify API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Shopify API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Shopify API");
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
                    _logger.LogError($"Shopify API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Shopify");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Shopify API");
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
                        response = await _httpClient.PutAsync($"{endpoint}/{id}.json", content);
                    }
                    else if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["id"]?.ToString();
                        if (string.IsNullOrEmpty(id))
                        {
                            _logger.LogError("Cannot delete entity without ID");
                            continue;
                        }
                        response = await _httpClient.DeleteAsync($"{endpoint}/{id}.json");
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Shopify");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Shopify API");
            }

            if (!parameters.ContainsKey("id"))
            {
                throw new ArgumentException("id parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
                var id = parameters["id"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}.json");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with id {id}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Shopify");
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
                "collections",
                "inventory",
                "analytics",
                "content",
                "discounts",
                "themes",
                "webhooks",
                "locations",
                "fulfillments",
                "transactions",
                "refunds",
                "policies",
                "redirects",
                "script_tags",
                "recurring_application_charges"
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
                    metadata.Rows.Add("id", "long", false, "Unique identifier for the product");
                    metadata.Rows.Add("title", "string", false, "Product title");
                    metadata.Rows.Add("body_html", "string", true, "Product description in HTML");
                    metadata.Rows.Add("vendor", "string", true, "Product vendor");
                    metadata.Rows.Add("product_type", "string", true, "Product type");
                    metadata.Rows.Add("created_at", "datetime", false, "Date the product was created");
                    metadata.Rows.Add("updated_at", "datetime", false, "Date the product was last updated");
                    metadata.Rows.Add("published_at", "datetime", true, "Date the product was published");
                    metadata.Rows.Add("status", "string", false, "Product status (active, archived, draft)");
                    metadata.Rows.Add("tags", "string", true, "Product tags");
                    metadata.Rows.Add("variants", "array", true, "Product variants");
                    metadata.Rows.Add("images", "array", true, "Product images");
                    metadata.Rows.Add("options", "array", true, "Product options");
                    metadata.Rows.Add("metafields", "array", true, "Product metafields");
                    break;

                case "orders":
                    metadata.Rows.Add("id", "long", false, "Unique identifier for the order");
                    metadata.Rows.Add("email", "string", true, "Customer email");
                    metadata.Rows.Add("created_at", "datetime", false, "Date the order was created");
                    metadata.Rows.Add("updated_at", "datetime", false, "Date the order was last updated");
                    metadata.Rows.Add("number", "integer", false, "Order number");
                    metadata.Rows.Add("name", "string", false, "Order name");
                    metadata.Rows.Add("total_price", "decimal", false, "Total order price");
                    metadata.Rows.Add("subtotal_price", "decimal", false, "Subtotal price");
                    metadata.Rows.Add("total_tax", "decimal", false, "Total tax");
                    metadata.Rows.Add("total_discounts", "decimal", false, "Total discounts");
                    metadata.Rows.Add("total_line_items_price", "decimal", false, "Total line items price");
                    metadata.Rows.Add("status", "string", false, "Order status");
                    metadata.Rows.Add("fulfillment_status", "string", true, "Fulfillment status");
                    metadata.Rows.Add("payment_status", "string", false, "Payment status");
                    metadata.Rows.Add("customer", "object", true, "Customer information");
                    metadata.Rows.Add("line_items", "array", false, "Order line items");
                    metadata.Rows.Add("shipping_address", "object", true, "Shipping address");
                    metadata.Rows.Add("billing_address", "object", true, "Billing address");
                    metadata.Rows.Add("discount_codes", "array", true, "Discount codes");
                    metadata.Rows.Add("note", "string", true, "Order note");
                    break;

                case "customers":
                    metadata.Rows.Add("id", "long", false, "Unique identifier for the customer");
                    metadata.Rows.Add("email", "string", false, "Customer email");
                    metadata.Rows.Add("first_name", "string", true, "Customer first name");
                    metadata.Rows.Add("last_name", "string", true, "Customer last name");
                    metadata.Rows.Add("phone", "string", true, "Customer phone");
                    metadata.Rows.Add("created_at", "datetime", false, "Date the customer was created");
                    metadata.Rows.Add("updated_at", "datetime", false, "Date the customer was last updated");
                    metadata.Rows.Add("orders_count", "integer", false, "Number of orders");
                    metadata.Rows.Add("total_spent", "decimal", false, "Total amount spent");
                    metadata.Rows.Add("last_order_id", "long", true, "Last order ID");
                    metadata.Rows.Add("last_order_name", "string", true, "Last order name");
                    metadata.Rows.Add("addresses", "array", true, "Customer addresses");
                    metadata.Rows.Add("default_address", "object", true, "Default address");
                    metadata.Rows.Add("metafields", "array", true, "Customer metafields");
                    metadata.Rows.Add("marketing_opt_in_level", "string", true, "Marketing opt-in level");
                    metadata.Rows.Add("tax_exempt", "boolean", false, "Tax exempt status");
                    metadata.Rows.Add("tags", "string", true, "Customer tags");
                    break;

                default:
                    metadata.Rows.Add("id", "long", false, "Unique identifier");
                    metadata.Rows.Add("created_at", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("updated_at", "datetime", true, "Last update timestamp");
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
                var response = await _httpClient.GetAsync("/shop.json");
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
                "products" => "/products.json",
                "orders" => "/orders.json",
                "customers" => "/customers.json",
                "collections" => "/collections.json",
                "inventory" => "/inventory_items.json",
                "analytics" => "/analytics.json",
                "content" => "/content.json",
                "discounts" => "/discounts.json",
                "themes" => "/themes.json",
                "webhooks" => "/webhooks.json",
                "locations" => "/locations.json",
                "fulfillments" => "/fulfillments.json",
                "transactions" => "/transactions.json",
                "refunds" => "/refunds.json",
                "policies" => "/policies.json",
                "redirects" => "/redirects.json",
                "script_tags" => "/script_tags.json",
                "recurring_application_charges" => "/recurring_application_charges.json",
                _ => $"/{entityName}.json"
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

                // Handle Shopify's response structure
                if (root.TryGetProperty(entityName, out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    // Array response (multiple items)
                    foreach (var item in dataArray.EnumerateArray())
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
                else if (root.TryGetProperty(entityName, out var dataObject) && dataObject.ValueKind == JsonValueKind.Object)
                {
                    // Single object response
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var property in dataObject.EnumerateObject())
                        {
                            dataTable.Columns.Add(property.Name, typeof(string));
                        }
                    }

                    var row = dataTable.NewRow();
                    foreach (var property in dataObject.EnumerateObject())
                    {
                        row[property.Name] = property.Value.ToString();
                    }
                    dataTable.Rows.Add(row);
                }
                else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("shop", out var shopObject))
                {
                    // Shop info response
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var property in shopObject.EnumerateObject())
                        {
                            dataTable.Columns.Add(property.Name, typeof(string));
                        }
                    }

                    var row = dataTable.NewRow();
                    foreach (var property in shopObject.EnumerateObject())
                    {
                        row[property.Name] = property.Value.ToString();
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON response from Shopify");
            }

            return dataTable;
        }

        private object ConvertDataRowToJson(DataRow row, string entityName)
        {
            var jsonObject = new Dictionary<string, object>
            {
                [entityName.TrimEnd('s')] = new Dictionary<string, object>()
            };

            var entityObject = jsonObject[entityName.TrimEnd('s')] as Dictionary<string, object>;

            foreach (DataColumn column in row.Table.Columns)
            {
                if (row[column] != DBNull.Value)
                {
                    entityObject[column.ColumnName] = row[column];
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
