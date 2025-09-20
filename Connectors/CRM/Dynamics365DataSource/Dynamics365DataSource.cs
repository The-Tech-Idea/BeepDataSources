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

namespace TheTechIdea.Beep.Connectors.Dynamics365
{
    /// <summary>
    /// Dynamics 365 data source implementation using WebAPIDataSource as base class
    /// Supports core CRM entities: Account, Contact, Lead, Opportunity, SystemUser, BusinessUnit, Team, Incident, Product
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MicrosoftDynamics365)]
    public class Dynamics365DataSource : WebAPIDataSource
    {
        private const string BaseUrl = "https://yourorg.crm.dynamics.com/api/data/v9.2";

        // Entity endpoints mapping for Dynamics 365 API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core CRM Entities
            ["accounts"] = $"{BaseUrl}/accounts",
            ["account_details"] = $"{BaseUrl}/accounts({{account_id}})",
            ["contacts"] = $"{BaseUrl}/contacts",
            ["contact_details"] = $"{BaseUrl}/contacts({{contact_id}})",
            ["leads"] = $"{BaseUrl}/leads",
            ["lead_details"] = $"{BaseUrl}/leads({{lead_id}})",
            ["opportunities"] = $"{BaseUrl}/opportunities",
            ["opportunity_details"] = $"{BaseUrl}/opportunities({{opportunity_id}})",
            ["systemusers"] = $"{BaseUrl}/systemusers",
            ["systemuser_details"] = $"{BaseUrl}/systemusers({{systemuser_id}})",
            ["businessunits"] = $"{BaseUrl}/businessunits",
            ["businessunit_details"] = $"{BaseUrl}/businessunits({{businessunit_id}})",
            ["teams"] = $"{BaseUrl}/teams",
            ["team_details"] = $"{BaseUrl}/teams({{team_id}})",
            ["incidents"] = $"{BaseUrl}/incidents",
            ["incident_details"] = $"{BaseUrl}/incidents({{incident_id}})",
            ["products"] = $"{BaseUrl}/products",
            ["product_details"] = $"{BaseUrl}/products({{product_id}})",

            // Metadata endpoints
            ["entity_metadata"] = $"{BaseUrl}/EntityDefinitions",
            ["entity_metadata_details"] = $"{BaseUrl}/EntityDefinitions({{entity_logical_name}})",
            ["attribute_metadata"] = $"{BaseUrl}/EntityDefinitions({{entity_logical_name}})/Attributes",

            // Query endpoints
            ["query_accounts"] = $"{BaseUrl}/accounts?$select=*&$orderby=createdon desc",
            ["query_contacts"] = $"{BaseUrl}/contacts?$select=*&$orderby=createdon desc",
            ["query_leads"] = $"{BaseUrl}/leads?$select=*&$orderby=createdon desc",
            ["query_opportunities"] = $"{BaseUrl}/opportunities?$select=*&$orderby=createdon desc",
            ["query_systemusers"] = $"{BaseUrl}/systemusers?$select=*&$orderby=createdon desc",
            ["query_businessunits"] = $"{BaseUrl}/businessunits?$select=*&$orderby=createdon desc",
            ["query_teams"] = $"{BaseUrl}/teams?$select=*&$orderby=createdon desc",
            ["query_incidents"] = $"{BaseUrl}/incidents?$select=*&$orderby=createdon desc",
            ["query_products"] = $"{BaseUrl}/products?$select=*&$orderby=createdon desc"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // Detail endpoints require ID parameters
            ["account_details"] = new[] { "account_id" },
            ["contact_details"] = new[] { "contact_id" },
            ["lead_details"] = new[] { "lead_id" },
            ["opportunity_details"] = new[] { "opportunity_id" },
            ["systemuser_details"] = new[] { "systemuser_id" },
            ["businessunit_details"] = new[] { "businessunit_id" },
            ["team_details"] = new[] { "team_id" },
            ["incident_details"] = new[] { "incident_id" },
            ["product_details"] = new[] { "product_id" },

            // Metadata endpoints
            ["entity_metadata_details"] = new[] { "entity_logical_name" },
            ["attribute_metadata"] = new[] { "entity_logical_name" },

            // List endpoints don't require filters but can use query parameters
            ["accounts"] = Array.Empty<string>(),
            ["contacts"] = Array.Empty<string>(),
            ["leads"] = Array.Empty<string>(),
            ["opportunities"] = Array.Empty<string>(),
            ["systemusers"] = Array.Empty<string>(),
            ["businessunits"] = Array.Empty<string>(),
            ["teams"] = Array.Empty<string>(),
            ["incidents"] = Array.Empty<string>(),
            ["products"] = Array.Empty<string>(),
            ["entity_metadata"] = Array.Empty<string>(),
            ["query_accounts"] = Array.Empty<string>(),
            ["query_contacts"] = Array.Empty<string>(),
            ["query_leads"] = Array.Empty<string>(),
            ["query_opportunities"] = Array.Empty<string>(),
            ["query_systemusers"] = Array.Empty<string>(),
            ["query_businessunits"] = Array.Empty<string>(),
            ["query_teams"] = Array.Empty<string>(),
            ["query_incidents"] = Array.Empty<string>(),
            ["query_products"] = Array.Empty<string>()
        };

        public Dynamics365DataSource(
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

        private static string GetQueryString()
        {
            // Default query parameters for list operations
            return "?$select=*&$orderby=createdon desc";
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
                throw new InvalidOperationException($"Unknown Dynamics 365 entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "value");
        }

        // Paged (Dynamics 365 uses OData pagination with @odata.nextLink)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Dynamics 365 entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            // Add pagination parameters for Dynamics 365
            q["$top"] = Math.Max(1, Math.Min(pageSize, 5000)).ToString(); // Dynamics 365 max is 5000
            if (pageNumber > 1)
            {
                q["$skip"] = ((pageNumber - 1) * pageSize).ToString();
            }

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);
            var resp = CallDynamics365(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, "value");

            // For Dynamics 365, we don't have total count in the response by default
            // We'd need a separate count query for accurate totals
            int estimatedTotal = pageNumber * pageSize + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = estimatedTotal,
                TotalPages = estimatedTotal / pageSize + (estimatedTotal % pageSize > 0 ? 1 : 0),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // If we got a full page, there might be more
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
                throw new ArgumentException($"Dynamics 365 entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {entity_logical_name}, {account_id}, etc. from filters if present
            var result = template;

            // Handle entity logical name for metadata
            if (result.Contains("{entity_logical_name}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("entity_logical_name", out var entityName) || string.IsNullOrWhiteSpace(entityName))
                    throw new ArgumentException("Missing required 'entity_logical_name' filter for this endpoint.");
                result = result.Replace("{entity_logical_name}", Uri.EscapeDataString(entityName));
            }

            // Handle account_id
            if (result.Contains("{account_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("account_id", out var accountId) || string.IsNullOrWhiteSpace(accountId))
                    throw new ArgumentException("Missing required 'account_id' filter for this endpoint.");
                result = result.Replace("{account_id}", Uri.EscapeDataString(accountId));
            }

            // Handle contact_id
            if (result.Contains("{contact_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("contact_id", out var contactId) || string.IsNullOrWhiteSpace(contactId))
                    throw new ArgumentException("Missing required 'contact_id' filter for this endpoint.");
                result = result.Replace("{contact_id}", Uri.EscapeDataString(contactId));
            }

            // Handle lead_id
            if (result.Contains("{lead_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("lead_id", out var leadId) || string.IsNullOrWhiteSpace(leadId))
                    throw new ArgumentException("Missing required 'lead_id' filter for this endpoint.");
                result = result.Replace("{lead_id}", Uri.EscapeDataString(leadId));
            }

            // Handle opportunity_id
            if (result.Contains("{opportunity_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("opportunity_id", out var opportunityId) || string.IsNullOrWhiteSpace(opportunityId))
                    throw new ArgumentException("Missing required 'opportunity_id' filter for this endpoint.");
                result = result.Replace("{opportunity_id}", Uri.EscapeDataString(opportunityId));
            }

            // Handle systemuser_id
            if (result.Contains("{systemuser_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("systemuser_id", out var systemUserId) || string.IsNullOrWhiteSpace(systemUserId))
                    throw new ArgumentException("Missing required 'systemuser_id' filter for this endpoint.");
                result = result.Replace("{systemuser_id}", Uri.EscapeDataString(systemUserId));
            }

            // Handle businessunit_id
            if (result.Contains("{businessunit_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("businessunit_id", out var businessUnitId) || string.IsNullOrWhiteSpace(businessUnitId))
                    throw new ArgumentException("Missing required 'businessunit_id' filter for this endpoint.");
                result = result.Replace("{businessunit_id}", Uri.EscapeDataString(businessUnitId));
            }

            // Handle team_id
            if (result.Contains("{team_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("team_id", out var teamId) || string.IsNullOrWhiteSpace(teamId))
                    throw new ArgumentException("Missing required 'team_id' filter for this endpoint.");
                result = result.Replace("{team_id}", Uri.EscapeDataString(teamId));
            }

            // Handle incident_id
            if (result.Contains("{incident_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("incident_id", out var incidentId) || string.IsNullOrWhiteSpace(incidentId))
                    throw new ArgumentException("Missing required 'incident_id' filter for this endpoint.");
                result = result.Replace("{incident_id}", Uri.EscapeDataString(incidentId));
            }

            // Handle product_id
            if (result.Contains("{product_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("product_id", out var productId) || string.IsNullOrWhiteSpace(productId))
                    throw new ArgumentException("Missing required 'product_id' filter for this endpoint.");
                result = result.Replace("{product_id}", Uri.EscapeDataString(productId));
            }

            return result;
        }

        private async Task<HttpResponseMessage> CallDynamics365(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "value" (array) into a List<object> (Dictionary<string,object> per item).
        // If root is null, wraps whole payload as a single object.
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
                    return list; // no "value" -> empty
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