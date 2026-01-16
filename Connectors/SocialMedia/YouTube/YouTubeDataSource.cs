using System;
using System.Collections.Generic;
using System.Data;
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
using TheTechIdea.Beep.Connectors.YouTube.Models;

namespace BeepDataSources.Connectors.SocialMedia.YouTube
{
    /// <summary>
    /// YouTube data source implementation for Beep framework.
    /// Supports YouTube Data API v3.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube)]
    public class YouTubeDataSource : WebAPIDataSource
    {
        #region Configuration Classes

        public sealed class YouTubeConfig
        {
            public string ApiKey { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string ChannelId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string ApiVersion { get; set; } = "v3";
            public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube";
            public int TimeoutSeconds { get; set; } = 30;
            public int MaxRetries { get; set; } = 3;
            public int RateLimitDelayMs { get; set; } = 1000;
            public int MaxResults { get; set; } = 25;
        }

        public sealed class YouTubeEntity
        {
            public string EntityName { get; set; } = string.Empty;
            public string Caption { get; set; } = string.Empty;
            public string ApiEndpoint { get; set; } = string.Empty;
            public Dictionary<string, string> Fields { get; set; } = new();
        }

        #endregion

        private readonly YouTubeConfig _config;
        private readonly Dictionary<string, YouTubeEntity> _entityCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public YouTubeDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new YouTubeConfig();

            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            InitializeEntities();
        }

        #region Private Helpers

        private void InitializeEntities()
        {
            var entities = new[]
            {
                new YouTubeEntity
                {
                    EntityName = "channels",
                    Caption = "Channels",
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
                    Caption = "Videos",
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
                    Caption = "Playlists",
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
                    Caption = "Playlist Items",
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
                    Caption = "Search Results",
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

            _entityCache.Clear();
            foreach (var entity in entities)
                _entityCache[entity.EntityName] = entity;

            EntitiesNames = _entityCache.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure
                {
                    EntityName = n,
                    DatasourceEntityName = n,
                    Caption = _entityCache[n].Caption,
                    Fields = _entityCache[n].Fields.Select(f => new EntityField
                    {
                        FieldName = f.Key,
                        Fieldtype = f.Value
                    }).ToList()
                })
                .ToList();
        }

        private void HydrateConfigFromConnectionProperties()
        {
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties props) return;

            _config.ApiKey = props.ApiKey ?? _config.ApiKey;
            _config.AccessToken = props.AccessToken
                                 ?? props.BearerToken
                                 ?? props.OAuthAccessToken
                                 ?? _config.AccessToken;

            _config.ApiVersion = props.ApiVersion ?? _config.ApiVersion;
            _config.BaseUrl = props.Url ?? _config.BaseUrl;

            if (props.TimeoutMs > 0)
                _config.TimeoutSeconds = Math.Max(1, props.TimeoutMs / 1000);
            if (props.MaxRetries > 0)
                _config.MaxRetries = props.MaxRetries;
            if (props.RetryDelayMs > 0)
                _config.RateLimitDelayMs = props.RetryDelayMs;

            if (props.ParameterList != null)
            {
                if (props.ParameterList.TryGetValue("ChannelId", out var channelId) && !string.IsNullOrWhiteSpace(channelId))
                    _config.ChannelId = channelId;
                if (props.ParameterList.TryGetValue("Username", out var username) && !string.IsNullOrWhiteSpace(username))
                    _config.Username = username;
            }
        }

        private string ApiBaseUrl
        {
            get
            {
                HydrateConfigFromConnectionProperties();
                var baseUrl = (_config.BaseUrl ?? string.Empty).TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = "https://www.googleapis.com/youtube";

                if (baseUrl.EndsWith("/" + _config.ApiVersion, StringComparison.OrdinalIgnoreCase))
                    return baseUrl;

                return $"{baseUrl}/{_config.ApiVersion}".TrimEnd('/');
            }
        }

        private string BuildApiUrl(string resourcePath)
        {
            resourcePath ??= string.Empty;
            return $"{ApiBaseUrl}/{resourcePath.TrimStart('/')}".TrimEnd('/');
        }

        private Dictionary<string, string> BuildAuthHeaders()
        {
            HydrateConfigFromConnectionProperties();

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_config.AccessToken))
                headers["Authorization"] = $"Bearer {_config.AccessToken}";

