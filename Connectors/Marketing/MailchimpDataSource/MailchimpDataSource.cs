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
using TheTechIdea.Beep.Connectors.Marketing.MailchimpDataSource.Models;

namespace TheTechIdea.Beep.Connectors.Marketing.Mailchimp
{
    /// <summary>
    /// Mailchimp data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Mailchimp)]
    public class MailchimpDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Mailchimp API v3
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Lists
            ["lists"] = "lists",
            ["list"] = "lists/{list_id}",
            ["list_members"] = "lists/{list_id}/members",
            ["list_member"] = "lists/{list_id}/members/{subscriber_hash}",
            ["list_merge_fields"] = "lists/{list_id}/merge-fields",
            ["list_segments"] = "lists/{list_id}/segments",
            ["list_segment"] = "lists/{list_id}/segments/{segment_id}",
            ["list_segment_members"] = "lists/{list_id}/segments/{segment_id}/members",
            ["list_interest_categories"] = "lists/{list_id}/interest-categories",
            ["list_interest_category"] = "lists/{list_id}/interest-categories/{interest_category_id}",
            ["list_interests"] = "lists/{list_id}/interest-categories/{interest_category_id}/interests",

            // Campaigns
            ["campaigns"] = "campaigns",
            ["campaign"] = "campaigns/{campaign_id}",
            ["campaign_content"] = "campaigns/{campaign_id}/content",
            ["campaign_send_checklist"] = "campaigns/{campaign_id}/send-checklist",

            // Templates
            ["templates"] = "templates",
            ["template"] = "templates/{template_id}",
            ["template_default_content"] = "templates/{template_id}/default-content",

            // Automations
            ["automations"] = "automations",
            ["automation"] = "automations/{workflow_id}",
            ["automation_emails"] = "automations/{workflow_id}/emails",
            ["automation_email"] = "automations/{workflow_id}/emails/{email_id}",
            ["automation_removed_subscribers"] = "automations/{workflow_id}/removed-subscribers",
            ["automation_recipients"] = "automations/{workflow_id}/recipients",

            // Reports
            ["reports"] = "reports",
            ["campaign_report"] = "reports/{campaign_id}",
            ["campaign_open_details"] = "reports/{campaign_id}/open-details",
            ["campaign_click_details"] = "reports/{campaign_id}/click-details",
            ["campaign_email_activity"] = "reports/{campaign_id}/email-activity",
            ["campaign_unsubscribed"] = "reports/{campaign_id}/unsubscribed",

            // E-commerce
            ["stores"] = "ecommerce/stores",
            ["store"] = "ecommerce/stores/{store_id}",
            ["store_products"] = "ecommerce/stores/{store_id}/products",
            ["store_product"] = "ecommerce/stores/{store_id}/products/{product_id}",
            ["store_carts"] = "ecommerce/stores/{store_id}/carts",
            ["store_cart"] = "ecommerce/stores/{store_id}/carts/{cart_id}",
            ["store_orders"] = "ecommerce/stores/{store_id}/orders",
            ["store_order"] = "ecommerce/stores/{store_id}/orders/{order_id}",
            ["store_customers"] = "ecommerce/stores/{store_id}/customers",

            // File Manager
            ["files"] = "file-manager/files",
            ["file"] = "file-manager/files/{file_id}",
            ["folders"] = "file-manager/folders",
            ["folder"] = "file-manager/folders/{folder_id}",

            // Conversations
            ["conversations"] = "conversations",
            ["conversation"] = "conversations/{conversation_id}",
            ["conversation_messages"] = "conversations/{conversation_id}/messages",

            // Authorized Apps
            ["authorized_apps"] = "authorized-apps",
            ["authorized_app"] = "authorized-apps/{app_id}",

            // Connected Sites
            ["connected_sites"] = "connected-sites",
            ["connected_site"] = "connected-sites/{site_id}",

            // Batch Operations
            ["batches"] = "batches",
            ["batch"] = "batches/{batch_id}",

