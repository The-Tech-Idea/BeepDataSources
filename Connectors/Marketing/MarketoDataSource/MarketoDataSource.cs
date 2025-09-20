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
using TheTechIdea.Beep.Connectors.Marketing.MarketoDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.Marketo
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Marketo)]
    public class MarketoDataSource : WebAPIDataSource
    {
        // Marketo API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Leads
            ["leads"] = ("rest/v1/leads.json", "result", new[] { "" }),
            ["lead"] = ("rest/v1/lead/{lead_id}.json", "result", new[] { "lead_id" }),
            ["lead_by_email"] = ("rest/v1/leads.json", "result", new[] { "" }),
            ["lead_activities"] = ("rest/v1/activities.json", "result", new[] { "" }),

            // Lists
            ["static_lists"] = ("rest/v1/lists.json", "result", new[] { "" }),
            ["static_list"] = ("rest/v1/lists/{list_id}.json", "result", new[] { "list_id" }),
            ["list_leads"] = ("rest/v1/lists/{list_id}/leads.json", "result", new[] { "list_id" }),

            // Smart Lists
            ["smart_lists"] = ("rest/v1/smartLists.json", "result", new[] { "" }),
            ["smart_list"] = ("rest/v1/smartLists/{smart_list_id}.json", "result", new[] { "smart_list_id" }),

            // Smart Campaigns
            ["smart_campaigns"] = ("rest/v1/campaigns.json", "result", new[] { "" }),
            ["smart_campaign"] = ("rest/v1/campaigns/{campaign_id}.json", "result", new[] { "campaign_id" }),
            ["campaign_schedule"] = ("rest/v1/campaigns/{campaign_id}/schedule.json", "result", new[] { "campaign_id" }),

            // Programs
            ["programs"] = ("rest/v1/programs.json", "result", new[] { "" }),
            ["program"] = ("rest/v1/programs/{program_id}.json", "result", new[] { "program_id" }),

            // Email Templates
            ["email_templates"] = ("rest/v1/emailTemplates.json", "result", new[] { "" }),
            ["email_template"] = ("rest/v1/emailTemplates/{template_id}.json", "result", new[] { "template_id" }),

            // Emails
            ["emails"] = ("rest/v1/emails.json", "result", new[] { "" }),
            ["email"] = ("rest/v1/emails/{email_id}.json", "result", new[] { "email_id" }),
            ["email_content"] = ("rest/v1/emails/{email_id}/content.json", "result", new[] { "email_id" }),

            // Landing Pages
            ["landing_pages"] = ("rest/v1/landingPages.json", "result", new[] { "" }),
            ["landing_page"] = ("rest/v1/landingPages/{page_id}.json", "result", new[] { "page_id" }),
            ["landing_page_content"] = ("rest/v1/landingPages/{page_id}/content.json", "result", new[] { "page_id" }),

            // Forms
            ["forms"] = ("rest/v1/forms.json", "result", new[] { "" }),
            ["form"] = ("rest/v1/forms/{form_id}.json", "result", new[] { "form_id" }),
            ["form_fields"] = ("rest/v1/forms/{form_id}/fields.json", "result", new[] { "form_id" }),

            // Opportunities
            ["opportunities"] = ("rest/v1/opportunities.json", "result", new[] { "" }),
            ["opportunity"] = ("rest/v1/opportunities/{opportunity_id}.json", "result", new[] { "opportunity_id" }),
            ["opportunity_roles"] = ("rest/v1/opportunities/{opportunity_id}/roles.json", "result", new[] { "opportunity_id" }),

            // Companies
            ["companies"] = ("rest/v1/companies.json", "result", new[] { "" }),
            ["company"] = ("rest/v1/companies/{company_id}.json", "result", new[] { "company_id" }),

            // Custom Objects
            ["custom_objects"] = ("rest/v1/customobjects.json", "result", new[] { "" }),
            ["custom_object"] = ("rest/v1/customobjects/{custom_object_name}.json", "result", new[] { "custom_object_name" }),

            // Segments
            ["segments"] = ("rest/v1/segments.json", "result", new[] { "" }),
            ["segment"] = ("rest/v1/segments/{segment_id}.json", "result", new[] { "segment_id" }),

            // Folders
            ["folders"] = ("rest/v1/folders.json", "result", new[] { "" }),
            ["folder"] = ("rest/v1/folders/{folder_id}.json", "result", new[] { "folder_id" }),

            // Tokens
            ["tokens"] = ("rest/v1/tokens.json", "result", new[] { "" }),

            // Activity Types
            ["activity_types"] = ("rest/v1/activities/types.json", "result", new[] { "" }),

            // Statistics
            ["stats"] = ("rest/v1/stats.json", "", new[] { "" }),

            // Usage
            ["usage"] = ("rest/v1/stats/usage.json", "result", new[] { "" })
        };

        public MarketoDataSource(
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
                throw new ArgumentException($"Entity '{entityName}' not found in Marketo API map");
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
                return new List<object>();
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
    }
}