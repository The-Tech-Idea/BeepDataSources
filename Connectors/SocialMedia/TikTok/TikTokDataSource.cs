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
using TheTechIdea.Beep.Vis;

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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TikTok)]
    public class TikTokDataSource : WebAPIDataSource
    {
        private readonly TikTokConfig _config;

        /// <summary>
        /// Constructor for TikTokDataSource
        /// </summary>
        public TikTokDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new TikTokConfig();
            _entityMetadata = InitializeEntityMetadata();
            InitializeEntities();
        }

        /// <summary>
        /// Initialize entities for TikTok data source
        /// </summary>
        private void InitializeEntities()
        {
            base.Entities.Clear();
            base.EntitiesNames.Clear();

            // User Info
            var userEntity = new EntityStructure
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
            base.Entities.Add("user", userEntity);
            base.EntitiesNames.Add("user");

            // Videos
            var videosEntity = new EntityStructure
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
            base.Entities.Add("videos", videosEntity);
            base.EntitiesNames.Add("videos");

            // Video List
            var videolistEntity = new EntityStructure
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
            base.Entities.Add("videolist", videolistEntity);
            base.EntitiesNames.Add("videolist");

            // Comments
            var commentsEntity = new EntityStructure
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
            base.Entities.Add("comments", commentsEntity);
            base.EntitiesNames.Add("comments");

            // Analytics
            var analyticsEntity = new EntityStructure
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
            base.Entities.Add("analytics", analyticsEntity);
            base.EntitiesNames.Add("analytics");

            // Followers
            var followersEntity = new EntityStructure
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
            base.Entities.Add("followers", followersEntity);
            base.EntitiesNames.Add("followers");
        }

        /// <summary>
        /// Connect to TikTok API
        /// </summary>
        public override async Task<ErrorObject> ConnectAsync()
        {
            var errorObject = new ErrorObject();

            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    errorObject.Flag = Errors.Failed;
                    errorObject.Message = "Access token is required for TikTok connection";
                    return errorObject;
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                HttpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                HttpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user info
                var testUrl = $"{_config.BaseUrl}/user/info/?open_id={_config.OpenId}";
                var response = await HttpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    errorObject.Flag = Errors.Ok;
                    errorObject.Message = "Successfully connected to TikTok API";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    errorObject.Flag = Errors.Failed;
                    errorObject.Message = $"TikTok API connection failed: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Failed to connect to TikTok API: {ex.Message}";
            }

            return errorObject;
        }

        /// <summary>
        /// Disconnect from TikTok API
        /// </summary>
        public override async Task<ErrorObject> DisconnectAsync()
        {
            var errorObject = new ErrorObject();

            try
            {
                if (HttpClient != null)
                {
                    HttpClient.Dispose();
                    HttpClient = null;
                }

                errorObject.Flag = Errors.Ok;
                errorObject.Message = "Successfully disconnected from TikTok API";
            }
            catch (Exception ex)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Failed to disconnect from TikTok API: {ex.Message}";
            }

            return errorObject;
        }

        /// <summary>
        /// Get data from TikTok API
        /// </summary>
        public override async Task<(DataTable, ErrorObject)> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            var errorObject = new ErrorObject();
            DataTable dataTable = null;

            try
            {
                parameters ??= new Dictionary<string, object>();

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
                        errorObject.Flag = Errors.Failed;
                        errorObject.Message = $"Unsupported entity: {entityName}";
                        return (null, errorObject);
                }

                // Rate limiting delay
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }

                var response = await HttpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    errorObject.Flag = Errors.Failed;
                    errorObject.Message = $"TikTok API request failed: {response.StatusCode} - {jsonContent}";
                    return (null, errorObject);
                }

                dataTable = ParseJsonToDataTable(jsonContent, entityName);
                errorObject.Flag = Errors.Ok;
                errorObject.Message = $"Successfully retrieved {entityName} data";
            }
            catch (Exception ex)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
            }

            return (dataTable, errorObject);
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

        [CommandAttribute(ObjectType = "TikTokUser", PointType = EnumPointType.Function, Name = "GetUserInfo", Caption = "Get TikTok User Info", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokUser>> GetUserInfo(string openId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/user/info/?open_id={openId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokUser>>(json);
        }

        [CommandAttribute(ObjectType = "TikTokVideo", PointType = EnumPointType.Function, Name = "GetUserVideos", Caption = "Get TikTok User Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetUserVideos(string openId, long cursor = 0, int maxCount = 20)
        {
            var url = $"{ConnectionProperties.BaseUrl}/video/list/?open_id={openId}&cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType = "TikTokVideo", PointType = EnumPointType.Function, Name = "GetVideoDetails", Caption = "Get TikTok Video Details", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetVideoDetails(string videoId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/video/query/?video_id={videoId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType = "TikTokVideo", PointType = EnumPointType.Function, Name = "GetTrendingVideos", Caption = "Get TikTok Trending Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetTrendingVideos(long cursor = 0, int maxCount = 20)
        {
            var url = $"{ConnectionProperties.BaseUrl}/video/trending/?cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType = "TikTokMusic", PointType = EnumPointType.Function, Name = "GetMusicInfo", Caption = "Get TikTok Music Info", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokMusic>> GetMusicInfo(string musicId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/music/info/?music_id={musicId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokMusic>>(json);
        }

        [CommandAttribute(ObjectType = "TikTokVideo", PointType = EnumPointType.Function, Name = "GetHashtagVideos", Caption = "Get TikTok Hashtag Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetHashtagVideos(string hashtagName, long cursor = 0, int maxCount = 20)
        {
            var url = $"{ConnectionProperties.BaseUrl}/hashtag/video/list/?hashtag_name={Uri.EscapeDataString(hashtagName)}&cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TikTok, PointType = EnumPointType.Function, ObjectType = "TikTokVideo", Name = "CreateVideo", Caption = "Create TikTok Video", ClassType = "TikTokDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "tiktok.png", misc = "ReturnType: IEnumerable<TikTokVideo>")]
        public async Task<IEnumerable<TikTokVideo>> CreateVideoAsync(TikTokVideo video)
        {
            try
            {
                // Note: TikTok video posting requires multipart upload, this is a placeholder
                var result = await PostAsync($"{ConnectionProperties.BaseUrl}/video/publish/", video);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var createdVideo = response?.Data?.FirstOrDefault();
                    if (createdVideo != null)
                    {
                        return new List<TikTokVideo> { createdVideo }.Select(v => v.Attach<TikTokVideo>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating video: {ex.Message}");
            }
            return new List<TikTokVideo>();
        }
    }
}
