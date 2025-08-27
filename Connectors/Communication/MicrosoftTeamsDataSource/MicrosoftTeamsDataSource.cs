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

namespace BeepDM.Connectors.Communication.MicrosoftTeams
{
    public class MicrosoftTeamsConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string AccessToken { get; set; }
    }

    public class MicrosoftTeamsDataSource : IDataSource
    {
        private readonly ILogger<MicrosoftTeamsDataSource> _logger;
        private HttpClient _httpClient;
        private MicrosoftTeamsConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Microsoft Teams";
        public string DataSourceType => "Communication";
        public string Version => "1.0.0";
        public string Description => "Microsoft Teams Enterprise Communication Platform Data Source";

        public MicrosoftTeamsDataSource(ILogger<MicrosoftTeamsDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("ClientId") || !parameters.ContainsKey("ClientSecret") || !parameters.ContainsKey("TenantId"))
                {
                    throw new ArgumentException("ClientId, ClientSecret, and TenantId are required parameters");
                }

                _config = new MicrosoftTeamsConfig
                {
                    ClientId = parameters["ClientId"].ToString(),
                    ClientSecret = parameters["ClientSecret"].ToString(),
                    TenantId = parameters["TenantId"].ToString()
                };

                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
                };

                // Get access token using client credentials flow
                var tokenResponse = await GetAccessTokenAsync();
                if (tokenResponse != null)
                {
                    _config.AccessToken = tokenResponse;
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
                    _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                    // Test connection by getting organization info
                    var response = await _httpClient.GetAsync("organization");
                    if (response.IsSuccessStatusCode)
                    {
                        _isConnected = true;
                        _logger.LogInformation("Successfully connected to Microsoft Teams API");
                        return true;
                    }
                }

                _logger.LogError("Failed to connect to Microsoft Teams API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Microsoft Teams API");
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
                _logger.LogInformation("Disconnected from Microsoft Teams API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Microsoft Teams API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Teams API");
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
                    _logger.LogError($"Microsoft Teams API error: {response.StatusCode} - {response.ReasonPhrase}");
                    return new DataTable();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(jsonString, entityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Microsoft Teams");
                throw;
            }
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Teams API");
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
                _logger.LogError(ex, $"Error updating entity {entityName} in Microsoft Teams");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Teams API");
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
                _logger.LogError(ex, $"Error deleting entity {entityName} from Microsoft Teams");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "teams",
                "channels",
                "messages",
                "users",
                "groups",
                "chats",
                "meetings",
                "calendar",
                "files",
                "tabs",
                "apps",
                "bots",
                "webhooks",
                "subscriptions",
                "organization",
                "directory",
                "reports",
                "audit",
                "compliance",
                "security"
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
                case "teams":
                    metadata.Rows.Add("id", "string", false, "Team unique identifier");
                    metadata.Rows.Add("displayName", "string", false, "Team display name");
                    metadata.Rows.Add("description", "string", true, "Team description");
                    metadata.Rows.Add("internalId", "string", true, "Internal team ID");
                    metadata.Rows.Add("classification", "string", true, "Team classification");
                    metadata.Rows.Add("specialization", "string", true, "Team specialization");
                    metadata.Rows.Add("visibility", "string", false, "Team visibility (public, private, hiddenMembership)");
                    metadata.Rows.Add("webUrl", "string", true, "Team web URL");
                    metadata.Rows.Add("isArchived", "boolean", false, "Whether the team is archived");
                    metadata.Rows.Add("isMembershipLimitedToOwners", "boolean", false, "Whether membership is limited to owners");
                    metadata.Rows.Add("memberSettings", "object", true, "Member settings");
                    metadata.Rows.Add("guestSettings", "object", true, "Guest settings");
                    metadata.Rows.Add("messagingSettings", "object", true, "Messaging settings");
                    metadata.Rows.Add("funSettings", "object", true, "Fun settings");
                    metadata.Rows.Add("discoverySettings", "object", true, "Discovery settings");
                    metadata.Rows.Add("createdDateTime", "datetime", false, "Team creation date and time");
                    metadata.Rows.Add("lastModifiedDateTime", "datetime", true, "Team last modified date and time");
                    break;

                case "channels":
                    metadata.Rows.Add("id", "string", false, "Channel unique identifier");
                    metadata.Rows.Add("displayName", "string", false, "Channel display name");
                    metadata.Rows.Add("description", "string", true, "Channel description");
                    metadata.Rows.Add("isFavoriteByDefault", "boolean", true, "Whether channel is favorited by default");
                    metadata.Rows.Add("email", "string", true, "Channel email address");
                    metadata.Rows.Add("webUrl", "string", true, "Channel web URL");
                    metadata.Rows.Add("membershipType", "string", false, "Channel membership type (standard, private, shared, unknownFutureValue)");
                    metadata.Rows.Add("createdDateTime", "datetime", false, "Channel creation date and time");
                    metadata.Rows.Add("lastModifiedDateTime", "datetime", true, "Channel last modified date and time");
                    metadata.Rows.Add("teamId", "string", false, "Parent team ID");
                    break;

                case "messages":
                    metadata.Rows.Add("id", "string", false, "Message unique identifier");
                    metadata.Rows.Add("replyToId", "string", true, "ID of the message being replied to");
                    metadata.Rows.Add("etag", "string", false, "ETag for the message");
                    metadata.Rows.Add("messageType", "string", false, "Message type (message, systemEventMessage, unknownFutureValue)");
                    metadata.Rows.Add("createdDateTime", "datetime", false, "Message creation date and time");
                    metadata.Rows.Add("lastModifiedDateTime", "datetime", false, "Message last modified date and time");
                    metadata.Rows.Add("lastEditedDateTime", "datetime", true, "Message last edited date and time");
                    metadata.Rows.Add("deletedDateTime", "datetime", true, "Message deletion date and time");
                    metadata.Rows.Add("subject", "string", true, "Message subject");
                    metadata.Rows.Add("summary", "string", true, "Message summary");
                    metadata.Rows.Add("chatId", "string", true, "Chat ID where message was sent");
                    metadata.Rows.Add("channelIdentity", "object", true, "Channel identity information");
                    metadata.Rows.Add("onBehalfOf", "object", true, "Information about the user sending on behalf of another");
                    metadata.Rows.Add("policyViolation", "object", true, "Policy violation information");
                    metadata.Rows.Add("eventDetail", "object", true, "Event detail for system messages");
                    metadata.Rows.Add("body", "object", false, "Message body content");
                    metadata.Rows.Add("from", "object", true, "Message sender information");
                    metadata.Rows.Add("attachments", "array", true, "Message attachments");
                    metadata.Rows.Add("mentions", "array", true, "Message mentions");
                    metadata.Rows.Add("reactions", "array", true, "Message reactions");
                    break;

                case "users":
                    metadata.Rows.Add("id", "string", false, "User unique identifier");
                    metadata.Rows.Add("displayName", "string", true, "User display name");
                    metadata.Rows.Add("givenName", "string", true, "User given name");
                    metadata.Rows.Add("surname", "string", true, "User surname");
                    metadata.Rows.Add("mail", "string", true, "User email address");
                    metadata.Rows.Add("mobilePhone", "string", true, "User mobile phone number");
                    metadata.Rows.Add("officeLocation", "string", true, "User office location");
                    metadata.Rows.Add("preferredLanguage", "string", true, "User preferred language");
                    metadata.Rows.Add("userPrincipalName", "string", false, "User principal name");
                    metadata.Rows.Add("jobTitle", "string", true, "User job title");
                    metadata.Rows.Add("department", "string", true, "User department");
                    metadata.Rows.Add("companyName", "string", true, "User company name");
                    metadata.Rows.Add("employeeId", "string", true, "User employee ID");
                    metadata.Rows.Add("createdDateTime", "datetime", true, "User creation date and time");
                    metadata.Rows.Add("lastPasswordChangeDateTime", "datetime", true, "Last password change date and time");
                    metadata.Rows.Add("accountEnabled", "boolean", true, "Whether account is enabled");
                    metadata.Rows.Add("assignedLicenses", "array", true, "Assigned licenses");
                    metadata.Rows.Add("assignedPlans", "array", true, "Assigned plans");
                    break;

                case "chats":
                    metadata.Rows.Add("id", "string", false, "Chat unique identifier");
                    metadata.Rows.Add("topic", "string", true, "Chat topic");
                    metadata.Rows.Add("createdDateTime", "datetime", false, "Chat creation date and time");
                    metadata.Rows.Add("lastUpdatedDateTime", "datetime", false, "Chat last updated date and time");
                    metadata.Rows.Add("chatType", "string", false, "Chat type (group, oneOnOne, meeting, unknownFutureValue)");
                    metadata.Rows.Add("webUrl", "string", true, "Chat web URL");
                    metadata.Rows.Add("tenantId", "string", true, "Tenant ID");
                    metadata.Rows.Add("onlineMeetingInfo", "object", true, "Online meeting information");
                    metadata.Rows.Add("members", "array", true, "Chat members");
                    metadata.Rows.Add("messages", "array", true, "Chat messages");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
                    metadata.Rows.Add("displayName", "string", true, "Display name");
                    metadata.Rows.Add("createdDateTime", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("lastModifiedDateTime", "datetime", true, "Last update timestamp");
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
                var response = await _httpClient.GetAsync("me");
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
                ["TenantId"] = _config?.TenantId ?? "Not configured",
                ["ClientId"] = _config?.ClientId ?? "Not configured",
                ["HasAccessToken"] = !string.IsNullOrEmpty(_config?.AccessToken)
            };
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token");
                var tokenParams = new Dictionary<string, string>
                {
                    ["client_id"] = _config.ClientId,
                    ["client_secret"] = _config.ClientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                };

                tokenRequest.Content = new FormUrlEncodedContent(tokenParams);

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
                _logger.LogError(ex, "Error getting access token for Microsoft Teams");
                return null;
            }
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "teams" => "teams",
                "channels" => "teams/{teamId}/channels",
                "messages" => "teams/{teamId}/channels/{channelId}/messages",
                "users" => "users",
                "groups" => "groups",
                "chats" => "chats",
                "meetings" => "me/onlineMeetings",
                "calendar" => "me/calendar/events",
                "files" => "me/drive/root/children",
                "tabs" => "teams/{teamId}/channels/{channelId}/tabs",
                "apps" => "appCatalogs/teamsApps",
                "bots" => "teams/{teamId}/installedApps",
                "webhooks" => "subscriptions",
                "subscriptions" => "subscriptions",
                "organization" => "organization",
                "directory" => "directoryObjects",
                "reports" => "reports",
                "audit" => "auditLogs/signIns",
                "compliance" => "compliance/ediscovery/cases",
                "security" => "security/alerts",
                _ => $"{entityName}"
            };
        }

        private string BuildQueryParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return "$top=50"; // Default pagination
            }

            var queryParams = new List<string> { "$top=50" }; // Default pagination
            foreach (var param in parameters)
            {
                if (param.Value != null)
                {
                    if (param.Key.StartsWith("$"))
                    {
                        // OData query parameters
                        queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value.ToString())}");
                    }
                    else
                    {
                        // Custom parameters
                        queryParams.Add($"${param.Key}={Uri.EscapeDataString(param.Value.ToString())}");
                    }
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

                // Handle Microsoft Graph API response structure
                if (root.TryGetProperty("value", out var valueArray))
                {
                    // Array response (multiple items)
                    foreach (var item in valueArray.EnumerateArray())
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
                _logger.LogError(ex, "Error parsing JSON response from Microsoft Teams");
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
