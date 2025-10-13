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
using TheTechIdea.Beep.Connectors.HubSpot.Models;

namespace TheTechIdea.Beep.Connectors.HubSpot
{
    /// <summary>
    /// HubSpot data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot)]
    public class HubSpotDataSource : WebAPIDataSource
    {
        // -------- Fixed, known entities (HubSpot CRM API v3) --------
        private static readonly List<string> KnownEntities = new()
        {
            "contacts",
            "companies",
            "deals",
            "tickets",
            "products",
            "line_items",
            "quotes",
            "owners",
            "pipelines.deals",
            "properties.contacts"
        };

        // entity -> (endpoint template, root path, required filter keys)
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["contacts"] = ("/crm/v3/objects/contacts", "results", Array.Empty<string>()),
                ["companies"] = ("/crm/v3/objects/companies", "results", Array.Empty<string>()),
                ["deals"] = ("/crm/v3/objects/deals", "results", Array.Empty<string>()),
                ["tickets"] = ("/crm/v3/objects/tickets", "results", Array.Empty<string>()),
                ["products"] = ("/crm/v3/objects/products", "results", Array.Empty<string>()),
                ["line_items"] = ("/crm/v3/objects/line_items", "results", Array.Empty<string>()),
                ["quotes"] = ("/crm/v3/objects/quotes", "results", Array.Empty<string>()),
                ["owners"] = ("/crm/v3/owners", "results", Array.Empty<string>()),
                ["pipelines.deals"] = ("/crm/v3/pipelines/deals", "results", Array.Empty<string>()),
                ["properties.contacts"] = ("/crm/v3/properties/contacts", "results", Array.Empty<string>()),
            };

