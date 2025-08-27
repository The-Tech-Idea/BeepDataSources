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

namespace BeepDM.Connectors.Communication.WhatsAppBusiness
{
    public class WhatsAppBusinessConfig
    {
        public string AccessToken { get; set; }
        public string PhoneNumberId { get; set; }
        public string BusinessAccountId { get; set; }
        public string ApiVersion { get; set; } = "v18.0"; // Latest stable version
        public string WebhookVerifyToken { get; set; }
        public string BaseUrl => $"https://graph.facebook.com/{ApiVersion}";
    }

    public class WhatsAppBusinessDataSource : IDataSource
    {
        private readonly ILogger<WhatsAppBusinessDataSource> _logger;
        private HttpClient _httpClient;
        private WhatsAppBusinessConfig _config;
        private bool _isConnected;

        public string DataSourceName => "WhatsAppBusiness";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "WhatsApp Business API Communication Platform Data Source";

        public WhatsAppBusinessDataSource(ILogger<WhatsAppBusinessDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new WhatsAppBusinessConfig();

                // Access token is required for WhatsApp Business API
                if (!parameters.ContainsKey("AccessToken"))
                {
                    throw new ArgumentException("AccessToken is required for WhatsApp Business API authentication");
                }

                _config.AccessToken = parameters["AccessToken"].ToString();
                _config.PhoneNumberId = parameters.ContainsKey("PhoneNumberId") ? parameters["PhoneNumberId"].ToString() : null;
                _config.BusinessAccountId = parameters.ContainsKey("BusinessAccountId") ? parameters["BusinessAccountId"].ToString() : null;
                _config.WebhookVerifyToken = parameters.ContainsKey("WebhookVerifyToken") ? parameters["WebhookVerifyToken"].ToString() : null;

                if (!string.IsNullOrEmpty(parameters.ContainsKey("ApiVersion") ? parameters["ApiVersion"].ToString() : null))
                {
                    _config.ApiVersion = parameters["ApiVersion"].ToString();
                }

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(_config.BaseUrl)
                };

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting business profile
                var testEndpoint = !string.IsNullOrEmpty(_config.PhoneNumberId) ?
                    $"{_config.PhoneNumberId}/messages" : "me";

                var response = await _httpClient.GetAsync(testEndpoint);
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to WhatsApp Business API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to WhatsApp Business API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to WhatsApp Business API");
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
                _logger.LogInformation("Disconnected from WhatsApp Business API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from WhatsApp Business API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WhatsApp Business API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);
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
                    _logger.LogError($"WhatsApp Business API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from WhatsApp Business");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WhatsApp Business API");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);

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
                        // WhatsApp Business API doesn't support direct updates for most entities
                        // This would typically involve sending a new message
                        if (entityName.ToLower() == "messages")
                        {
                            response = await _httpClient.PostAsync(endpoint, content);
                        }
                        else
                        {
                            _logger.LogWarning("Update not supported for entity type: " + entityName);
                            continue;
                        }
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
                _logger.LogError(ex, $"Error updating entity {entityName} in WhatsApp Business");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to WhatsApp Business API");
            }

            // WhatsApp Business API has limited delete capabilities
            // Media can be deleted, but messages cannot be deleted after sending
            if (entityName.ToLower() != "media")
            {
                throw new NotSupportedException("Delete operation is not supported for this entity type in WhatsApp Business API");
            }

