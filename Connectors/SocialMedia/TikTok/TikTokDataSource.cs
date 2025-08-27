using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.TikTok
{
    /// <summary>
    /// Configuration class for TikTok data source
    /// </summary>
    public class TikTokConfig
    {
        /// <summary>
        /// TikTok App ID
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// TikTok App Secret
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for TikTok API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Open ID for TikTok user
        /// </summary>
        public string OpenId { get; set; } = string.Empty;

        /// <summary>
        /// TikTok Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// API version for TikTok API (default: v2)
        /// </summary>
        public string ApiVersion { get; set; } = "v2";

        /// <summary>
        /// Base URL for TikTok API
        /// </summary>
        public string BaseUrl => $"https://open-api.tiktok.com";

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

        /// <summary>
        /// Maximum results per request
        /// </summary>
        public int MaxResults { get; set; } = 20;
    }

    /// <summary>
    /// TikTok data source implementation for Beep framework
    /// Supports TikTok for Developers API
    /// </summary>
    public class TikTokDataSource : IDataSource
    {
        private readonly TikTokConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for TikTokDataSource
        /// </summary>
        /// <param name="config">TikTok configuration</param>
        public TikTokDataSource(TikTokConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for TikTokDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: AppId=xxx;AppSecret=xxx;AccessToken=xxx;OpenId=xxx</param>
        public TikTokDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into TikTokConfig
        /// </summary>
        private TikTokConfig ParseConnectionString(string connectionString)
        {
            var config = new TikTokConfig();
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
                        case "openid":
                            config.OpenId = value;
                            break;
                        case "username":
                            config.Username = value;
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
                        case "maxresults":
                            if (int.TryParse(value, out var maxResults))
                                config.MaxResults = maxResults;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for TikTok entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // User Info
            metadata["user"] = new EntityMetadata
            {
                EntityName = "user",
                DisplayName = "User Info",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "open_id", Type = "string", IsPrimaryKey = true, DisplayName = "Open ID" },
                    new EntityField { Name = "union_id", Type = "string", DisplayName = "Union ID" },
                    new EntityField { Name = "avatar_url", Type = "string", DisplayName = "Avatar URL" },
                    new EntityField { Name = "avatar_url_100", Type = "string", DisplayName = "Avatar URL 100" },
                    new EntityField { Name = "avatar_large_url", Type = "string", DisplayName = "Avatar Large URL" },
                    new EntityField { Name = "display_name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "bio_description", Type = "string", DisplayName = "Bio Description" },
                    new EntityField { Name = "profile_deep_link", Type = "string", DisplayName = "Profile Deep Link" },
                    new EntityField { Name = "is_verified", Type = "boolean", DisplayName = "Is Verified" },
                    new EntityField { Name = "follower_count", Type = "integer", DisplayName = "Follower Count" },
                    new EntityField { Name = "following_count", Type = "integer", DisplayName = "Following Count" },
                    new EntityField { Name = "likes_count", Type = "integer", DisplayName = "Likes Count" },
                    new EntityField { Name = "video_count", Type = "integer", DisplayName = "Video Count" }
                }
            };

            // Videos
            metadata["videos"] = new EntityMetadata
            {
                EntityName = "videos",
                DisplayName = "Videos",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Video ID" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "cover_image_url", Type = "string", DisplayName = "Cover Image URL" },
                    new EntityField { Name = "share_url", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "video_description", Type = "string", DisplayName = "Video Description" },
                    new EntityField { Name = "duration", Type = "integer", DisplayName = "Duration" },
                    new EntityField { Name = "height", Type = "integer", DisplayName = "Height" },
                    new EntityField { Name = "width", Type = "integer", DisplayName = "Width" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "embed_html", Type = "string", DisplayName = "Embed HTML" },
                    new EntityField { Name = "embed_link", Type = "string", DisplayName = "Embed Link" },
                    new EntityField { Name = "like_count", Type = "integer", DisplayName = "Like Count" },
                    new EntityField { Name = "comment_count", Type = "integer", DisplayName = "Comment Count" },
                    new EntityField { Name = "share_count", Type = "integer", DisplayName = "Share Count" },
                    new EntityField { Name = "view_count", Type = "integer", DisplayName = "View Count" },
                    new EntityField { Name = "play_count", Type = "integer", DisplayName = "Play Count" }
                }
            };

            // Video List
            metadata["videolist"] = new EntityMetadata
            {
                EntityName = "videolist",
                DisplayName = "Video List",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Video ID" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "cover_image_url", Type = "string", DisplayName = "Cover Image URL" },
                    new EntityField { Name = "share_url", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "video_description", Type = "string", DisplayName = "Video Description" },
                    new EntityField { Name = "region_code", Type = "string", DisplayName = "Region Code" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "like_count", Type = "integer", DisplayName = "Like Count" },
                    new EntityField { Name = "comment_count", Type = "integer", DisplayName = "Comment Count" },
                    new EntityField { Name = "share_count", Type = "integer", DisplayName = "Share Count" },
                    new EntityField { Name = "view_count", Type = "integer", DisplayName = "View Count" },
                    new EntityField { Name = "play_count", Type = "integer", DisplayName = "Play Count" }
                }
            };

            // Comments
            metadata["comments"] = new EntityMetadata
            {
                EntityName = "comments",
                DisplayName = "Comments",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Comment ID" },
                    new EntityField { Name = "video_id", Type = "string", DisplayName = "Video ID" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Comment Text" },
                    new EntityField { Name = "create_time", Type = "datetime", DisplayName = "Create Time" },
                    new EntityField { Name = "parent_comment_id", Type = "string", DisplayName = "Parent Comment ID" },
                    new EntityField { Name = "reply_comment_total", Type = "integer", DisplayName = "Reply Count" },
                    new EntityField { Name = "like_count", Type = "integer", DisplayName = "Like Count" },
                    new EntityField { Name = "reply_to_reply_id", Type = "string", DisplayName = "Reply To Reply ID" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "video_id", Type = "string", IsPrimaryKey = true, DisplayName = "Video ID" },
                    new EntityField { Name = "view_count", Type = "integer", DisplayName = "View Count" },
                    new EntityField { Name = "like_count", Type = "integer", DisplayName = "Like Count" },
                    new EntityField { Name = "comment_count", Type = "integer", DisplayName = "Comment Count" },
                    new EntityField { Name = "share_count", Type = "integer", DisplayName = "Share Count" },
                    new EntityField { Name = "play_count", Type = "integer", DisplayName = "Play Count" },
                    new EntityField { Name = "download_count", Type = "integer", DisplayName = "Download Count" },
                    new EntityField { Name = "reach", Type = "integer", DisplayName = "Reach" },
                    new EntityField { Name = "video_views_paid", Type = "integer", DisplayName = "Paid Views" },
                    new EntityField { Name = "video_views_organic", Type = "integer", DisplayName = "Organic Views" }
                }
            };

            // Followers
            metadata["followers"] = new EntityMetadata
            {
                EntityName = "followers",
                DisplayName = "Followers",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "open_id", Type = "string", IsPrimaryKey = true, DisplayName = "Open ID" },
                    new EntityField { Name = "union_id", Type = "string", DisplayName = "Union ID" },
                    new EntityField { Name = "avatar_url", Type = "string", DisplayName = "Avatar URL" },
                    new EntityField { Name = "display_name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "bio_description", Type = "string", DisplayName = "Bio Description" },
                    new EntityField { Name = "follower_count", Type = "integer", DisplayName = "Follower Count" },
                    new EntityField { Name = "following_count", Type = "integer", DisplayName = "Following Count" },
                    new EntityField { Name = "likes_count", Type = "integer", DisplayName = "Likes Count" },
                    new EntityField { Name = "video_count", Type = "integer", DisplayName = "Video Count" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to TikTok API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for TikTok connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user info
                var testUrl = $"{_config.BaseUrl}/user/info/?open_id={_config.OpenId}";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"TikTok API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to TikTok API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from TikTok API
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
        /// Get data from TikTok API
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
                var maxResults = parameters.ContainsKey("max_count") ? parameters["max_count"].ToString() : _config.MaxResults.ToString();
                var cursor = parameters.ContainsKey("cursor") ? parameters["cursor"].ToString() : "0";

                switch (entityName.ToLower())
                {
                    case "user":
                        url = $"{_config.BaseUrl}/user/info/?open_id={_config.OpenId}";
                        break;

                    case "videos":
                        var videoIds = parameters.ContainsKey("video_ids") ? parameters["video_ids"].ToString() : "";
                        url = $"{_config.BaseUrl}/video/query/?open_id={_config.OpenId}&video_ids={videoIds}";
                        break;

                    case "videolist":
                        url = $"{_config.BaseUrl}/video/list/?open_id={_config.OpenId}&cursor={cursor}&max_count={maxResults}";
                        break;

                    case "comments":
                        var videoCommentsId = parameters.ContainsKey("video_id") ? parameters["video_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/video/comment/list/?open_id={_config.OpenId}&video_id={videoCommentsId}&cursor={cursor}&max_count={maxResults}";
                        break;

                    case "analytics":
                        var analyticsVideoId = parameters.ContainsKey("video_id") ? parameters["video_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/video/data/?open_id={_config.OpenId}&video_id={analyticsVideoId}";
                        break;

                    case "followers":
                        url = $"{_config.BaseUrl}/user/following/list/?open_id={_config.OpenId}&cursor={cursor}&max_count={maxResults}";
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
                    throw new Exception($"TikTok API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle TikTok API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataProp))
                {
                    // Check for list response
                    if (dataProp.TryGetProperty("list", out var listProp))
                    {
                        dataElement = listProp;
                    }
                    else if (dataProp.TryGetProperty("video_list", out var videoListProp))
                    {
                        dataElement = videoListProp;
                    }
                    else if (dataProp.TryGetProperty("comment_list", out var commentListProp))
                    {
                        dataElement = commentListProp;
                    }
                    else if (dataProp.TryGetProperty("user_list", out var userListProp))
                    {
                        dataElement = userListProp;
                    }
                    else
                    {
                        dataElement = dataProp;
                    }
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
            return new List<string> { "user", "videos", "videolist", "comments", "analytics", "followers" };
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
        /// Insert data (not supported for TikTok API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for TikTok API");
        }

        /// <summary>
        /// Update data (not supported for TikTok API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for TikTok API");
        }

        /// <summary>
        /// Delete data (not supported for TikTok API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for TikTok API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For TikTok, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "TikTok";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "TikTok Data Source";

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
