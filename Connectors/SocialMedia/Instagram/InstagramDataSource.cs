using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Instagram
{
    /// <summary>
    /// Configuration class for Instagram data source
    /// </summary>
    public class InstagramConfig
    {
        /// <summary>
        /// Instagram App ID from Facebook Developers Console
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Instagram App Secret from Facebook Developers Console
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Instagram Basic Display API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// User ID for the Instagram account
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// API version for Instagram Graph API (default: v18.0)
        /// </summary>
        public string ApiVersion { get; set; } = "v18.0";

        /// <summary>
        /// Base URL for Instagram Graph API
        /// </summary>
        public string BaseUrl => $"https://graph.instagram.com/{ApiVersion}";

        /// <summary>
        /// Base URL for Instagram Basic Display API
        /// </summary>
        public string BasicDisplayUrl => "https://graph.instagram.com";

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
    /// Instagram data source implementation for Beep framework
    /// Supports Instagram Graph API and Basic Display API
    /// </summary>
    public class InstagramDataSource : IDataSource
    {
        private readonly InstagramConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for InstagramDataSource
        /// </summary>
        /// <param name="config">Instagram configuration</param>
        public InstagramDataSource(InstagramConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for InstagramDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: AppId=xxx;AppSecret=xxx;AccessToken=xxx;UserId=xxx</param>
        public InstagramDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into InstagramConfig
        /// </summary>
        private InstagramConfig ParseConnectionString(string connectionString)
        {
            var config = new InstagramConfig();
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
                        case "appid":
                            config.AppId = value;
                            break;
                        case "appsecret":
                            config.AppSecret = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "userid":
                            config.UserId = value;
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
        /// Initialize entity metadata for Instagram entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // User Profile
            metadata["user"] = new EntityMetadata
            {
                EntityName = "user",
                DisplayName = "User Profile",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "account_type", Type = "string", DisplayName = "Account Type" },
                    new EntityField { Name = "media_count", Type = "integer", DisplayName = "Media Count" },
                    new EntityField { Name = "follows_count", Type = "integer", DisplayName = "Follows Count" },
                    new EntityField { Name = "followed_by_count", Type = "integer", DisplayName = "Followers Count" }
                }
            };

            // Media (Posts)
            metadata["media"] = new EntityMetadata
            {
                EntityName = "media",
                DisplayName = "Media Posts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Media ID" },
                    new EntityField { Name = "media_type", Type = "string", DisplayName = "Media Type" },
                    new EntityField { Name = "media_url", Type = "string", DisplayName = "Media URL" },
                    new EntityField { Name = "permalink", Type = "string", DisplayName = "Permalink" },
                    new EntityField { Name = "thumbnail_url", Type = "string", DisplayName = "Thumbnail URL" },
                    new EntityField { Name = "caption", Type = "string", DisplayName = "Caption" },
                    new EntityField { Name = "timestamp", Type = "datetime", DisplayName = "Timestamp" },
                    new EntityField { Name = "like_count", Type = "integer", DisplayName = "Like Count" },
                    new EntityField { Name = "comments_count", Type = "integer", DisplayName = "Comments Count" }
                }
            };

            // Stories
            metadata["stories"] = new EntityMetadata
            {
                EntityName = "stories",
                DisplayName = "Stories",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Story ID" },
                    new EntityField { Name = "media_type", Type = "string", DisplayName = "Media Type" },
                    new EntityField { Name = "media_url", Type = "string", DisplayName = "Media URL" },
                    new EntityField { Name = "thumbnail_url", Type = "string", DisplayName = "Thumbnail URL" },
                    new EntityField { Name = "timestamp", Type = "datetime", DisplayName = "Timestamp" },
                    new EntityField { Name = "expires_at", Type = "datetime", DisplayName = "Expires At" }
                }
            };

            // Insights
            metadata["insights"] = new EntityMetadata
            {
                EntityName = "insights",
                DisplayName = "Insights",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "media_id", Type = "string", IsPrimaryKey = true, DisplayName = "Media ID" },
                    new EntityField { Name = "metric", Type = "string", IsPrimaryKey = true, DisplayName = "Metric" },
                    new EntityField { Name = "value", Type = "integer", DisplayName = "Value" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "period", Type = "string", DisplayName = "Period" }
                }
            };

            // Tags
            metadata["tags"] = new EntityMetadata
            {
                EntityName = "tags",
                DisplayName = "Tags",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Tag ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Tag Name" },
                    new EntityField { Name = "media_count", Type = "integer", DisplayName = "Media Count" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Instagram API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for Instagram connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Test connection by getting user profile
                var testUrl = $"{_config.BasicDisplayUrl}/me?fields=id,username,account_type&access_token={_config.AccessToken}";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Instagram API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Instagram API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Instagram API
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
        /// Get data from Instagram API
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
                var fields = parameters.ContainsKey("fields") ? parameters["fields"].ToString() : GetDefaultFields(entityName);

                switch (entityName.ToLower())
                {
                    case "user":
                        url = $"{_config.BasicDisplayUrl}/me?fields={fields}&access_token={_config.AccessToken}";
                        break;

                    case "media":
                        var mediaLimit = parameters.ContainsKey("limit") ? parameters["limit"].ToString() : "25";
                        url = $"{_config.BasicDisplayUrl}/me/media?fields={fields}&limit={mediaLimit}&access_token={_config.AccessToken}";
                        break;

                    case "stories":
                        url = $"{_config.BasicDisplayUrl}/me/stories?fields={fields}&access_token={_config.AccessToken}";
                        break;

                    case "insights":
                        var mediaId = parameters.ContainsKey("media_id") ? parameters["media_id"].ToString() : "";
                        var metrics = parameters.ContainsKey("metrics") ? parameters["metrics"].ToString() : "engagement,impressions,reach,saved";
                        url = $"{_config.BaseUrl}/{mediaId}/insights?metric={metrics}&access_token={_config.AccessToken}";
                        break;

                    case "tags":
                        var tagName = parameters.ContainsKey("tag_name") ? parameters["tag_name"].ToString() : "";
                        url = $"{_config.BaseUrl}/ig_hashtag_search?user_id={_config.UserId}&q={tagName}&access_token={_config.AccessToken}";
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
                    throw new Exception($"Instagram API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get default fields for entity
        /// </summary>
        private string GetDefaultFields(string entityName)
        {
            return entityName.ToLower() switch
            {
                "user" => "id,username,account_type,media_count",
                "media" => "id,media_type,media_url,permalink,thumbnail_url,caption,timestamp,like_count,comments_count",
                "stories" => "id,media_type,media_url,thumbnail_url,timestamp,expires_at",
                "insights" => "value,title,description,period",
                "tags" => "id,name,media_count",
                _ => "*"
            };
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

                // Handle different response structures
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataProp))
                {
                    dataElement = dataProp;
                }
                else
                {
                    // Single object response
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
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
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

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Object)
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
                else if (dataElement.ValueKind == JsonValueKind.Array)
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
            return new List<string> { "user", "media", "stories", "insights", "tags" };
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
        /// Insert data (not supported for Instagram API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Instagram API");
        }

        /// <summary>
        /// Update data (not supported for Instagram API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Instagram API");
        }

        /// <summary>
        /// Delete data (not supported for Instagram API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Instagram API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Instagram, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Instagram";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Instagram Data Source";

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