            if (!parameters.ContainsKey("media_id"))
            {
                throw new ArgumentException("media_id parameter is required for media deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);
                var mediaId = parameters["media_id"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{mediaId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete media with id {mediaId}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from WhatsApp Business");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "messages",
                "contacts",
                "business_profile",
                "phone_numbers",
                "media",
                "templates",
                "flows",
                "webhooks",
                "qr_codes",
                "business_accounts",
                "conversations",
                "analytics"
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
                case "messages":
                    metadata.Rows.Add("id", "string", false, "Unique message identifier");
                    metadata.Rows.Add("from", "string", false, "Sender phone number");
                    metadata.Rows.Add("to", "string", false, "Recipient phone number");
                    metadata.Rows.Add("type", "string", false, "Message type (text, image, video, audio, document, sticker, location, contacts, interactive)");
                    metadata.Rows.Add("timestamp", "datetime", false, "Message timestamp");
                    metadata.Rows.Add("text", "object", true, "Text message content");
                    metadata.Rows.Add("image", "object", true, "Image message content");
                    metadata.Rows.Add("video", "object", true, "Video message content");
                    metadata.Rows.Add("audio", "object", true, "Audio message content");
                    metadata.Rows.Add("document", "object", true, "Document message content");
                    metadata.Rows.Add("sticker", "object", true, "Sticker message content");
                    metadata.Rows.Add("location", "object", true, "Location message content");
                    metadata.Rows.Add("contacts", "array", true, "Contacts message content");
                    metadata.Rows.Add("interactive", "object", true, "Interactive message content");
                    metadata.Rows.Add("context", "object", true, "Message context (reply information)");
                    metadata.Rows.Add("referral", "object", true, "Referral information");
                    metadata.Rows.Add("errors", "array", true, "Message errors");
                    metadata.Rows.Add("status", "string", true, "Message status (sent, delivered, read, failed)");
                    break;

                case "contacts":
                    metadata.Rows.Add("wa_id", "string", false, "WhatsApp ID of the contact");
                    metadata.Rows.Add("profile", "object", true, "Contact profile information");
                    metadata.Rows.Add("name", "string", true, "Contact name");
                    metadata.Rows.Add("phone_number", "string", true, "Contact phone number");
                    metadata.Rows.Add("about", "string", true, "Contact about text");
                    metadata.Rows.Add("email", "string", true, "Contact email");
                    metadata.Rows.Add("addresses", "array", true, "Contact addresses");
                    metadata.Rows.Add("urls", "array", true, "Contact URLs");
                    metadata.Rows.Add("org", "object", true, "Contact organization");
                    metadata.Rows.Add("birthday", "string", true, "Contact birthday");
                    break;

                case "business_profile":
                    metadata.Rows.Add("about", "string", true, "Business about text");
                    metadata.Rows.Add("address", "string", true, "Business address");
                    metadata.Rows.Add("description", "string", true, "Business description");
                    metadata.Rows.Add("email", "string", true, "Business email");
                    metadata.Rows.Add("messaging_product", "string", false, "Messaging product (whatsapp)");
                    metadata.Rows.Add("profile_picture_url", "string", true, "Profile picture URL");
                    metadata.Rows.Add("vertical", "string", true, "Business vertical");
                    metadata.Rows.Add("websites", "array", true, "Business websites");
                    break;

                case "phone_numbers":
                    metadata.Rows.Add("id", "string", false, "Phone number ID");
                    metadata.Rows.Add("display_phone_number", "string", false, "Display phone number");
                    metadata.Rows.Add("verified_name", "string", false, "Verified name");
                    metadata.Rows.Add("quality_rating", "string", true, "Quality rating");
                    metadata.Rows.Add("code_verification_status", "string", false, "Code verification status");
                    metadata.Rows.Add("platform_type", "string", true, "Platform type");
                    metadata.Rows.Add("throughput", "object", true, "Throughput information");
                    metadata.Rows.Add("last_onboarded_time", "datetime", true, "Last onboarded time");
                    metadata.Rows.Add("account_mode", "string", true, "Account mode");
                    metadata.Rows.Add("is_official_business_account", "boolean", true, "Whether it's an official business account");
                    metadata.Rows.Add("is_pin_protected", "boolean", true, "Whether PIN is protected");
                    metadata.Rows.Add("pin_status", "string", true, "PIN status");
                    break;

                case "media":
                    metadata.Rows.Add("id", "string", false, "Media unique identifier");
                    metadata.Rows.Add("url", "string", false, "Media URL");
                    metadata.Rows.Add("mime_type", "string", false, "Media MIME type");
                    metadata.Rows.Add("sha256", "string", false, "Media SHA256 hash");
                    metadata.Rows.Add("file_size", "integer", false, "Media file size in bytes");
                    metadata.Rows.Add("messaging_product", "string", false, "Messaging product");
                    metadata.Rows.Add("caption", "string", true, "Media caption");
                    metadata.Rows.Add("filename", "string", true, "Media filename");
                    break;

                case "templates":
                    metadata.Rows.Add("id", "string", false, "Template unique identifier");
                    metadata.Rows.Add("name", "string", false, "Template name");
                    metadata.Rows.Add("language", "string", false, "Template language");
                    metadata.Rows.Add("status", "string", false, "Template status");
                    metadata.Rows.Add("category", "string", false, "Template category");
                    metadata.Rows.Add("components", "array", false, "Template components");
                    metadata.Rows.Add("rejected_reason", "string", true, "Rejection reason");
                    metadata.Rows.Add("quality_score", "object", true, "Quality score");
                    break;

                case "conversations":
                    metadata.Rows.Add("id", "string", false, "Conversation unique identifier");
                    metadata.Rows.Add("origin", "object", false, "Conversation origin");
                    metadata.Rows.Add("expiration_timestamp", "datetime", true, "Expiration timestamp");
                    metadata.Rows.Add("last_message", "object", true, "Last message in conversation");
                    break;

                case "analytics":
                    metadata.Rows.Add("phone_number", "string", false, "Phone number");
                    metadata.Rows.Add("country_code", "string", false, "Country code");
                    metadata.Rows.Add("start_date", "date", false, "Analytics start date");
                    metadata.Rows.Add("end_date", "date", false, "Analytics end date");
                    metadata.Rows.Add("granularity", "string", false, "Analytics granularity (day, month)");
                    metadata.Rows.Add("data_points", "array", false, "Analytics data points");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
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
                var testEndpoint = !string.IsNullOrEmpty(_config.PhoneNumberId) ?
                    $"{_config.PhoneNumberId}/messages" : "me";
                var response = await _httpClient.GetAsync(testEndpoint);
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
                ["PhoneNumberId"] = _config?.PhoneNumberId ?? "Not configured",
                ["BusinessAccountId"] = _config?.BusinessAccountId ?? "Not configured",
                ["ApiVersion"] = _config?.ApiVersion ?? "Not configured",
                ["HasWebhookVerifyToken"] = !string.IsNullOrEmpty(_config?.WebhookVerifyToken),
                ["AuthType"] = "Access Token"
            };
        }

        private string GetEndpointForEntity(string entityName, Dictionary<string, object> parameters = null)
        {
            var phoneNumberId = parameters?.ContainsKey("phone_number_id") == true ?
                               parameters["phone_number_id"].ToString() :
                               _config?.PhoneNumberId;

            return entityName.ToLower() switch
            {
                "messages" => $"{phoneNumberId}/messages",
                "contacts" => $"{phoneNumberId}/contacts",
                "business_profile" => $"{phoneNumberId}/business_profile",
                "phone_numbers" => $"{phoneNumberId}/phone_numbers",
                "media" => $"{phoneNumberId}/media",
                "templates" => $"{phoneNumberId}/message_templates",
                "flows" => $"{phoneNumberId}/flows",
                "webhooks" => $"{phoneNumberId}/webhooks",
                "qr_codes" => $"{phoneNumberId}/qr_codes",
                "business_accounts" => $"{_config?.BusinessAccountId}/business_accounts",
                "conversations" => $"{phoneNumberId}/conversations",
                "analytics" => $"{phoneNumberId}/analytics",
                _ => $"{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return "";
            }

            var queryParams = new List<string>();
            foreach (var param in parameters)
            {
                if (param.Value != null && !param.Key.Contains("phone_number_id"))
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

                // Handle WhatsApp Business API response structure
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
                else
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
                _logger.LogError(ex, "Error parsing JSON response from WhatsApp Business");
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
