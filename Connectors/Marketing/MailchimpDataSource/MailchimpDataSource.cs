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

namespace BeepDM.Connectors.Marketing.Mailchimp
{
    public class MailchimpConfig
    {
        public string ApiKey { get; set; }
        public string DataCenter { get; set; }
        public string BaseUrl => $"https://{DataCenter}.api.mailchimp.com/3.0";
    }

    public class MailchimpDataSource : IDataSource
    {
        private readonly ILogger<MailchimpDataSource> _logger;
        private HttpClient _httpClient;
        private MailchimpConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Mailchimp";
        public string DataSourceType => "Marketing";
        public string Version => "1.0.0";
        public string Description => "Mailchimp Marketing Automation Platform Data Source";

        public MailchimpDataSource(ILogger<MailchimpDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("ApiKey") || !parameters.ContainsKey("DataCenter"))
                {
                    throw new ArgumentException("ApiKey and DataCenter are required parameters");
                }

                _config = new MailchimpConfig
                {
                    ApiKey = parameters["ApiKey"].ToString(),
                    DataCenter = parameters["DataCenter"].ToString()
                };

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(_config.BaseUrl)
                };

                // Set authentication header
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                // Test connection by getting account info
                var response = await _httpClient.GetAsync("/");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Mailchimp API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Mailchimp API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Mailchimp API");
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
                _logger.LogInformation("Disconnected from Mailchimp API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Mailchimp API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Mailchimp API");
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
                    _logger.LogError($"Mailchimp API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Mailchimp");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Mailchimp API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);

                foreach (DataRow row in data.Rows)
                {
                    var jsonData = ConvertDataRowToJson(row);
                    var content = JsonContent.Create(jsonData);

                    HttpResponseMessage response;
                    if (row.RowState == DataRowState.Added)
                    {
                        response = await _httpClient.PostAsync(endpoint, content);
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        var id = row["id"]?.ToString();
                        response = await _httpClient.PatchAsync($"{endpoint}/{id}", content);
                    }
                    else if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["id"]?.ToString();
                        response = await _httpClient.DeleteAsync($"{endpoint}/{id}");
                    }
                    else
                    {
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to update {entityName}: {response.StatusCode} - {response.ReasonPhrase}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Mailchimp");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Mailchimp API");
            }

            if (!parameters.ContainsKey("id"))
            {
                throw new ArgumentException("id parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName);
                var id = parameters["id"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with id {id}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Mailchimp");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "lists",
                "campaigns",
                "templates",
                "automations",
                "reports",
                "segments",
                "members",
                "merge-fields",
                "interest-categories",
                "interests"
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
                case "lists":
                    metadata.Rows.Add("id", "string", false, "Unique identifier for the list");
                    metadata.Rows.Add("name", "string", false, "Name of the list");
                    metadata.Rows.Add("contact", "object", false, "Contact information for the list");
                    metadata.Rows.Add("permission_reminder", "string", false, "Permission reminder text");
                    metadata.Rows.Add("use_archive_bar", "boolean", true, "Whether to use the archive bar");
                    metadata.Rows.Add("campaign_defaults", "object", false, "Default campaign settings");
                    metadata.Rows.Add("notify_on_subscribe", "string", true, "Email to notify on subscribe");
                    metadata.Rows.Add("notify_on_unsubscribe", "string", true, "Email to notify on unsubscribe");
                    metadata.Rows.Add("date_created", "datetime", false, "Date the list was created");
                    metadata.Rows.Add("list_rating", "integer", false, "List rating");
                    metadata.Rows.Add("email_type_option", "boolean", false, "Whether email type option is enabled");
                    metadata.Rows.Add("subscribe_url_short", "string", false, "Short subscribe URL");
                    metadata.Rows.Add("subscribe_url_long", "string", false, "Long subscribe URL");
                    metadata.Rows.Add("beamer_address", "string", false, "Beamer address");
                    metadata.Rows.Add("visibility", "string", false, "List visibility");
                    metadata.Rows.Add("double_optin", "boolean", false, "Whether double opt-in is enabled");
                    metadata.Rows.Add("has_welcome", "boolean", false, "Whether welcome email is enabled");
                    metadata.Rows.Add("marketing_permissions", "boolean", false, "Whether marketing permissions are enabled");
                    metadata.Rows.Add("modules", "array", true, "List of enabled modules");
                    metadata.Rows.Add("stats", "object", false, "List statistics");
                    break;

                case "campaigns":
                    metadata.Rows.Add("id", "string", false, "Unique identifier for the campaign");
                    metadata.Rows.Add("type", "string", false, "Campaign type");
                    metadata.Rows.Add("create_time", "datetime", false, "Date the campaign was created");
                    metadata.Rows.Add("archive_url", "string", true, "Archive URL");
                    metadata.Rows.Add("long_archive_url", "string", true, "Long archive URL");
                    metadata.Rows.Add("status", "string", false, "Campaign status");
                    metadata.Rows.Add("emails_sent", "integer", false, "Number of emails sent");
                    metadata.Rows.Add("send_time", "datetime", true, "Date the campaign was sent");
                    metadata.Rows.Add("content_type", "string", false, "Content type");
                    metadata.Rows.Add("needs_block_refresh", "boolean", false, "Whether block refresh is needed");
                    metadata.Rows.Add("resendable", "boolean", false, "Whether campaign is resendable");
                    metadata.Rows.Add("recipients", "object", false, "Recipient information");
                    metadata.Rows.Add("settings", "object", false, "Campaign settings");
                    metadata.Rows.Add("variate_settings", "object", true, "Variate settings");
                    metadata.Rows.Add("tracking", "object", true, "Tracking settings");
                    metadata.Rows.Add("rss_opts", "object", true, "RSS options");
                    metadata.Rows.Add("ab_split_opts", "object", true, "A/B split options");
                    metadata.Rows.Add("social_card", "object", true, "Social card settings");
                    metadata.Rows.Add("report_summary", "object", true, "Report summary");
                    metadata.Rows.Add("delivery_status", "object", false, "Delivery status");
                    break;

                case "members":
                    metadata.Rows.Add("id", "string", false, "Unique identifier for the member");
                    metadata.Rows.Add("email_address", "string", false, "Email address");
                    metadata.Rows.Add("unique_email_id", "string", false, "Unique email ID");
                    metadata.Rows.Add("contact_id", "string", false, "Contact ID");
                    metadata.Rows.Add("full_name", "string", true, "Full name");
                    metadata.Rows.Add("web_id", "integer", false, "Web ID");
                    metadata.Rows.Add("email_type", "string", true, "Email type preference");
                    metadata.Rows.Add("status", "string", false, "Subscription status");
                    metadata.Rows.Add("consents_to_one_to_one_messaging", "boolean", true, "Consent to one-to-one messaging");
                    metadata.Rows.Add("merge_fields", "object", true, "Merge field data");
                    metadata.Rows.Add("interests", "object", true, "Interest data");
                    metadata.Rows.Add("stats", "object", false, "Member statistics");
                    metadata.Rows.Add("ip_signup", "string", true, "IP address at signup");
                    metadata.Rows.Add("timestamp_signup", "datetime", true, "Signup timestamp");
                    metadata.Rows.Add("ip_opt", "string", true, "IP address at opt-in");
                    metadata.Rows.Add("timestamp_opt", "datetime", true, "Opt-in timestamp");
                    metadata.Rows.Add("member_rating", "integer", false, "Member rating");
                    metadata.Rows.Add("last_changed", "datetime", true, "Last changed timestamp");
                    metadata.Rows.Add("language", "string", true, "Language preference");
                    metadata.Rows.Add("vip", "boolean", true, "VIP status");
                    metadata.Rows.Add("email_client", "string", true, "Email client");
                    metadata.Rows.Add("location", "object", true, "Location information");
                    metadata.Rows.Add("marketing_permissions", "array", true, "Marketing permissions");
                    metadata.Rows.Add("last_note", "object", true, "Last note");
                    metadata.Rows.Add("source", "string", true, "Source of signup");
                    metadata.Rows.Add("tags_count", "integer", false, "Number of tags");
                    metadata.Rows.Add("tags", "array", true, "Tags");
                    metadata.Rows.Add("list_id", "string", false, "List ID");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
                    metadata.Rows.Add("name", "string", true, "Name");
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
                var response = await _httpClient.GetAsync("/");
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
                ["BaseUrl"] = _config?.BaseUrl ?? "Not configured",
                ["DataCenter"] = _config?.DataCenter ?? "Not configured"
            };
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "lists" => "/lists",
                "campaigns" => "/campaigns",
                "templates" => "/templates",
                "automations" => "/automations",
                "reports" => "/reports",
                "segments" => "/lists/segments",
                "members" => "/lists/members",
                "merge-fields" => "/lists/merge-fields",
                "interest-categories" => "/lists/interest-categories",
                "interests" => "/lists/interests",
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

                // Handle different response structures
                JsonElement dataElement;
                if (root.TryGetProperty(entityName, out dataElement) || root.TryGetProperty("data", out dataElement))
                {
                    // Paginated response
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataElement.EnumerateArray())
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
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    // Direct array response
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
                    foreach (var property in root.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
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
                _logger.LogError(ex, "Error parsing JSON response from Mailchimp");
            }

            return dataTable;
        }

        private object ConvertDataRowToJson(DataRow row)
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
