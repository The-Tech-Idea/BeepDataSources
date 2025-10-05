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

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        public override List<string> GetEntitiesList() => KnownEntities.ToList();

        public override List<EntityStructure> GetEntityStructure(string EntityName, bool refresh)
            => GetEntityStructureAsync(EntityName, refresh).ConfigureAwait(false).GetAwaiter().GetResult();

        public override async Task<List<EntityStructure>> GetEntityStructureAsync(string EntityName, bool refresh)
        {
            if (!Map.TryGetValue(EntityName, out var entity))
                throw new InvalidOperationException($"Unknown Zendesk entity '{EntityName}'.");

            using var response = await GetAsync(entity.Endpoint).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode)
                return new List<EntityStructure>();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var table = ZendeskHelpers.ParseEntityData(
                json,
                string.IsNullOrWhiteSpace(entity.RootPath) ? EntityName : entity.RootPath);

            return new List<EntityStructure>
            {
                new EntityStructure
                {
                    EntityName = EntityName,
                    DatasourceEntityName = EntityName,
                    DataSourceID = DatasourceName,
                    Fields = table.Columns.Cast<DataColumn>().Select(c => new EntityField
                    {
                        fieldname = c.ColumnName,
                        fieldtype = c.DataType.Name,
                        AllowDBNull = true,
                        ValueRetrievedFromParent = false,
                        IsAutoIncrement = false,
                        IsIdentity = false,
                        IsKey = false,
                        IsUnique = false
                    }).ToList()
                }
            };
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
    }
}