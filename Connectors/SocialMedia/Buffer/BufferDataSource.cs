using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Buffer
{
    /// <summary>
    /// Configuration class for Buffer data source
    /// </summary>
    public class BufferConfig
    {
        /// <summary>
        /// Buffer App Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Buffer App Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Buffer API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Buffer API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for Buffer API
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// API version for Buffer API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Buffer API
        /// </summary>
        public string BaseUrl => $"https://api.bufferapp.com/{ApiVersion}";

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
    /// Buffer data source implementation for Beep framework
    /// Supports Buffer API v1
    /// </summary>
    public class BufferDataSource : IDataSource
    {
        private readonly BufferConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for BufferDataSource
        /// </summary>
        /// <param name="config">Buffer configuration</param>
        public BufferDataSource(BufferConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for BufferDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ClientId=xxx;ClientSecret=xxx;ApiKey=xxx;AccessToken=xxx</param>
        public BufferDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into BufferConfig
        /// </summary>
        private BufferConfig ParseConnectionString(string connectionString)
        {
            var config = new BufferConfig();
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
        /// Initialize entity metadata for Buffer entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Posts
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Post ID" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Post Text" },
                    new EntityField { Name = "profile_id", Type = "string", DisplayName = "Profile ID" },
                    new EntityField { Name = "profile_service", Type = "string", DisplayName = "Social Network" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "scheduled_at", Type = "datetime", DisplayName = "Scheduled At" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "sent_at", Type = "datetime", DisplayName = "Sent At" },
                    new EntityField { Name = "media", Type = "string", DisplayName = "Media" },
                    new EntityField { Name = "statistics", Type = "string", DisplayName = "Statistics" },
                    new EntityField { Name = "is_draft", Type = "boolean", DisplayName = "Is Draft" },
                    new EntityField { Name = "is_pinned", Type = "boolean", DisplayName = "Is Pinned" },
                    new EntityField { Name = "via", Type = "string", DisplayName = "Posted Via" }
                }
            };

            // Profiles
            metadata["profiles"] = new EntityMetadata
            {
                EntityName = "profiles",
                DisplayName = "Social Profiles",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Profile ID" },
                    new EntityField { Name = "service", Type = "string", DisplayName = "Social Network" },
                    new EntityField { Name = "service_id", Type = "string", DisplayName = "Service ID" },
                    new EntityField { Name = "service_username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "service_name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "avatar", Type = "string", DisplayName = "Avatar URL" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Timezone" },
                    new EntityField { Name = "is_connected", Type = "boolean", DisplayName = "Is Connected" },
                    new EntityField { Name = "is_disabled", Type = "boolean", DisplayName = "Is Disabled" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "profile_id", Type = "string", IsPrimaryKey = true, DisplayName = "Profile ID" },
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "likes", Type = "integer", DisplayName = "Likes" },
                    new EntityField { Name = "shares", Type = "integer", DisplayName = "Shares" },
                    new EntityField { Name = "comments", Type = "integer", DisplayName = "Comments" },
                    new EntityField { Name = "reach", Type = "integer", DisplayName = "Reach" },
                    new EntityField { Name = "engagement_rate", Type = "decimal", DisplayName = "Engagement Rate" },
                    new EntityField { Name = "follower_count", Type = "integer", DisplayName = "Follower Count" },
                    new EntityField { Name = "following_count", Type = "integer", DisplayName = "Following Count" }
                }
            };

            // Campaigns
            metadata["campaigns"] = new EntityMetadata
            {
                EntityName = "campaigns",
                DisplayName = "Campaigns",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Campaign ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Campaign Name" },
                    new EntityField { Name = "color", Type = "string", DisplayName = "Color" },
                    new EntityField { Name = "is_active", Type = "boolean", DisplayName = "Is Active" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Links
            metadata["links"] = new EntityMetadata
            {
                EntityName = "links",
                DisplayName = "Links",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Link ID" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "shortened_url", Type = "string", DisplayName = "Shortened URL" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "thumbnail", Type = "string", DisplayName = "Thumbnail" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Buffer API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken) && string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("Access token or API key is required for Buffer connection");
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
                var testUrl = $"{_config.BaseUrl}/user.json";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Buffer API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Buffer API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Buffer API
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
        /// Get data from Buffer API
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
                        var profileId = parameters.ContainsKey("profile_id") ? parameters["profile_id"].ToString() : "";
                        var page = parameters.ContainsKey("page") ? (int)parameters["page"] : 1;
                        var count = parameters.ContainsKey("count") ? (int)parameters["count"] : 25;
                        var status = parameters.ContainsKey("status") ? parameters["status"].ToString() : "";
                        var profileParam = string.IsNullOrEmpty(profileId) ? "" : $"&profile_id={profileId}";
                        var statusParam = string.IsNullOrEmpty(status) ? "" : $"&status={status}";
                        url = $"{_config.BaseUrl}/updates.json?page={page}&count={count}{profileParam}{statusParam}";
                        break;

                    case "profiles":
                        url = $"{_config.BaseUrl}/profiles.json";
                        break;

                    case "analytics":
                        var analyticsProfileId = parameters.ContainsKey("profile_id") ? parameters["profile_id"].ToString() : "";
                        var startDate = parameters.ContainsKey("start_date") ? parameters["start_date"].ToString() : DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                        var endDate = parameters.ContainsKey("end_date") ? parameters["end_date"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        url = $"{_config.BaseUrl}/analytics/{analyticsProfileId}.json?start_date={startDate}&end_date={endDate}";
                        break;

                    case "campaigns":
                        url = $"{_config.BaseUrl}/campaigns.json";
                        break;

                    case "links":
                        var linkId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = $"{_config.BaseUrl}/links/{linkId}.json";
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
                    throw new Exception($"Buffer API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Buffer API response structure
                JsonElement dataElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
                }
                else if (root.TryGetProperty("updates", out var updatesProp))
                {
                    dataElement = updatesProp;
                }
                else if (root.TryGetProperty("profiles", out var profilesProp))
                {
                    dataElement = profilesProp;
                }
                else if (root.TryGetProperty("campaigns", out var campaignsProp))
                {
                    dataElement = campaignsProp;
                }
                else if (root.TryGetProperty("links", out var linksProp))
                {
                    dataElement = linksProp;
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
            return new List<string> { "posts", "profiles", "analytics", "campaigns", "links" };
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
        /// Insert data (limited support for Buffer API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "posts")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating posts would go here
            // This is a placeholder as Buffer API has specific requirements for post creation
            throw new NotImplementedException("Post creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Buffer API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Buffer API");
        }

        /// <summary>
        /// Delete data (limited support for Buffer API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Buffer API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Buffer, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Buffer";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Buffer Data Source";

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
