using System;
using System.Collections.Generic;
using System.Data;
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
using TheTechIdea.Beep.Connectors.YouTube.Models;

namespace BeepDataSources.Connectors.SocialMedia.YouTube
{
    /// <summary>
    /// Configuration class for YouTube data source
    /// </summary>
    public class YouTubeConfig
    {
        /// <summary>
        /// YouTube Data API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 Access Token (optional, for authenticated requests)
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// YouTube Channel ID
        /// </summary>
        public string ChannelId { get; set; } = string.Empty;

        /// <summary>
        /// YouTube Username (alternative to Channel ID)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// API version for YouTube Data API (default: v3)
        /// </summary>
        public string ApiVersion { get; set; } = "v3";

        /// <summary>
        /// Base URL for YouTube Data API
        /// </summary>
        public string BaseUrl => $"https://www.googleapis.com/youtube/{ApiVersion}";

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
        /// Maximum results per request (default: 25, max: 50)
        /// </summary>
        public int MaxResults { get; set; } = 25;
    }

    /// <summary>
    /// YouTube data source implementation for Beep framework
    /// Supports YouTube Data API v3
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube)]
    public class YouTubeDataSource : WebAPIDataSource
    {
        #region Configuration Classes

        /// <summary>
        /// Configuration for YouTube connection
        /// </summary>
        public class YouTubeConfig
        {
            public string ApiKey { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string ChannelId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string ApiVersion { get; set; } = "v3";
            public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube";

            /// <summary>
            /// Request timeout in seconds
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
            /// Maximum results per request (default: 25, max: 50)
            /// </summary>
            public int MaxResults { get; set; } = 25;
        }

        /// <summary>
        /// YouTube entity metadata
        /// </summary>
        public class YouTubeEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        #region Private Fields

        private readonly YouTubeConfig _config;
        private readonly Dictionary<string, YouTubeEntity> _entityCache = new();

        #endregion

        #region Constructor

        public YouTubeDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new YouTubeConfig();

            // Ensure we're on WebAPI connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Initialize entities
            InitializeEntities();
        }

        #endregion

        #region Private Methods

        private void InitializeEntities()
        {
            // YouTube API v3 entities
            var entities = new[]
            {
                new YouTubeEntity
                {
                    EntityName = "channels",
                    DisplayName = "Channels",
                    ApiEndpoint = "channels",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "string",
                        ["title"] = "string",
                        ["description"] = "string",
                        ["publishedAt"] = "datetime",
                        ["subscriberCount"] = "long",
                        ["videoCount"] = "long",
                        ["viewCount"] = "long"
                    }
                },
                new YouTubeEntity
                {
                    EntityName = "videos",
                    DisplayName = "Videos",
                    ApiEndpoint = "videos",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "string",
                        ["title"] = "string",
                        ["description"] = "string",
                        ["publishedAt"] = "datetime",
                        ["duration"] = "string",
                        ["viewCount"] = "long",
                        ["likeCount"] = "long",
                        ["commentCount"] = "long"
                    }
                },
                new YouTubeEntity
                {
                    EntityName = "playlists",
                    DisplayName = "Playlists",
                    ApiEndpoint = "playlists",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "string",
                        ["title"] = "string",
                        ["description"] = "string",
                        ["publishedAt"] = "datetime",
                        ["itemCount"] = "long"
                    }
                },
                new YouTubeEntity
                {
                    EntityName = "playlistItems",
                    DisplayName = "Playlist Items",
                    ApiEndpoint = "playlistItems",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "string",
                        ["title"] = "string",
                        ["description"] = "string",
                        ["publishedAt"] = "datetime",
                        ["position"] = "long"
                    }
                },
                new YouTubeEntity
                {
                    EntityName = "search",
                    DisplayName = "Search Results",
                    ApiEndpoint = "search",
                    Fields = new Dictionary<string, string>
                    {
                        ["id"] = "string",
                        ["title"] = "string",
                        ["description"] = "string",
                        ["publishedAt"] = "datetime",
                        ["channelTitle"] = "string"
                    }
                }
            };

            foreach (var entity in entities)
            {
                _entityCache[entity.EntityName] = entity;
            }

            // Register entities with base class
            EntitiesNames = _entityCache.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure
                {
                    EntityName = n,
                    DatasourceEntityName = n,
                    Caption = _entityCache[n].DisplayName,
                    Fields = _entityCache[n].Fields.Select(f => new EntityField
                    {
                        fieldname = f.Key,
                        fieldtype = f.Value
                    }).ToList()
                })
                .ToList();
        }

        #endregion

        #region IDataSource Implementation

        public override async Task<bool> ConnectAsync()
        {
            try
            {
                Logger.WriteLog($"Connecting to YouTube API: {_config.BaseUrl}");

                // Get API key from connection properties
                if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
                {
                    _config.ApiKey = webApiProps.ApiKey ?? string.Empty;
                    _config.AccessToken = webApiProps.AccessToken ?? string.Empty;
                }

                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "API key is required for YouTube connection";
                    return false;
                }

                // Test connection by getting channel info if ChannelId is provided
                if (!string.IsNullOrEmpty(_config.ChannelId))
                {
                    var testUrl = $"{_config.BaseUrl}/{_config.ApiVersion}/channels?part=snippet&id={_config.ChannelId}&key={_config.ApiKey}";
                    var response = await GetAsync(testUrl);

                    if (response?.IsSuccessStatusCode == true)
                    {
                        ConnectionStatus = ConnectionState.Open;
                        Logger.WriteLog("Successfully connected to YouTube API");
                        return true;
                    }
                    else
                    {
                        var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No response";
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"YouTube API connection test failed: {response?.StatusCode} - {errorContent}";
                        return false;
                    }
                }

                // If no ChannelId, just test basic connectivity
                ConnectionStatus = ConnectionState.Open;
                Logger.WriteLog("Connected to YouTube API (no channel validation)");
                return true;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to connect to YouTube API: {ex.Message}";
                Logger.WriteLog($"YouTube connection error: {ex.Message}");
                return false;
            }
        }

        public override Task<bool> DisconnectAsync()
        {
            try
            {
                ConnectionStatus = ConnectionState.Closed;
                _entityCache.Clear();
                Logger.WriteLog("Disconnected from YouTube API");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Disconnect failed: {ex.Message}";
                return Task.FromResult(false);
            }
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (!_entityCache.TryGetValue(EntityName, out var entity))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown YouTube entity: {EntityName}";
                    return Array.Empty<object>();
                }

                var q = FiltersToQuery(Filter);
                var endpoint = $"{_config.BaseUrl}/{_config.ApiVersion}/{entity.ApiEndpoint}";

                // Add API key to query parameters
                if (!string.IsNullOrEmpty(_config.ApiKey))
                {
                    q["key"] = _config.ApiKey;
                }

                var response = await GetAsync(endpoint, q);
                if (response?.IsSuccessStatusCode != true)
                {
                    var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No response";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Failed to get {EntityName}: {response?.StatusCode} - {errorContent}";
                    return Array.Empty<object>();
                }

                return ExtractArray(response, "items");
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get entity data: {ex.Message}";
                return Array.Empty<object>();
            }
        }

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
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

        #endregion

        /// <summary>
        /// Parse connection string into YouTubeConfig
        /// </summary>
        private YouTubeConfig ParseConnectionString(string connectionString)
        {
            var config = new YouTubeConfig();
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
                        case "apikey":
                            config.ApiKey = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "channelid":
                            config.ChannelId = value;
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
                                config.MaxResults = Math.Min(maxResults, 50);
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for YouTube entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Channels
            metadata["channels"] = new EntityMetadata
            {
                EntityName = "channels",
                DisplayName = "Channels",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Channel ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Channel Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "publishedAt", Type = "datetime", DisplayName = "Published At" },
                    new EntityField { Name = "subscriberCount", Type = "long", DisplayName = "Subscriber Count" },
                    new EntityField { Name = "videoCount", Type = "long", DisplayName = "Video Count" },
                    new EntityField { Name = "viewCount", Type = "long", DisplayName = "View Count" },
                    new EntityField { Name = "customUrl", Type = "string", DisplayName = "Custom URL" },
                    new EntityField { Name = "thumbnailUrl", Type = "string", DisplayName = "Thumbnail URL" }
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
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "publishedAt", Type = "datetime", DisplayName = "Published At" },
                    new EntityField { Name = "channelId", Type = "string", DisplayName = "Channel ID" },
                    new EntityField { Name = "channelTitle", Type = "string", DisplayName = "Channel Title" },
                    new EntityField { Name = "duration", Type = "string", DisplayName = "Duration" },
                    new EntityField { Name = "viewCount", Type = "long", DisplayName = "View Count" },
                    new EntityField { Name = "likeCount", Type = "long", DisplayName = "Like Count" },
                    new EntityField { Name = "commentCount", Type = "long", DisplayName = "Comment Count" },
                    new EntityField { Name = "thumbnailUrl", Type = "string", DisplayName = "Thumbnail URL" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" }
                }
            };

            // Playlists
            metadata["playlists"] = new EntityMetadata
            {
                EntityName = "playlists",
                DisplayName = "Playlists",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Playlist ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "publishedAt", Type = "datetime", DisplayName = "Published At" },
                    new EntityField { Name = "channelId", Type = "string", DisplayName = "Channel ID" },
                    new EntityField { Name = "channelTitle", Type = "string", DisplayName = "Channel Title" },
                    new EntityField { Name = "itemCount", Type = "integer", DisplayName = "Item Count" },
                    new EntityField { Name = "thumbnailUrl", Type = "string", DisplayName = "Thumbnail URL" }
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
                    new EntityField { Name = "videoId", Type = "string", DisplayName = "Video ID" },
                    new EntityField { Name = "textDisplay", Type = "string", DisplayName = "Comment Text" },
                    new EntityField { Name = "textOriginal", Type = "string", DisplayName = "Original Text" },
                    new EntityField { Name = "authorDisplayName", Type = "string", DisplayName = "Author Name" },
                    new EntityField { Name = "authorChannelId", Type = "string", DisplayName = "Author Channel ID" },
                    new EntityField { Name = "publishedAt", Type = "datetime", DisplayName = "Published At" },
                    new EntityField { Name = "updatedAt", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "likeCount", Type = "long", DisplayName = "Like Count" },
                    new EntityField { Name = "totalReplyCount", Type = "integer", DisplayName = "Reply Count" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "channelId", Type = "string", IsPrimaryKey = true, DisplayName = "Channel ID" },
                    new EntityField { Name = "videoId", Type = "string", IsPrimaryKey = true, DisplayName = "Video ID" },
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "views", Type = "long", DisplayName = "Views" },
                    new EntityField { Name = "estimatedMinutesWatched", Type = "long", DisplayName = "Minutes Watched" },
                    new EntityField { Name = "averageViewDuration", Type = "long", DisplayName = "Avg View Duration" },
                    new EntityField { Name = "subscribersGained", Type = "long", DisplayName = "Subscribers Gained" },
                    new EntityField { Name = "subscribersLost", Type = "long", DisplayName = "Subscribers Lost" },
                    new EntityField { Name = "likes", Type = "long", DisplayName = "Likes" },
                    new EntityField { Name = "dislikes", Type = "long", DisplayName = "Dislikes" },
                    new EntityField { Name = "comments", Type = "long", DisplayName = "Comments" },
                    new EntityField { Name = "shares", Type = "long", DisplayName = "Shares" }
                }
            };

            // Search
            metadata["search"] = new EntityMetadata
            {
                EntityName = "search",
                DisplayName = "Search Results",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "videoId", Type = "string", IsPrimaryKey = true, DisplayName = "Video ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "channelId", Type = "string", DisplayName = "Channel ID" },
                    new EntityField { Name = "channelTitle", Type = "string", DisplayName = "Channel Title" },
                    new EntityField { Name = "publishedAt", Type = "datetime", DisplayName = "Published At" },
                    new EntityField { Name = "thumbnailUrl", Type = "string", DisplayName = "Thumbnail URL" },
                    new EntityField { Name = "viewCount", Type = "long", DisplayName = "View Count" },
                    new EntityField { Name = "duration", Type = "string", DisplayName = "Duration" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to YouTube API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("API key is required for YouTube connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Test connection by getting channel info
                var testUrl = $"{_config.BaseUrl}/channels?part=snippet&id={_config.ChannelId}&key={_config.ApiKey}";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"YouTube API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to YouTube API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from YouTube API
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
        /// Get data from YouTube API
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
                var part = parameters.ContainsKey("part") ? parameters["part"].ToString() : GetDefaultPart(entityName);
                var maxResults = parameters.ContainsKey("maxResults") ? parameters["maxResults"].ToString() : _config.MaxResults.ToString();

                switch (entityName.ToLower())
                {
                    case "channels":
                        var channelId = parameters.ContainsKey("channelId") ? parameters["channelId"].ToString() : _config.ChannelId;
                        url = $"{_config.BaseUrl}/channels?part={part}&id={channelId}&key={_config.ApiKey}";
                        break;

                    case "videos":
                        var videoId = parameters.ContainsKey("videoId") ? parameters["videoId"].ToString() : "";
                        url = $"{_config.BaseUrl}/videos?part={part}&id={videoId}&key={_config.ApiKey}";
                        break;

                    case "channelvideos":
                        var channelVideosId = parameters.ContainsKey("channelId") ? parameters["channelId"].ToString() : _config.ChannelId;
                        var order = parameters.ContainsKey("order") ? parameters["order"].ToString() : "date";
                        url = $"{_config.BaseUrl}/search?part=snippet&channelId={channelVideosId}&order={order}&type=video&maxResults={maxResults}&key={_config.ApiKey}";
                        break;

                    case "playlists":
                        var playlistId = parameters.ContainsKey("playlistId") ? parameters["playlistId"].ToString() : "";
                        url = $"{_config.BaseUrl}/playlists?part={part}&id={playlistId}&key={_config.ApiKey}";
                        break;

                    case "playlistitems":
                        var playlistItemsId = parameters.ContainsKey("playlistId") ? parameters["playlistId"].ToString() : "";
                        url = $"{_config.BaseUrl}/playlistItems?part={part}&playlistId={playlistItemsId}&maxResults={maxResults}&key={_config.ApiKey}";
                        break;

                    case "comments":
                        var videoCommentsId = parameters.ContainsKey("videoId") ? parameters["videoId"].ToString() : "";
                        url = $"{_config.BaseUrl}/commentThreads?part={part}&videoId={videoCommentsId}&maxResults={maxResults}&key={_config.ApiKey}";
                        break;

                    case "search":
                        var query = parameters.ContainsKey("q") ? parameters["q"].ToString() : "";
                        var searchType = parameters.ContainsKey("type") ? parameters["type"].ToString() : "video";
                        url = $"{_config.BaseUrl}/search?part=snippet&q={Uri.EscapeDataString(query)}&type={searchType}&maxResults={maxResults}&key={_config.ApiKey}";
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
                    throw new Exception($"YouTube API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get default part parameter for entity
        /// </summary>
        private string GetDefaultPart(string entityName)
        {
            return entityName.ToLower() switch
            {
                "channels" => "snippet,statistics",
                "videos" => "snippet,statistics",
                "playlists" => "snippet",
                "playlistitems" => "snippet",
                "comments" => "snippet",
                "search" => "snippet",
                _ => "snippet"
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
                if (root.TryGetProperty("items", out var itemsProp))
                {
                    dataElement = itemsProp;
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

        // ---------------- Specific YouTube Methods ----------------

        /// <summary>
        /// Gets videos from a channel
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubeVideo", PointType = EnumPointType.Function, Name = "GetVideos", Caption = "Get YouTube Videos", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeVideo>")]
        public async Task<IEnumerable<YouTubeVideo>> GetVideos(string channelId, int maxResults = 25, string order = "date")
        {
            string endpoint = $"search?part=snippet&channelId={channelId}&order={order}&type=video&maxResults={maxResults}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubeVideo>>(json);
            return data?.Items ?? new List<YouTubeVideo>();
        }

        /// <summary>
        /// Gets channel information
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubeChannel", PointType = EnumPointType.Function, Name = "GetChannel", Caption = "Get YouTube Channel", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeChannel>")]
        public async Task<IEnumerable<YouTubeChannel>> GetChannel(string channelId)
        {
            string endpoint = $"channels?part=snippet,statistics,status,brandingSettings,contentDetails&id={channelId}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubeChannel>>(json);
            return data?.Items ?? new List<YouTubeChannel>();
        }

        /// <summary>
        /// Gets playlists from a channel
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubePlaylist", PointType = EnumPointType.Function, Name = "GetPlaylists", Caption = "Get YouTube Playlists", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> GetPlaylists(string channelId, int maxResults = 25)
        {
            string endpoint = $"playlists?part=snippet,status,contentDetails&channelId={channelId}&maxResults={maxResults}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubePlaylist>>(json);
            return data?.Items ?? new List<YouTubePlaylist>();
        }

        /// <summary>
        /// Gets comments for a video
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubeCommentThread", PointType = EnumPointType.Function, Name = "GetComments", Caption = "Get YouTube Comments", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeCommentThread>")]
        public async Task<IEnumerable<YouTubeCommentThread>> GetComments(string videoId, int maxResults = 25)
        {
            string endpoint = $"commentThreads?part=snippet,replies&videoId={videoId}&maxResults={maxResults}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubeCommentThread>>(json);
            return data?.Items ?? new List<YouTubeCommentThread>();
        }

        /// <summary>
        /// Searches for videos
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubeSearchResult", PointType = EnumPointType.Function, Name = "SearchVideos", Caption = "Search YouTube Videos", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeSearchResult>")]
        public async Task<IEnumerable<YouTubeSearchResult>> SearchVideos(string query, int maxResults = 25, string type = "video")
        {
            string endpoint = $"search?part=snippet&q={Uri.EscapeDataString(query)}&type={type}&maxResults={maxResults}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubeSearchResult>>(json);
            return data?.Items ?? new List<YouTubeSearchResult>();
        }

        /// <summary>
        /// Gets video details
        /// </summary>
        [CommandAttribute(ObjectType = "YouTubeVideo", PointType = EnumPointType.Function, Name = "GetVideoDetails", Caption = "Get YouTube Video Details", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeVideo>")]
        public async Task<IEnumerable<YouTubeVideo>> GetVideoDetails(string videoId)
        {
            string endpoint = $"videos?part=snippet,statistics,status,contentDetails&id={videoId}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<YouTubeResponse<YouTubeVideo>>(json);
            return data?.Items ?? new List<YouTubeVideo>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube, PointType = EnumPointType.Function, ObjectType = "YouTubePlaylist", Name = "CreatePlaylist", Caption = "Create YouTube Playlist", ClassType = "YouTubeDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "youtube.png", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> CreatePlaylistAsync(YouTubePlaylist playlist)
        {
            try
            {
                var result = await PostAsync("playlists?part=snippet,status", playlist);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdPlaylist = JsonSerializer.Deserialize<YouTubePlaylist>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<YouTubePlaylist> { createdPlaylist }.Select(p => p.Attach<YouTubePlaylist>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating playlist: {ex.Message}");
            }
            return new List<YouTubePlaylist>();
        }

        // PUT methods for updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube, PointType = EnumPointType.Function, ObjectType = "YouTubePlaylist", Name = "UpdatePlaylist", Caption = "Update YouTube Playlist", ClassType = "YouTubeDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "youtube.png", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> UpdatePlaylistAsync(YouTubePlaylist playlist)
        {
            try
            {
                var result = await PutAsync($"playlists?part=snippet,status&id={playlist.Id}", playlist);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedPlaylist = JsonSerializer.Deserialize<YouTubePlaylist>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<YouTubePlaylist> { updatedPlaylist }.Select(p => p.Attach<YouTubePlaylist>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating playlist: {ex.Message}");
            }
            return new List<YouTubePlaylist>();
        }
    }
}
