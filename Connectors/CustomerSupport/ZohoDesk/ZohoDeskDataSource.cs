using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.AppManager;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.ZohoDesk
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk)]
    public class ZohoDeskDataSource : WebAPIDataSource
    {
        public ZohoDeskDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        private record EntityMapping(string endpoint, string root, string[] requiredFilters);

        private static readonly Dictionary<string, EntityMapping> Map = new()
        {
            ["tickets"] = new("/api/v1/tickets", "data", Array.Empty<string>()),
            ["contacts"] = new("/api/v1/contacts", "data", Array.Empty<string>()),
            ["accounts"] = new("/api/v1/accounts", "data", Array.Empty<string>()),
            ["agents"] = new("/api/v1/agents", "data", Array.Empty<string>()),
            ["departments"] = new("/api/v1/departments", "data", Array.Empty<string>()),
            ["comments"] = new("/api/v1/tickets/{ticketId}/comments", "data", new[] { "ticketId" }),
            ["tasks"] = new("/api/v1/tasks", "data", Array.Empty<string>()),
            ["organizations"] = new("/api/v1/organizations", "data", Array.Empty<string>()),
        };

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).GetAwaiter().GetResult();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoho Desk entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Zoho Desk uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoho Desk entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Zoho Desk pagination
            q["from"] = ((pageNumber - 1) * pageSize).ToString();
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            if (resp is null || !resp.IsSuccessStatusCode) return new PagedResult();

            var data = ExtractArray(resp, m.root);
            return new PagedResult
            {
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 1, // Zoho Desk doesn't provide total count in response
                TotalRecords = data.Count()
            };
        }

        private void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrEmpty(q[r])).ToArray();
            if (missing.Length > 0)
                throw new ArgumentException($"Zoho Desk entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private string ResolveEndpoint(string endpoint, Dictionary<string, string> q)
        {
            var result = endpoint;
            foreach (var (key, value) in q.Where(kv => endpoint.Contains($"{{{kv.Key}}}")))
            {
                result = result.Replace($"{{{key}}}", value);
            }
            return result;
        }

        private IEnumerable<object> ExtractArray(HttpResponseMessage resp, string rootPath)
        {
            var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var doc = JsonDocument.Parse(json);
            var root = GetJsonProperty(doc.RootElement, rootPath.Split('.'));

            if (root.ValueKind != JsonValueKind.Array) return Array.Empty<object>();

            return root.EnumerateArray().Select(item => item.Deserialize<object>());
        }

        private JsonElement GetJsonProperty(JsonElement element, string[] path)
        {
            var current = element;
            foreach (var part in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                    return default;
            }
            return current;
        }

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var query = new Dictionary<string, string>();
            foreach (var filter in filters ?? new List<AppFilter>())
            {
                query[filter.FieldName] = filter.FilterValue;
            }
            return query;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "tickets", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get tickets")]
        public IEnumerable<object> GetTickets(List<AppFilter> filter = null) => GetEntity("tickets", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "contacts", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get contacts")]
        public IEnumerable<object> GetContacts(List<AppFilter> filter = null) => GetEntity("contacts", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "accounts", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get accounts")]
        public IEnumerable<object> GetAccounts(List<AppFilter> filter = null) => GetEntity("accounts", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "agents", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get agents")]
        public IEnumerable<object> GetAgents(List<AppFilter> filter = null) => GetEntity("agents", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "departments", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get departments")]
        public IEnumerable<object> GetDepartments(List<AppFilter> filter = null) => GetEntity("departments", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "comments", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get comments for a ticket")]
        public IEnumerable<object> GetComments(string ticketId, List<AppFilter> filter = null)
        {
            var f = filter ?? new List<AppFilter>();
            f.Add(new AppFilter { FieldName = "ticketId", FilterValue = ticketId });
            return GetEntity("comments", f);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "tasks", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get tasks")]
        public IEnumerable<object> GetTasks(List<AppFilter> filter = null) => GetEntity("tasks", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "organizations", ClassName = "ZohoDeskDataSource", Showin = ShowinType.Both, misc = "Get organizations")]
        public IEnumerable<object> GetOrganizations(List<AppFilter> filter = null) => GetEntity("organizations", filter ?? new List<AppFilter>());

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "tickets", Name = "CreateTicket", Caption = "Create Zoho Desk Ticket", ClassType = "ZohoDeskDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "zohodesk.png", misc = "ReturnType: IEnumerable<tickets>")]
        public async Task<IEnumerable<tickets>> CreateTicketAsync(tickets ticket)
        {
            try
            {
                var result = await PostAsync("api/v1/tickets", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTicket = JsonSerializer.Deserialize<tickets>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<tickets> { createdTicket }.Select(t => t.Attach<tickets>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating ticket: {ex.Message}");
            }
            return new List<tickets>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ZohoDesk, PointType = EnumPointType.Function, ObjectType = "tickets", Name = "UpdateTicket", Caption = "Update Zoho Desk Ticket", ClassType = "ZohoDeskDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "zohodesk.png", misc = "ReturnType: IEnumerable<tickets>")]
        public async Task<IEnumerable<tickets>> UpdateTicketAsync(tickets ticket)
        {
            try
            {
                var result = await PutAsync($"api/v1/tickets/{ticket.Id}", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTicket = JsonSerializer.Deserialize<tickets>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<tickets> { updatedTicket }.Select(t => t.Attach<tickets>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating ticket: {ex.Message}");
            }
            return new List<tickets>();
        }
    }
}
