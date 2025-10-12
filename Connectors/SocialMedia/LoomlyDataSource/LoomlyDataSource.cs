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
using TheTechIdea.Beep.Connectors.Loomly.Models;

namespace TheTechIdea.Beep.Connectors.Loomly
{
    /// <summary>
    /// Loomly data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Loomly)]
    public class LoomlyDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Loomly API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Posts
            ["posts"] = "posts",
            ["posts.get"] = "posts/{id}",
            // Campaigns
            ["campaigns"] = "campaigns",
            ["campaigns.get"] = "campaigns/{id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["posts.get"] = new[] { "id" },
            ["campaigns.get"] = new[] { "id" }
        };

        public LoomlyDataSource(
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
                throw new InvalidOperationException($"Unknown Loomly entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Build the full URL
            var url = $"https://api.loomly.com/v1/{endpoint}";
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
                "posts" => ParsePosts(json),
                "posts.get" => ParsePost(json),
                "campaigns" => ParseCampaigns(json),
                "campaigns.get" => ParseCampaign(json),
                _ => throw new NotSupportedException($"Entity '{EntityName}' parsing not implemented.")
            };
        }

        // Helper methods for parsing
        private IEnumerable<object> ParseEntityResponse(string entityName, string json)
        {
            return entityName.ToLower() switch
            {
                "posts" => ParsePosts(json),
                "posts.get" => ParsePost(json),
                "campaigns" => ParseCampaigns(json),
                "campaigns.get" => ParseCampaign(json),
                _ => Array.Empty<object>()
            };
        }

        private IEnumerable<object> ParsePosts(string json)
        {
            var response = JsonSerializer.Deserialize<LoomlyApiResponse<List<LoomlyPost>>>(json);
            return response?.Data ?? new List<LoomlyPost>();
        }

        private IEnumerable<object> ParsePost(string json)
        {
            var response = JsonSerializer.Deserialize<LoomlyApiResponse<LoomlyPost>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<LoomlyPost>();
        }

        private IEnumerable<object> ParseCampaigns(string json)
        {
            var response = JsonSerializer.Deserialize<LoomlyApiResponse<List<LoomlyCampaign>>>(json);
            return response?.Data ?? new List<LoomlyCampaign>();
        }

        private IEnumerable<object> ParseCampaign(string json)
        {
            var response = JsonSerializer.Deserialize<LoomlyApiResponse<LoomlyCampaign>>(json);
            return response?.Data != null ? new[] { response.Data } : Array.Empty<LoomlyCampaign>();
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
                throw new ArgumentException($"Loomly entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePlaceholders(string url, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (url.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                url = url.Replace("{id}", Uri.EscapeDataString(id));
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

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "LoomlyPost",
            PointType = EnumPointType.Function,
            Name = "GetPosts",
            Caption = "Get Loomly Posts",
            ClassName = "LoomlyDataSource",
            misc = "ReturnType: IEnumerable<LoomlyPost>"
        )]
        public IEnumerable<LoomlyPost> GetPosts()
        {
            return GetEntity<LoomlyPost>("posts");
        }

        [CommandAttribute(
            ObjectType = "LoomlyPost",
            PointType = EnumPointType.Function,
            Name = "GetPost",
            Caption = "Get Loomly Post by ID",
            ClassName = "LoomlyDataSource",
            misc = "ReturnType: IEnumerable<LoomlyPost>"
        )]
        public IEnumerable<LoomlyPost> GetPost(string id)
        {
            return GetEntity<LoomlyPost>("posts.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } });
        }

        [CommandAttribute(
            ObjectType = "LoomlyCampaign",
            PointType = EnumPointType.Function,
            Name = "GetCampaigns",
            Caption = "Get Loomly Campaigns",
            ClassName = "LoomlyDataSource",
            misc = "ReturnType: IEnumerable<LoomlyCampaign>"
        )]
        public IEnumerable<LoomlyCampaign> GetCampaigns()
        {
            return GetEntity<LoomlyCampaign>("campaigns");
        }

        [CommandAttribute(
            ObjectType = "LoomlyCampaign",
            PointType = EnumPointType.Function,
            Name = "GetCampaign",
            Caption = "Get Loomly Campaign by ID",
            ClassName = "LoomlyDataSource",
            misc = "ReturnType: IEnumerable<LoomlyCampaign>"
        )]
        public IEnumerable<LoomlyCampaign> GetCampaign(string id)
        {
            return GetEntity<LoomlyCampaign>("campaigns.get", new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id } });
        }

        // POST methods for creating entities
        [CommandAttribute(ObjectType = "LoomlyPost", PointType = EnumPointType.Function, Name = "CreatePost", Caption = "Create Loomly Post", ClassName = "LoomlyDataSource", misc = "ReturnType: LoomlyPost")]
        public async Task<LoomlyPost> CreatePostAsync(LoomlyPost post)
        {
            var response = await PostAsync("posts", post);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoomlyPost>(json);
        }

        [CommandAttribute(ObjectType = "LoomlyPost", PointType = EnumPointType.Function, Name = "UpdatePost", Caption = "Update Loomly Post", ClassName = "LoomlyDataSource", misc = "ReturnType: LoomlyPost")]
        public async Task<LoomlyPost> UpdatePostAsync(string postId, LoomlyPost post)
        {
            var response = await PutAsync($"posts/{postId}", post);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoomlyPost>(json);
        }

        [CommandAttribute(ObjectType = "LoomlyCampaign", PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Loomly Campaign", ClassName = "LoomlyDataSource", misc = "ReturnType: LoomlyCampaign")]
        public async Task<LoomlyCampaign> CreateCampaignAsync(LoomlyCampaign campaign)
        {
            var response = await PostAsync("campaigns", campaign);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoomlyCampaign>(json);
        }

        [CommandAttribute(ObjectType = "LoomlyCampaign", PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update Loomly Campaign", ClassName = "LoomlyDataSource", misc = "ReturnType: LoomlyCampaign")]
        public async Task<LoomlyCampaign> UpdateCampaignAsync(string campaignId, LoomlyCampaign campaign)
        {
            var response = await PutAsync($"campaigns/{campaignId}", campaign);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoomlyCampaign>(json);
        }
    }
}