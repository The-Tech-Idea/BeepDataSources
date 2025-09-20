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

namespace TheTechIdea.Beep.HootsuiteDataSource
{
    /// <summary>
    /// Hootsuite data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Hootsuite)]
    public class HootsuiteDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (Hootsuite API v1) --------
        private static readonly List<string> KnownEntities = new()
        {
            // Posts/Messages
            "posts",              // GET /posts
            "posts.scheduled",    // GET /posts?state=SCHEDULED
            "posts.published",    // GET /posts?state=PUBLISHED
            // Social Profiles
            "socialprofiles",     // GET /socialProfiles
            // Analytics
            "analytics",          // GET /analytics
            // Organizations
            "organizations",      // GET /organizations
            // Teams
            "teams"               // GET /teams
        };

        // entity -> (endpoint template, root path, required filter keys)
        // endpoint supports {id} substitution taken from filters.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["posts"] = ("posts", "data", new string[] { }),
                ["posts.scheduled"] = ("posts?state=SCHEDULED", "data", new string[] { }),
                ["posts.published"] = ("posts?state=PUBLISHED", "data", new string[] { }),
                ["socialprofiles"] = ("socialProfiles", "data", new string[] { }),
                ["analytics"] = ("analytics", "data", new string[] { }),
                ["organizations"] = ("organizations", "data", new string[] { }),
                ["teams"] = ("teams", "data", new string[] { })
            };

        public HootsuiteDataSource(
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
                throw new InvalidOperationException($"Unknown Hootsuite entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Build the full URL
            var endpoint = m.endpoint;
            var fullUrl = $"{BaseURL}/v1/{endpoint}{q}";

            // Make the request
            var response = await GetAsync(fullUrl);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Hootsuite API request failed: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity type
            var result = EntityName switch
            {
                "posts" or "posts.scheduled" or "posts.published" => ParsePosts(json),
                "socialprofiles" => ParseSocialProfiles(json),
                "analytics" => ParseAnalytics(json),
                "organizations" => ParseOrganizations(json),
                "teams" => ParseTeams(json),
                _ => throw new InvalidOperationException($"Unsupported entity: {EntityName}")
            };

            return result;
        }

        // -------------------- Entity-specific parsers --------------------

        private IEnumerable<HootsuitePost> ParsePosts(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<HootsuitePostsResponse>(json, options);
                return response?.Data ?? Array.Empty<HootsuitePost>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Hootsuite posts: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<HootsuitePost>();
            }
        }

        private IEnumerable<HootsuiteSocialProfile> ParseSocialProfiles(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<HootsuiteSocialProfilesResponse>(json, options);
                return response?.Data ?? Array.Empty<HootsuiteSocialProfile>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Hootsuite social profiles: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<HootsuiteSocialProfile>();
            }
        }

        private IEnumerable<HootsuiteAnalytics> ParseAnalytics(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<HootsuiteAnalyticsResponse>(json, options);
                return response?.Data ?? Array.Empty<HootsuiteAnalytics>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Hootsuite analytics: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<HootsuiteAnalytics>();
            }
        }

        private IEnumerable<HootsuiteOrganization> ParseOrganizations(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<HootsuiteOrganizationsResponse>(json, options);
                return response?.Data ?? Array.Empty<HootsuiteOrganization>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Hootsuite organizations: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<HootsuiteOrganization>();
            }
        }

        private IEnumerable<HootsuiteTeam> ParseTeams(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = JsonSerializer.Deserialize<HootsuiteTeamsResponse>(json, options);
                return response?.Data ?? Array.Empty<HootsuiteTeam>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Hootsuite teams: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<HootsuiteTeam>();
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

        private void RequireFilters(string entityName, string query, string[] required)
        {
            foreach (var req in required)
            {
                if (!query.Contains(req))
                    throw new InvalidOperationException($"Entity '{entityName}' requires filter '{req}'");
            }
        }

        // -------------------- Response classes for JSON parsing --------------------

        private class HootsuitePostsResponse
        {
            [JsonPropertyName("data")]
            public List<HootsuitePost>? Data { get; set; }
        }

        private class HootsuiteSocialProfilesResponse
        {
            [JsonPropertyName("data")]
            public List<HootsuiteSocialProfile>? Data { get; set; }
        }

        private class HootsuiteAnalyticsResponse
        {
            [JsonPropertyName("data")]
            public List<HootsuiteAnalytics>? Data { get; set; }
        }

        private class HootsuiteOrganizationsResponse
        {
            [JsonPropertyName("data")]
            public List<HootsuiteOrganization>? Data { get; set; }
        }

        private class HootsuiteTeamsResponse
        {
            [JsonPropertyName("data")]
            public List<HootsuiteTeam>? Data { get; set; }
        }
    }
}
