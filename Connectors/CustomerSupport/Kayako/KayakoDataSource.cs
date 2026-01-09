using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.DataSources
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako)]
    public class KayakoDataSource : WebAPIDataSource
    {
        public KayakoDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            // Ensure WebAPI connection props exist
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

        private record EntityMapping(string endpoint, string root, string[] requiredFilters);

        private static readonly Dictionary<string, EntityMapping> Map = new()
        {
            ["tickets"] = new("/api/v1/tickets.json", "tickets", Array.Empty<string>()),
            ["users"] = new("/api/v1/users.json", "users", Array.Empty<string>()),
            ["organizations"] = new("/api/v1/organizations.json", "organizations", Array.Empty<string>()),
            ["teams"] = new("/api/v1/teams.json", "teams", Array.Empty<string>()),
            ["comments"] = new("/api/v1/tickets/{ticketId}/posts.json", "posts", new[] { "ticketId" }),
            ["knowledgebase"] = new("/api/v1/helpcenter/articles.json", "articles", Array.Empty<string>()),
            ["departments"] = new("/api/v1/departments.json", "departments", Array.Empty<string>()),
            ["conversations"] = new("/api/v1/conversations.json", "conversations", Array.Empty<string>()),
        };

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).GetAwaiter().GetResult();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Kayako entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Kayako uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Kayako entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Kayako pagination
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
                TotalPages = 1, // Kayako doesn't provide total count in response
                TotalRecords = data.Count()
            };
        }

        private void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrEmpty(q[r])).ToArray();
            if (missing.Length > 0)
                throw new ArgumentException($"Kayako entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return query;

            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName)) continue;
                query[filter.FieldName.Trim()] = filter.FilterValue ?? string.Empty;
            }
            return query;
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoTicket", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoTicket>")]
        public IEnumerable<object> GetTickets(List<AppFilter> filter = null) => GetEntity("tickets", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoUser", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoUser>")]
        public IEnumerable<object> GetUsers(List<AppFilter> filter = null) => GetEntity("users", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoOrganization", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoOrganization>")]
        public IEnumerable<object> GetOrganizations(List<AppFilter> filter = null) => GetEntity("organizations", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoTeam", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoTeam>")]
        public IEnumerable<object> GetTeams(List<AppFilter> filter = null) => GetEntity("teams", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoComment", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoComment>")]
        public IEnumerable<object> GetComments(int ticketId, List<AppFilter> filter = null)
        {
            filter ??= new List<AppFilter>();
            filter.Add(new AppFilter { FieldName = "ticketId", FilterValue = ticketId.ToString() });
            return GetEntity("comments", filter);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoKnowledgebase", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoKnowledgebase>")]
        public IEnumerable<object> GetKnowledgebase(List<AppFilter> filter = null) => GetEntity("knowledgebase", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoDepartment", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoDepartment>")]
        public IEnumerable<object> GetDepartments(List<AppFilter> filter = null) => GetEntity("departments", filter ?? new List<AppFilter>());

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoConversation", ClassName = "KayakoDataSource", Showin = ShowinType.Both, misc = "List<KayakoConversation>")]
        public IEnumerable<object> GetConversations(List<AppFilter> filter = null) => GetEntity("conversations", filter ?? new List<AppFilter>());

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoTicket", Name = "CreateTicket", Caption = "Create Kayako Ticket", ClassType = "KayakoDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "kayako.png", misc = "ReturnType: IEnumerable<KayakoTicket>")]
        public async Task<IEnumerable<KayakoTicket>> CreateTicketAsync(KayakoTicket ticket)
        {
            try
            {
                var result = await PostAsync("api/v1/tickets.json", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTicket = JsonSerializer.Deserialize<KayakoTicket>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<KayakoTicket> { createdTicket }.Select(t => t.Attach<KayakoTicket>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating ticket: {ex.Message}");
            }
            return new List<KayakoTicket>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kayako, PointType = EnumPointType.Function, ObjectType = "KayakoTicket", Name = "UpdateTicket", Caption = "Update Kayako Ticket", ClassType = "KayakoDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "kayako.png", misc = "ReturnType: IEnumerable<KayakoTicket>")]
        public async Task<IEnumerable<KayakoTicket>> UpdateTicketAsync(KayakoTicket ticket)
        {
            try
            {
                var result = await PutAsync($"api/v1/tickets/{ticket.Id}.json", ticket);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedTicket = JsonSerializer.Deserialize<KayakoTicket>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<KayakoTicket> { updatedTicket }.Select(t => t.Attach<KayakoTicket>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating ticket: {ex.Message}");
            }
            return new List<KayakoTicket>();
        }
    }
}
