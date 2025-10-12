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

namespace TheTechIdea.Beep.FreshdeskDataSource
{
    /// <summary>
    /// Freshdesk data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk)]
    public class FreshdeskDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (Freshdesk API v2) --------
        private static readonly List<string> KnownEntities = new()
        {
            // Tickets
            "tickets",              // GET /api/v2/tickets
            "tickets.search",       // GET /api/v2/search/tickets
            // Contacts
            "contacts",             // GET /api/v2/contacts
            "contacts.search",      // GET /api/v2/contacts/autocomplete
            // Companies
            "companies",            // GET /api/v2/companies
            // Agents
            "agents",               // GET /api/v2/agents
            // Groups
            "groups",               // GET /api/v2/groups
            // Products
            "products",             // GET /api/v2/products
            // Conversations
            "conversations",        // GET /api/v2/tickets/{id}/conversations
            // Time Entries
            "time_entries",         // GET /api/v2/time_entries
            // Satisfaction Ratings
            "satisfaction_ratings", // GET /api/v2/surveys/satisfaction_ratings
            // Canned Responses
            "canned_responses",     // GET /api/v2/canned_responses
            // Ticket Fields
            "ticket_fields",        // GET /api/v2/ticket_fields
            // Contact Fields
            "contact_fields",       // GET /api/v2/contact_fields
            // Company Fields
            "company_fields"        // GET /api/v2/company_fields
        };

        // entity -> (endpoint template, root path, required filter keys)
        // endpoint supports {id} substitution taken from filters.
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["tickets"] = ("tickets", "", new string[] { }),
                ["tickets.search"] = ("search/tickets", "results", new[] { "query" }),
                ["contacts"] = ("contacts", "", new string[] { }),
                ["contacts.search"] = ("contacts/autocomplete", "contacts", new[] { "term" }),
                ["companies"] = ("companies", "", new string[] { }),
                ["agents"] = ("agents", "", new string[] { }),
                ["groups"] = ("groups", "", new string[] { }),
                ["products"] = ("products", "", new string[] { }),
                ["conversations"] = ("tickets/{id}/conversations", "", new[] { "id" }),
                ["time_entries"] = ("time_entries", "", new string[] { }),
                ["satisfaction_ratings"] = ("surveys/satisfaction_ratings", "", new string[] { }),
                ["canned_responses"] = ("canned_responses", "", new string[] { }),
                ["ticket_fields"] = ("ticket_fields", "", new string[] { }),
                ["contact_fields"] = ("contact_fields", "", new string[] { }),
                ["company_fields"] = ("company_fields", "", new string[] { })
            };

        public FreshdeskDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (URL/Auth configured outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'override' if base is virtual; otherwise this hides the base)
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
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Freshdesk entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Freshdesk uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Freshdesk entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Freshdesk pagination
            q["per_page"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();
            q["page"] = Math.Max(1, pageNumber).ToString();

            string endpoint = ResolveEndpoint(m.endpoint, q);

            var resp = CallFreshdesk(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

            return new PagedResult
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = items.Count, // Freshdesk doesn't provide total count in all endpoints
                TotalPages = 1, // Simplified
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more if we got full page
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
                throw new ArgumentException($"Freshdesk entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {id} from filters if present
            if (template.Contains("{id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Missing required 'id' filter for this endpoint.");
                template = template.Replace("{id}", Uri.EscapeDataString(id));
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallFreshdesk(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts data from Freshdesk API response
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

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskTicket", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskTicket>")]
        public List<FreshdeskTicket> GetTickets()
        {
            return GetEntity("tickets", new List<AppFilter>()).Cast<FreshdeskTicket>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskTicket", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "FreshdeskTicket")]
        public FreshdeskTicket GetTicket(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("tickets", filters).Cast<FreshdeskTicket>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskTicket", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskTicket>")]
        public List<FreshdeskTicket> SearchTickets(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" } };
            return GetEntity("tickets.search", filters).Cast<FreshdeskTicket>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskContact", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskContact>")]
        public List<FreshdeskContact> GetContacts()
        {
            return GetEntity("contacts", new List<AppFilter>()).Cast<FreshdeskContact>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskContact", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "FreshdeskContact")]
        public FreshdeskContact GetContact(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("contacts", filters).Cast<FreshdeskContact>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskContact", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskContact>")]
        public List<FreshdeskContact> SearchContacts(string term)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "term", FilterValue = term, Operator = "=" } };
            return GetEntity("contacts.search", filters).Cast<FreshdeskContact>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskCompany", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskCompany>")]
        public List<FreshdeskCompany> GetCompanies()
        {
            return GetEntity("companies", new List<AppFilter>()).Cast<FreshdeskCompany>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskCompany", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "FreshdeskCompany")]
        public FreshdeskCompany GetCompany(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("companies", filters).Cast<FreshdeskCompany>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskAgent", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskAgent>")]
        public List<FreshdeskAgent> GetAgents()
        {
            return GetEntity("agents", new List<AppFilter>()).Cast<FreshdeskAgent>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskGroup", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskGroup>")]
        public List<FreshdeskGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<FreshdeskGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskProduct", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskProduct>")]
        public List<FreshdeskProduct> GetProducts()
        {
            return GetEntity("products", new List<AppFilter>()).Cast<FreshdeskProduct>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskConversation", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskConversation>")]
        public List<FreshdeskConversation> GetTicketConversations(long ticketId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = ticketId.ToString(), Operator = "=" } };
            return GetEntity("conversations", filters).Cast<FreshdeskConversation>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskTimeEntry", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskTimeEntry>")]
        public List<FreshdeskTimeEntry> GetTimeEntries()
        {
            return GetEntity("time_entries", new List<AppFilter>()).Cast<FreshdeskTimeEntry>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskSatisfactionRating", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskSatisfactionRating>")]
        public List<FreshdeskSatisfactionRating> GetSatisfactionRatings()
        {
            return GetEntity("satisfaction_ratings", new List<AppFilter>()).Cast<FreshdeskSatisfactionRating>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskCannedResponse", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskCannedResponse>")]
        public List<FreshdeskCannedResponse> GetCannedResponses()
        {
            return GetEntity("canned_responses", new List<AppFilter>()).Cast<FreshdeskCannedResponse>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskTicketField", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskTicketField>")]
        public List<FreshdeskTicketField> GetTicketFields()
        {
            return GetEntity("ticket_fields", new List<AppFilter>()).Cast<FreshdeskTicketField>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskContactField", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskContactField>")]
        public List<FreshdeskContactField> GetContactFields()
        {
            return GetEntity("contact_fields", new List<AppFilter>()).Cast<FreshdeskContactField>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshdesk, PointType = EnumPointType.Function, ObjectType = "FreshdeskCompanyField", ClassName = "FreshdeskDataSource", Showin = ShowinType.Both, misc = "List<FreshdeskCompanyField>")]
        public List<FreshdeskCompanyField> GetCompanyFields()
        {
            return GetEntity("company_fields", new List<AppFilter>()).Cast<FreshdeskCompanyField>().ToList();
        }
    }
}
