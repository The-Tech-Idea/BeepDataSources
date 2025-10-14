using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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

namespace TheTechIdea.Beep.BufferDataSource
{
    /// <summary>
    /// Buffer data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Buffer)]
    public class BufferDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (Buffer API v1) --------
        private static readonly List<string> KnownEntities = new()
        {
            // Posts
            "posts",              // GET /updates.json
            "posts.pending",      // GET /updates/pending.json
            "posts.sent",         // GET /updates/sent.json
            // Profiles
            "profiles",           // GET /profiles.json
            // Analytics
            "analytics",          // GET /analytics/{profile_id}.json
            // Campaigns
            "campaigns",          // GET /campaigns.json
            // Links
            "links"               // GET /links.json
        };

        // entity -> (endpoint template, root path, required filter keys)
        // endpoint supports {id} substitution taken from filters.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["posts"] = ("updates", "", new string[] { }),
                ["posts.pending"] = ("updates/pending", "", new string[] { }),
                ["posts.sent"] = ("updates/sent", "", new string[] { }),
                ["profiles"] = ("profiles", "", new string[] { }),
                ["analytics"] = ("analytics/{profile_id}", "", new[] { "profile_id" }),
                ["campaigns"] = ("campaigns", "", new string[] { }),
                ["links"] = ("links", "", new string[] { })
            };

        public BufferDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (URL/Auth configured outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'override' if base is virtual; otherwise this hides the base)
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
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Buffer entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Build the full URL
            var endpoint = m.endpoint;
            if (endpoint.Contains("{profile_id}"))
            {
                var profileId = q.FirstOrDefault(f => f.FieldName == "profile_id")?.FieldValue?.ToString();
                if (string.IsNullOrEmpty(profileId))
                    throw new InvalidOperationException("profile_id is required for analytics entity");
                endpoint = endpoint.Replace("{profile_id}", profileId);
            }

            var fullUrl = $"{BaseURL}/v1/{endpoint}.json{q}";

            // Make the request
            var response = await GetAsync(fullUrl);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Buffer API request failed: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity type
            var result = EntityName switch
            {
                "posts" or "posts.pending" or "posts.sent" => ParsePosts(json),
                "profiles" => ParseProfiles(json),
                "analytics" => ParseAnalytics(json),
                "campaigns" => ParseCampaigns(json),
                "links" => ParseLinks(json),
                _ => throw new InvalidOperationException($"Unsupported entity: {EntityName}")
            };

            return result;
        }

        // -------------------- Entity-specific parsers --------------------

        private IEnumerable<BufferPost> ParsePosts(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                // Buffer API returns updates array
                var response = JsonSerializer.Deserialize<BufferPostsResponse>(json, options);
                return response?.Updates ?? Array.Empty<BufferPost>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Buffer posts: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<BufferPost>();
            }
        }

        private IEnumerable<BufferProfile> ParseProfiles(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<BufferProfilesResponse>(json, options);
                return response?.Profiles ?? Array.Empty<BufferProfile>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Buffer profiles: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<BufferProfile>();
            }
        }

        private IEnumerable<BufferAnalytics> ParseAnalytics(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<BufferAnalyticsResponse>(json, options);
                return response != null ? new[] { response } : Array.Empty<BufferAnalytics>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Buffer analytics: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<BufferAnalytics>();
            }
        }

        private IEnumerable<BufferCampaign> ParseCampaigns(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<BufferCampaignsResponse>(json, options);
                return response?.Campaigns ?? Array.Empty<BufferCampaign>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Buffer campaigns: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<BufferCampaign>();
            }
        }

        private IEnumerable<BufferLink> ParseLinks(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<BufferLinksResponse>(json, options);
                return response?.Links ?? Array.Empty<BufferLink>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Buffer links: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<BufferLink>();
            }
        }

        // -------------------- Helper methods --------------------

        private string FiltersToQuery(List<AppFilter> filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var queryParts = new List<string>();
            foreach (var f in filters)
            {
                if (f.FieldName != null && f.FieldValue != null)
                {
                    var value = Uri.EscapeDataString(f.FieldValue.ToString() ?? "");
                    queryParts.Add($"{f.FieldName}={value}");
                }
            }

            return queryParts.Any() ? "?" + string.Join("&", queryParts) : string.Empty;
        }

        /// <summary>
        /// Gets all posts/updates
        /// </summary>
        [CommandAttribute(ObjectType = "BufferPost", PointType = EnumPointType.Function, Name = "GetPosts", Caption = "Get Buffer Posts", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferPost>")]
        public async Task<IEnumerable<BufferPost>> GetPosts()
        {
            string endpoint = "updates.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<BufferResponse<BufferPost>>(json);
            return responseObj?.Data ?? new List<BufferPost>();
        }

        /// <summary>
        /// Gets pending posts
        /// </summary>
        [CommandAttribute(ObjectType = "BufferPost", PointType = EnumPointType.Function, Name = "GetPendingPosts", Caption = "Get Buffer Pending Posts", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferPost>")]
        public async Task<IEnumerable<BufferPost>> GetPendingPosts()
        {
            string endpoint = "updates/pending.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<BufferResponse<BufferPost>>(json);
            return responseObj?.Data ?? new List<BufferPost>();
        }

        /// <summary>
        /// Gets sent posts
        /// </summary>
        [CommandAttribute(ObjectType = "BufferPost", PointType = EnumPointType.Function, Name = "GetSentPosts", Caption = "Get Buffer Sent Posts", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferPost>")]
        public async Task<IEnumerable<BufferPost>> GetSentPosts()
        {
            string endpoint = "updates/sent.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<BufferResponse<BufferPost>>(json);
            return responseObj?.Data ?? new List<BufferPost>();
        }

        /// <summary>
        /// Gets all social media profiles
        /// </summary>
        [CommandAttribute(ObjectType = "BufferProfile", PointType = EnumPointType.Function, Name = "GetProfiles", Caption = "Get Buffer Profiles", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferProfile>")]
        public async Task<IEnumerable<BufferProfile>> GetProfiles()
        {
            string endpoint = "profiles.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<BufferResponse<BufferProfile>>(json);
            return responseObj?.Data ?? new List<BufferProfile>();
        }

        /// <summary>
        /// Gets analytics for a specific profile
        /// </summary>
        [CommandAttribute(ObjectType = "BufferAnalytics", PointType = EnumPointType.Function, Name = "GetAnalytics", Caption = "Get Buffer Analytics", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferAnalytics>")]
        public async Task<IEnumerable<BufferAnalytics>> GetAnalytics(string profileId)
        {
            string endpoint = $"analytics/{profileId}.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var analytics = JsonSerializer.Deserialize<BufferAnalytics>(json);
            return analytics != null ? new List<BufferAnalytics> { analytics } : new List<BufferAnalytics>();
        }

        /// <summary>
        /// Gets all campaigns
        /// </summary>
        [CommandAttribute(ObjectType = "BufferCampaign", PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Buffer Campaigns", ClassName = "BufferDataSource", misc = "ReturnType: IEnumerable<BufferCampaign>")]
        public async Task<IEnumerable<BufferCampaign>> GetCampaigns()
        {
            string endpoint = "campaigns.json";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<BufferResponse<BufferCampaign>>(json);
            return responseObj?.Data ?? new List<BufferCampaign>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Buffer, PointType = EnumPointType.Function, ObjectType = "BufferPost", Name = "CreatePost", Caption = "Create Buffer Post", ClassType = "BufferDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "buffer.png", misc = "ReturnType: IEnumerable<BufferPost>")]
        public async Task<IEnumerable<BufferPost>> CreatePostAsync(BufferPost post)
        {
            try
            {
                var result = await PostAsync("updates/create.json", post);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdPost = JsonSerializer.Deserialize<BufferPost>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<BufferPost> { createdPost };
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating post: {ex.Message}");
            }
            return new List<BufferPost>();
        }

        private void RequireFilters(string entityName, string query, string[] required)
        {
            foreach (var req in required)
            {
                if (!query.Contains(req))
                    throw new InvalidOperationException($"Entity '{entityName}' requires filter '{req}'");
            }
        }

        // -------------------- Response classes for JSON parsing --------------------

        private class BufferPostsResponse
        {
            [JsonPropertyName("updates")]
            public List<BufferPost>? Updates { get; set; }
        }

        private class BufferProfilesResponse
        {
            [JsonPropertyName("profiles")]
            public List<BufferProfile>? Profiles { get; set; }
        }

        private class BufferCampaignsResponse
        {
            [JsonPropertyName("campaigns")]
            public List<BufferCampaign>? Campaigns { get; set; }
        }

        private class BufferLinksResponse
        {
            [JsonPropertyName("links")]
            public List<BufferLink>? Links { get; set; }
        }
    }
}
