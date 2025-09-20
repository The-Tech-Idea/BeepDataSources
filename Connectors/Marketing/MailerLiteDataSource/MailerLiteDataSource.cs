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
using TheTechIdea.Beep.Connectors.Marketing.MailerLiteDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.MailerLite
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MailerLite)]
    public class MailerLiteDataSource : WebAPIDataSource
    {
        // MailerLite API Map with endpoints, roots, and required filters
        public static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map = new()
        {
            // Subscribers
            ["subscribers"] = ("api/v2/subscribers", "subscribers", new[] { "" }),
            ["subscriber"] = ("api/v2/subscribers/{subscriber_id}", "", new[] { "subscriber_id" }),
            ["subscriber_groups"] = ("api/v2/subscribers/{subscriber_id}/groups", "groups", new[] { "subscriber_id" }),
            ["subscriber_activity"] = ("api/v2/subscribers/{subscriber_id}/activity", "activity", new[] { "subscriber_id" }),

            // Groups
            ["groups"] = ("api/v2/groups", "groups", new[] { "" }),
            ["group"] = ("api/v2/groups/{group_id}", "", new[] { "group_id" }),
            ["group_subscribers"] = ("api/v2/groups/{group_id}/subscribers", "subscribers", new[] { "group_id" }),

            // Campaigns
            ["campaigns"] = ("api/v2/campaigns", "campaigns", new[] { "" }),
            ["campaign"] = ("api/v2/campaigns/{campaign_id}", "", new[] { "campaign_id" }),
            ["campaign_recipients"] = ("api/v2/campaigns/{campaign_id}/recipients", "recipients", new[] { "campaign_id" }),
            ["campaign_opens"] = ("api/v2/campaigns/{campaign_id}/opens", "opens", new[] { "campaign_id" }),
            ["campaign_clicks"] = ("api/v2/campaigns/{campaign_id}/clicks", "clicks", new[] { "campaign_id" }),
            ["campaign_unsubscribes"] = ("api/v2/campaigns/{campaign_id}/unsubscribes", "unsubscribes", new[] { "campaign_id" }),

            // Forms
            ["forms"] = ("api/v2/forms", "forms", new[] { "" }),
            ["form"] = ("api/v2/forms/{form_id}", "", new[] { "form_id" }),
            ["form_subscribers"] = ("api/v2/forms/{form_id}/subscribers", "subscribers", new[] { "form_id" }),

            // Segments
            ["segments"] = ("api/v2/segments", "segments", new[] { "" }),
            ["segment"] = ("api/v2/segments/{segment_id}", "", new[] { "segment_id" }),
            ["segment_subscribers"] = ("api/v2/segments/{segment_id}/subscribers", "subscribers", new[] { "segment_id" }),

            // Automations
            ["automations"] = ("api/v2/automations", "automations", new[] { "" }),
            ["automation"] = ("api/v2/automations/{automation_id}", "", new[] { "automation_id" }),
            ["automation_triggers"] = ("api/v2/automations/{automation_id}/triggers", "triggers", new[] { "automation_id" }),
            ["automation_actions"] = ("api/v2/automations/{automation_id}/actions", "actions", new[] { "automation_id" }),

            // Websites
            ["websites"] = ("api/v2/websites", "websites", new[] { "" }),
            ["website"] = ("api/v2/websites/{website_id}", "", new[] { "website_id" }),
            ["website_subscribers"] = ("api/v2/websites/{website_id}/subscribers", "subscribers", new[] { "website_id" }),

            // E-commerce
            ["ecommerce_customers"] = ("api/v2/ecommerce/customers", "customers", new[] { "" }),
            ["ecommerce_customer"] = ("api/v2/ecommerce/customers/{customer_id}", "", new[] { "customer_id" }),
            ["ecommerce_orders"] = ("api/v2/ecommerce/orders", "orders", new[] { "" }),
            ["ecommerce_order"] = ("api/v2/ecommerce/orders/{order_id}", "", new[] { "order_id" }),
            ["ecommerce_products"] = ("api/v2/ecommerce/products", "products", new[] { "" }),
            ["ecommerce_product"] = ("api/v2/ecommerce/products/{product_id}", "", new[] { "product_id" }),

            // Timezones
            ["timezones"] = ("api/v2/timezones", "timezones", new[] { "" }),

            // Account
            ["account"] = ("api/v2/me", "", new[] { "" }),

            // Stats
            ["stats"] = ("api/v2/stats", "", new[] { "" }),

            // Batch operations
            ["batch_subscribers"] = ("api/v2/batch/subscribers", "", new[] { "" }),
            ["batch_unsubscribers"] = ("api/v2/batch/unsubscribers", "", new[] { "" })
        };

        public MailerLiteDataSource(
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
                throw new ArgumentException($"Entity '{entityName}' not found in MailerLite API map");
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