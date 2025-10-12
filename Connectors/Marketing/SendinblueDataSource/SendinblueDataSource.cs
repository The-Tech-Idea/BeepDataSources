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
using TheTechIdea.Beep.Connectors.Marketing.SendinblueDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.Sendinblue
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Sendinblue)]
    public class SendinblueDataSource : WebAPIDataSource
    {
        // Sendinblue (Brevo) API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Contacts
            ["contacts"] = ("contacts", "contacts", new[] { "" }),
            ["contact"] = ("contacts/{contact_id}", "", new[] { "contact_id" }),
            ["contact_attributes"] = ("contacts/attributes", "attributes", new[] { "" }),
            ["contact_folders"] = ("contacts/folders", "folders", new[] { "" }),
            ["contact_lists"] = ("contacts/lists", "lists", new[] { "" }),
            ["contact_list_contacts"] = ("contacts/lists/{list_id}/contacts", "contacts", new[] { "list_id" }),

            // Email Campaigns
            ["email_campaigns"] = ("emailCampaigns", "campaigns", new[] { "" }),
            ["email_campaign"] = ("emailCampaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_recipients"] = ("emailCampaigns/{campaign_id}/recipients", "contacts", new[] { "campaign_id" }),

            // SMS Campaigns
            ["sms_campaigns"] = ("smsCampaigns", "campaigns", new[] { "" }),
            ["sms_campaign"] = ("smsCampaigns/{campaign_id}", "", new[] { "campaign_id" }),

            // Templates
            ["email_templates"] = ("smtp/templates", "templates", new[] { "" }),
            ["email_template"] = ("smtp/templates/{template_id}", "", new[] { "template_id" }),

            // Transactional Emails
            ["smtp_emails"] = ("smtp/emails", "emails", new[] { "" }),

            // Webhooks
            ["webhooks"] = ("webhooks", "webhooks", new[] { "" }),
            ["webhook"] = ("webhooks/{webhook_id}", "", new[] { "webhook_id" }),

            // Account
            ["account"] = ("account", "", new[] { "" }),

            // Statistics
            ["email_statistics"] = ("statistics", "", new[] { "" }),
            ["campaign_statistics"] = ("emailCampaigns/{campaign_id}/statistics", "", new[] { "campaign_id" }),

            // Processes
            ["processes"] = ("processes", "processes", new[] { "" }),
            ["process"] = ("processes/{process_id}", "", new[] { "process_id" }),

            // Segments
            ["segments"] = ("contacts/segments", "segments", new[] { "" }),
            ["segment"] = ("contacts/segments/{segment_id}", "", new[] { "segment_id" }),

            // Companies
            ["companies"] = ("companies", "companies", new[] { "" }),
            ["company"] = ("companies/{company_id}", "", new[] { "company_id" }),

            // Deals
            ["deals"] = ("crm/deals", "items", new[] { "" }),
            ["deal"] = ("crm/deals/{deal_id}", "", new[] { "deal_id" }),
            ["deal_attributes"] = ("crm/deals/attributes", "attributes", new[] { "" }),

            // Tasks
            ["tasks"] = ("crm/tasks", "items", new[] { "" }),
            ["task"] = ("crm/tasks/{task_id}", "", new[] { "task_id" }),
            ["task_types"] = ("crm/tasktypes", "types", new[] { "" }),

            // Notes
            ["notes"] = ("crm/notes", "items", new[] { "" }),
            ["note"] = ("crm/notes/{note_id}", "", new[] { "note_id" }),

            // Events
            ["events"] = ("events", "events", new[] { "" }),

            // Domains
            ["domains"] = ("senders/domains", "domains", new[] { "" }),

            // Senders
            ["senders"] = ("senders", "senders", new[] { "" }),

            // IP Addresses
            ["ips"] = ("senders/ips", "ips", new[] { "" }),

            // Reports
            ["reports"] = ("reports", "reports", new[] { "" })
        };

        public SendinblueDataSource(
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
                throw new InvalidOperationException($"Unknown Sendinblue entity '{EntityName}'.");

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
                throw new ArgumentException($"Sendinblue entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "contact_id", "list_id", "campaign_id", "template_id", "webhook_id", "process_id", "segment_id", "company_id", "deal_id", "task_id", "note_id" })
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

        [CommandAttribute(ObjectType = "SendinblueContact", PointType = EnumPointType.Function, Name = "GetContacts", Caption = "Get Contacts", ClassName = "SendinblueDataSource")]
        public async Task<List<SendinblueContact>> GetContacts()
        {
            var result = await GetEntityAsync("contacts", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<SendinblueContact>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<SendinblueContact>().ToList();
        }

        [CommandAttribute(ObjectType = "SendinblueList", PointType = EnumPointType.Function, Name = "GetLists", Caption = "Get Lists", ClassName = "SendinblueDataSource")]
        public async Task<List<SendinblueList>> GetLists()
        {
            var result = await GetEntityAsync("contact_lists", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<SendinblueList>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<SendinblueList>().ToList();
        }

        [CommandAttribute(ObjectType = "SendinblueEmailCampaign", PointType = EnumPointType.Function, Name = "GetEmailCampaigns", Caption = "Get Email Campaigns", ClassName = "SendinblueDataSource")]
        public async Task<List<SendinblueEmailCampaign>> GetEmailCampaigns()
        {
            var result = await GetEntityAsync("email_campaigns", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<SendinblueEmailCampaign>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<SendinblueEmailCampaign>().ToList();
        }

        [CommandAttribute(ObjectType = "SendinblueSmsCampaign", PointType = EnumPointType.Function, Name = "GetSmsCampaigns", Caption = "Get SMS Campaigns", ClassName = "SendinblueDataSource")]
        public async Task<List<SendinblueSmsCampaign>> GetSmsCampaigns()
        {
            var result = await GetEntityAsync("sms_campaigns", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<SendinblueSmsCampaign>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<SendinblueSmsCampaign>().ToList();
        }

        [CommandAttribute(ObjectType = "SendinblueTemplate", PointType = EnumPointType.Function, Name = "GetTemplates", Caption = "Get Templates", ClassName = "SendinblueDataSource")]
        public async Task<List<SendinblueTemplate>> GetTemplates()
        {
            var result = await GetEntityAsync("email_templates", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<SendinblueTemplate>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<SendinblueTemplate>().ToList();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = "SendinblueContact", PointType = EnumPointType.Function, Name = "CreateContact", Caption = "Create Contact", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueContact> CreateContact(SendinblueContact contact)
        {
            var endpoint = "contacts";
            var response = await PostAsync<SendinblueContact>(endpoint, contact);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueContact", PointType = EnumPointType.Function, Name = "UpdateContact", Caption = "Update Contact", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueContact> UpdateContact(string contactId, SendinblueContact contact)
        {
            var endpoint = $"contacts/{contactId}";
            var response = await PutAsync<SendinblueContact>(endpoint, contact);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueEmailCampaign", PointType = EnumPointType.Function, Name = "CreateEmailCampaign", Caption = "Create Email Campaign", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueEmailCampaign> CreateEmailCampaign(SendinblueEmailCampaign campaign)
        {
            var endpoint = "emailCampaigns";
            var response = await PostAsync<SendinblueEmailCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueEmailCampaign", PointType = EnumPointType.Function, Name = "UpdateEmailCampaign", Caption = "Update Email Campaign", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueEmailCampaign> UpdateEmailCampaign(string campaignId, SendinblueEmailCampaign campaign)
        {
            var endpoint = $"emailCampaigns/{campaignId}";
            var response = await PutAsync<SendinblueEmailCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueSmsCampaign", PointType = EnumPointType.Function, Name = "CreateSmsCampaign", Caption = "Create SMS Campaign", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueSmsCampaign> CreateSmsCampaign(SendinblueSmsCampaign campaign)
        {
            var endpoint = "smsCampaigns";
            var response = await PostAsync<SendinblueSmsCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueSmsCampaign", PointType = EnumPointType.Function, Name = "UpdateSmsCampaign", Caption = "Update SMS Campaign", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueSmsCampaign> UpdateSmsCampaign(string campaignId, SendinblueSmsCampaign campaign)
        {
            var endpoint = $"smsCampaigns/{campaignId}";
            var response = await PutAsync<SendinblueSmsCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueTemplate", PointType = EnumPointType.Function, Name = "CreateTemplate", Caption = "Create Template", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueTemplate> CreateTemplate(SendinblueTemplate template)
        {
            var endpoint = "smtp/templates";
            var response = await PostAsync<SendinblueTemplate>(endpoint, template);
            return response;
        }

        [CommandAttribute(ObjectType = "SendinblueTemplate", PointType = EnumPointType.Function, Name = "UpdateTemplate", Caption = "Update Template", ClassName = "SendinblueDataSource")]
        public async Task<SendinblueTemplate> UpdateTemplate(string templateId, SendinblueTemplate template)
        {
            var endpoint = $"smtp/templates/{templateId}";
            var response = await PutAsync<SendinblueTemplate>(endpoint, template);
            return response;
        }
    }
}