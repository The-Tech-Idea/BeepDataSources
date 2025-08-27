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

namespace BeepDM.Connectors.Communication.Zoom
{
    public class ZoomConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccountId { get; set; }
        public string AccessToken { get; set; }
        public string ApiKey { get; set; } // For JWT authentication (deprecated but still supported)
        public string ApiSecret { get; set; } // For JWT authentication (deprecated but still supported)
    }

    public class ZoomDataSource : IDataSource
    {
        private readonly ILogger<ZoomDataSource> _logger;
        private HttpClient _httpClient;
        private ZoomConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Zoom";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Zoom Video Conferencing and Communication Platform Data Source";

        public ZoomDataSource(ILogger<ZoomDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new ZoomConfig();

                // Support both OAuth 2.0 and JWT authentication
                if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    // OAuth 2.0 flow
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.AccountId = parameters.ContainsKey("AccountId") ? parameters["AccountId"].ToString() : null;

                    var tokenResponse = await GetOAuthAccessTokenAsync();
                    if (tokenResponse == null)
                    {
                        _logger.LogError("Failed to get OAuth access token for Zoom");
                        return false;
                    }
                    _config.AccessToken = tokenResponse;
                }
                else if (parameters.ContainsKey("ApiKey") && parameters.ContainsKey("ApiSecret"))
                {
                    // JWT flow (deprecated but still supported)
                    _config.ApiKey = parameters["ApiKey"].ToString();
                    _config.ApiSecret = parameters["ApiSecret"].ToString();
                    _config.AccessToken = GenerateJWTToken();
                }
                else
                {
                    throw new ArgumentException("Either (ClientId, ClientSecret) for OAuth 2.0 or (ApiKey, ApiSecret) for JWT authentication is required");
                }

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://api.zoom.us/v2/")
                };

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting user info
                var response = await _httpClient.GetAsync("users/me");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Zoom API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Zoom API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Zoom API");
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
                _logger.LogInformation("Disconnected from Zoom API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Zoom API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Zoom API");
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
                    _logger.LogError($"Zoom API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Zoom");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Zoom API");
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
                        response = await _httpClient.PatchAsync($"{endpoint}/{id}", content);
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Zoom");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Zoom API");
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
                _logger.LogError(ex, $"Error deleting entity {entityName} from Zoom");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "users",
                "meetings",
                "recordings",
                "reports",
                "webinars",
                "groups",
                "roles",
                "billing",
                "accounts",
                "tracking_sources",
                "devices",
                "phone",
                "h323",
                "sip",
                "contacts",
                "chat",
                "channels",
                "files",
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
                case "users":
                    metadata.Rows.Add("id", "string", false, "User unique identifier");
                    metadata.Rows.Add("first_name", "string", true, "User first name");
                    metadata.Rows.Add("last_name", "string", true, "User last name");
                    metadata.Rows.Add("email", "string", false, "User email address");
                    metadata.Rows.Add("type", "integer", false, "User type (1=Basic, 2=Pro, 3=Corp)");
                    metadata.Rows.Add("pmi", "integer", true, "Personal meeting ID");
                    metadata.Rows.Add("timezone", "string", true, "User timezone");
                    metadata.Rows.Add("verified", "integer", false, "Whether email is verified (0=No, 1=Yes)");
                    metadata.Rows.Add("created_at", "datetime", false, "Account creation date");
                    metadata.Rows.Add("last_login_time", "datetime", true, "Last login time");
                    metadata.Rows.Add("last_client_version", "string", true, "Last client version used");
                    metadata.Rows.Add("language", "string", true, "User language");
                    metadata.Rows.Add("status", "string", false, "User status (active, inactive, pending)");
                    metadata.Rows.Add("role_name", "string", true, "User role name");
                    break;

                case "meetings":
                    metadata.Rows.Add("id", "integer", false, "Meeting unique identifier");
                    metadata.Rows.Add("topic", "string", false, "Meeting topic");
                    metadata.Rows.Add("type", "integer", false, "Meeting type (1=Instant, 2=Scheduled, 3=Recurring, 8=Recurring with fixed time)");
                    metadata.Rows.Add("start_time", "datetime", true, "Meeting start time");
                    metadata.Rows.Add("duration", "integer", true, "Meeting duration in minutes");
                    metadata.Rows.Add("timezone", "string", true, "Meeting timezone");
                    metadata.Rows.Add("agenda", "string", true, "Meeting agenda");
                    metadata.Rows.Add("created_at", "datetime", false, "Meeting creation time");
                    metadata.Rows.Add("join_url", "string", false, "Meeting join URL");
                    metadata.Rows.Add("start_url", "string", true, "Meeting start URL (for host)");
                    metadata.Rows.Add("password", "string", true, "Meeting password");
                    metadata.Rows.Add("h323_password", "string", true, "H.323/SIP password");
                    metadata.Rows.Add("pstn_password", "string", true, "PSTN password");
                    metadata.Rows.Add("encrypted_password", "string", true, "Encrypted password");
                    metadata.Rows.Add("status", "string", false, "Meeting status (ended, started)");
                    metadata.Rows.Add("host_id", "string", false, "Host user ID");
                    metadata.Rows.Add("host_email", "string", false, "Host email");
                    metadata.Rows.Add("participants", "array", true, "Meeting participants");
                    metadata.Rows.Add("settings", "object", true, "Meeting settings");
                    break;

                case "recordings":
                    metadata.Rows.Add("id", "string", false, "Recording unique identifier");
                    metadata.Rows.Add("meeting_id", "string", false, "Associated meeting ID");
                    metadata.Rows.Add("account_id", "string", false, "Account ID");
                    metadata.Rows.Add("host_id", "string", false, "Host user ID");
                    metadata.Rows.Add("topic", "string", false, "Recording topic");
                    metadata.Rows.Add("start_time", "datetime", false, "Recording start time");
                    metadata.Rows.Add("timezone", "string", false, "Recording timezone");
                    metadata.Rows.Add("duration", "integer", false, "Recording duration in seconds");
                    metadata.Rows.Add("total_size", "integer", false, "Total recording size in bytes");
                    metadata.Rows.Add("recording_count", "integer", false, "Number of recording files");
                    metadata.Rows.Add("recording_files", "array", false, "Recording files array");
                    metadata.Rows.Add("password", "string", true, "Recording password");
                    metadata.Rows.Add("share_url", "string", true, "Recording share URL");
                    break;

                case "webinars":
                    metadata.Rows.Add("id", "integer", false, "Webinar unique identifier");
                    metadata.Rows.Add("topic", "string", false, "Webinar topic");
                    metadata.Rows.Add("type", "integer", false, "Webinar type (5=Webinar, 6=Recurring webinar)");
                    metadata.Rows.Add("start_time", "datetime", true, "Webinar start time");
                    metadata.Rows.Add("duration", "integer", true, "Webinar duration in minutes");
                    metadata.Rows.Add("timezone", "string", true, "Webinar timezone");
                    metadata.Rows.Add("agenda", "string", true, "Webinar agenda");
                    metadata.Rows.Add("created_at", "datetime", false, "Webinar creation time");
                    metadata.Rows.Add("join_url", "string", false, "Webinar join URL");
                    metadata.Rows.Add("start_url", "string", true, "Webinar start URL (for host)");
                    metadata.Rows.Add("password", "string", true, "Webinar password");
                    metadata.Rows.Add("status", "string", false, "Webinar status");
                    metadata.Rows.Add("host_id", "string", false, "Host user ID");
                    metadata.Rows.Add("host_email", "string", false, "Host email");
                    metadata.Rows.Add("settings", "object", true, "Webinar settings");
                    break;

                case "reports":
                    metadata.Rows.Add("id", "string", false, "Report unique identifier");
                    metadata.Rows.Add("host_id", "string", false, "Host user ID");
                    metadata.Rows.Add("email", "string", false, "Host email");
                    metadata.Rows.Add("type", "string", false, "Report type");
                    metadata.Rows.Add("start_time", "datetime", false, "Report start time");
                    metadata.Rows.Add("end_time", "datetime", false, "Report end time");
                    metadata.Rows.Add("total_records", "integer", false, "Total number of records");
                    metadata.Rows.Add("data", "array", false, "Report data array");
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
                var response = await _httpClient.GetAsync("users/me");
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
                ["AccountId"] = _config?.AccountId ?? "Not configured",
                ["ClientId"] = _config?.ClientId ?? "Not configured",
                ["HasAccessToken"] = !string.IsNullOrEmpty(_config?.AccessToken),
                ["AuthType"] = !string.IsNullOrEmpty(_config?.ClientId) ? "OAuth 2.0" : "JWT"
            };
        }

        private async Task<string> GetOAuthAccessTokenAsync()
        {
            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");
                var tokenParams = new Dictionary<string, string>
                {
                    ["grant_type"] = "account_credentials",
                    ["account_id"] = _config.AccountId
                };

                tokenRequest.Content = new FormUrlEncodedContent(tokenParams);
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                tokenRequest.Headers.Add("Authorization", $"Basic {credentials}");

                using var tokenClient = new HttpClient();
                var tokenResponse = await tokenClient.SendAsync(tokenRequest);

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var tokenDoc = JsonDocument.Parse(tokenJson);
                    return tokenDoc.RootElement.GetProperty("access_token").GetString();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OAuth access token for Zoom");
                return null;
            }
        }

        private string GenerateJWTToken()
        {
            // Note: JWT authentication is deprecated by Zoom but still supported
            // In a real implementation, you would use a JWT library to generate the token
            // For now, returning a placeholder - implement proper JWT generation as needed
            _logger.LogWarning("JWT authentication is deprecated by Zoom. Consider using OAuth 2.0 instead.");
            return "jwt_token_placeholder";
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "users" => "users",
                "meetings" => "users/{userId}/meetings",
                "recordings" => "users/{userId}/recordings",
                "reports" => "report/users",
                "webinars" => "users/{userId}/webinars",
                "groups" => "groups",
                "roles" => "roles",
                "billing" => "accounts/{accountId}/billing",
                "accounts" => "accounts",
                "tracking_sources" => "tracking_sources",
                "devices" => "users/{userId}/devices",
                "phone" => "phone",
                "h323" => "h323",
                "sip" => "sip",
                "contacts" => "chat/users/{userId}/contacts",
                "chat" => "chat/users/{userId}/messages",
                "channels" => "chat/users/{userId}/channels",
                "files" => "users/{userId}/files",
                "analytics" => "metrics/users/{userId}/analytics",
                _ => $"{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return "page_size=50"; // Default pagination
            }

            var queryParams = new List<string> { "page_size=50" }; // Default pagination
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

                // Handle Zoom API response structure
                if (root.TryGetProperty(entityName, out var entityArray) && entityArray.ValueKind == JsonValueKind.Array)
                {
                    // Array response (multiple items)
                    foreach (var item in entityArray.EnumerateArray())
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
                _logger.LogError(ex, "Error parsing JSON response from Zoom");
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