        public HubSpotDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist (API Key configuration is provided externally)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list (use 'new' if base is virtual; otherwise this hides the base)
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
                throw new InvalidOperationException($"Unknown HubSpot entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (HubSpot uses offset-based pagination with "after" parameter)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown HubSpot entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // HubSpot pagination
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();
            if (pageNumber > 1)
            {
                // HubSpot uses "after" token for pagination, simplified to offset calculation
                q["after"] = ((pageNumber - 1) * pageSize).ToString();
            }

            var endpoint = ResolveEndpoint(m.endpoint, q);

            var finalResp = CallHubSpot(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(finalResp, m.root);

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = items.Count, // HubSpot doesn't provide total count in standard response
                TotalPages = pageNumber, // Conservative estimate
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more data if we got a full page
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
                q[f.FieldName] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"HubSpot entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute any {parameter} placeholders from filters if present
            foreach (var kvp in q)
            {
                if (template.Contains($"{{{kvp.Key}}}", StringComparison.Ordinal))
                {
                    template = template.Replace($"{{{kvp.Key}}}", Uri.EscapeDataString(kvp.Value));
                }
            }
            return template;
        }

        private async Task<HttpResponseMessage> CallHubSpot(string endpoint, Dictionary<string, string> query, CancellationToken ct = default)
            => await GetAsync(endpoint, query, cancellationToken: ct).ConfigureAwait(false);

        // Extracts "results" (array) into a List<object> (Dictionary<string,object> per item).
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
                    return list; // no "results" -> empty
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


        // ------------------------------------------------------------------
        // COMMAND ATTRIBUTE METHODS - Strongly typed HubSpot CRM operations
        // ------------------------------------------------------------------

        [CommandAttribute(Name = "GetContacts", Caption = "Get HubSpot Contacts", ObjectType = "Contact", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Contact", Showin = ShowinType.Both, Order = 1, iconimage = "contact.png")]
        public async Task<IEnumerable<Contact>> GetContacts(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("contacts", filters ?? new List<AppFilter>());
            return result.Cast<Contact>().Select(c => c.Attach<Contact>(this));
        }

        [CommandAttribute(Name = "GetCompanies", Caption = "Get HubSpot Companies", ObjectType = "Company", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Company", Showin = ShowinType.Both, Order = 2, iconimage = "company.png")]
        public async Task<IEnumerable<Company>> GetCompanies(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("companies", filters ?? new List<AppFilter>());
            return result.Cast<Company>().Select(c => c.Attach<Company>(this));
        }

        [CommandAttribute(Name = "GetDeals", Caption = "Get HubSpot Deals", ObjectType = "Deal", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Deal", Showin = ShowinType.Both, Order = 3, iconimage = "deal.png")]
        public async Task<IEnumerable<Deal>> GetDeals(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("deals", filters ?? new List<AppFilter>());
            return result.Cast<Deal>().Select(d => d.Attach<Deal>(this));
        }

        [CommandAttribute(Name = "GetTickets", Caption = "Get HubSpot Tickets", ObjectType = "Ticket", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Ticket", Showin = ShowinType.Both, Order = 4, iconimage = "ticket.png")]
        public async Task<IEnumerable<Ticket>> GetTickets(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("tickets", filters ?? new List<AppFilter>());
            return result.Cast<Ticket>().Select(t => t.Attach<Ticket>(this));
        }

        [CommandAttribute(Name = "GetProducts", Caption = "Get HubSpot Products", ObjectType = "Product", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Product", Showin = ShowinType.Both, Order = 5, iconimage = "product.png")]
        public async Task<IEnumerable<Product>> GetProducts(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("products", filters ?? new List<AppFilter>());
            return result.Cast<Product>().Select(p => p.Attach<Product>(this));
        }

        [CommandAttribute(Name = "GetLineItems", Caption = "Get HubSpot Line Items", ObjectType = "LineItem", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "LineItem", Showin = ShowinType.Both, Order = 6, iconimage = "lineitem.png")]
        public async Task<IEnumerable<LineItem>> GetLineItems(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("line_items", filters ?? new List<AppFilter>());
            return result.Cast<LineItem>().Select(l => l.Attach<LineItem>(this));
        }

        [CommandAttribute(Name = "GetQuotes", Caption = "Get HubSpot Quotes", ObjectType = "Quote", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Quote", Showin = ShowinType.Both, Order = 7, iconimage = "quote.png")]
        public async Task<IEnumerable<Quote>> GetQuotes(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("quotes", filters ?? new List<AppFilter>());
            return result.Cast<Quote>().Select(q => q.Attach<Quote>(this));
        }

        [CommandAttribute(Name = "GetOwners", Caption = "Get HubSpot Owners", ObjectType = "Owner", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Owner", Showin = ShowinType.Both, Order = 8, iconimage = "owner.png")]
        public async Task<IEnumerable<Owner>> GetOwners(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("owners", filters ?? new List<AppFilter>());
            return result.Cast<Owner>().Select(o => o.Attach(this));
        }

        // -------------------- Create / Update (POST/PATCH) methods --------------------

        [CommandAttribute(Name = "CreateContact", Caption = "Create HubSpot Contact", ObjectType = "Contact", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Contact", Showin = ShowinType.Both, Order = 9, iconimage = "contact.png", misc = "Contact")]
        public async Task<IEnumerable<Contact>> CreateContactAsync(Contact contact)
        {
            if (contact == null) return Array.Empty<Contact>();
            using var resp = await PostAsync("/crm/v3/objects/contacts", contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Contact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Contact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Contact>();
            }
            catch
            {
                return Array.Empty<Contact>();
            }
        }

        [CommandAttribute(Name = "UpdateContact", Caption = "Update HubSpot Contact", ObjectType = "Contact", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Contact", Showin = ShowinType.Both, Order = 10, iconimage = "contact.png", misc = "Contact")]
        public async Task<IEnumerable<Contact>> UpdateContactAsync(string contactId, Contact contact)
        {
            if (string.IsNullOrWhiteSpace(contactId) || contact == null) return Array.Empty<Contact>();
            var endpoint = $"/crm/v3/objects/contacts/{Uri.EscapeDataString(contactId)}";
            using var resp = await PatchAsync(endpoint, contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Contact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Contact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Contact>();
            }
            catch
            {
                return Array.Empty<Contact>();
            }
        }

        [CommandAttribute(Name = "CreateCompany", Caption = "Create HubSpot Company", ObjectType = "Company", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Company", Showin = ShowinType.Both, Order = 11, iconimage = "company.png", misc = "Company")]
        public async Task<IEnumerable<Company>> CreateCompanyAsync(Company company)
        {
            if (company == null) return Array.Empty<Company>();
            using var resp = await PostAsync("/crm/v3/objects/companies", company).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Company>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Company>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Company>();
            }
            catch
            {
                return Array.Empty<Company>();
            }
        }

        [CommandAttribute(Name = "UpdateCompany", Caption = "Update HubSpot Company", ObjectType = "Company", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Company", Showin = ShowinType.Both, Order = 12, iconimage = "company.png", misc = "Company")]
        public async Task<IEnumerable<Company>> UpdateCompanyAsync(string companyId, Company company)
        {
            if (string.IsNullOrWhiteSpace(companyId) || company == null) return Array.Empty<Company>();
            var endpoint = $"/crm/v3/objects/companies/{Uri.EscapeDataString(companyId)}";
            using var resp = await PatchAsync(endpoint, company).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Company>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Company>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Company>();
            }
            catch
            {
                return Array.Empty<Company>();
            }
        }

        [CommandAttribute(Name = "CreateDeal", Caption = "Create HubSpot Deal", ObjectType = "Deal", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Deal", Showin = ShowinType.Both, Order = 13, iconimage = "deal.png", misc = "Deal")]
        public async Task<IEnumerable<Deal>> CreateDealAsync(Deal deal)
        {
            if (deal == null) return Array.Empty<Deal>();
            using var resp = await PostAsync("/crm/v3/objects/deals", deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Deal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Deal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Deal>();
            }
            catch
            {
                return Array.Empty<Deal>();
            }
        }

        [CommandAttribute(Name = "UpdateDeal", Caption = "Update HubSpot Deal", ObjectType = "Deal", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Deal", Showin = ShowinType.Both, Order = 14, iconimage = "deal.png", misc = "Deal")]
        public async Task<IEnumerable<Deal>> UpdateDealAsync(string dealId, Deal deal)
        {
            if (string.IsNullOrWhiteSpace(dealId) || deal == null) return Array.Empty<Deal>();
            var endpoint = $"/crm/v3/objects/deals/{Uri.EscapeDataString(dealId)}";
            using var resp = await PatchAsync(endpoint, deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Deal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Deal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Deal>();
            }
            catch
            {
                return Array.Empty<Deal>();
            }
        }

        [CommandAttribute(Name = "CreateTicket", Caption = "Create HubSpot Ticket", ObjectType = "Ticket", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Ticket", Showin = ShowinType.Both, Order = 15, iconimage = "ticket.png", misc = "Ticket")]
        public async Task<IEnumerable<Ticket>> CreateTicketAsync(Ticket ticket)
        {
            if (ticket == null) return Array.Empty<Ticket>();
            using var resp = await PostAsync("/crm/v3/objects/tickets", ticket).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Ticket>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Ticket>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Ticket>();
            }
            catch
            {
                return Array.Empty<Ticket>();
            }
        }

        [CommandAttribute(Name = "UpdateTicket", Caption = "Update HubSpot Ticket", ObjectType = "Ticket", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Ticket", Showin = ShowinType.Both, Order = 16, iconimage = "ticket.png", misc = "Ticket")]
        public async Task<IEnumerable<Ticket>> UpdateTicketAsync(string ticketId, Ticket ticket)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || ticket == null) return Array.Empty<Ticket>();
            var endpoint = $"/crm/v3/objects/tickets/{Uri.EscapeDataString(ticketId)}";
            using var resp = await PatchAsync(endpoint, ticket).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Ticket>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Ticket>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Ticket>();
            }
            catch
            {
                return Array.Empty<Ticket>();
            }
        }

        [CommandAttribute(Name = "CreateProduct", Caption = "Create HubSpot Product", ObjectType = "Product", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Product", Showin = ShowinType.Both, Order = 17, iconimage = "product.png", misc = "Product")]
        public async Task<IEnumerable<Product>> CreateProductAsync(Product product)
        {
            if (product == null) return Array.Empty<Product>();
            using var resp = await PostAsync("/crm/v3/objects/products", product).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Product>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Product>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Product>();
            }
            catch
            {
                return Array.Empty<Product>();
            }
        }

        [CommandAttribute(Name = "UpdateProduct", Caption = "Update HubSpot Product", ObjectType = "Product", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot, ClassType = "Product", Showin = ShowinType.Both, Order = 18, iconimage = "product.png", misc = "Product")]
        public async Task<IEnumerable<Product>> UpdateProductAsync(string productId, Product product)
        {
            if (string.IsNullOrWhiteSpace(productId) || product == null) return Array.Empty<Product>();
            var endpoint = $"/crm/v3/objects/products/{Uri.EscapeDataString(productId)}";
            using var resp = await PatchAsync(endpoint, product).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<Product>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<Product>(json, opts);
                return result != null ? new[] { result } : Array.Empty<Product>();
            }
            catch
            {
                return Array.Empty<Product>();
            }
        }
    }
}