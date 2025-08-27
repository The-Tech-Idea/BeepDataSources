using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Hootsuite
{
    /// <summary>
    /// Configuration class for Hootsuite data source
    /// </summary>
    public class HootsuiteConfig
    {
        /// <summary>
        /// Hootsuite App Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Hootsuite App Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Hootsuite API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Hootsuite API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for Hootsuite API
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// API version for Hootsuite API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Hootsuite API
        /// </summary>
        public string BaseUrl => $"https://platform.hootsuite.com/{ApiVersion}";

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Rate limit delay between requests in milliseconds
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// Hootsuite data source implementation for Beep framework
    /// Supports Hootsuite API v1
    /// </summary>
    public class HootsuiteDataSource : IDataSource
    {
        private readonly HootsuiteConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for HootsuiteDataSource
        /// </summary>
        /// <param name="config">Hootsuite configuration</param>
        public HootsuiteDataSource(HootsuiteConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for HootsuiteDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ClientId=xxx;ClientSecret=xxx;ApiKey=xxx;AccessToken=xxx</param>
        public HootsuiteDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into HootsuiteConfig
        /// </summary>
        private HootsuiteConfig ParseConnectionString(string connectionString)
        {
            var config = new HootsuiteConfig();
            var parts = connectionString.Split(';');

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "clientid":
                            config.ClientId = value;
                            break;
                        case "clientsecret":
                            config.ClientSecret = value;
                            break;
                        case "apikey":
                            config.ApiKey = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "refreshtoken":
                            config.RefreshToken = value;
                            break;
                        case "apiversion":
                            config.ApiVersion = value;
                            break;
                        case "timeoutseconds":
                            if (int.TryParse(value, out var timeout))
                                config.TimeoutSeconds = timeout;
                            break;
                        case "maxretries":
                            if (int.TryParse(value, out var retries))
                                config.MaxRetries = retries;
                            break;
                        case "ratelimitdelayms":
                            if (int.TryParse(value, out var delay))
                                config.RateLimitDelayMs = delay;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for Hootsuite entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Posts/Messages
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Post ID" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Post Text" },
                    new EntityField { Name = "socialProfileId", Type = "string", DisplayName = "Social Profile ID" },
                    new EntityField { Name = "socialNetworkId", Type = "string", DisplayName = "Social Network ID" },
                    new EntityField { Name = "socialNetworkName", Type = "string", DisplayName = "Social Network" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "scheduledSendTime", Type = "datetime", DisplayName = "Scheduled Send Time" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updatedAt", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "mediaUrls", Type = "string", DisplayName = "Media URLs" },
                    new EntityField { Name = "webLinks", Type = "string", DisplayName = "Web Links" },
                    new EntityField { Name = "statistics", Type = "string", DisplayName = "Statistics" },
                    new EntityField { Name = "isDraft", Type = "boolean", DisplayName = "Is Draft" },
                    new EntityField { Name = "isPublished", Type = "boolean", DisplayName = "Is Published" }
                }
            };

            // Social Profiles
            metadata["socialprofiles"] = new EntityMetadata
            {
                EntityName = "socialprofiles",
                DisplayName = "Social Profiles",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Profile ID" },
                    new EntityField { Name = "socialNetworkId", Type = "string", DisplayName = "Social Network ID" },
                    new EntityField { Name = "socialNetworkName", Type = "string", DisplayName = "Social Network" },
                    new EntityField { Name = "socialNetworkUsername", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "avatarUrl", Type = "string", DisplayName = "Avatar URL" },
                    new EntityField { Name = "profileUrl", Type = "string", DisplayName = "Profile URL" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "isActive", Type = "boolean", DisplayName = "Is Active" },
                    new EntityField { Name = "isBusinessAccount", Type = "boolean", DisplayName = "Is Business Account" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updatedAt", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "socialProfileId", Type = "string", IsPrimaryKey = true, DisplayName = "Profile ID" },
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "likes", Type = "integer", DisplayName = "Likes" },
                    new EntityField { Name = "shares", Type = "integer", DisplayName = "Shares" },
                    new EntityField { Name = "comments", Type = "integer", DisplayName = "Comments" },
                    new EntityField { Name = "reach", Type = "integer", DisplayName = "Reach" },
                    new EntityField { Name = "engagement", Type = "integer", DisplayName = "Engagement" },
                    new EntityField { Name = "engagementRate", Type = "decimal", DisplayName = "Engagement Rate" },
                    new EntityField { Name = "followerCount", Type = "integer", DisplayName = "Follower Count" },
                    new EntityField { Name = "followingCount", Type = "integer", DisplayName = "Following Count" }
                }
            };

            // Organizations
            metadata["organizations"] = new EntityMetadata
            {
                EntityName = "organizations",
                DisplayName = "Organizations",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Organization ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Organization Name" },
                    new EntityField { Name = "companyName", Type = "string", DisplayName = "Company Name" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "city", Type = "string", DisplayName = "City" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updatedAt", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Teams
            metadata["teams"] = new EntityMetadata
            {
                EntityName = "teams",
                DisplayName = "Teams",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Team ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Team Name" },
                    new EntityField { Name = "organizationId", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "isActive", Type = "boolean", DisplayName = "Is Active" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updatedAt", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Hootsuite API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken) && string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("Access token or API key is required for Hootsuite connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                if (!string.IsNullOrEmpty(_config.AccessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);
                }
                else if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _config.ApiKey);
                }

                // Test connection by getting user info
                var testUrl = $"{_config.BaseUrl}/me";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Hootsuite API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Hootsuite API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Hootsuite API
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
            return true;
        }

        /// <summary>
        /// Get data from Hootsuite API
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
            }

            parameters ??= new Dictionary<string, object>();

            try
            {
                string url;

                switch (entityName.ToLower())
                {
                    case "posts":
                        var socialProfileId = parameters.ContainsKey("socialProfileId") ? parameters["socialProfileId"].ToString() : "";
                        var state = parameters.ContainsKey("state") ? parameters["state"].ToString() : "";
                        var limit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 25;
                        var profileParam = string.IsNullOrEmpty(socialProfileId) ? "" : $"&socialProfileId={socialProfileId}";
                        var stateParam = string.IsNullOrEmpty(state) ? "" : $"&state={state}";
                        url = $"{_config.BaseUrl}/messages?limit={limit}{profileParam}{stateParam}";
                        break;

                    case "socialprofiles":
                        url = $"{_config.BaseUrl}/socialProfiles";
                        break;

                    case "analytics":
                        var analyticsProfileId = parameters.ContainsKey("socialProfileId") ? parameters["socialProfileId"].ToString() : "";
                        var startDate = parameters.ContainsKey("startDate") ? parameters["startDate"].ToString() : DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                        var endDate = parameters.ContainsKey("endDate") ? parameters["endDate"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        url = $"{_config.BaseUrl}/analytics/{analyticsProfileId}?startDate={startDate}&endDate={endDate}";
                        break;

                    case "organizations":
                        url = $"{_config.BaseUrl}/organizations";
                        break;

                    case "teams":
                        var orgId = parameters.ContainsKey("organizationId") ? parameters["organizationId"].ToString() : "";
                        url = $"{_config.BaseUrl}/teams?organizationId={orgId}";
                        break;

                    default:
                        throw new ArgumentException($"Unsupported entity: {entityName}");
                }

                // Rate limiting delay
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }

                var response = await _httpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Hootsuite API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata.ContainsKey(entityName.ToLower()) ? _entityMetadata[entityName.ToLower()] : null;

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Handle Hootsuite API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataProp))
                {
                    dataElement = dataProp;
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
                }
                else
                {
                    dataElement = root;
                }

                // Create columns based on metadata or first object
                if (metadata != null)
                {
                    foreach (var field in metadata.Fields)
                    {
                        dataTable.Columns.Add(field.Name, GetFieldType(field.Type));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (dataTable.Columns.Contains(property.Name))
                            {
                                row[property.Name] = GetJsonValue(property.Value);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            row[property.Name] = GetJsonValue(property.Value);
                        }
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse JSON response: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "long" => typeof(long),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get value from JSON element
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// Get available entities
        /// </summary>
        public List<string> GetEntities()
        {
            return new List<string> { "posts", "socialprofiles", "analytics", "organizations", "teams" };
        }

        /// <summary>
        /// Get entity metadata
        /// </summary>
        public EntityMetadata GetEntityMetadata(string entityName)
        {
            if (_entityMetadata.ContainsKey(entityName.ToLower()))
            {
                return _entityMetadata[entityName.ToLower()];
            }
            throw new ArgumentException($"Entity '{entityName}' not found");
        }

        /// <summary>
        /// Insert data (limited support for Hootsuite API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "posts")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating posts would go here
            // This is a placeholder as Hootsuite API has specific requirements for post creation
            throw new NotImplementedException("Post creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Hootsuite API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Hootsuite API");
        }

        /// <summary>
        /// Delete data (limited support for Hootsuite API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Hootsuite API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Hootsuite, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Hootsuite";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Hootsuite Data Source";

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
        }
    }
}
