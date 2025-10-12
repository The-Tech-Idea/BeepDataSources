using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.ZendeskDataSource;

namespace TheTechIdea.Beep.Connectors.Zendesk
{
    /// <summary>
    /// Zendesk data source implementation aligned with the Twitter pattern.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk)]
    public class ZendeskDataSource : WebAPIDataSource
    {
        private record EntityDefinition(string Endpoint, string RootPath, string[] RequiredFilters, Type ModelType);

        private static readonly Dictionary<string, EntityDefinition> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tickets"] = new("api/v2/tickets.json", "tickets", Array.Empty<string>(), typeof(ZendeskTicket)),
            ["ticket_comments"] = new("api/v2/tickets/{ticket_id}/comments.json", "comments", new[] { "ticket_id" }, typeof(ZendeskComment)),
            ["users"] = new("api/v2/users.json", "users", Array.Empty<string>(), typeof(ZendeskUser)),
            ["organizations"] = new("api/v2/organizations.json", "organizations", Array.Empty<string>(), typeof(ZendeskOrganization)),
            ["groups"] = new("api/v2/groups.json", "groups", Array.Empty<string>(), typeof(ZendeskGroup)),
            ["macros"] = new("api/v2/macros.json", "macros", Array.Empty<string>(), typeof(ZendeskMacro)),
            ["views"] = new("api/v2/views.json", "views", Array.Empty<string>(), typeof(ZendeskView)),
            ["satisfaction_ratings"] = new("api/v2/satisfaction_ratings.json", "satisfaction_ratings", Array.Empty<string>(), typeof(ZendeskSatisfactionRating)),
            ["tickets.search"] = new("api/v2/search.json", "results", new[] { "query" }, typeof(ZendeskTicket))
        };

        private static readonly List<string> KnownEntities = Map.Keys.ToList();

        public ZendeskDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                {
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
                }
            }

            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            return GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var entity))
                throw new InvalidOperationException($"Unknown Zendesk entity '{EntityName}'.");

            var query = FiltersToQuery(Filter);
            RequireFilters(EntityName, query, entity.RequiredFilters);

            var endpoint = ResolveEndpoint(entity.Endpoint, query);

            using var response = await GetAsync(endpoint, query).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode)
                return Array.Empty<object>();

            return Deserialize(response, entity);
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var entity))
                throw new InvalidOperationException($"Unknown Zendesk entity '{EntityName}'.");

            var query = FiltersToQuery(filter);
            RequireFilters(EntityName, query, entity.RequiredFilters);

            query["page"] = Math.Max(1, pageNumber).ToString();
            query["per_page"] = Math.Max(1, Math.Min(pageSize, 100)).ToString();

            var endpoint = ResolveEndpoint(entity.Endpoint, query);

            using var response = CallZendesk(endpoint, query).ConfigureAwait(false).GetAwaiter().GetResult();
            var data = Deserialize(response, entity);

            return new PagedResult
            {
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = data.Count(),
                TotalPages = data.Any() && data.Count() == pageSize ? pageNumber + 1 : pageNumber,
                HasNextPage = data.Count() == pageSize,
                HasPreviousPage = pageNumber > 1
            };
        }

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return query;

            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.FieldName)) continue;
                query[filter.FieldName.Trim()] = filter.FilterValue?.ToString() ?? string.Empty;
            }

            return query;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> query, string[] required)
        {
            if (required == null || required.Length == 0) return;

            var missing = required
                .Where(r => !query.ContainsKey(r) || string.IsNullOrWhiteSpace(query[r]))
                .ToList();

            if (missing.Count > 0)
                throw new ArgumentException($"Zendesk entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> query)
        {
            if (string.IsNullOrWhiteSpace(template)) return template;

            foreach (var (key, value) in query)
            {
                var placeholder = "{" + key + "}";
                if (template.Contains(placeholder, StringComparison.Ordinal))
                {
                    template = template.Replace(placeholder, Uri.EscapeDataString(value ?? string.Empty));
                }
            }

            return template;
        }

        private async Task<HttpResponseMessage> CallZendesk(string endpoint, Dictionary<string, string> query, CancellationToken cancellationToken = default)
        {
            return await GetAsync(endpoint, query, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static IEnumerable<object> Deserialize(HttpResponseMessage response, EntityDefinition entity)
        {
            var results = new List<object>();
            if (response == null) return results;

            var payload = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var document = JsonDocument.Parse(payload);

            JsonElement node = document.RootElement;
            if (!string.IsNullOrWhiteSpace(entity.RootPath))
            {
                var segments = entity.RootPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
                foreach (var segment in segments)
                {
                    if (!node.TryGetProperty(segment, out node))
                        return results;
                }
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (entity.ModelType != null)
            {
                if (node.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in node.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize(element.GetRawText(), entity.ModelType, options);
                        if (obj != null) results.Add(obj);
                    }
                }
                else if (node.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize(node.GetRawText(), entity.ModelType, options);
                    if (obj != null) results.Add(obj);
                }
            }

            if (results.Count == 0)
            {
                if (node.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in node.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), options);
                        if (obj != null) results.Add(obj);
                    }
                }
                else if (node.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), options);
                    if (obj != null) results.Add(obj);
                }
            }

            return results;
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskTicket", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskTicket>")]
        public List<ZendeskTicket> GetTickets()
        {
            return GetEntity("tickets", new List<AppFilter>()).Cast<ZendeskTicket>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskTicket", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "ZendeskTicket")]
        public ZendeskTicket GetTicket(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("tickets", filters).Cast<ZendeskTicket>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskComment", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskComment>")]
        public List<ZendeskComment> GetTicketComments(long ticketId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "ticket_id", FilterValue = ticketId.ToString(), Operator = "=" } };
            return GetEntity("ticket_comments", filters).Cast<ZendeskComment>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskUser", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskUser>")]
        public List<ZendeskUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<ZendeskUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskUser", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "ZendeskUser")]
        public ZendeskUser GetUser(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("users", filters).Cast<ZendeskUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskOrganization", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskOrganization>")]
        public List<ZendeskOrganization> GetOrganizations()
        {
            return GetEntity("organizations", new List<AppFilter>()).Cast<ZendeskOrganization>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskOrganization", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "ZendeskOrganization")]
        public ZendeskOrganization GetOrganization(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("organizations", filters).Cast<ZendeskOrganization>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskGroup", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskGroup>")]
        public List<ZendeskGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<ZendeskGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskGroup", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "ZendeskGroup")]
        public ZendeskGroup GetGroup(long id)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = id.ToString(), Operator = "=" } };
            return GetEntity("groups", filters).Cast<ZendeskGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskMacro", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskMacro>")]
        public List<ZendeskMacro> GetMacros()
        {
            return GetEntity("macros", new List<AppFilter>()).Cast<ZendeskMacro>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskView", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskView>")]
        public List<ZendeskView> GetViews()
        {
            return GetEntity("views", new List<AppFilter>()).Cast<ZendeskView>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskSatisfactionRating", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskSatisfactionRating>")]
        public List<ZendeskSatisfactionRating> GetSatisfactionRatings()
        {
            return GetEntity("satisfaction_ratings", new List<AppFilter>()).Cast<ZendeskSatisfactionRating>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Zendesk, PointType = EnumPointType.Function, ObjectType = "ZendeskTicket", ClassName = "ZendeskDataSource", Showin = ShowinType.Both, misc = "List<ZendeskTicket>")]
        public List<ZendeskTicket> SearchTickets(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" } };
            return GetEntity("tickets.search", filters).Cast<ZendeskTicket>().ToList();
        }
    }
}