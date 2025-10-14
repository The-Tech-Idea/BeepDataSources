using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Twitter.Models;

namespace TheTechIdea.Beep.Connectors.Twitter
{
    /// <summary>
    /// Twitter data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Twitter)]
    public class TwitterDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Twitter API v2
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tweets
            ["tweets.search"] = "tweets/search/recent",
            ["tweets.by_id"] = "tweets",
            // Users
            ["users.by_username"] = "users/by",
            ["users.by_id"] = "users",
            ["users.tweets"] = "users/{id}/tweets",
            ["users.followers"] = "users/{id}/followers",
            ["users.following"] = "users/{id}/following",
            // Lists
            ["lists.by_user"] = "users/{id}/owned_lists",
            ["lists.tweets"] = "lists/{id}/tweets",
            // Spaces
            ["spaces.search"] = "spaces/search"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tweets.search"] = new[] { "query" },
            ["tweets.by_id"] = new[] { "ids" },
            ["users.by_username"] = new[] { "usernames" },
            ["users.by_id"] = new[] { "ids" },
            ["users.tweets"] = new[] { "id" },
            ["users.followers"] = new[] { "id" },
            ["users.following"] = new[] { "id" },
            ["lists.by_user"] = new[] { "id" },
            ["lists.tweets"] = new[] { "id" },
            ["spaces.search"] = new[] { "query" }
        };

        public TwitterDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Twitter entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "data");
        }

        // Paged (Twitter uses cursor-based via meta.next_token / pagination_token)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Twitter entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));
            q["max_results"] = Math.Max(10, Math.Min(pageSize, 100)).ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);
            string? cursor = q.TryGetValue("pagination_token", out var tok) ? tok : null;

            for (int i = 1; i < Math.Max(1, pageNumber); i++)
            {
                var step = CallTwitter(resolvedEndpoint, MergeCursor(q, cursor)).ConfigureAwait(false).GetAwaiter().GetResult();
                cursor = GetNextToken(step);
                if (string.IsNullOrEmpty(cursor)) break;
            }

            var finalResp = CallTwitter(resolvedEndpoint, MergeCursor(q, cursor)).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, "data");
            var nextTok = GetNextToken(finalResp);

            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = nextTok == null ? pageNumber : pageNumber + 1, // estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = !string.IsNullOrEmpty(nextTok)
            };
        }

        // ---------------------------- Twitter-Specific Functions with Command Attributes ----------------------------

        /// <summary>
        /// Search for recent tweets based on a query
        /// </summary>
        [CommandAttribute(
            Name = "GetTweets",
            Caption = "Search Tweets",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "twitter.png",
            misc = "ReturnType: IEnumerable<TwitterTweet>"
        )]
        public async Task<IEnumerable<TwitterTweet>> GetTweets(string query, int maxResults = 10, string? nextToken = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            if (!string.IsNullOrEmpty(nextToken))
            {
                filters.Add(new AppFilter { FieldName = "pagination_token", FilterValue = nextToken, Operator = "=" });
            }

            var result = await GetEntityAsync("tweets.search", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterTweet>(JsonSerializer.Serialize(item))).Where(tweet => tweet != null).Cast<TwitterTweet>();
        }

        /// <summary>
        /// Get tweets from a specific user's timeline
        /// </summary>
        [CommandAttribute(
            Name = "GetUserTimeline",
            Caption = "Get User Timeline",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "timeline.png",
            misc = "ReturnType: IEnumerable<TwitterTweet>"
        )]
        public async Task<IEnumerable<TwitterTweet>> GetUserTimeline(string userId, int maxResults = 10, string? nextToken = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "id", FilterValue = userId, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            if (!string.IsNullOrEmpty(nextToken))
            {
                filters.Add(new AppFilter { FieldName = "pagination_token", FilterValue = nextToken, Operator = "=" });
            }

            var result = await GetEntityAsync("users.tweets", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterTweet>(JsonSerializer.Serialize(item))).Where(tweet => tweet != null).Cast<TwitterTweet>();
        }

        /// <summary>
        /// Get user information by username
        /// </summary>
        [CommandAttribute(
            Name = "GetUserByUsername",
            Caption = "Get User by Username",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterUser",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "user.png",
            misc = "ReturnType: IEnumerable<TwitterUser>"
        )]
        public async Task<IEnumerable<TwitterUser>> GetUserByUsername(string username)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "usernames", FilterValue = username, Operator = "=" }
            };

            var result = await GetEntityAsync("users.by_username", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterUser>(JsonSerializer.Serialize(item))).Where(user => user != null).Cast<TwitterUser>();
        }

        /// <summary>
        /// Get user information by user ID
        /// </summary>
        [CommandAttribute(
            Name = "GetUserById",
            Caption = "Get User by ID",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterUser",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "user_id.png",
            misc = "ReturnType: IEnumerable<TwitterUser>"
        )]
        public async Task<IEnumerable<TwitterUser>> GetUserById(string userId)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "ids", FilterValue = userId, Operator = "=" }
            };

            var result = await GetEntityAsync("users.by_id", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterUser>(JsonSerializer.Serialize(item))).Where(user => user != null).Cast<TwitterUser>();
        }

        /// <summary>
        /// Get followers of a specific user
        /// </summary>
        [CommandAttribute(
            Name = "GetFollowers",
            Caption = "Get Followers",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterUser",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "followers.png",
            misc = "ReturnType: IEnumerable<TwitterUser>"
        )]
        public async Task<IEnumerable<TwitterUser>> GetFollowers(string userId, int maxResults = 10, string? nextToken = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "id", FilterValue = userId, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            if (!string.IsNullOrEmpty(nextToken))
            {
                filters.Add(new AppFilter { FieldName = "pagination_token", FilterValue = nextToken, Operator = "=" });
            }

            var result = await GetEntityAsync("users.followers", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterUser>(JsonSerializer.Serialize(item))).Where(user => user != null).Cast<TwitterUser>();
        }

        /// <summary>
        /// Get users that a specific user is following
        /// </summary>
        [CommandAttribute(
            Name = "GetFollowing",
            Caption = "Get Following",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterUser",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "following.png",
            misc = "ReturnType: IEnumerable<TwitterUser>"
        )]
        public async Task<IEnumerable<TwitterUser>> GetFollowing(string userId, int maxResults = 10, string? nextToken = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "id", FilterValue = userId, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            if (!string.IsNullOrEmpty(nextToken))
            {
                filters.Add(new AppFilter { FieldName = "pagination_token", FilterValue = nextToken, Operator = "=" });
            }

            var result = await GetEntityAsync("users.following", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterUser>(JsonSerializer.Serialize(item))).Where(user => user != null).Cast<TwitterUser>();
        }

        /// <summary>
        /// Get tweets from a specific list
        /// </summary>
        [CommandAttribute(
            Name = "GetListTweets",
            Caption = "Get List Tweets",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "list.png",
            misc = "ReturnType: IEnumerable<TwitterTweet>"
        )]
        public async Task<IEnumerable<TwitterTweet>> GetListTweets(string listId, int maxResults = 10, string? nextToken = null)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "id", FilterValue = listId, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            if (!string.IsNullOrEmpty(nextToken))
            {
                filters.Add(new AppFilter { FieldName = "pagination_token", FilterValue = nextToken, Operator = "=" });
            }

            var result = await GetEntityAsync("lists.tweets", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterTweet>(JsonSerializer.Serialize(item))).Where(tweet => tweet != null).Cast<TwitterTweet>();
        }

        /// <summary>
        /// Search for Twitter Spaces
        /// </summary>
        [CommandAttribute(
            Name = "SearchSpaces",
            Caption = "Search Spaces",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterSpace",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "spaces.png",
            misc = "ReturnType: IEnumerable<TwitterSpace>"
        )]
        public async Task<IEnumerable<TwitterSpace>> SearchSpaces(string query, string state = "live", int maxResults = 10)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" },
                new AppFilter { FieldName = "state", FilterValue = state, Operator = "=" },
                new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
            };

            var result = await GetEntityAsync("spaces.search", filters);
            return result.Select(item => JsonSerializer.Deserialize<TwitterSpace>(JsonSerializer.Serialize(item))).Where(space => space != null).Cast<TwitterSpace>();
        }

        // ---------------------------- helpers ----------------------------

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Twitter entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallTwitter(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        private static Dictionary<string, string> MergeCursor(Dictionary<string, string> q, string? cursor)
        {
            var m = new Dictionary<string, string>(q, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(cursor)) m["pagination_token"] = cursor;
            return m;
        }

        private static string? GetNextToken(HttpResponseMessage resp)
        {
            if (resp == null) return null;
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("meta", out var meta) &&
                    meta.ValueKind == JsonValueKind.Object &&
                    meta.TryGetProperty("next_token", out var tok) &&
                    tok.ValueKind == JsonValueKind.String)
                {
                    var s = tok.GetString();
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        // Extracts "data" (array or object) into a List<object> (Dictionary<string,object> per item).
        // If root is null, wraps whole payload as a single object.
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list; // no "data" -> empty
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (node.ValueKind == JsonValueKind.Array)
            {
                list.Capacity = node.GetArrayLength();
                foreach (var el in node.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(
            Name = "CreateTweet",
            Caption = "Create Tweet",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "tweet.png",
            misc = "ReturnType: TwitterTweet"
        )]
        public async Task<TwitterTweet> CreateTweet(string text, string replyToTweetId = null, bool isReply = false)
        {
            var tweetData = new Dictionary<string, object> { ["text"] = text };
            if (isReply && !string.IsNullOrEmpty(replyToTweetId))
            {
                tweetData["reply"] = new Dictionary<string, object> { ["in_reply_to_tweet_id"] = replyToTweetId };
            }

            var response = await PostAsync<TwitterTweet>("tweets", tweetData);
            return response;
        }

        [CommandAttribute(
            Name = "DeleteTweet",
            Caption = "Delete Tweet",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "delete.png",
            misc = "ReturnType: bool"
        )]
        public async Task<bool> DeleteTweet(string tweetId)
        {
            try
            {
                var response = await DeleteAsync($"tweets/{tweetId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        [CommandAttribute(
            Name = "LikeTweet",
            Caption = "Like Tweet",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "like.png",
            misc = "ReturnType: bool"
        )]
        public async Task<bool> LikeTweet(string tweetId, string userId)
        {
            try
            {
                var likeData = new { tweet_id = tweetId };
                var response = await PostAsync($"users/{userId}/likes", likeData);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        [CommandAttribute(
            Name = "Retweet",
            Caption = "Retweet",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "retweet.png",
            misc = "ReturnType: bool"
        )]
        public async Task<bool> Retweet(string tweetId, string userId)
        {
            try
            {
                var retweetData = new { tweet_id = tweetId };
                var response = await PostAsync($"users/{userId}/retweets", retweetData);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // PUT methods for updating entities
        [CommandAttribute(
            Name = "UpdateTweet",
            Caption = "Update Tweet",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twitter,
            PointType = EnumPointType.Function,
            ObjectType = "TwitterTweet",
            ClassType = "TwitterDataSource",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "update.png",
            misc = "ReturnType: TwitterTweet"
        )]
        public async Task<TwitterTweet> UpdateTweet(string tweetId, TwitterTweet tweet)
        {
            var response = await PutAsync($"tweets/{tweetId}", tweet);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TwitterTweet>(json);
        }
    }
}
