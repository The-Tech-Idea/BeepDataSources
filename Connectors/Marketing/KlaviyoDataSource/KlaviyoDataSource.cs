using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Marketing.KlaviyoDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.Klaviyo
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Klaviyo)]
    public class KlaviyoDataSource : WebAPIDataSource
    {
        // Klaviyo API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Profiles (Contacts)
            ["profiles"] = ("api/profiles", "data", new[] { "" }),
            ["profile"] = ("api/profiles/{profile_id}", "", new[] { "profile_id" }),

            // Lists
            ["lists"] = ("api/lists", "data", new[] { "" }),
            ["list"] = ("api/lists/{list_id}", "", new[] { "list_id" }),
            ["list_profiles"] = ("api/lists/{list_id}/profiles", "data", new[] { "list_id" }),
            ["list_relationships"] = ("api/lists/{list_id}/relationships/profiles", "data", new[] { "list_id" }),

            // Segments
            ["segments"] = ("api/segments", "data", new[] { "" }),
            ["segment"] = ("api/segments/{segment_id}", "", new[] { "segment_id" }),
            ["segment_profiles"] = ("api/segments/{segment_id}/profiles", "data", new[] { "segment_id" }),
            ["segment_relationships"] = ("api/segments/{segment_id}/relationships/profiles", "data", new[] { "segment_id" }),

            // Campaigns
            ["campaigns"] = ("api/campaigns", "data", new[] { "" }),
            ["campaign"] = ("api/campaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_recipients"] = ("api/campaigns/{campaign_id}/recipients", "data", new[] { "campaign_id" }),
            ["campaign_relationships"] = ("api/campaigns/{campaign_id}/relationships/tags", "data", new[] { "campaign_id" }),

            // Flows
            ["flows"] = ("api/flows", "data", new[] { "" }),
            ["flow"] = ("api/flows/{flow_id}", "", new[] { "flow_id" }),
            ["flow_actions"] = ("api/flows/{flow_id}/flow-actions", "data", new[] { "flow_id" }),

            // Events
            ["events"] = ("api/events", "data", new[] { "" }),
            ["profile_events"] = ("api/profiles/{profile_id}/events", "data", new[] { "profile_id" }),

            // Metrics
            ["metrics"] = ("api/metrics", "data", new[] { "" }),
            ["metric"] = ("api/metrics/{metric_id}", "", new[] { "metric_id" }),

            // Tags
            ["tags"] = ("api/tags", "data", new[] { "" }),
            ["tag"] = ("api/tags/{tag_id}", "", new[] { "tag_id" }),
            ["tag_relationships"] = ("api/tags/{tag_id}/relationships/campaigns", "data", new[] { "tag_id" }),

            // Templates
            ["templates"] = ("api/templates", "data", new[] { "" }),
            ["template"] = ("api/templates/{template_id}", "", new[] { "template_id" }),

            // Coupons
            ["coupons"] = ("api/coupons", "data", new[] { "" }),
            ["coupon"] = ("api/coupons/{coupon_id}", "", new[] { "coupon_id" }),

            // Catalogs
            ["catalogs"] = ("api/catalogs", "data", new[] { "" }),
            ["catalog"] = ("api/catalogs/{catalog_id}", "", new[] { "catalog_id" }),
            ["catalog_items"] = ("api/catalogs/{catalog_id}/items", "data", new[] { "catalog_id" }),
            ["catalog_categories"] = ("api/catalogs/{catalog_id}/categories", "data", new[] { "catalog_id" }),
            ["catalog_variants"] = ("api/catalogs/{catalog_id}/variants", "data", new[] { "catalog_id" }),

            // Webhooks
            ["webhooks"] = ("api/webhooks", "data", new[] { "" }),
            ["webhook"] = ("api/webhooks/{webhook_id}", "", new[] { "webhook_id" }),

            // Account
            ["account"] = ("api/accounts", "data", new[] { "" })
        };

        public KlaviyoDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure we're on WebAPI connection properties
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities from Map
            EntitiesNames = Map.Keys.ToList();
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
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Klaviyo entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter ?? new List<AppFilter>());
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root);
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
                throw new ArgumentException($"Klaviyo entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "profile_id", "list_id", "segment_id", "campaign_id", "flow_id", "metric_id", "tag_id", "template_id", "coupon_id", "catalog_id", "webhook_id" })
            {
                if (result.Contains($"{{{param}}}", StringComparison.Ordinal))
                {
                    if (!q.TryGetValue(param, out var value) || string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException($"Missing required '{param}' filter for this endpoint.");
                    result = result.Replace($"{{{param}}}", Uri.EscapeDataString(value));
                }
            }
            return result;
        }

        // Extracts array from response
        private static List<object> ExtractArray(HttpResponseMessage resp, string? root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list;
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

        // CommandAttribute methods for framework integration
    [CommandAttribute(ObjectType = nameof(KlaviyoProfile), PointType = EnumPointType.Function, Name = "GetProfiles", Caption = "Get Profiles", ClassName = "KlaviyoDataSource", misc = "GetProfiles")]
        public IEnumerable<KlaviyoProfile> GetProfiles()
        {
            return GetEntity("profiles", null).Cast<KlaviyoProfile>();
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoList), PointType = EnumPointType.Function, Name = "GetLists", Caption = "Get Lists", ClassName = "KlaviyoDataSource", misc = "GetLists")]
        public IEnumerable<KlaviyoList> GetLists()
        {
            return GetEntity("lists", null).Cast<KlaviyoList>();
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoSegment), PointType = EnumPointType.Function, Name = "GetSegments", Caption = "Get Segments", ClassName = "KlaviyoDataSource", misc = "GetSegments")]
        public IEnumerable<KlaviyoSegment> GetSegments()
        {
            return GetEntity("segments", null).Cast<KlaviyoSegment>();
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoCampaign), PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "KlaviyoDataSource", misc = "GetCampaigns")]
        public IEnumerable<KlaviyoCampaign> GetCampaigns()
        {
            return GetEntity("campaigns", null).Cast<KlaviyoCampaign>();
        }

        // POST/PUT methods for creating and updating entities
    [CommandAttribute(ObjectType = nameof(KlaviyoProfile), PointType = EnumPointType.Function, Name = "CreateProfile", Caption = "Create Profile", ClassName = "KlaviyoDataSource", misc = "CreateProfile")]
        public async Task<KlaviyoProfile> CreateProfile(KlaviyoProfile profile)
        {
            var endpoint = "profiles";
            var response = await PostAsync<KlaviyoProfile>(endpoint, profile);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoProfile), PointType = EnumPointType.Function, Name = "UpdateProfile", Caption = "Update Profile", ClassName = "KlaviyoDataSource", misc = "UpdateProfile")]
        public async Task<KlaviyoProfile> UpdateProfile(string profileId, KlaviyoProfile profile)
        {
            var endpoint = $"profiles/{profileId}";
            var response = await PutAsync<KlaviyoProfile>(endpoint, profile);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoList), PointType = EnumPointType.Function, Name = "CreateList", Caption = "Create List", ClassName = "KlaviyoDataSource", misc = "CreateList")]
        public async Task<KlaviyoList> CreateList(KlaviyoList list)
        {
            var endpoint = "lists";
            var response = await PostAsync<KlaviyoList>(endpoint, list);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoList), PointType = EnumPointType.Function, Name = "UpdateList", Caption = "Update List", ClassName = "KlaviyoDataSource", misc = "UpdateList")]
        public async Task<KlaviyoList> UpdateList(string listId, KlaviyoList list)
        {
            var endpoint = $"lists/{listId}";
            var response = await PutAsync<KlaviyoList>(endpoint, list);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoCampaign), PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "KlaviyoDataSource", misc = "CreateCampaign")]
        public async Task<KlaviyoCampaign> CreateCampaign(KlaviyoCampaign campaign)
        {
            var endpoint = "campaigns";
            var response = await PostAsync<KlaviyoCampaign>(endpoint, campaign);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoCampaign), PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "KlaviyoDataSource", misc = "UpdateCampaign")]
        public async Task<KlaviyoCampaign> UpdateCampaign(string campaignId, KlaviyoCampaign campaign)
        {
            var endpoint = $"campaigns/{campaignId}";
            var response = await PutAsync<KlaviyoCampaign>(endpoint, campaign);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoSegment), PointType = EnumPointType.Function, Name = "CreateSegment", Caption = "Create Segment", ClassName = "KlaviyoDataSource", misc = "CreateSegment")]
        public async Task<KlaviyoSegment> CreateSegment(KlaviyoSegment segment)
        {
            var endpoint = "segments";
            var response = await PostAsync<KlaviyoSegment>(endpoint, segment);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(KlaviyoSegment), PointType = EnumPointType.Function, Name = "UpdateSegment", Caption = "Update Segment", ClassName = "KlaviyoDataSource", misc = "UpdateSegment")]
        public async Task<KlaviyoSegment> UpdateSegment(string segmentId, KlaviyoSegment segment)
        {
            var endpoint = $"segments/{segmentId}";
            var response = await PutAsync<KlaviyoSegment>(endpoint, segment);
            return response;
        }
    }
}