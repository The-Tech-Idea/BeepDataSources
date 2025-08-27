using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Reddit
{
    /// <summary>
    /// Configuration class for Reddit data source
    /// </summary>
    public class RedditConfig
    {
        /// <summary>
        /// Reddit App Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Reddit App Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Reddit Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Reddit Password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Reddit API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for Reddit API
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// User Agent string for Reddit API
        /// </summary>
        public string UserAgent { get; set; } = "BeepDataSources.Reddit/1.0.0";

        /// <summary>
        /// API version for Reddit API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Reddit API
        /// </summary>
        public string BaseUrl => "https://www.reddit.com/api";

        /// <summary>
        /// OAuth URL for Reddit API
        /// </summary>
        public string OAuthUrl => "https://oauth.reddit.com";

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
        public int RateLimitDelayMs { get; set; } = 2000;
    }

    /// <summary>
    /// Reddit data source implementation for Beep framework
    /// Supports Reddit API
    /// </summary>
    public class RedditDataSource : IDataSource
    {
        private readonly RedditConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for RedditDataSource
        /// </summary>
        /// <param name="config">Reddit configuration</param>
        public RedditDataSource(RedditConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for RedditDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ClientId=xxx;ClientSecret=xxx;Username=xxx;Password=xxx;UserAgent=xxx</param>
        public RedditDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into RedditConfig
        /// </summary>
        private RedditConfig ParseConnectionString(string connectionString)
        {
            var config = new RedditConfig();
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
                        case "username":
                            config.Username = value;
                            break;
                        case "password":
                            config.Password = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "refreshtoken":
                            config.RefreshToken = value;
                            break;
                        case "useragent":
                            config.UserAgent = value;
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
        /// Initialize entity metadata for Reddit entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Posts/Submissions
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Post ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "selftext", Type = "string", DisplayName = "Self Text" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "subreddit", Type = "string", DisplayName = "Subreddit" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author" },
                    new EntityField { Name = "score", Type = "integer", DisplayName = "Score" },
                    new EntityField { Name = "num_comments", Type = "integer", DisplayName = "Number of Comments" },
                    new EntityField { Name = "created_utc", Type = "datetime", DisplayName = "Created UTC" },
                    new EntityField { Name = "upvote_ratio", Type = "decimal", DisplayName = "Upvote Ratio" },
                    new EntityField { Name = "is_self", Type = "boolean", DisplayName = "Is Self Post" },
                    new EntityField { Name = "over_18", Type = "boolean", DisplayName = "NSFW" },
                    new EntityField { Name = "stickied", Type = "boolean", DisplayName = "Stickied" },
                    new EntityField { Name = "locked", Type = "boolean", DisplayName = "Locked" },
                    new EntityField { Name = "archived", Type = "boolean", DisplayName = "Archived" }
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
                    new EntityField { Name = "body", Type = "string", DisplayName = "Body" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author" },
                    new EntityField { Name = "subreddit", Type = "string", DisplayName = "Subreddit" },
                    new EntityField { Name = "link_id", Type = "string", DisplayName = "Link ID" },
                    new EntityField { Name = "parent_id", Type = "string", DisplayName = "Parent ID" },
                    new EntityField { Name = "score", Type = "integer", DisplayName = "Score" },
                    new EntityField { Name = "created_utc", Type = "datetime", DisplayName = "Created UTC" },
                    new EntityField { Name = "edited", Type = "boolean", DisplayName = "Edited" },
                    new EntityField { Name = "is_submitter", Type = "boolean", DisplayName = "Is Submitter" },
                    new EntityField { Name = "stickied", Type = "boolean", DisplayName = "Stickied" },
                    new EntityField { Name = "locked", Type = "boolean", DisplayName = "Locked" },
                    new EntityField { Name = "collapsed", Type = "boolean", DisplayName = "Collapsed" }
                }
            };

            // Subreddits
            metadata["subreddits"] = new EntityMetadata
            {
                EntityName = "subreddits",
                DisplayName = "Subreddits",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Subreddit ID" },
                    new EntityField { Name = "display_name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "display_name_prefixed", Type = "string", DisplayName = "Display Name Prefixed" },
                    new EntityField { Name = "subscribers", Type = "integer", DisplayName = "Subscribers" },
                    new EntityField { Name = "active_user_count", Type = "integer", DisplayName = "Active Users" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "public_description", Type = "string", DisplayName = "Public Description" },
                    new EntityField { Name = "created_utc", Type = "datetime", DisplayName = "Created UTC" },
                    new EntityField { Name = "over18", Type = "boolean", DisplayName = "NSFW" },
                    new EntityField { Name = "subreddit_type", Type = "string", DisplayName = "Subreddit Type" },
                    new EntityField { Name = "lang", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" }
                }
            };

            // Users
            metadata["users"] = new EntityMetadata
            {
                EntityName = "users",
                DisplayName = "Users",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "date", Type = "datetime", DisplayName = "Date" },
                    new EntityField { Name = "comment_karma", Type = "integer", DisplayName = "Comment Karma" },
                    new EntityField { Name = "link_karma", Type = "integer", DisplayName = "Link Karma" },
                    new EntityField { Name = "is_gold", Type = "boolean", DisplayName = "Gold Member" },
                    new EntityField { Name = "is_mod", Type = "boolean", DisplayName = "Moderator" },
                    new EntityField { Name = "is_employee", Type = "boolean", DisplayName = "Reddit Employee" },
                    new EntityField { Name = "has_verified_email", Type = "boolean", DisplayName = "Verified Email" },
                    new EntityField { Name = "created_utc", Type = "datetime", DisplayName = "Created UTC" },
                    new EntityField { Name = "snoovatar_img", Type = "string", DisplayName = "Avatar Image" }
                }
            };

            // Search Results
            metadata["search"] = new EntityMetadata
            {
                EntityName = "search",
                DisplayName = "Search Results",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Result ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "selftext", Type = "string", DisplayName = "Self Text" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "subreddit", Type = "string", DisplayName = "Subreddit" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author" },
                    new EntityField { Name = "score", Type = "integer", DisplayName = "Score" },
                    new EntityField { Name = "num_comments", Type = "integer", DisplayName = "Number of Comments" },
                    new EntityField { Name = "created_utc", Type = "datetime", DisplayName = "Created UTC" },
                    new EntityField { Name = "relevance_score", Type = "decimal", DisplayName = "Relevance Score" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Authenticate with Reddit API
        /// </summary>
        private async Task<string> AuthenticateAsync()
        {
            try
            {
                using var authClient = new HttpClient();
                authClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);

                var authData = new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["username"] = _config.Username,
                    ["password"] = _config.Password
                };

                var content = new FormUrlEncodedContent(authData);
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
                authClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var response = await authClient.PostAsync($"{_config.BaseUrl}/v1/access_token", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Reddit authentication failed: {response.StatusCode} - {responseContent}");
                }

                using var doc = JsonDocument.Parse(responseContent);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();
                var refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();

                _config.AccessToken = accessToken;
                _config.RefreshToken = refreshToken;

                return accessToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to authenticate with Reddit API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Connect to Reddit API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Authenticate if no access token
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    await AuthenticateAsync();
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user info
                var testUrl = $"{_config.OAuthUrl}/api/v1/me";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    // Try to re-authenticate
                    await AuthenticateAsync();
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                    response = await _httpClient.GetAsync(testUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        _isConnected = true;
                        return true;
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Reddit API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Reddit API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Reddit API
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
        /// Get data from Reddit API
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
                        var subreddit = parameters.ContainsKey("subreddit") ? parameters["subreddit"].ToString() : "all";
                        var sort = parameters.ContainsKey("sort") ? parameters["sort"].ToString() : "hot";
                        var limit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 25;
                        url = $"{_config.OAuthUrl}/r/{subreddit}/{sort}?limit={limit}";
                        break;

                    case "comments":
                        var linkId = parameters.ContainsKey("link_id") ? parameters["link_id"].ToString() : "";
                        var commentSort = parameters.ContainsKey("sort") ? parameters["sort"].ToString() : "top";
                        var commentLimit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 25;
                        url = $"{_config.OAuthUrl}/comments/{linkId}?sort={commentSort}&limit={commentLimit}";
                        break;

                    case "subreddits":
                        var subredditType = parameters.ContainsKey("type") ? parameters["type"].ToString() : "popular";
                        var subredditLimit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 25;
                        url = $"{_config.OAuthUrl}/subreddits/{subredditType}?limit={subredditLimit}";
                        break;

                    case "users":
                        var username = parameters.ContainsKey("username") ? parameters["username"].ToString() : "";
                        url = $"{_config.OAuthUrl}/user/{username}/about";
                        break;

                    case "search":
                        var query = parameters.ContainsKey("query") ? parameters["query"].ToString() : "";
                        var searchSubreddit = parameters.ContainsKey("subreddit") ? parameters["subreddit"].ToString() : "";
                        var searchLimit = parameters.ContainsKey("limit") ? (int)parameters["limit"] : 25;
                        var subredditParam = string.IsNullOrEmpty(searchSubreddit) ? "" : $"&subreddit={searchSubreddit}";
                        url = $"{_config.OAuthUrl}/search?q={Uri.EscapeDataString(query)}&limit={searchLimit}&sort=relevance&type=link{subredditParam}";
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
                    throw new Exception($"Reddit API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Reddit API response structure
                JsonElement dataElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    // For posts/comments, Reddit returns an array with [posts, comments]
                    dataElement = root[0];
                }
                else if (root.TryGetProperty("data", out var dataProp))
                {
                    dataElement = dataProp;
                }
                else if (root.TryGetProperty("children", out var childrenProp))
                {
                    dataElement = childrenProp;
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

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var row = dataTable.NewRow();

                        if (item.TryGetProperty("data", out var itemData))
                        {
                            foreach (var property in itemData.EnumerateObject())
                            {
                                if (dataTable.Columns.Contains(property.Name))
                                {
                                    row[property.Name] = GetJsonValue(property.Value);
                                }
                            }
                        }
                        else
                        {
                            foreach (var property in item.EnumerateObject())
                            {
                                if (dataTable.Columns.Contains(property.Name))
                                {
                                    row[property.Name] = GetJsonValue(property.Value);
                                }
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
            return new List<string> { "posts", "comments", "subreddits", "users", "search" };
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
        /// Insert data (limited support for Reddit API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Reddit API");
        }

        /// <summary>
        /// Update data (limited support for Reddit API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Reddit API");
        }

        /// <summary>
        /// Delete data (limited support for Reddit API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Reddit API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Reddit, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Reddit";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Reddit Data Source";

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