            // Account
            ["account"] = "",
            ["account_activity"] = "activity-feed"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            ["list"] = new[] { "list_id" },
            ["list_members"] = new[] { "list_id" },
            ["list_member"] = new[] { "list_id", "subscriber_hash" },
            ["list_merge_fields"] = new[] { "list_id" },
            ["list_segments"] = new[] { "list_id" },
            ["list_segment"] = new[] { "list_id", "segment_id" },
            ["list_segment_members"] = new[] { "list_id", "segment_id" },
            ["list_interest_categories"] = new[] { "list_id" },
            ["list_interest_category"] = new[] { "list_id", "interest_category_id" },
            ["list_interests"] = new[] { "list_id", "interest_category_id" },
            ["campaign"] = new[] { "campaign_id" },
            ["campaign_content"] = new[] { "campaign_id" },
            ["campaign_send_checklist"] = new[] { "campaign_id" },
            ["template"] = new[] { "template_id" },
            ["template_default_content"] = new[] { "template_id" },
            ["automation"] = new[] { "workflow_id" },
            ["automation_emails"] = new[] { "workflow_id" },
            ["automation_email"] = new[] { "workflow_id", "email_id" },
            ["automation_removed_subscribers"] = new[] { "workflow_id" },
            ["automation_recipients"] = new[] { "workflow_id" },
            ["campaign_report"] = new[] { "campaign_id" },
            ["campaign_open_details"] = new[] { "campaign_id" },
            ["campaign_click_details"] = new[] { "campaign_id" },
            ["campaign_email_activity"] = new[] { "campaign_id" },
            ["campaign_unsubscribed"] = new[] { "campaign_id" },
            ["store"] = new[] { "store_id" },
            ["store_products"] = new[] { "store_id" },
            ["store_product"] = new[] { "store_id", "product_id" },
            ["store_carts"] = new[] { "store_id" },
            ["store_cart"] = new[] { "store_id", "cart_id" },
            ["store_orders"] = new[] { "store_id" },
            ["store_order"] = new[] { "store_id", "order_id" },
            ["store_customers"] = new[] { "store_id" },
            ["file"] = new[] { "file_id" },
            ["folder"] = new[] { "folder_id" },
            ["conversation"] = new[] { "conversation_id" },
            ["conversation_messages"] = new[] { "conversation_id" },
            ["authorized_app"] = new[] { "app_id" },
            ["connected_site"] = new[] { "site_id" },
            ["batch"] = new[] { "batch_id" }
        };

        public MailchimpDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
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
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Mailchimp entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, GetRootElement(EntityName));
        }

        // Paged (Mailchimp uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Mailchimp entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Mailchimp uses offset-based pagination
            int offset = (pageNumber - 1) * pageSize;
            q["offset"] = offset.ToString();
            q["count"] = Math.Max(10, Math.Min(pageSize, 1000)).ToString();

            string resolvedEndpoint = ResolveEndpoint(endpoint, q);

            var resp = CallMailchimp(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, GetRootElement(EntityName));

            // Try to get total count from response
            int totalCount = GetTotalCount(resp) ?? items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = offset + items.Count < totalCount
            };
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
                throw new ArgumentException($"Mailchimp entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute path parameters from filters
            var resolved = template;

            // Handle list_id
            if (resolved.Contains("{list_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("list_id", out var listId) || string.IsNullOrWhiteSpace(listId))
                    throw new ArgumentException("Missing required 'list_id' filter for this endpoint.");
                resolved = resolved.Replace("{list_id}", Uri.EscapeDataString(listId));
            }

            // Handle campaign_id
            if (resolved.Contains("{campaign_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("campaign_id", out var campaignId) || string.IsNullOrWhiteSpace(campaignId))
                    throw new ArgumentException("Missing required 'campaign_id' filter for this endpoint.");
                resolved = resolved.Replace("{campaign_id}", Uri.EscapeDataString(campaignId));
            }

            // Handle workflow_id
            if (resolved.Contains("{workflow_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("workflow_id", out var workflowId) || string.IsNullOrWhiteSpace(workflowId))
                    throw new ArgumentException("Missing required 'workflow_id' filter for this endpoint.");
                resolved = resolved.Replace("{workflow_id}", Uri.EscapeDataString(workflowId));
            }

            // Handle email_id
            if (resolved.Contains("{email_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("email_id", out var emailId) || string.IsNullOrWhiteSpace(emailId))
                    throw new ArgumentException("Missing required 'email_id' filter for this endpoint.");
                resolved = resolved.Replace("{email_id}", Uri.EscapeDataString(emailId));
            }

            // Handle segment_id
            if (resolved.Contains("{segment_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("segment_id", out var segmentId) || string.IsNullOrWhiteSpace(segmentId))
                    throw new ArgumentException("Missing required 'segment_id' filter for this endpoint.");
                resolved = resolved.Replace("{segment_id}", Uri.EscapeDataString(segmentId));
            }

            // Handle interest_category_id
            if (resolved.Contains("{interest_category_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("interest_category_id", out var categoryId) || string.IsNullOrWhiteSpace(categoryId))
                    throw new ArgumentException("Missing required 'interest_category_id' filter for this endpoint.");
                resolved = resolved.Replace("{interest_category_id}", Uri.EscapeDataString(categoryId));
            }

            // Handle subscriber_hash
            if (resolved.Contains("{subscriber_hash}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("subscriber_hash", out var subscriberHash) || string.IsNullOrWhiteSpace(subscriberHash))
                    throw new ArgumentException("Missing required 'subscriber_hash' filter for this endpoint.");
                resolved = resolved.Replace("{subscriber_hash}", Uri.EscapeDataString(subscriberHash));
            }

            // Handle store_id
            if (resolved.Contains("{store_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("store_id", out var storeId) || string.IsNullOrWhiteSpace(storeId))
                    throw new ArgumentException("Missing required 'store_id' filter for this endpoint.");
                resolved = resolved.Replace("{store_id}", Uri.EscapeDataString(storeId));
            }

            // Handle product_id
            if (resolved.Contains("{product_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("product_id", out var productId) || string.IsNullOrWhiteSpace(productId))
                    throw new ArgumentException("Missing required 'product_id' filter for this endpoint.");
                resolved = resolved.Replace("{product_id}", Uri.EscapeDataString(productId));
            }

            // Handle cart_id
            if (resolved.Contains("{cart_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("cart_id", out var cartId) || string.IsNullOrWhiteSpace(cartId))
                    throw new ArgumentException("Missing required 'cart_id' filter for this endpoint.");
                resolved = resolved.Replace("{cart_id}", Uri.EscapeDataString(cartId));
            }

            // Handle order_id
            if (resolved.Contains("{order_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("order_id", out var orderId) || string.IsNullOrWhiteSpace(orderId))
                    throw new ArgumentException("Missing required 'order_id' filter for this endpoint.");
                resolved = resolved.Replace("{order_id}", Uri.EscapeDataString(orderId));
            }

            // Handle template_id
            if (resolved.Contains("{template_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("template_id", out var templateId) || string.IsNullOrWhiteSpace(templateId))
                    throw new ArgumentException("Missing required 'template_id' filter for this endpoint.");
                resolved = resolved.Replace("{template_id}", Uri.EscapeDataString(templateId));
            }

            // Handle file_id
            if (resolved.Contains("{file_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("file_id", out var fileId) || string.IsNullOrWhiteSpace(fileId))
                    throw new ArgumentException("Missing required 'file_id' filter for this endpoint.");
                resolved = resolved.Replace("{file_id}", Uri.EscapeDataString(fileId));
            }

            // Handle folder_id
            if (resolved.Contains("{folder_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("folder_id", out var folderId) || string.IsNullOrWhiteSpace(folderId))
                    throw new ArgumentException("Missing required 'folder_id' filter for this endpoint.");
                resolved = resolved.Replace("{folder_id}", Uri.EscapeDataString(folderId));
            }

            // Handle conversation_id
            if (resolved.Contains("{conversation_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("conversation_id", out var conversationId) || string.IsNullOrWhiteSpace(conversationId))
                    throw new ArgumentException("Missing required 'conversation_id' filter for this endpoint.");
                resolved = resolved.Replace("{conversation_id}", Uri.EscapeDataString(conversationId));
            }

            // Handle app_id
            if (resolved.Contains("{app_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("app_id", out var appId) || string.IsNullOrWhiteSpace(appId))
                    throw new ArgumentException("Missing required 'app_id' filter for this endpoint.");
                resolved = resolved.Replace("{app_id}", Uri.EscapeDataString(appId));
            }

            // Handle site_id
            if (resolved.Contains("{site_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("site_id", out var siteId) || string.IsNullOrWhiteSpace(siteId))
                    throw new ArgumentException("Missing required 'site_id' filter for this endpoint.");
                resolved = resolved.Replace("{site_id}", Uri.EscapeDataString(siteId));
            }

            // Handle batch_id
            if (resolved.Contains("{batch_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("batch_id", out var batchId) || string.IsNullOrWhiteSpace(batchId))
                    throw new ArgumentException("Missing required 'batch_id' filter for this endpoint.");
                resolved = resolved.Replace("{batch_id}", Uri.EscapeDataString(batchId));
            }

            return resolved;
        }

        private async Task<HttpResponseMessage> CallMailchimp(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        private static string GetRootElement(string entityName)
        {
            // Mailchimp API response structure varies by endpoint
            return entityName.ToLower() switch
            {
                "lists" => "lists",
                "list" => "",
                "list_members" => "members",
                "list_member" => "",
                "list_merge_fields" => "merge_fields",
                "list_segments" => "segments",
                "list_segment" => "",
                "list_segment_members" => "members",
                "list_interest_categories" => "categories",
                "list_interest_category" => "",
                "list_interests" => "interests",
                "campaigns" => "campaigns",
                "campaign" => "",
                "campaign_content" => "",
                "campaign_send_checklist" => "items",
                "templates" => "templates",
                "template" => "",
                "template_default_content" => "",
                "automations" => "automations",
                "automation" => "",
                "automation_emails" => "emails",
                "automation_email" => "",
                "automation_removed_subscribers" => "subscribers",
                "automation_recipients" => "",
                "reports" => "reports",
                "campaign_report" => "",
                "campaign_open_details" => "members",
                "campaign_click_details" => "urls",
                "campaign_email_activity" => "emails",
                "campaign_unsubscribed" => "unsubscribes",
                "stores" => "stores",
                "store" => "",
                "store_products" => "products",
                "store_product" => "",
                "store_carts" => "carts",
                "store_cart" => "",
                "store_orders" => "orders",
                "store_order" => "",
                "store_customers" => "customers",
                "files" => "files",
                "file" => "",
                "folders" => "folders",
                "folder" => "",
                "conversations" => "conversations",
                "conversation" => "",
                "conversation_messages" => "conversation_messages",
                "authorized_apps" => "apps",
                "authorized_app" => "",
                "connected_sites" => "sites",
                "connected_site" => "",
                "batches" => "batches",
                "batch" => "",
                "account" => "",
                "account_activity" => "activity",
                _ => ""
            };
        }

        private static int? GetTotalCount(HttpResponseMessage resp)
        {
            if (resp == null) return null;
            try
            {
                var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("total_items", out var totalItems) &&
                    totalItems.ValueKind == JsonValueKind.Number)
                {
                    return totalItems.GetInt32();
                }
            }
            catch { /* ignore */ }
            return null;
        }

        // Extracts array or object into a List<object> (Dictionary<string,object> per item).
        // If root is null or empty, wraps whole payload as a single object.
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list; // no root element -> empty
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
        [CommandAttribute(ObjectType = typeof(MailchimpList), PointType = EnumPointType.Function, Name = "GetLists", Caption = "Get Lists", ClassName = "MailchimpDataSource", misc = "GetLists")]
        public IEnumerable<MailchimpList> GetLists()
        {
            return GetEntity("lists", null).Cast<MailchimpList>();
        }

        [CommandAttribute(ObjectType = typeof(MailchimpCampaign), PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "MailchimpDataSource", misc = "GetCampaigns")]
        public IEnumerable<MailchimpCampaign> GetCampaigns()
        {
            return GetEntity("campaigns", null).Cast<MailchimpCampaign>();
        }

        [CommandAttribute(ObjectType = typeof(MailchimpMember), PointType = EnumPointType.Function, Name = "GetMembers", Caption = "Get Members", ClassName = "MailchimpDataSource", misc = "GetMembers")]
        public IEnumerable<MailchimpMember> GetMembers(string listId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "list_id", FilterValue = listId } };
            return GetEntity("list_members", filters).Cast<MailchimpMember>();
        }

        [CommandAttribute(ObjectType = typeof(MailchimpTemplate), PointType = EnumPointType.Function, Name = "GetTemplates", Caption = "Get Templates", ClassName = "MailchimpDataSource", misc = "GetTemplates")]
        public IEnumerable<MailchimpTemplate> GetTemplates()
        {
            return GetEntity("templates", null).Cast<MailchimpTemplate>();
        }

        [CommandAttribute(ObjectType = typeof(MailchimpAutomation), PointType = EnumPointType.Function, Name = "GetAutomations", Caption = "Get Automations", ClassName = "MailchimpDataSource", misc = "GetAutomations")]
        public IEnumerable<MailchimpAutomation> GetAutomations()
        {
            return GetEntity("automations", null).Cast<MailchimpAutomation>();
        }

        [CommandAttribute(ObjectType = typeof(MailchimpReport), PointType = EnumPointType.Function, Name = "GetReports", Caption = "Get Reports", ClassName = "MailchimpDataSource", misc = "GetReports")]
        public IEnumerable<MailchimpReport> GetReports()
        {
            return GetEntity("reports", null).Cast<MailchimpReport>();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType = typeof(MailchimpMember), PointType = EnumPointType.Function, Name = "CreateListMember", Caption = "Create List Member", ClassName = "MailchimpDataSource", misc = "CreateListMember")]
        public async Task<MailchimpMember> CreateListMember(string listId, MailchimpMember member)
        {
            var endpoint = $"lists/{listId}/members";
            var response = await PostAsync<MailchimpMember>(endpoint, member);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpMember), PointType = EnumPointType.Function, Name = "UpdateListMember", Caption = "Update List Member", ClassName = "MailchimpDataSource", misc = "UpdateListMember")]
        public async Task<MailchimpMember> UpdateListMember(string listId, string subscriberHash, MailchimpMember member)
        {
            var endpoint = $"lists/{listId}/members/{subscriberHash}";
            var response = await PutAsync<MailchimpMember>(endpoint, member);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpCampaign), PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "MailchimpDataSource", misc = "CreateCampaign")]
        public async Task<MailchimpCampaign> CreateCampaign(MailchimpCampaign campaign)
        {
            var endpoint = "campaigns";
            var response = await PostAsync<MailchimpCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpCampaign), PointType = EnumPointType.Function, Name = "UpdateCampaign", Caption = "Update Campaign", ClassName = "MailchimpDataSource", misc = "UpdateCampaign")]
        public async Task<MailchimpCampaign> UpdateCampaign(string campaignId, MailchimpCampaign campaign)
        {
            var endpoint = $"campaigns/{campaignId}";
            var response = await PutAsync<MailchimpCampaign>(endpoint, campaign);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpTemplate), PointType = EnumPointType.Function, Name = "CreateTemplate", Caption = "Create Template", ClassName = "MailchimpDataSource", misc = "CreateTemplate")]
        public async Task<MailchimpTemplate> CreateTemplate(MailchimpTemplate template)
        {
            var endpoint = "templates";
            var response = await PostAsync<MailchimpTemplate>(endpoint, template);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpTemplate), PointType = EnumPointType.Function, Name = "UpdateTemplate", Caption = "Update Template", ClassName = "MailchimpDataSource", misc = "UpdateTemplate")]
        public async Task<MailchimpTemplate> UpdateTemplate(string templateId, MailchimpTemplate template)
        {
            var endpoint = $"templates/{templateId}";
            var response = await PutAsync<MailchimpTemplate>(endpoint, template);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpList), PointType = EnumPointType.Function, Name = "CreateList", Caption = "Create List", ClassName = "MailchimpDataSource", misc = "CreateList")]
        public async Task<MailchimpList> CreateList(MailchimpList list)
        {
            var endpoint = "lists";
            var response = await PostAsync<MailchimpList>(endpoint, list);
            return response;
        }

        [CommandAttribute(ObjectType = typeof(MailchimpList), PointType = EnumPointType.Function, Name = "UpdateList", Caption = "Update List", ClassName = "MailchimpDataSource", misc = "UpdateList")]
        public async Task<MailchimpList> UpdateList(string listId, MailchimpList list)
        {
            var endpoint = $"lists/{listId}";
            var response = await PutAsync<MailchimpList>(endpoint, list);
            return response;
        }
    }
}
