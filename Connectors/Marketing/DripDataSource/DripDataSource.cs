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
using TheTechIdea.Beep.Connectors.Marketing.DripDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.Drip
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Drip)]
    public class DripDataSource : WebAPIDataSource
    {
        // Drip API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Subscribers
            ["subscribers"] = ("v2/subscribers", "subscribers", new[] { "" }),
            ["subscriber"] = ("v2/subscribers/{subscriber_id}", "", new[] { "subscriber_id" }),
            ["subscriber_tags"] = ("v2/subscribers/{subscriber_id}/tags", "tags", new[] { "subscriber_id" }),
            ["subscriber_events"] = ("v2/subscribers/{subscriber_id}/events", "events", new[] { "subscriber_id" }),

            // Campaigns
            ["campaigns"] = ("v2/campaigns", "campaigns", new[] { "" }),
            ["campaign"] = ("v2/campaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_subscribers"] = ("v2/campaigns/{campaign_id}/subscribers", "subscribers", new[] { "campaign_id" }),

            // Tags
            ["tags"] = ("v2/tags", "tags", new[] { "" }),
            ["tag"] = ("v2/tags/{tag_id}", "", new[] { "tag_id" }),
            ["tag_subscribers"] = ("v2/tags/{tag_id}/subscribers", "subscribers", new[] { "tag_id" }),

            // Workflows
            ["workflows"] = ("v2/workflows", "workflows", new[] { "" }),
            ["workflow"] = ("v2/workflows/{workflow_id}", "", new[] { "workflow_id" }),
            ["workflow_triggers"] = ("v2/workflows/{workflow_id}/triggers", "triggers", new[] { "workflow_id" }),
            ["workflow_actions"] = ("v2/workflows/{workflow_id}/actions", "actions", new[] { "workflow_id" }),

            // Forms
            ["forms"] = ("v2/forms", "forms", new[] { "" }),
            ["form"] = ("v2/forms/{form_id}", "", new[] { "form_id" }),
            ["form_submissions"] = ("v2/forms/{form_id}/submissions", "submissions", new[] { "form_id" }),

            // Accounts
            ["accounts"] = ("v2/accounts", "accounts", new[] { "" }),
            ["account"] = ("v2/accounts/{account_id}", "", new[] { "account_id" }),

            // Custom Fields
            ["custom_fields"] = ("v2/custom_fields", "custom_fields", new[] { "" }),
            ["custom_field"] = ("v2/custom_fields/{custom_field_id}", "", new[] { "custom_field_id" }),

            // Events
            ["events"] = ("v2/events", "events", new[] { "" }),
            ["event"] = ("v2/events/{event_id}", "", new[] { "event_id" }),

            // Webhooks
            ["webhooks"] = ("v2/webhooks", "webhooks", new[] { "" }),
            ["webhook"] = ("v2/webhooks/{webhook_id}", "", new[] { "webhook_id" }),

            // Reports
            ["reports"] = ("v2/reports", "reports", new[] { "" }),
            ["campaign_report"] = ("v2/reports/campaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["workflow_report"] = ("v2/reports/workflows/{workflow_id}", "", new[] { "workflow_id" })
        };

        public DripDataSource(
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

            // Register entities from the Map
            var entitiesNames = Map.Keys.ToList();
            Entities = entitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string entityName, List<AppFilter>? filter)
        {
            if (!Map.TryGetValue(entityName, out var mapping))
            {
                throw new ArgumentException($"Entity '{entityName}' not found in Drip API map");
            }

            var (endpoint, root, requiredFilters) = mapping;

            // Validate required filters
            if (requiredFilters.Length > 0 && requiredFilters[0] != "" && (filter == null || !filter.Any()))
            {
                throw new ArgumentException($"Entity '{entityName}' requires filters: {string.Join(", ", requiredFilters)}");
            }

            // Replace placeholders in endpoint
            var finalEndpoint = endpoint;
            if (filter != null)
            {
                foreach (var f in filter)
                {
                    finalEndpoint = finalEndpoint.Replace($"{{{f.FieldName}}}", f.FilterValue?.ToString() ?? "");
                }
            }

            // Make the API call
            var response = await GetAsync(finalEndpoint);
            if (response == null)
            {
                return null;
            }

            // Extract the array from the response
            var result = ExtractArray(response, root);
            return result ?? new List<object>();
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
        [CommandAttribute(ObjectType = nameof(DripSubscriber), PointType = PointType.Function, Name = "GetSubscribers", Caption = "Get Subscribers", ClassName = "DripDataSource", misc = "GetSubscribers")]
        public IEnumerable<DripSubscriber> GetSubscribers()
        {
            return GetEntity("subscribers", null).Cast<DripSubscriber>();
        }

        [CommandAttribute(ObjectType = nameof(DripCampaign), PointType = PointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "DripDataSource", misc = "GetCampaigns")]
        public IEnumerable<DripCampaign> GetCampaigns()
        {
            return GetEntity("campaigns", null).Cast<DripCampaign>();
        }

        [CommandAttribute(ObjectType = nameof(DripTag), PointType = PointType.Function, Name = "GetTags", Caption = "Get Tags", ClassName = "DripDataSource", misc = "GetTags")]
        public IEnumerable<DripTag> GetTags()
        {
            return GetEntity("tags", null).Cast<DripTag>();
        }

        [CommandAttribute(ObjectType = nameof(DripWorkflow), PointType = PointType.Function, Name = "GetWorkflows", Caption = "Get Workflows", ClassName = "DripDataSource", misc = "GetWorkflows")]
        public IEnumerable<DripWorkflow> GetWorkflows()
        {
            return GetEntity("workflows", null).Cast<DripWorkflow>();
        }

        [CommandAttribute(ObjectType = nameof(DripForm), PointType = PointType.Function, Name = "GetForms", Caption = "Get Forms", ClassName = "DripDataSource", misc = "GetForms")]
        public IEnumerable<DripForm> GetForms()
        {
            return GetEntity("forms", null).Cast<DripForm>();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = nameof(DripSubscriber), PointType = PointType.Function, Name = "CreateSubscriber", Caption = "Create Subscriber", ClassName = "DripDataSource", misc = "CreateSubscriber")]
        public async Task<DripSubscriber> CreateSubscriber(DripSubscriber subscriber)
        {
            var endpoint = "v2/subscribers";
            var response = await PostAsync<DripSubscriber>(endpoint, subscriber);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripSubscriber), PointType = PointType.Function, Name = "UpdateSubscriber", Caption = "Update Subscriber", ClassName = "DripDataSource", misc = "UpdateSubscriber")]
        public async Task<DripSubscriber> UpdateSubscriber(string subscriberId, DripSubscriber subscriber)
        {
            var endpoint = $"v2/subscribers/{subscriberId}";
            var response = await PutAsync<DripSubscriber>(endpoint, subscriber);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripCampaign), PointType = PointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "DripDataSource", misc = "CreateCampaign")]
        public async Task<DripCampaign> CreateCampaign(DripCampaign campaign)
        {
            var endpoint = "v2/campaigns";
            var response = await PostAsync<DripCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripCampaign), PointType = PointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "DripDataSource", misc = "UpdateCampaign")]
        public async Task<DripCampaign> UpdateCampaign(string campaignId, DripCampaign campaign)
        {
            var endpoint = $"v2/campaigns/{campaignId}";
            var response = await PutAsync<DripCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripTag), PointType = PointType.Function, Name = "CreateTag", Caption = "Create Tag", ClassName = "DripDataSource", misc = "CreateTag")]
        public async Task<DripTag> CreateTag(DripTag tag)
        {
            var endpoint = "v2/tags";
            var response = await PostAsync<DripTag>(endpoint, tag);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripTag), PointType = PointType.Function, Name = "UpdateTag", Caption = "Update Tag", ClassName = "DripDataSource", misc = "UpdateTag")]
        public async Task<DripTag> UpdateTag(string tagId, DripTag tag)
        {
            var endpoint = $"v2/tags/{tagId}";
            var response = await PutAsync<DripTag>(endpoint, tag);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripWorkflow), PointType = PointType.Function, Name = "CreateWorkflow", Caption = "Create Workflow", ClassName = "DripDataSource", misc = "CreateWorkflow")]
        public async Task<DripWorkflow> CreateWorkflow(DripWorkflow workflow)
        {
            var endpoint = "v2/workflows";
            var response = await PostAsync<DripWorkflow>(endpoint, workflow);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripWorkflow), PointType = PointType.Function, Name = "UpdateWorkflow", Caption = "Update Workflow", ClassName = "DripDataSource", misc = "UpdateWorkflow")]
        public async Task<DripWorkflow> UpdateWorkflow(string workflowId, DripWorkflow workflow)
        {
            var endpoint = $"v2/workflows/{workflowId}";
            var response = await PutAsync<DripWorkflow>(endpoint, workflow);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripForm), PointType = PointType.Function, Name = "CreateForm", Caption = "Create Form", ClassName = "DripDataSource", misc = "CreateForm")]
        public async Task<DripForm> CreateForm(DripForm form)
        {
            var endpoint = "v2/forms";
            var response = await PostAsync<DripForm>(endpoint, form);
            return response;
        }

        [CommandAttribute(ObjectType = nameof(DripForm), PointType = PointType.Function, Name = "UpdateForm", Caption = "Update Form", ClassName = "DripDataSource", misc = "UpdateForm")]
        public async Task<DripForm> UpdateForm(string formId, DripForm form)
        {
            var endpoint = $"v2/forms/{formId}";
            var response = await PutAsync<DripForm>(endpoint, form);
            return response;
        }
    }
}