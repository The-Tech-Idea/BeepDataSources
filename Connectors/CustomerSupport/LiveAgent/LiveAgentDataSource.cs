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

namespace TheTechIdea.Beep.Connectors.LiveAgent
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent)]
    public class LiveAgentDataSource : WebAPIDataSource
    {
        public LiveAgentDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        private record EntityMapping(string endpoint, string root, string[] requiredFilters);

        private static readonly Dictionary<string, EntityMapping> Map = new()
        {
            ["tickets"] = new("/api/v3/tickets", "tickets", Array.Empty<string>()),
            ["chats"] = new("/api/v3/chats", "chats", Array.Empty<string>()),
            ["calls"] = new("/api/v3/calls", "calls", Array.Empty<string>()),
            ["customers"] = new("/api/v3/customers", "customers", Array.Empty<string>()),
            ["agents"] = new("/api/v3/agents", "agents", Array.Empty<string>()),
            ["departments"] = new("/api/v3/departments", "departments", Array.Empty<string>()),
            ["messages"] = new("/api/v3/conversations/{conversationId}/messages", "messages", new[] { "conversationId" }),
            ["conversations"] = new("/api/v3/conversations", "conversations", Array.Empty<string>()),
        };

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).GetAwaiter().GetResult();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown LiveAgent entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (LiveAgent uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown LiveAgent entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // LiveAgent pagination
            q["offset"] = ((pageNumber - 1) * pageSize).ToString();
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
                TotalPages = 1, // LiveAgent doesn't provide total count in response
                TotalRecords = data.Count()
            };
        }

        private void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrEmpty(q[r])).ToArray();
            if (missing.Length > 0)
                throw new ArgumentException($"LiveAgent entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

        private string MapJsonType(JsonValueKind kind) => kind switch
        {
            JsonValueKind.String => "System.String",
            JsonValueKind.Number => "System.Decimal",
            JsonValueKind.True or JsonValueKind.False => "System.Boolean",
            JsonValueKind.Array => "System.Object[]",
            JsonValueKind.Object => "System.Object",
            _ => "System.String"
        };

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return query;

            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName)) continue;
                query[filter.FieldName.Trim()] = filter.FilterValue ?? string.Empty;
            }
            return query;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentTicket", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentTicket>")]
        public IEnumerable<object> GetTickets(List<AppFilter> filter = null) => GetEntity("tickets", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentChat", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentChat>")]
        public IEnumerable<object> GetChats(List<AppFilter> filter = null) => GetEntity("chats", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentCall", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentCall>")]
        public IEnumerable<object> GetCalls(List<AppFilter> filter = null) => GetEntity("calls", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentCustomer", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentCustomer>")]
        public IEnumerable<object> GetCustomers(List<AppFilter> filter = null) => GetEntity("customers", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentAgent", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentAgent>")]
        public IEnumerable<object> GetAgents(List<AppFilter> filter = null) => GetEntity("agents", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentDepartment", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentDepartment>")]
        public IEnumerable<object> GetDepartments(List<AppFilter> filter = null) => GetEntity("departments", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentMessage", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentMessage>")]
        public IEnumerable<object> GetMessages(string conversationId, List<AppFilter> filter = null)
        {
            filter ??= new List<AppFilter>();
            filter.Add(new AppFilter { FieldName = "conversationId", FilterValue = conversationId });
            return GetEntity("messages", filter);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentConversation", ClassName = "LiveAgentDataSource", Showin = ShowinType.Both, misc = "List<LiveAgentConversation>")]
        public IEnumerable<object> GetConversations(List<AppFilter> filter = null) => GetEntity("conversations", filter ?? new List<AppFilter>());

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentTicket", Name = "CreateTicket", Caption = "Create LiveAgent Ticket", ClassType = "LiveAgentDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "liveagent.png", misc = "ReturnType: IEnumerable<LiveAgentTicket>")]
        public async Task<IEnumerable<LiveAgentTicket>> CreateTicketAsync(LiveAgentTicket ticket)
        {
            try
            {
                var result = await PostAsync("api/v3/tickets", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTicket = JsonSerializer.Deserialize<LiveAgentTicket>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<LiveAgentTicket> { createdTicket }.Select(t => t.Attach<LiveAgentTicket>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating ticket: {ex.Message}");
            }
            return new List<LiveAgentTicket>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LiveAgent, PointType = EnumPointType.Function, ObjectType = "LiveAgentTicket", Name = "UpdateTicket", Caption = "Update LiveAgent Ticket", ClassType = "LiveAgentDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "liveagent.png", misc = "ReturnType: IEnumerable<LiveAgentTicket>")]
        public async Task<IEnumerable<LiveAgentTicket>> UpdateTicketAsync(LiveAgentTicket ticket)
        {
            try
            {
                var result = await PutAsync($"api/v3/tickets/{ticket.Id}", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTicket = JsonSerializer.Deserialize<LiveAgentTicket>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<LiveAgentTicket> { updatedTicket }.Select(t => t.Attach<LiveAgentTicket>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating ticket: {ex.Message}");
            }
            return new List<LiveAgentTicket>();
        }
    }
}
