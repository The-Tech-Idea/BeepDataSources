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

namespace BeepDM.Connectors.Communication.GoogleChat
{
    public class GoogleChatConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ServiceAccountKeyPath { get; set; } // For service account authentication
        public string ProjectId { get; set; }
        public string SpaceName { get; set; } // For specific space operations
    }

    public class GoogleChatDataSource : IDataSource
    {
        private readonly ILogger<GoogleChatDataSource> _logger;
        private HttpClient _httpClient;
        private GoogleChatConfig _config;
        private bool _isConnected;

        public string DataSourceName => "GoogleChat";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Google Chat Communication Platform Data Source";

        public GoogleChatDataSource(ILogger<GoogleChatDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new GoogleChatConfig();

                // Support both OAuth 2.0 and Service Account authentication
                if (parameters.ContainsKey("ServiceAccountKeyPath"))
                {
                    // Service Account authentication (for server-to-server)
                    _config.ServiceAccountKeyPath = parameters["ServiceAccountKeyPath"].ToString();
                    _config.ProjectId = parameters.ContainsKey("ProjectId") ? parameters["ProjectId"].ToString() : null;

                    var tokenResponse = await GetServiceAccountAccessTokenAsync();
                    if (tokenResponse == null)
                    {
                        _logger.LogError("Failed to get service account access token for Google Chat");
                        return false;
                    }
                    _config.AccessToken = tokenResponse;
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    // OAuth 2.0 flow (for user access)
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();

                    if (parameters.ContainsKey("Code"))
                    {
                        // Exchange authorization code for access token
                        var tokenResponse = await ExchangeCodeForTokenAsync(parameters["Code"].ToString());
                        if (tokenResponse == null)
                        {
                            _logger.LogError("Failed to get OAuth access token for Google Chat");
                            return false;
                        }
                        _config.AccessToken = tokenResponse.AccessToken;
                        _config.RefreshToken = tokenResponse.RefreshToken;
                    }
                    else if (parameters.ContainsKey("AccessToken"))
                    {
                        // Use provided access token
                        _config.AccessToken = parameters["AccessToken"].ToString();
                    }
                    else
                    {
                        throw new ArgumentException("Either Code or AccessToken is required for OAuth 2.0 flow");
                    }
                }
                else
                {
                    throw new ArgumentException("Either (ServiceAccountKeyPath) for service account auth or (ClientId, ClientSecret) for OAuth 2.0 is required");
                }

                _config.SpaceName = parameters.ContainsKey("SpaceName") ? parameters["SpaceName"].ToString() : null;

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://chat.googleapis.com/v1/")
                };

                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // Test connection by getting spaces list
                var response = await _httpClient.GetAsync("spaces");
                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Google Chat API");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to connect to Google Chat API: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Google Chat API");
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
                _logger.LogInformation("Disconnected from Google Chat API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Google Chat API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Chat API");
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
                    _logger.LogError($"Google Chat API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Google Chat");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Chat API");
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
                        var name = row["name"]?.ToString();
                        if (string.IsNullOrEmpty(name))
                        {
                            _logger.LogError("Cannot update entity without name");
                            continue;
                        }
                        response = await _httpClient.PatchAsync($"{endpoint}/{name}", content);
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Google Chat");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Chat API");
            }

            if (!parameters.ContainsKey("name"))
            {
                throw new ArgumentException("name parameter is required for deletion");
            }

            try
            {
                string endpoint = GetEndpointForEntity(entityName, parameters);
                var name = parameters["name"].ToString();

                var response = await _httpClient.DeleteAsync($"{endpoint}/{name}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to delete {entityName} with name {name}: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Google Chat");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "spaces",
                "memberships",
                "messages",
                "attachments",
                "reactions",
                "users",
                "media"
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
                case "spaces":
                    metadata.Rows.Add("name", "string", false, "Resource name of the space");
                    metadata.Rows.Add("displayName", "string", true, "Display name of the space");
                    metadata.Rows.Add("type", "string", false, "Type of space (SPACE, GROUP_CHAT, DIRECT_MESSAGE)");
                    metadata.Rows.Add("spaceType", "string", true, "Space type (space, group_chat, direct_message)");
                    metadata.Rows.Add("spaceThreadingState", "string", true, "Threading state (THREADED_MESSAGES, UNTHREADED_MESSAGES)");
                    metadata.Rows.Add("spaceHistoryState", "string", true, "History state (HISTORY_ON, HISTORY_OFF)");
                    metadata.Rows.Add("importMode", "boolean", true, "Whether space is in import mode");
                    metadata.Rows.Add("createTime", "datetime", true, "Creation time");
                    metadata.Rows.Add("lastActiveTime", "datetime", true, "Last active time");
                    metadata.Rows.Add("adminInstalled", "boolean", true, "Whether app is admin installed");
                    metadata.Rows.Add("singleUserBotDm", "boolean", true, "Whether it's a single user bot DM");
                    metadata.Rows.Add("spaceDetails", "object", true, "Space details");
                    metadata.Rows.Add("threaded", "boolean", true, "Whether space is threaded");
                    break;

                case "messages":
                    metadata.Rows.Add("name", "string", false, "Resource name of the message");
                    metadata.Rows.Add("sender", "object", false, "Sender of the message");
                    metadata.Rows.Add("createTime", "datetime", false, "Creation time");
                    metadata.Rows.Add("lastUpdateTime", "datetime", true, "Last update time");
                    metadata.Rows.Add("deleteTime", "datetime", true, "Deletion time");
                    metadata.Rows.Add("text", "string", true, "Plain text of the message");
                    metadata.Rows.Add("formattedText", "string", true, "Formatted text of the message");
                    metadata.Rows.Add("cards", "array", true, "Rich cards in the message");
                    metadata.Rows.Add("cardsV2", "array", true, "Rich cards v2 in the message");
                    metadata.Rows.Add("annotations", "array", true, "Annotations in the message");
                    metadata.Rows.Add("thread", "object", true, "Thread the message belongs to");
                    metadata.Rows.Add("space", "object", true, "Space the message belongs to");
                    metadata.Rows.Add("fallbackText", "string", true, "Fallback text for clients that don't support formatting");
                    metadata.Rows.Add("argumentText", "string", true, "Text with slash command arguments");
                    metadata.Rows.Add("slashCommand", "object", true, "Slash command information");
                    metadata.Rows.Add("attachment", "array", true, "File attachments");
                    metadata.Rows.Add("matchedUrl", "object", true, "Matched URL information");
                    metadata.Rows.Add("threadReply", "boolean", true, "Whether message is a thread reply");
                    metadata.Rows.Add("clientAssignedMessageId", "string", true, "Client assigned message ID");
                    break;

                case "memberships":
                    metadata.Rows.Add("name", "string", false, "Resource name of the membership");
                    metadata.Rows.Add("state", "string", false, "State of the membership (JOINED, INVITED, NOT_A_MEMBER)");
                    metadata.Rows.Add("role", "string", true, "Role of the member (ROLE_MEMBER, ROLE_MANAGER)");
                    metadata.Rows.Add("createTime", "datetime", true, "Creation time");
                    metadata.Rows.Add("deleteTime", "datetime", true, "Deletion time");
                    metadata.Rows.Add("member", "object", false, "Member information");
                    break;

                case "users":
                    metadata.Rows.Add("name", "string", false, "Resource name of the user");
                    metadata.Rows.Add("displayName", "string", true, "Display name of the user");
                    metadata.Rows.Add("domainId", "string", true, "Domain ID of the user");
                    metadata.Rows.Add("type", "string", false, "Type of user (HUMAN, BOT)");
                    metadata.Rows.Add("isAnonymous", "boolean", true, "Whether user is anonymous");
                    break;

                case "reactions":
                    metadata.Rows.Add("name", "string", false, "Resource name of the reaction");
                    metadata.Rows.Add("user", "object", false, "User who reacted");
                    metadata.Rows.Add("emoji", "object", false, "Emoji used in reaction");
                    metadata.Rows.Add("createTime", "datetime", true, "Creation time");
                    break;

                default:
                    metadata.Rows.Add("name", "string", false, "Unique identifier");
                    metadata.Rows.Add("createTime", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("updateTime", "datetime", true, "Last update timestamp");
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
                var response = await _httpClient.GetAsync("spaces");
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
                ["ClientId"] = _config?.ClientId ?? "Not configured",
                ["ProjectId"] = _config?.ProjectId ?? "Not configured",
                ["SpaceName"] = _config?.SpaceName ?? "Not configured",
                ["HasAccessToken"] = !string.IsNullOrEmpty(_config?.AccessToken),
                ["AuthType"] = !string.IsNullOrEmpty(_config?.ServiceAccountKeyPath) ? "Service Account" : "OAuth 2.0"
            };
        }

        private async Task<string> GetServiceAccountAccessTokenAsync()
        {
            try
            {
                // Note: In a real implementation, you would use Google.Apis.Auth library
                // to authenticate with service account credentials and get access token
                // For now, returning a placeholder - implement proper service account auth as needed
                _logger.LogWarning("Service account authentication requires Google.Apis.Auth NuGet package");
                return "service_account_token_placeholder";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service account access token for Google Chat");
                return null;
            }
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
                var tokenParams = new Dictionary<string, string>
                {
                    ["client_id"] = _config.ClientId,
                    ["client_secret"] = _config.ClientSecret,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code",
                    ["redirect_uri"] = "urn:ietf:wg:oauth:2.0:oob" // For desktop apps
                };

                tokenRequest.Content = new FormUrlEncodedContent(tokenParams);

                using var tokenClient = new HttpClient();
                var tokenResponse = await tokenClient.SendAsync(tokenRequest);

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var tokenDoc = JsonDocument.Parse(tokenJson);
                    var root = tokenDoc.RootElement;

                    return new TokenResponse
                    {
                        AccessToken = root.GetProperty("access_token").GetString(),
                        RefreshToken = root.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : null,
                        ExpiresIn = root.GetProperty("expires_in").GetInt32()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging code for token with Google Chat");
                return null;
            }
        }

        private string GetEndpointForEntity(string entityName, Dictionary<string, object> parameters = null)
        {
            var spaceName = parameters?.ContainsKey("space_name") == true ? parameters["space_name"].ToString() :
                           _config?.SpaceName;

            return entityName.ToLower() switch
            {
                "spaces" => "spaces",
                "memberships" => spaceName != null ? $"spaces/{spaceName}/members" : "spaces/{space_name}/members",
                "messages" => spaceName != null ? $"spaces/{spaceName}/messages" : "spaces/{space_name}/messages",
                "attachments" => parameters?.ContainsKey("message_name") == true ?
                               $"media/{parameters["message_name"]}/attachments" : "media/{message_name}/attachments",
                "reactions" => parameters?.ContainsKey("message_name") == true ?
                              $"spaces/{spaceName}/messages/{parameters["message_name"]}/reactions" :
                              "spaces/{space_name}/messages/{message_name}/reactions",
                "users" => "users",
                "media" => "media",
                _ => $"{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return "pageSize=100"; // Default pagination
            }

            var queryParams = new List<string> { "pageSize=100" }; // Default pagination
            foreach (var param in parameters)
            {
                if (param.Value != null && !param.Key.Contains("space_name") && !param.Key.Contains("message_name"))
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

                // Handle Google Chat API response structure
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
                _logger.LogError(ex, "Error parsing JSON response from Google Chat");
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

        private class TokenResponse
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public int ExpiresIn { get; set; }
        }
    }
}
