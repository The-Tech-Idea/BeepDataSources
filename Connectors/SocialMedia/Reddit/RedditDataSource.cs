using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Reddit.Models;

namespace TheTechIdea.Beep.Connectors.Reddit
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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Reddit)]
    public class RedditDataSource : WebAPIDataSource
    {
        /// <summary>
        /// Gets the WebAPI connection properties
        /// </summary>
        protected WebAPIConnectionProperties ConnectionProperties => Dataconnection?.ConnectionProp as WebAPIConnectionProperties;

        /// <summary>
        /// Reddit configuration
        /// </summary>
        private RedditConfig _config = new();

        /// <summary>
        /// Entity structures dictionary
        /// </summary>
        private Dictionary<string, EntityStructure> _entities = new();

        /// <summary>
        /// Constructor for RedditDataSource
        /// </summary>
        public RedditDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            InitializeEntities();
        }

    /// <summary>
    /// Initialize entities for Reddit data source
    /// </summary>
    private void InitializeEntities()
    {
        // Posts/Submissions
        _entities["posts"] = new EntityStructure
        {
            EntityName = "posts",
            ViewID = 1,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "title", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "selftext", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "url", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "subreddit", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "author", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "score", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "num_comments", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_utc", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "upvote_ratio", fieldtype = "decimal", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "is_self", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "over_18", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "stickied", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "locked", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "archived", fieldtype = "boolean", ValueRetrievedFromParent = false }
            }
        };

        // Comments
        _entities["comments"] = new EntityStructure
        {
            EntityName = "comments",
            ViewID = 2,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "body", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "author", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "subreddit", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "link_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "parent_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "score", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_utc", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "edited", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "is_submitter", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "stickied", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "locked", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "collapsed", fieldtype = "boolean", ValueRetrievedFromParent = false }
            }
        };

        // Subreddits
        _entities["subreddits"] = new EntityStructure
        {
            EntityName = "subreddits",
            ViewID = 3,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "display_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "display_name_prefixed", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "subscribers", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "active_user_count", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "description", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "public_description", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_utc", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "over18", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "subreddit_type", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "lang", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "url", fieldtype = "string", ValueRetrievedFromParent = false }
            }
        };

        // Users
        _entities["users"] = new EntityStructure
        {
            EntityName = "users",
            ViewID = 4,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "date", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "comment_karma", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "link_karma", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "is_gold", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "is_mod", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "is_employee", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "has_verified_email", fieldtype = "boolean", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_utc", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "snoovatar_img", fieldtype = "string", ValueRetrievedFromParent = false }
            }
        };

        // Search Results
        // Search
        _entities["search"] = new EntityStructure
        {
            EntityName = "search",
            ViewID = 5,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "title", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "selftext", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "url", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "subreddit", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "author", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "score", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "num_comments", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_utc", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "relevance_score", fieldtype = "decimal", ValueRetrievedFromParent = false }
            }
        };

        // Update EntitiesNames collection
        EntitiesNames.AddRange(Entities.Keys);
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
    public async Task<IErrorsInfo> ConnectAsync(WebAPIConnectionProperties properties)
    {
        try
        {
            if (string.IsNullOrEmpty(properties.AccessToken))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Access token is required for Reddit connection";
                return ErrorObject;
            }

            // Test connection by getting user info
            var testUrl = $"{properties.BaseUrl}/api/v1/me";
            var response = await GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Successfully connected to Reddit API";
                return ErrorObject;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Reddit API connection failed: {response.StatusCode} - {errorContent}";
                return ErrorObject;
            }
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to connect to Reddit API: {ex.Message}";
            return ErrorObject;
        }
    }

    /// <summary>
    /// Disconnect from Reddit API
    /// </summary>
    public async Task<IErrorsInfo> DisconnectAsync()
    {
        ErrorObject.Flag = Errors.Ok;
        ErrorObject.Message = "Successfully disconnected from Reddit API";
        return ErrorObject;
    }

    // Sync
    public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
    {
        var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
        return data ?? Array.Empty<object>();
    }

    /// <summary>
    /// Get data from Reddit API
    /// </summary>
    public override async Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter> filters)
    {
        try
        {
            if (filters == null)
                filters = new List<AppFilter>();

            string url;

            switch (entityName.ToLower())
            {
                case "posts":
                    var subreddit = filters.FirstOrDefault(f => f.FieldName == "subreddit")?.FilterValue?.ToString() ?? "all";
                    var sort = filters.FirstOrDefault(f => f.FieldName == "sort")?.FilterValue?.ToString() ?? "hot";
                    var limit = filters.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue != null ? Convert.ToInt32(filters.FirstOrDefault(f => f.FieldName == "limit").FilterValue) : 25;
                    url = $"{ConnectionProperties.BaseUrl}/r/{subreddit}/{sort}?limit={limit}";
                    break;

                case "comments":
                    var linkId = filters.FirstOrDefault(f => f.FieldName == "link_id")?.FilterValue?.ToString() ?? "";
                    var commentSort = filters.FirstOrDefault(f => f.FieldName == "sort")?.FilterValue?.ToString() ?? "top";
                    var commentLimit = filters.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue != null ? Convert.ToInt32(filters.FirstOrDefault(f => f.FieldName == "limit").FilterValue) : 25;
                    url = $"{ConnectionProperties.BaseUrl}/comments/{linkId}?sort={commentSort}&limit={commentLimit}";
                    break;

                case "subreddits":
                    var subredditType = filters.FirstOrDefault(f => f.FieldName == "type")?.FilterValue?.ToString() ?? "popular";
                    var subredditLimit = filters.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue != null ? Convert.ToInt32(filters.FirstOrDefault(f => f.FieldName == "limit").FilterValue) : 25;
                    url = $"{ConnectionProperties.BaseUrl}/subreddits/{subredditType}?limit={subredditLimit}";
                    break;

                case "users":
                    var username = filters.FirstOrDefault(f => f.FieldName == "username")?.FilterValue?.ToString() ?? "";
                    url = $"{ConnectionProperties.BaseUrl}/user/{username}/about";
                    break;

                case "search":
                    var query = filters.FirstOrDefault(f => f.FieldName == "query")?.FilterValue?.ToString() ?? "";
                    var searchSubreddit = filters.FirstOrDefault(f => f.FieldName == "subreddit")?.FilterValue?.ToString() ?? "";
                    var searchLimit = filters.FirstOrDefault(f => f.FieldName == "limit")?.FilterValue != null ? Convert.ToInt32(filters.FirstOrDefault(f => f.FieldName == "limit").FilterValue) : 25;
                    var subredditParam = string.IsNullOrEmpty(searchSubreddit) ? "" : $"&subreddit={searchSubreddit}";
                    url = $"{ConnectionProperties.BaseUrl}/search?q={Uri.EscapeDataString(query)}&limit={searchLimit}&sort=relevance&type=link{subredditParam}";
                    break;

                default:
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unsupported entity: {entityName}";
                    return new List<object>();
            }

            // Rate limiting delay
            var rateLimitDelay = ConnectionProperties.GetPropertyValue("RateLimitDelayMs");
            if (rateLimitDelay != null && int.TryParse(rateLimitDelay.ToString(), out var delay) && delay > 0)
            {
                await Task.Delay(delay);
            }

            var response = await GetAsync(url);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Reddit API request failed: {response.StatusCode} - {jsonContent}";
                return new List<object>();
            }

            // Parse and store the data
            var dataTable = ParseJsonToDataTable(jsonContent, entityName);
            if (_entities.ContainsKey(entityName))
            {
                _entities[entityName].EntityData = dataTable;
            }

            // Convert DataTable to list of objects
            var result = new List<object>();
            foreach (DataRow row in dataTable.Rows)
            {
                result.Add(row);
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
            return new List<object>();
        }
    }

    // Paged
    public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
    {
        var items = GetEntity(EntityName, filter).ToList();
        var totalRecords = items.Count;
        var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult
        {
            Data = pagedItems,
            PageNumber = Math.Max(1, pageNumber),
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber * pageSize < totalRecords
        };
    }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var entityStructure = _entities.ContainsKey(entityName.ToLower()) ? _entities[entityName.ToLower()] : null;

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
                if (entityStructure != null)
                {
                    foreach (var field in entityStructure.Fields)
                    {
                        dataTable.Columns.Add(field.fieldname, GetFieldType(field.fieldtype));
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

        [CommandAttribute(ObjectType = "RedditPost", PointType = EnumPointType.Function, Name = "GetPosts", Caption = "Get Reddit Posts", ClassName = "RedditDataSource")]
        public async Task<RedditResponse<RedditPost>> GetPosts(string subreddit = "all", string sort = "hot", int limit = 25)
        {
            var url = $"{ConnectionProperties.BaseUrl}/r/{subreddit}/{sort}?limit={limit}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditResponse<RedditPost>>(json);
        }

        [CommandAttribute(ObjectType = "RedditSubreddit", PointType = EnumPointType.Function, Name = "GetSubredditInfo", Caption = "Get Reddit Subreddit Info", ClassName = "RedditDataSource")]
        public async Task<RedditSubredditResponse> GetSubredditInfo(string subreddit)
        {
            var url = $"{ConnectionProperties.BaseUrl}/r/{subreddit}/about";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditSubredditResponse>(json);
        }

        [CommandAttribute(ObjectType = "RedditUser", PointType = EnumPointType.Function, Name = "GetUserInfo", Caption = "Get Reddit User Info", ClassName = "RedditDataSource")]
        public async Task<RedditUserResponse> GetUserInfo(string username)
        {
            var url = $"{ConnectionProperties.BaseUrl}/user/{username}/about";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditUserResponse>(json);
        }

        [CommandAttribute(ObjectType = "RedditComment", PointType = EnumPointType.Function, Name = "GetComments", Caption = "Get Reddit Comments", ClassName = "RedditDataSource")]
        public async Task<RedditResponse<RedditComment>> GetComments(string postId, string sort = "top", int limit = 25)
        {
            var url = $"{ConnectionProperties.BaseUrl}/comments/{postId}?sort={sort}&limit={limit}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditResponse<RedditComment>>(json);
        }

        [CommandAttribute(ObjectType = "RedditSearchResult", PointType = EnumPointType.Function, Name = "GetSearchResults", Caption = "Get Reddit Search Results", ClassName = "RedditDataSource")]
        public async Task<RedditResponse<RedditSearchResult>> GetSearchResults(string query, string subreddit = null, int limit = 25)
        {
            var subredditParam = string.IsNullOrEmpty(subreddit) ? "" : $"&subreddit={subreddit}";
            var url = $"{ConnectionProperties.BaseUrl}/search?q={Uri.EscapeDataString(query)}&limit={limit}&sort=relevance&type=link{subredditParam}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditResponse<RedditSearchResult>>(json);
        }

        [CommandAttribute(ObjectType = "RedditPost", PointType = EnumPointType.Function, Name = "GetHotPosts", Caption = "Get Reddit Hot Posts", ClassName = "RedditDataSource")]
        public async Task<RedditResponse<RedditPost>> GetHotPosts(string subreddit = "all", int limit = 25)
        {
            var url = $"{ConnectionProperties.BaseUrl}/r/{subreddit}/hot?limit={limit}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RedditResponse<RedditPost>>(json);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Reddit, PointType = EnumPointType.Function, ObjectType = "RedditPost", Name = "CreatePost", Caption = "Create Reddit Post", ClassType = "RedditDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "reddit.png", misc = "ReturnType: IEnumerable<RedditPost>")]
        public async Task<IEnumerable<RedditPost>> CreatePostAsync(RedditPost post)
        {
            try
            {
                string endpoint = $"{ConnectionProperties.BaseUrl}/api/submit";
                var query = new Dictionary<string, string>
                {
                    ["title"] = post.Title,
                    ["text"] = post.SelfText ?? "",
                    ["sr"] = post.Subreddit,
                    ["kind"] = "self" // for text posts
                };
                var result = await PostAsync(endpoint, null, query);
                if (result.IsSuccessStatusCode)
                {
                    string json = await result.Content.ReadAsStringAsync();
                    var submitResult = JsonSerializer.Deserialize<RedditSubmitResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (submitResult?.Success == true && submitResult?.Data?.Name != null)
                    {
                        // Fetch the created post
                        string postId = submitResult.Data.Name.Split('_')[1]; // t3_xxxx
                        var createdPost = await GetPostAsync(postId);
                        if (createdPost != null)
                        {
                            return new List<RedditPost> { createdPost }.Select(p => p.Attach<RedditPost>(this));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating post: {ex.Message}");
            }
            return new List<RedditPost>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Reddit, PointType = EnumPointType.Function, ObjectType = "RedditPost", Name = "UpdatePost", Caption = "Update Reddit Post", ClassType = "RedditDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "reddit.png", misc = "ReturnType: IEnumerable<RedditPost>")]
        public async Task<IEnumerable<RedditPost>> UpdatePostAsync(RedditPost post)
        {
            try
            {
                string endpoint = $"{ConnectionProperties.BaseUrl}/api/editusertext";
                var query = new Dictionary<string, string>
                {
                    ["thing_id"] = $"t3_{post.Id}",
                    ["text"] = post.SelfText ?? ""
                };
                var result = await PostAsync(endpoint, null, query);
                if (result.IsSuccessStatusCode)
                {
                    // Fetch updated post
                    var updatedPost = await GetPostAsync(post.Id);
                    if (updatedPost != null)
                    {
                        return new List<RedditPost> { updatedPost }.Select(p => p.Attach<RedditPost>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating post: {ex.Message}");
            }
            return new List<RedditPost>();
        }

        private async Task<RedditPost> GetPostAsync(string postId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/by_id/t3_{postId}";
            var response = await GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var redditResponse = JsonSerializer.Deserialize<RedditResponse<RedditPost>>(json);
                return redditResponse?.Data?.Children?.FirstOrDefault()?.Data;
            }
            return null;
        }
    }
}
