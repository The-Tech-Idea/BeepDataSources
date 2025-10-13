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
using TheTechIdea.Beep.Connectors.Marketing.ConstantContactDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.ConstantContact
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ConstantContact)]
    public class ConstantContactDataSource : WebAPIDataSource
    {
        // ConstantContact API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Contacts
            ["contacts"] = ("v2/contacts", "results", new[] { "" }),
            ["contact"] = ("v2/contacts/{contact_id}", "", new[] { "contact_id" }),

            // Lists
            ["lists"] = ("v2/lists", "lists", new[] { "" }),
            ["list"] = ("v2/lists/{list_id}", "", new[] { "list_id" }),
            ["list_contacts"] = ("v2/lists/{list_id}/contacts", "results", new[] { "list_id" }),

            // Campaigns
            ["campaigns"] = ("v2/emailmarketing/campaigns", "results", new[] { "" }),
            ["campaign"] = ("v2/emailmarketing/campaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_schedules"] = ("v2/emailmarketing/campaigns/{campaign_id}/schedules", "schedules", new[] { "campaign_id" }),

            // Email Campaigns
            ["email_campaigns"] = ("v3/emailcampaigns", "campaigns", new[] { "" }),
            ["email_campaign"] = ("v3/emailcampaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_activities"] = ("v3/emailcampaigns/{campaign_id}/activities", "activities", new[] { "campaign_id" }),

            // Activities
            ["activities"] = ("v3/activities", "activities", new[] { "" }),
            ["activity"] = ("v3/activities/{activity_id}", "", new[] { "activity_id" }),

            // Contact Lists
            ["contact_lists"] = ("v2/contacts/{contact_id}/lists", "lists", new[] { "contact_id" }),

            // Account
            ["account"] = ("v2/account/info", "", new[] { "" }),

            // Tags
            ["tags"] = ("v2/tags", "tags", new[] { "" }),
            ["contact_tags"] = ("v2/contacts/{contact_id}/tags", "tags", new[] { "contact_id" }),

            // Custom Fields
            ["custom_fields"] = ("v2/contact_custom_fields", "custom_fields", new[] { "" }),

            // Tracking
            ["campaign_tracking"] = ("v2/emailmarketing/campaigns/{campaign_id}/tracking", "", new[] { "campaign_id" }),
            ["contact_tracking"] = ("v2/contacts/{contact_id}/tracking", "", new[] { "contact_id" }),

            // Reports
            ["campaign_reports"] = ("v2/emailmarketing/campaigns/{campaign_id}/tracking/reports/summary", "", new[] { "campaign_id" }),
            ["contact_reports"] = ("v2/contacts/{contact_id}/tracking/reports/summary", "", new[] { "contact_id" })
        };

        public ConstantContactDataSource(
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
                throw new InvalidOperationException($"Unknown ConstantContact entity '{EntityName}'.");

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
                throw new ArgumentException($"ConstantContact entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "contact_id", "list_id", "campaign_id", "activity_id" })
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
        [CommandAttribute(ObjectType = nameof(ConstantContactContact), PointType = EnumPointType.Function, Name = "GetContacts", Caption = "Get Contacts", ClassName = "ConstantContactDataSource", misc = "GetContacts")]
        public IEnumerable<ConstantContactContact> GetContacts()
        {
            return GetEntity("contacts", null).Cast<ConstantContactContact>();
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactList), PointType = EnumPointType.Function, Name = "GetLists", Caption = "Get Lists", ClassName = "ConstantContactDataSource", misc = "GetLists")]
        public IEnumerable<ConstantContactList> GetLists()
        {
            return GetEntity("lists", null).Cast<ConstantContactList>();
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactCampaign), PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "ConstantContactDataSource", misc = "GetCampaigns")]
        public IEnumerable<ConstantContactCampaign> GetCampaigns()
        {
            return GetEntity("campaigns", null).Cast<ConstantContactCampaign>();
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactEmailCampaign), PointType = EnumPointType.Function, Name = "GetEmailCampaigns", Caption = "Get Email Campaigns", ClassName = "ConstantContactDataSource", misc = "GetEmailCampaigns")]
        public IEnumerable<ConstantContactEmailCampaign> GetEmailCampaigns()
        {
            return GetEntity("email_campaigns", null).Cast<ConstantContactEmailCampaign>();
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactActivity), PointType = PointType.Function, Name = "GetActivities", Caption = "Get Activities", ClassName = "ConstantContactDataSource", misc = "GetActivities")]
        public IEnumerable<ConstantContactActivity> GetActivities()
        {
            return GetEntity("activities", null).Cast<ConstantContactActivity>();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = nameof(ConstantContactContact), PointType = PointType.Function, Name = "CreateContact", Caption = "Create Contact", ClassName = "ConstantContactDataSource", misc = "CreateContact")]
        public async Task<ConstantContactContact> CreateContact(ConstantContactContact contact)
        {
            var endpoint = "v2/contacts";
            var response = await PostAsync<ConstantContactContact>(endpoint, contact);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactContact), PointType = PointType.Function, Name = "UpdateContact", Caption = "Update Contact", ClassName = "ConstantContactDataSource", misc = "UpdateContact")]
        public async Task<ConstantContactContact> UpdateContact(string contactId, ConstantContactContact contact)
        {
            var endpoint = $"v2/contacts/{contactId}";
            var response = await PutAsync<ConstantContactContact>(endpoint, contact);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactList), PointType = PointType.Function, Name = "CreateList", Caption = "Create List", ClassName = "ConstantContactDataSource", misc = "CreateList")]
        public async Task<ConstantContactList> CreateList(ConstantContactList list)
        {
            var endpoint = "v2/lists";
            var response = await PostAsync<ConstantContactList>(endpoint, list);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactList), PointType = PointType.Function, Name = "UpdateList", Caption = "Update List", ClassName = "ConstantContactDataSource", misc = "UpdateList")]
        public async Task<ConstantContactList> UpdateList(string listId, ConstantContactList list)
        {
            var endpoint = $"v2/lists/{listId}";
            var response = await PutAsync<ConstantContactList>(endpoint, list);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactCampaign), PointType = PointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "ConstantContactDataSource", misc = "CreateCampaign")]
        public async Task<ConstantContactCampaign> CreateCampaign(ConstantContactCampaign campaign)
        {
            var endpoint = "v2/emailmarketing/campaigns";
            var response = await PostAsync<ConstantContactCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactCampaign), PointType = PointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "ConstantContactDataSource", misc = "UpdateCampaign")]
        public async Task<ConstantContactCampaign> UpdateCampaign(string campaignId, ConstantContactCampaign campaign)
        {
            var endpoint = $"v2/emailmarketing/campaigns/{campaignId}";
            var response = await PutAsync<ConstantContactCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactEmailCampaign), PointType = PointType.Function, Name = "CreateEmailCampaign", Caption = "Create Email Campaign", ClassName = "ConstantContactDataSource", misc = "CreateEmailCampaign")]
        public async Task<ConstantContactEmailCampaign> CreateEmailCampaign(ConstantContactEmailCampaign emailCampaign)
        {
            var endpoint = "v3/emailcampaigns";
            var response = await PostAsync<ConstantContactEmailCampaign>(endpoint, emailCampaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactEmailCampaign), PointType = PointType.Function, Name = "UpdateEmailCampaign", Caption = "Update Email Campaign", ClassName = "ConstantContactDataSource", misc = "UpdateEmailCampaign")]
        public async Task<ConstantContactEmailCampaign> UpdateEmailCampaign(string campaignId, ConstantContactEmailCampaign emailCampaign)
        {
            var endpoint = $"v3/emailcampaigns/{campaignId}";
            var response = await PutAsync<ConstantContactEmailCampaign>(endpoint, emailCampaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactActivity), PointType = PointType.Function, Name = "CreateActivity", Caption = "Create Activity", ClassName = "ConstantContactDataSource", misc = "CreateActivity")]
        public async Task<ConstantContactActivity> CreateActivity(ConstantContactActivity activity)
        {
            var endpoint = "v3/activities";
            var response = await PostAsync<ConstantContactActivity>(endpoint, activity);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(ConstantContactActivity), PointType = PointType.Function, Name = "UpdateActivity", Caption = "Update Activity", ClassName = "ConstantContactDataSource", misc = "UpdateActivity")]
        public async Task<ConstantContactActivity> UpdateActivity(string activityId, ConstantContactActivity activity)
        {
            var endpoint = $"v3/activities/{activityId}";
            var response = await PutAsync<ConstantContactActivity>(endpoint, activity);
            return response;
        }
    }
}