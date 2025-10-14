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
using TheTechIdea.Beep.Connectors.Marketing.CampaignMonitorDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.CampaignMonitor
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CampaignMonitor)]
    public class CampaignMonitorDataSource : WebAPIDataSource
    {
        // CampaignMonitor API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Clients
            ["clients"] = ("clients.json", "", new[] { "" }),
            ["client"] = ("clients/{client_id}.json", "", new[] { "client_id" }),

            // Lists
            ["lists"] = ("clients/{client_id}/lists.json", "Lists", new[] { "client_id" }),
            ["list"] = ("lists/{list_id}.json", "", new[] { "list_id" }),
            ["list_stats"] = ("lists/{list_id}/stats.json", "", new[] { "list_id" }),
            ["list_custom_fields"] = ("lists/{list_id}/customfields.json", "CustomFields", new[] { "list_id" }),

            // Subscribers
            ["subscribers"] = ("lists/{list_id}/active.json", "Subscribers", new[] { "list_id" }),
            ["subscriber"] = ("subscribers/{list_id}.json", "", new[] { "list_id" }),
            ["subscriber_history"] = ("subscribers/{list_id}/history.json", "History", new[] { "list_id" }),
            ["unsubscribed"] = ("lists/{list_id}/unsubscribed.json", "Unsubscribed", new[] { "list_id" }),
            ["bounced"] = ("lists/{list_id}/bounced.json", "Bounced", new[] { "list_id" }),

            // Campaigns
            ["campaigns"] = ("clients/{client_id}/campaigns.json", "", new[] { "client_id" }),
            ["campaign"] = ("campaigns/{campaign_id}.json", "", new[] { "campaign_id" }),
            ["campaign_summary"] = ("campaigns/{campaign_id}/summary.json", "", new[] { "campaign_id" }),
            ["campaign_recipients"] = ("campaigns/{campaign_id}/recipients.json", "", new[] { "campaign_id" }),
            ["campaign_opens"] = ("campaigns/{campaign_id}/opens.json", "Opens", new[] { "campaign_id" }),
            ["campaign_clicks"] = ("campaigns/{campaign_id}/clicks.json", "Clicks", new[] { "campaign_id" }),
            ["campaign_unsubscribes"] = ("campaigns/{campaign_id}/unsubscribes.json", "Unsubscribes", new[] { "campaign_id" }),
            ["campaign_bounces"] = ("campaigns/{campaign_id}/bounces.json", "Bounces", new[] { "campaign_id" }),

            // Templates
            ["templates"] = ("clients/{client_id}/templates.json", "", new[] { "client_id" }),
            ["template"] = ("templates/{template_id}.json", "", new[] { "template_id" }),

            // Segments
            ["segments"] = ("lists/{list_id}/segments.json", "", new[] { "list_id" }),
            ["segment"] = ("segments/{segment_id}.json", "", new[] { "segment_id" }),

            // Administrators
            ["administrators"] = ("clients/{client_id}/people.json", "", new[] { "client_id" }),
            ["administrator"] = ("clients/{client_id}/people/{email}.json", "", new[] { "client_id", "email" }),

            // Suppressions
            ["suppressions"] = ("clients/{client_id}/suppressions.json", "", new[] { "client_id" }),

            // Journeys
            ["journeys"] = ("clients/{client_id}/journeys.json", "", new[] { "client_id" }),
            ["journey"] = ("journeys/{journey_id}.json", "", new[] { "journey_id" }),

            // Account
            ["account"] = ("account.json", "", new[] { "" }),

            // Timezones
            ["timezones"] = ("timezones.json", "", new[] { "" }),

            // Countries
            ["countries"] = ("countries.json", "", new[] { "" })
        };

        public CampaignMonitorDataSource(
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
                throw new InvalidOperationException($"Unknown CampaignMonitor entity '{EntityName}'.");

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
                throw new ArgumentException($"CampaignMonitor entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "client_id", "list_id", "campaign_id", "template_id", "segment_id", "journey_id", "email" })
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
    [CommandAttribute(ObjectType = nameof(CampaignMonitorClient), PointType = EnumPointType.Function, Name = "GetClients", Caption = "Get Clients", ClassName = "CampaignMonitorDataSource", misc = "GetClients")]
        public IEnumerable<CampaignMonitorClient> GetClients()
        {
            return GetEntity("clients", null).Cast<CampaignMonitorClient>();
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorList), PointType = EnumPointType.Function, Name = "GetLists", Caption = "Get Lists", ClassName = "CampaignMonitorDataSource", misc = "GetLists")]
        public IEnumerable<CampaignMonitorList> GetLists(string clientId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "client_id", FilterValue = clientId } };
            return GetEntity("lists", filters).Cast<CampaignMonitorList>();
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorSubscriber), PointType = EnumPointType.Function, Name = "GetSubscribers", Caption = "Get Subscribers", ClassName = "CampaignMonitorDataSource", misc = "GetSubscribers")]
        public IEnumerable<CampaignMonitorSubscriber> GetSubscribers(string listId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "list_id", FilterValue = listId } };
            return GetEntity("subscribers", filters).Cast<CampaignMonitorSubscriber>();
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorCampaign), PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "CampaignMonitorDataSource", misc = "GetCampaigns")]
        public IEnumerable<CampaignMonitorCampaign> GetCampaigns(string clientId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "client_id", FilterValue = clientId } };
            return GetEntity("campaigns", filters).Cast<CampaignMonitorCampaign>();
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorTemplate), PointType = EnumPointType.Function, Name = "GetTemplates", Caption = "Get Templates", ClassName = "CampaignMonitorDataSource", misc = "GetTemplates")]
        public IEnumerable<CampaignMonitorTemplate> GetTemplates(string clientId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "client_id", FilterValue = clientId } };
            return GetEntity("templates", filters).Cast<CampaignMonitorTemplate>();
        }

        // POST/PUT methods for creating and updating entities
    [CommandAttribute(ObjectType = nameof(CampaignMonitorClient), PointType = EnumPointType.Function, Name = "CreateClient", Caption = "Create Client", ClassName = "CampaignMonitorDataSource", misc = "CreateClient")]
        public async Task<CampaignMonitorClient> CreateClient(CampaignMonitorClient client)
        {
            var endpoint = "clients.json";
            var response = await PostAsync<CampaignMonitorClient>(endpoint, client);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorClient), PointType = EnumPointType.Function, Name = "UpdateClient", Caption = "Update Client", ClassName = "CampaignMonitorDataSource", misc = "UpdateClient")]
        public async Task<CampaignMonitorClient> UpdateClient(string clientId, CampaignMonitorClient client)
        {
            var endpoint = $"clients/{clientId}.json";
            var response = await PutAsync<CampaignMonitorClient>(endpoint, client);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorList), PointType = EnumPointType.Function, Name = "CreateList", Caption = "Create List", ClassName = "CampaignMonitorDataSource", misc = "CreateList")]
        public async Task<CampaignMonitorList> CreateList(string clientId, CampaignMonitorList list)
        {
            var endpoint = $"clients/{clientId}/lists.json";
            var response = await PostAsync<CampaignMonitorList>(endpoint, list);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorList), PointType = EnumPointType.Function, Name = "UpdateList", Caption = "Update List", ClassName = "CampaignMonitorDataSource", misc = "UpdateList")]
        public async Task<CampaignMonitorList> UpdateList(string listId, CampaignMonitorList list)
        {
            var endpoint = $"lists/{listId}.json";
            var response = await PutAsync<CampaignMonitorList>(endpoint, list);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorSubscriber), PointType = EnumPointType.Function, Name = "CreateSubscriber", Caption = "Create Subscriber", ClassName = "CampaignMonitorDataSource", misc = "CreateSubscriber")]
        public async Task<CampaignMonitorSubscriber> CreateSubscriber(string listId, CampaignMonitorSubscriber subscriber)
        {
            var endpoint = $"subscribers/{listId}.json";
            var response = await PostAsync<CampaignMonitorSubscriber>(endpoint, subscriber);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorSubscriber), PointType = EnumPointType.Function, Name = "UpdateSubscriber", Caption = "Update Subscriber", ClassName = "CampaignMonitorDataSource", misc = "UpdateSubscriber")]
        public async Task<CampaignMonitorSubscriber> UpdateSubscriber(string listId, CampaignMonitorSubscriber subscriber)
        {
            var endpoint = $"subscribers/{listId}.json";
            var response = await PutAsync<CampaignMonitorSubscriber>(endpoint, subscriber);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorCampaign), PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "CampaignMonitorDataSource", misc = "CreateCampaign")]
        public async Task<CampaignMonitorCampaign> CreateCampaign(string clientId, CampaignMonitorCampaign campaign)
        {
            var endpoint = $"clients/{clientId}/campaigns.json";
            var response = await PostAsync<CampaignMonitorCampaign>(endpoint, campaign);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorCampaign), PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "CampaignMonitorDataSource", misc = "UpdateCampaign")]
        public async Task<CampaignMonitorCampaign> UpdateCampaign(string campaignId, CampaignMonitorCampaign campaign)
        {
            var endpoint = $"campaigns/{campaignId}.json";
            var response = await PutAsync<CampaignMonitorCampaign>(endpoint, campaign);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorTemplate), PointType = EnumPointType.Function, Name = "CreateTemplate", Caption = "Create Template", ClassName = "CampaignMonitorDataSource", misc = "CreateTemplate")]
        public async Task<CampaignMonitorTemplate> CreateTemplate(string clientId, CampaignMonitorTemplate template)
        {
            var endpoint = $"clients/{clientId}/templates.json";
            var response = await PostAsync<CampaignMonitorTemplate>(endpoint, template);
            return response;
        }

    [CommandAttribute(ObjectType = nameof(CampaignMonitorTemplate), PointType = EnumPointType.Function, Name = "UpdateTemplate", Caption = "Update Template", ClassName = "CampaignMonitorDataSource", misc = "UpdateTemplate")]
        public async Task<CampaignMonitorTemplate> UpdateTemplate(string templateId, CampaignMonitorTemplate template)
        {
            var endpoint = $"templates/{templateId}.json";
            var response = await PutAsync<CampaignMonitorTemplate>(endpoint, template);
            return response;
        }
    }
}