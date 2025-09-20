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
using TheTechIdea.Beep.Connectors.Marketing.ActiveCampaignDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.ActiveCampaign
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ActiveCampaign)]
    public class ActiveCampaignDataSource : WebAPIDataSource
    {
        // ActiveCampaign API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Contacts
            ["contacts"] = ("api/3/contacts", "contacts", new[] { "" }),
            ["contact_lists"] = ("api/3/contacts/{contact_id}/contactLists", "contactLists", new[] { "contact_id" }),
            ["contact_tags"] = ("api/3/contacts/{contact_id}/contactTags", "contactTags", new[] { "contact_id" }),
            ["contact_automations"] = ("api/3/contacts/{contact_id}/contactAutomations", "contactAutomations", new[] { "contact_id" }),

            // Lists
            ["lists"] = ("api/3/lists", "lists", new[] { "" }),
            ["list_contacts"] = ("api/3/lists/{list_id}/contacts", "contacts", new[] { "list_id" }),

            // Tags
            ["tags"] = ("api/3/tags", "tags", new[] { "" }),
            ["contact_tags_full"] = ("api/3/contactTags", "contactTags", new[] { "" }),

            // Campaigns
            ["campaigns"] = ("api/3/campaigns", "campaigns", new[] { "" }),
            ["campaign_messages"] = ("api/3/campaigns/{campaign_id}/messages", "campaignMessages", new[] { "campaign_id" }),
            ["campaign_links"] = ("api/3/campaigns/{campaign_id}/links", "links", new[] { "campaign_id" }),

            // Messages
            ["messages"] = ("api/3/messages", "messages", new[] { "" }),

            // Automations
            ["automations"] = ("api/3/automations", "automations", new[] { "" }),
            ["automation_contacts"] = ("api/3/automations/{automation_id}/contacts", "contacts", new[] { "automation_id" }),
            ["automation_blocks"] = ("api/3/automations/{automation_id}/blocks", "blocks", new[] { "automation_id" }),

            // Deals
            ["deals"] = ("api/3/deals", "deals", new[] { "" }),
            ["deal_stages"] = ("api/3/dealStages", "dealStages", new[] { "" }),
            ["deal_groups"] = ("api/3/dealGroups", "dealGroups", new[] { "" }),
            ["deal_tasks"] = ("api/3/deals/{deal_id}/dealTasks", "dealTasks", new[] { "deal_id" }),

            // Accounts
            ["accounts"] = ("api/3/accounts", "accounts", new[] { "" }),
            ["account_contacts"] = ("api/3/accounts/{account_id}/accountContacts", "accountContacts", new[] { "account_id" }),

            // Segments
            ["segments"] = ("api/3/segments", "segments", new[] { "" }),
            ["segment_contacts"] = ("api/3/segments/{segment_id}/contacts", "contacts", new[] { "segment_id" }),

            // Forms
            ["forms"] = ("api/3/forms", "forms", new[] { "" }),

            // Webhooks
            ["webhooks"] = ("api/3/webhooks", "webhooks", new[] { "" }),

            // Users
            ["users"] = ("api/3/users", "users", new[] { "" }),

            // Saved Responses
            ["saved_responses"] = ("api/3/savedResponses", "savedResponses", new[] { "" }),

            // Custom Fields
            ["fields"] = ("api/3/fields", "fields", new[] { "" }),

            // Site & Tracking
            ["site"] = ("api/3/site", "", new[] { "" }),
            ["tracking"] = ("api/3/tracking", "tracking", new[] { "" }),

            // Reports
            ["reports"] = ("api/3/reports", "reports", new[] { "" })
        };

        public ActiveCampaignDataSource(
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
                throw new InvalidOperationException($"Unknown ActiveCampaign entity '{EntityName}'.");

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
                throw new ArgumentException($"ActiveCampaign entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "contact_id", "list_id", "campaign_id", "automation_id", "deal_id", "account_id", "segment_id" })
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
    }
}