            return headers;
        }

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter>? filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;

            foreach (var filter in filters)
            {
                if (filter == null) continue;
                if (string.IsNullOrWhiteSpace(filter.FieldName)) continue;
                if (filter.FilterValue == null) continue;
                q[filter.FieldName] = filter.FilterValue.ToString() ?? string.Empty;
            }

            return q;
        }

        private static string DefaultPartForEntity(string entityName)
        {
            return entityName.ToLowerInvariant() switch
            {
                "channels" => "snippet,statistics",
                "videos" => "snippet,contentDetails,statistics",
                "playlists" => "snippet,contentDetails,status",
                "playlistitems" => "snippet,contentDetails",
                "search" => "snippet",
                _ => "snippet"
            };
        }

        private static object? JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.TryGetDouble(out var d) ? d : element.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value), StringComparer.OrdinalIgnoreCase),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
                _ => element.GetRawText()
            };
        }

        private static async Task<IEnumerable<object>> ExtractArrayAsync(HttpResponseMessage response, string arrayPropertyName)
        {
            if (response?.Content == null) return Array.Empty<object>();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<object>();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return Array.Empty<object>();
            if (!doc.RootElement.TryGetProperty(arrayPropertyName, out var items)) return Array.Empty<object>();
            if (items.ValueKind != JsonValueKind.Array) return Array.Empty<object>();

            var result = new List<object>();
            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    result.Add(item.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value), StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    result.Add(JsonElementToObject(item) ?? item.GetRawText());
                }
            }

            return result;
        }

        private async Task<HttpResponseMessage?> GetYouTubeAsync(string resource, Dictionary<string, string> query)
        {
            HydrateConfigFromConnectionProperties();
            if (!string.IsNullOrWhiteSpace(_config.ApiKey))
                query["key"] = _config.ApiKey;

            if (_config.RateLimitDelayMs > 0)
                await Task.Delay(_config.RateLimitDelayMs).ConfigureAwait(false);

            var url = BuildApiUrl(resource);
            return await GetAsync(url, query, BuildAuthHeaders(), default).ConfigureAwait(false);
        }

        private async Task<T?> GetYouTubeJsonAsync<T>(string resource, Dictionary<string, string> query)
        {
            using var response = await GetYouTubeAsync(resource, query).ConfigureAwait(false);
            if (response == null) return default;

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"YouTube API request failed: {response.StatusCode} - {json}";
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        #endregion

        #region Convenience Connect/Disconnect

        public async Task<bool> ConnectAsync()
        {
            try
            {
                HydrateConfigFromConnectionProperties();
                Logger.WriteLog($"Connecting to YouTube API: {ApiBaseUrl}");

                if (string.IsNullOrWhiteSpace(_config.ApiKey) && string.IsNullOrWhiteSpace(_config.AccessToken))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "API key or access token is required for YouTube connection";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(_config.ChannelId))
                {
                    var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["part"] = "snippet",
                        ["id"] = _config.ChannelId
                    };

                    using var response = await GetYouTubeAsync("channels", q).ConfigureAwait(false);
                    if (response?.IsSuccessStatusCode == true)
                    {
                        ConnectionStatus = ConnectionState.Open;
                        ErrorObject.Flag = Errors.Ok;
                        ErrorObject.Message = "Successfully connected to YouTube API";
                        return true;
                    }

                    var errorContent = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : "No response";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"YouTube API connection test failed: {response?.StatusCode} - {errorContent}";
                    return false;
                }

                ConnectionStatus = ConnectionState.Open;
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Connected to YouTube API (no channel validation)";
                return true;
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to connect to YouTube API: {ex.Message}";
                return false;
            }
        }

        public Task<bool> DisconnectAsync()
        {
            ConnectionStatus = ConnectionState.Closed;
            return Task.FromResult(true);
        }

        #endregion

        #region IDataSource Overrides

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
                if (!q.ContainsKey("part"))
                    q["part"] = DefaultPartForEntity(EntityName);

                if (EntityName.Equals("channels", StringComparison.OrdinalIgnoreCase) &&
                    !q.ContainsKey("id") &&
                    !string.IsNullOrWhiteSpace(_config.ChannelId))
                {
                    q["id"] = _config.ChannelId;
                }

                using var response = await GetYouTubeAsync(entity.ApiEndpoint, q).ConfigureAwait(false);
                if (response?.IsSuccessStatusCode != true)
                {
                    var errorContent = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : "No response";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Failed to get {EntityName}: {response?.StatusCode} - {errorContent}";
                    return Array.Empty<object>();
                }

                return await ExtractArrayAsync(response, "items").ConfigureAwait(false);
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

        // ---------------- Specific YouTube Methods ----------------

        [CommandAttribute(ObjectType = "YouTubeVideo", PointType = EnumPointType.Function, Name = "GetVideos", Caption = "Get YouTube Videos", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeVideo>")]
        public async Task<IEnumerable<YouTubeVideo>> GetVideos(string channelId, int maxResults = 25, string order = "date")
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet",
                ["channelId"] = channelId,
                ["order"] = order,
                ["type"] = "video",
                ["maxResults"] = maxResults.ToString()
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubeVideo>>("search", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubeVideo>();
        }

        [CommandAttribute(ObjectType = "YouTubeChannel", PointType = EnumPointType.Function, Name = "GetChannel", Caption = "Get YouTube Channel", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeChannel>")]
        public async Task<IEnumerable<YouTubeChannel>> GetChannel(string channelId)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet,statistics,status,brandingSettings,contentDetails",
                ["id"] = channelId
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubeChannel>>("channels", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubeChannel>();
        }

        [CommandAttribute(ObjectType = "YouTubePlaylist", PointType = EnumPointType.Function, Name = "GetPlaylists", Caption = "Get YouTube Playlists", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> GetPlaylists(string channelId, int maxResults = 25)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet,status,contentDetails",
                ["channelId"] = channelId,
                ["maxResults"] = maxResults.ToString()
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubePlaylist>>("playlists", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubePlaylist>();
        }

        [CommandAttribute(ObjectType = "YouTubeCommentThread", PointType = EnumPointType.Function, Name = "GetComments", Caption = "Get YouTube Comments", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeCommentThread>")]
        public async Task<IEnumerable<YouTubeCommentThread>> GetComments(string videoId, int maxResults = 25)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet,replies",
                ["videoId"] = videoId,
                ["maxResults"] = maxResults.ToString()
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubeCommentThread>>("commentThreads", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubeCommentThread>();
        }

        [CommandAttribute(ObjectType = "YouTubeSearchResult", PointType = EnumPointType.Function, Name = "SearchVideos", Caption = "Search YouTube Videos", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeSearchResult>")]
        public async Task<IEnumerable<YouTubeSearchResult>> SearchVideos(string query, int maxResults = 25, string type = "video")
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet",
                ["q"] = query,
                ["type"] = type,
                ["maxResults"] = maxResults.ToString()
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubeSearchResult>>("search", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubeSearchResult>();
        }

        [CommandAttribute(ObjectType = "YouTubeVideo", PointType = EnumPointType.Function, Name = "GetVideoDetails", Caption = "Get YouTube Video Details", ClassName = "YouTubeDataSource", misc = "ReturnType: IEnumerable<YouTubeVideo>")]
        public async Task<IEnumerable<YouTubeVideo>> GetVideoDetails(string videoId)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["part"] = "snippet,statistics,status,contentDetails",
                ["id"] = videoId
            };

            var data = await GetYouTubeJsonAsync<YouTubeResponse<YouTubeVideo>>("videos", q).ConfigureAwait(false);
            return data?.Items ?? new List<YouTubeVideo>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube, PointType = EnumPointType.Function, ObjectType = "YouTubePlaylist", Name = "CreatePlaylist", Caption = "Create YouTube Playlist", ClassType = "YouTubeDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "youtube.png", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> CreatePlaylistAsync(YouTubePlaylist playlist)
        {
            try
            {
                HydrateConfigFromConnectionProperties();
                var url = $"{BuildApiUrl("playlists")}?part=snippet,status";
                if (!string.IsNullOrWhiteSpace(_config.ApiKey))
                    url += $"&key={Uri.EscapeDataString(_config.ApiKey)}";

                var result = await PostAsync(url, playlist).ConfigureAwait(false);
                if (!result.IsSuccessStatusCode) return new List<YouTubePlaylist>();

                var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                var created = JsonSerializer.Deserialize<YouTubePlaylist>(content, JsonOptions);
                return created != null
                    ? new List<YouTubePlaylist> { created }.Select(p => p.Attach<YouTubePlaylist>(this))
                    : new List<YouTubePlaylist>();
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating playlist: {ex.Message}");
                return new List<YouTubePlaylist>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.YouTube, PointType = EnumPointType.Function, ObjectType = "YouTubePlaylist", Name = "UpdatePlaylist", Caption = "Update YouTube Playlist", ClassType = "YouTubeDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "youtube.png", misc = "ReturnType: IEnumerable<YouTubePlaylist>")]
        public async Task<IEnumerable<YouTubePlaylist>> UpdatePlaylistAsync(YouTubePlaylist playlist)
        {
            try
            {
                HydrateConfigFromConnectionProperties();
                var url = $"{BuildApiUrl("playlists")}?part=snippet,status&id={Uri.EscapeDataString(playlist.Id ?? string.Empty)}";
                if (!string.IsNullOrWhiteSpace(_config.ApiKey))
                    url += $"&key={Uri.EscapeDataString(_config.ApiKey)}";

                var result = await PutAsync(url, playlist).ConfigureAwait(false);
                if (!result.IsSuccessStatusCode) return new List<YouTubePlaylist>();

                var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                var updated = JsonSerializer.Deserialize<YouTubePlaylist>(content, JsonOptions);
                return updated != null
                    ? new List<YouTubePlaylist> { updated }.Select(p => p.Attach<YouTubePlaylist>(this))
                    : new List<YouTubePlaylist>();
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating playlist: {ex.Message}");
                return new List<YouTubePlaylist>();
            }
        }
    }
}
