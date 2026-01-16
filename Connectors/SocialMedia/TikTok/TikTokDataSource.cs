using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using TheTechIdea.Beep.Connectors.TikTok.Models;

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

            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            InitializeEntities();
        }

        /// <summary>
        /// Initialize entities for TikTok data source
        /// </summary>
        private void InitializeEntities()
        {
            Entities.Clear();
            EntitiesNames.Clear();

            // User Info
            var userEntity = new EntityStructure
            {
                EntityName = "user",
                Caption = "User Info",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "open_id", Fieldtype ="string", IsKey = true, Caption = "Open ID" },
                    new EntityField {FieldName = "union_id", Fieldtype ="string", Caption = "Union ID" },
                    new EntityField {FieldName = "avatar_url", Fieldtype ="string", Caption = "Avatar URL" },
                    new EntityField {FieldName = "avatar_url_100", Fieldtype ="string", Caption = "Avatar URL 100" },
                    new EntityField {FieldName = "avatar_large_url", Fieldtype ="string", Caption = "Avatar Large URL" },
                    new EntityField {FieldName = "display_name", Fieldtype ="string", Caption = "Display Name" },
                    new EntityField {FieldName = "bio_description", Fieldtype ="string", Caption = "Bio Description" },
                    new EntityField {FieldName = "profile_deep_link", Fieldtype ="string", Caption = "Profile Deep Link" },
                    new EntityField {FieldName = "is_verified", Fieldtype ="boolean", Caption = "Is Verified" },
                    new EntityField {FieldName = "follower_count", Fieldtype ="integer", Caption = "Follower Count" },
                    new EntityField {FieldName = "following_count", Fieldtype ="integer", Caption = "Following Count" },
                    new EntityField {FieldName = "likes_count", Fieldtype ="integer", Caption = "Likes Count" },
                    new EntityField {FieldName = "video_count", Fieldtype ="integer", Caption = "Video Count" }
                }
            };
            Entities.Add(userEntity);
            EntitiesNames.Add("user");

            // Videos
            var videosEntity = new EntityStructure
            {
                EntityName = "videos",
                Caption = "Videos",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Video ID" },
                    new EntityField {FieldName = "create_time", Fieldtype ="datetime", Caption = "Create Time" },
                    new EntityField {FieldName = "cover_image_url", Fieldtype ="string", Caption = "Cover Image URL" },
                    new EntityField {FieldName = "share_url", Fieldtype ="string", Caption = "Share URL" },
                    new EntityField {FieldName = "video_description", Fieldtype ="string", Caption = "Video Description" },
                    new EntityField {FieldName = "duration", Fieldtype ="integer", Caption = "Duration" },
                    new EntityField {FieldName = "height", Fieldtype ="integer", Caption = "Height" },
                    new EntityField {FieldName = "width", Fieldtype ="integer", Caption = "Width" },
                    new EntityField {FieldName = "title", Fieldtype ="string", Caption = "Title" },
                    new EntityField {FieldName = "embed_html", Fieldtype ="string", Caption = "Embed HTML" },
                    new EntityField {FieldName = "embed_link", Fieldtype ="string", Caption = "Embed Link" },
                    new EntityField {FieldName = "like_count", Fieldtype ="integer", Caption = "Like Count" },
                    new EntityField {FieldName = "comment_count", Fieldtype ="integer", Caption = "Comment Count" },
                    new EntityField {FieldName = "share_count", Fieldtype ="integer", Caption = "Share Count" },
                    new EntityField {FieldName = "view_count", Fieldtype ="integer", Caption = "View Count" },
                    new EntityField {FieldName = "play_count", Fieldtype ="integer", Caption = "Play Count" }
                }
            };
            Entities.Add(videosEntity);
            EntitiesNames.Add("videos");

            // Video List
            var videolistEntity = new EntityStructure
            {
                EntityName = "videolist",
                Caption = "Video List",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Video ID" },
                    new EntityField {FieldName = "create_time", Fieldtype ="datetime", Caption = "Create Time" },
                    new EntityField {FieldName = "cover_image_url", Fieldtype ="string", Caption = "Cover Image URL" },
                    new EntityField {FieldName = "share_url", Fieldtype ="string", Caption = "Share URL" },
                    new EntityField {FieldName = "video_description", Fieldtype ="string", Caption = "Video Description" },
                    new EntityField {FieldName = "region_code", Fieldtype ="string", Caption = "Region Code" },
                    new EntityField {FieldName = "title", Fieldtype ="string", Caption = "Title" },
                    new EntityField {FieldName = "like_count", Fieldtype ="integer", Caption = "Like Count" },
                    new EntityField {FieldName = "comment_count", Fieldtype ="integer", Caption = "Comment Count" },
                    new EntityField {FieldName = "share_count", Fieldtype ="integer", Caption = "Share Count" },
                    new EntityField {FieldName = "view_count", Fieldtype ="integer", Caption = "View Count" },
                    new EntityField {FieldName = "play_count", Fieldtype ="integer", Caption = "Play Count" }
                }
            };
            Entities.Add(videolistEntity);
            EntitiesNames.Add("videolist");

            // Comments
            var commentsEntity = new EntityStructure
            {
                EntityName = "comments",
                Caption = "Comments",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Comment ID" },
                    new EntityField {FieldName = "video_id", Fieldtype ="string", Caption = "Video ID" },
                    new EntityField {FieldName = "text", Fieldtype ="string", Caption = "Comment Text" },
                    new EntityField {FieldName = "create_time", Fieldtype ="datetime", Caption = "Create Time" },
                    new EntityField {FieldName = "parent_comment_id", Fieldtype ="string", Caption = "Parent Comment ID" },
                    new EntityField {FieldName = "reply_comment_total", Fieldtype ="integer", Caption = "Reply Count" },
                    new EntityField {FieldName = "like_count", Fieldtype ="integer", Caption = "Like Count" },
                    new EntityField {FieldName = "reply_to_reply_id", Fieldtype ="string", Caption = "Reply To Reply ID" }
                }
            };
            Entities.Add(commentsEntity);
            EntitiesNames.Add("comments");

            // Analytics
            var analyticsEntity = new EntityStructure
            {
                EntityName = "analytics",
                Caption = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "date", Fieldtype ="datetime", IsKey = true, Caption = "Date" },
                    new EntityField {FieldName = "video_id", Fieldtype ="string", IsKey = true, Caption = "Video ID" },
                    new EntityField {FieldName = "view_count", Fieldtype ="integer", Caption = "View Count" },
                    new EntityField {FieldName = "like_count", Fieldtype ="integer", Caption = "Like Count" },
                    new EntityField {FieldName = "comment_count", Fieldtype ="integer", Caption = "Comment Count" },
                    new EntityField {FieldName = "share_count", Fieldtype ="integer", Caption = "Share Count" },
                    new EntityField {FieldName = "play_count", Fieldtype ="integer", Caption = "Play Count" },
                    new EntityField {FieldName = "download_count", Fieldtype ="integer", Caption = "Download Count" },
                    new EntityField {FieldName = "reach", Fieldtype ="integer", Caption = "Reach" },
                    new EntityField {FieldName = "video_views_paid", Fieldtype ="integer", Caption = "Paid Views" },
                    new EntityField {FieldName = "video_views_organic", Fieldtype ="integer", Caption = "Organic Views" }
                }
            };
            Entities.Add(analyticsEntity);
            EntitiesNames.Add("analytics");

            // Followers
            var followersEntity = new EntityStructure
            {
                EntityName = "followers",
                Caption = "Followers",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "open_id", Fieldtype ="string", IsKey = true, Caption = "Open ID" },
                    new EntityField {FieldName = "union_id", Fieldtype ="string", Caption = "Union ID" },
                    new EntityField {FieldName = "avatar_url", Fieldtype ="string", Caption = "Avatar URL" },
                    new EntityField {FieldName = "display_name", Fieldtype ="string", Caption = "Display Name" },
                    new EntityField {FieldName = "bio_description", Fieldtype ="string", Caption = "Bio Description" },
                    new EntityField {FieldName = "follower_count", Fieldtype ="integer", Caption = "Follower Count" },
                    new EntityField {FieldName = "following_count", Fieldtype ="integer", Caption = "Following Count" },
                    new EntityField {FieldName = "likes_count", Fieldtype ="integer", Caption = "Likes Count" },
                    new EntityField {FieldName = "video_count", Fieldtype ="integer", Caption = "Video Count" }
                }
            };
            Entities.Add(followersEntity);
            EntitiesNames.Add("followers");
        }

        /// <summary>
        /// Resolve API base URL from connection properties (Url) or default.
        /// </summary>
        private string ApiBaseUrl => (Dataconnection?.ConnectionProp?.Url ?? _config.BaseUrl).TrimEnd('/');

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;

            foreach (var f in filters.Where(f => f != null && !string.IsNullOrWhiteSpace(f.FieldName)))
            {
                var value = !string.IsNullOrWhiteSpace(f.FilterValue) ? f.FilterValue : f.FilterValue1;
                if (value != null)
                    q[f.FieldName.Trim()] = value;
            }

            return q;
        }

        private static void RequireQuery(string entity, Dictionary<string, string> q, params string[] required)
        {
            foreach (var r in required ?? Array.Empty<string>())
            {
                if (!q.TryGetValue(r, out var v) || string.IsNullOrWhiteSpace(v))
                    throw new ArgumentException($"TikTok entity '{entity}' requires '{r}'.");
            }
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EntityName))
                    return Array.Empty<object>();

                var entity = EntityName.Trim().ToLowerInvariant();
                var q = FiltersToQuery(Filter);

                if (!q.ContainsKey("open_id") && !string.IsNullOrWhiteSpace(_config.OpenId))
                    q["open_id"] = _config.OpenId;

                string url = entity switch
                {
                    "user" => $"{ApiBaseUrl}/user/info/",
                    "videos" => $"{ApiBaseUrl}/video/query/",
                    "videolist" => $"{ApiBaseUrl}/video/list/",
                    "comments" => $"{ApiBaseUrl}/video/comment/list/",
                    "analytics" => $"{ApiBaseUrl}/video/data/",
                    "followers" => $"{ApiBaseUrl}/user/following/list/",
                    _ => throw new NotSupportedException($"Unsupported TikTok entity: {EntityName}")
                };

                if (_config.RateLimitDelayMs > 0)
                    await Task.Delay(_config.RateLimitDelayMs).ConfigureAwait(false);

                return entity switch
                {
                    "user" => await GetUsers(url, q).ConfigureAwait(false),
                    "videos" => await GetVideos(url, q, "videos", required: new[] { "video_ids" }).ConfigureAwait(false),
                    "videolist" => await GetVideos(url, q, "videolist").ConfigureAwait(false),
                    "comments" => await GetComments(url, q).ConfigureAwait(false),
                    "analytics" => await GetAnalytics(url, q).ConfigureAwait(false),
                    "followers" => await GetFollowers(url, q).ConfigureAwait(false),
                    _ => Array.Empty<object>()
                };
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return Array.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> GetUsers(string url, Dictionary<string, string> q)
        {
            RequireQuery("user", q, "open_id");
            var response = await GetAsync<TikTokResponse<TikTokUser>>(url, q).ConfigureAwait(false);
            var item = response?.Data?.UserList?.FirstOrDefault()
                       ?? response?.Data?.List?.FirstOrDefault();
            return item != null ? new object[] { item.Attach<TikTokUser>(this) } : Array.Empty<object>();
        }

        private async Task<IEnumerable<object>> GetVideos(string url, Dictionary<string, string> q, string entity, string[] required = null)
        {
            RequireQuery(entity, q, (required ?? Array.Empty<string>()).Append("open_id").ToArray());
            var response = await GetAsync<TikTokResponse<TikTokVideo>>(url, q).ConfigureAwait(false);
            var items = response?.Data?.VideoList ?? response?.Data?.List ?? new List<TikTokVideo>();
            return items.Select(v => v.Attach<TikTokVideo>(this)).Cast<object>();
        }

        private async Task<IEnumerable<object>> GetComments(string url, Dictionary<string, string> q)
        {
            RequireQuery("comments", q, "open_id", "video_id");
            var response = await GetAsync<TikTokResponse<TikTokComment>>(url, q).ConfigureAwait(false);
            var items = response?.Data?.List ?? new List<TikTokComment>();
            return items.Select(c => c.Attach<TikTokComment>(this)).Cast<object>();
        }

        private async Task<IEnumerable<object>> GetAnalytics(string url, Dictionary<string, string> q)
        {
            RequireQuery("analytics", q, "open_id", "video_id");
            var response = await GetAsync<TikTokResponse<TikTokAnalytics>>(url, q).ConfigureAwait(false);
            var items = response?.Data?.List ?? new List<TikTokAnalytics>();
            return items.Select(a => a.Attach<TikTokAnalytics>(this)).Cast<object>();
        }

        private async Task<IEnumerable<object>> GetFollowers(string url, Dictionary<string, string> q)
        {
            RequireQuery("followers", q, "open_id");
            var response = await GetAsync<TikTokResponse<TikTokFollower>>(url, q).ConfigureAwait(false);
            var items = response?.Data?.UserList ?? response?.Data?.List ?? new List<TikTokFollower>();
            return items.Select(f => f.Attach<TikTokFollower>(this)).Cast<object>();
        }

        [CommandAttribute(ObjectType ="TikTokUser", PointType = EnumPointType.Function, Name = "GetUserInfo", Caption = "Get TikTok User Info", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokUser>> GetUserInfo(string openId)
        {
            var url = $"{ApiBaseUrl}/user/info/?open_id={openId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokUser>>(json);
        }

        [CommandAttribute(ObjectType ="TikTokVideo", PointType = EnumPointType.Function, Name = "GetUserVideos", Caption = "Get TikTok User Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetUserVideos(string openId, long cursor = 0, int maxCount = 20)
        {
            var url = $"{ApiBaseUrl}/video/list/?open_id={openId}&cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType ="TikTokVideo", PointType = EnumPointType.Function, Name = "GetVideoDetails", Caption = "Get TikTok Video Details", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetVideoDetails(string videoId)
        {
            var url = $"{ApiBaseUrl}/video/query/?video_id={videoId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType ="TikTokVideo", PointType = EnumPointType.Function, Name = "GetTrendingVideos", Caption = "Get TikTok Trending Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetTrendingVideos(long cursor = 0, int maxCount = 20)
        {
            var url = $"{ApiBaseUrl}/video/trending/?cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(ObjectType ="TikTokMusic", PointType = EnumPointType.Function, Name = "GetMusicInfo", Caption = "Get TikTok Music Info", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokMusic>> GetMusicInfo(string musicId)
        {
            var url = $"{ApiBaseUrl}/music/info/?music_id={musicId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokMusic>>(json);
        }

        [CommandAttribute(ObjectType ="TikTokVideo", PointType = EnumPointType.Function, Name = "GetHashtagVideos", Caption = "Get TikTok Hashtag Videos", ClassName = "TikTokDataSource")]
        public async Task<TikTokResponse<TikTokVideo>> GetHashtagVideos(string hashtagName, long cursor = 0, int maxCount = 20)
        {
            var url = $"{ApiBaseUrl}/hashtag/video/list/?hashtag_name={Uri.EscapeDataString(hashtagName)}&cursor={cursor}&max_count={maxCount}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(json);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.TikTok, PointType = EnumPointType.Function, ObjectType ="TikTokVideo", Name = "CreateVideo", Caption = "Create TikTok Video", ClassType ="TikTokDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "tiktok.png", misc = "ReturnType: IEnumerable<TikTokVideo>")]
        public async Task<IEnumerable<TikTokVideo>> CreateVideoAsync(TikTokVideo video)
        {
            try
            {
                // Note: TikTok video posting requires multipart upload, this is a placeholder
                var result = await PostAsync($"{ApiBaseUrl}/video/publish/", video);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<TikTokResponse<TikTokVideo>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var createdVideo = response?.Data?.VideoList?.FirstOrDefault()
                                       ?? response?.Data?.List?.FirstOrDefault();
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
