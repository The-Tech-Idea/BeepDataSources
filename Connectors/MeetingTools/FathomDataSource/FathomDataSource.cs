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
using TheTechIdea.Beep.Connectors.Fathom.Models;

namespace TheTechIdea.Beep.Connectors.Fathom
{
    /// <summary>
    /// Fathom data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Fathom)]
    public class FathomDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Fathom API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Videos
            ["videos"] = "videos",
            ["videos.get"] = "videos/{id}",
            // Analytics
            ["analytics"] = "videos/{video_id}/analytics",
            // Insights
            ["insights"] = "videos/{video_id}/insights",
            // Chapters
            ["chapters"] = "videos/{video_id}/chapters",
            // Transcripts
            ["transcripts"] = "videos/{video_id}/transcripts",
            // Summaries
            ["summaries"] = "videos/{video_id}/summaries",
            // Comments
            ["comments"] = "videos/{video_id}/comments",
            // Shares
            ["shares"] = "videos/{video_id}/shares",
            // Teams
            ["teams"] = "teams"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["videos.get"] = new[] { "id" },
            ["analytics"] = new[] { "video_id" },
            ["insights"] = new[] { "video_id" },
            ["chapters"] = new[] { "video_id" },
            ["transcripts"] = new[] { "video_id" },
            ["summaries"] = new[] { "video_id" },
            ["comments"] = new[] { "video_id" },
            ["shares"] = new[] { "video_id" }
        };

        public FathomDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
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

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Fathom entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.fathom.video/v1/{endpoint}";
            url = ReplacePlaceholders(url, q);

            // Add query parameters
            var queryParams = BuildQueryParameters(q);
            if (!string.IsNullOrEmpty(queryParams))
                url += "?" + queryParams;

            // Make the request
            var response = await GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            // Parse based on entity
            return EntityName switch
            {
                "videos" => ParseVideos(json),
                "videos.get" => ParseVideo(json),
                "analytics" => ParseAnalytics(json),
                "insights" => ParseInsights(json),
                "chapters" => ParseChapters(json),
                "transcripts" => ParseTranscripts(json),
                "summaries" => ParseSummaries(json),
                "comments" => ParseComments(json),
                "shares" => ParseShares(json),
                "teams" => ParseTeams(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseVideos(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomVideo>>>(json);
            return response?.Data ?? new List<FathomVideo>();
        }

        private IEnumerable<object> ParseVideo(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<FathomVideo>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<FathomVideo>();
        }

        private IEnumerable<object> ParseAnalytics(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<FathomAnalytics>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<FathomAnalytics>();
        }

        private IEnumerable<object> ParseInsights(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomInsight>>>(json);
            return response?.Data ?? new List<FathomInsight>();
        }

        private IEnumerable<object> ParseChapters(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomChapter>>>(json);
            return response?.Data ?? new List<FathomChapter>();
        }

        private IEnumerable<object> ParseTranscripts(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<FathomTranscript>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<FathomTranscript>();
        }

        private IEnumerable<object> ParseSummaries(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<FathomSummary>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<FathomSummary>();
        }

        private IEnumerable<object> ParseComments(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomComment>>>(json);
            return response?.Data ?? new List<FathomComment>();
        }

        private IEnumerable<object> ParseShares(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomShare>>>(json);
            return response?.Data ?? new List<FathomShare>();
        }

        private IEnumerable<object> ParseTeams(string json)
        {
            var response = JsonSerializer.Deserialize<FathomApiResponse<List<FathomTeam>>>(json);
            return response?.Data ?? new List<FathomTeam>();
        }

        // Helper methods
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
                throw new ArgumentException($"Fathom entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} and {video_id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
            }
            if (url.Contains("{video_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("video_id", out var videoId) || string.IsNullOrWhiteSpace(videoId))
                    throw new ArgumentException("Missing required 'video_id' filter for this endpoint.");
                url = url.Replace("{video_id}", Uri.EscapeDataString(videoId));
            }
            return url;
        }

        private static string BuildQueryParameters(Dictionary<string, string> q)
        {
            var query = new List<string>();
            foreach (var kvp in q)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value) && !kvp.Key.Contains("{") && !kvp.Key.Contains("}"))
                    query.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
            }
            return string.Join("&", query);
        }

        [CommandAttribute(ObjectType = "FathomVideo", PointType = EnumPointType.Function, Name = "GetVideos", Caption = "Get Videos", ClassName = "FathomDataSource")]
        public async Task<List<FathomVideo>> GetVideos()
        {
            var result = await GetEntityAsync("videos", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<FathomVideo>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomVideo>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomInsight", PointType = EnumPointType.Function, Name = "GetInsights", Caption = "Get Insights", ClassName = "FathomDataSource")]
        public async Task<List<FathomInsight>> GetInsights(string videoId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "video_id", FilterValue = videoId, Operator = "=" } };
            var result = await GetEntityAsync("insights", filters);
            return result.Select(item => JsonSerializer.Deserialize<FathomInsight>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomInsight>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomChapter", PointType = EnumPointType.Function, Name = "GetChapters", Caption = "Get Chapters", ClassName = "FathomDataSource")]
        public async Task<List<FathomChapter>> GetChapters(string videoId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "video_id", FilterValue = videoId, Operator = "=" } };
            var result = await GetEntityAsync("chapters", filters);
            return result.Select(item => JsonSerializer.Deserialize<FathomChapter>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomChapter>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomTranscript", PointType = EnumPointType.Function, Name = "GetTranscripts", Caption = "Get Transcripts", ClassName = "FathomDataSource")]
        public async Task<List<FathomTranscript>> GetTranscripts(string videoId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "video_id", FilterValue = videoId, Operator = "=" } };
            var result = await GetEntityAsync("transcripts", filters);
            return result.Select(item => JsonSerializer.Deserialize<FathomTranscript>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomTranscript>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomSummary", PointType = EnumPointType.Function, Name = "GetSummaries", Caption = "Get Summaries", ClassName = "FathomDataSource")]
        public async Task<List<FathomSummary>> GetSummaries(string videoId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "video_id", FilterValue = videoId, Operator = "=" } };
            var result = await GetEntityAsync("summaries", filters);
            return result.Select(item => JsonSerializer.Deserialize<FathomSummary>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomSummary>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomComment", PointType = EnumPointType.Function, Name = "GetComments", Caption = "Get Comments", ClassName = "FathomDataSource")]
        public async Task<List<FathomComment>> GetComments(string videoId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "video_id", FilterValue = videoId, Operator = "=" } };
            var result = await GetEntityAsync("comments", filters);
            return result.Select(item => JsonSerializer.Deserialize<FathomComment>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomComment>().ToList();
        }

        [CommandAttribute(ObjectType = "FathomTeam", PointType = EnumPointType.Function, Name = "GetTeams", Caption = "Get Teams", ClassName = "FathomDataSource")]
        public async Task<List<FathomTeam>> GetTeams()
        {
            var result = await GetEntityAsync("teams", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<FathomTeam>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<FathomTeam>().ToList();
        }
    }
}