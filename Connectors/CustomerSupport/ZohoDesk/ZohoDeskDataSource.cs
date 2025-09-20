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

namespace TheTechIdea.Beep.DataSources
{
    [AddinAttribute(Catagory = "Connector", DatasourceType = DatasourceType.ZohoDesk)]
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

        public override string GetConnectionString()
        {
            return $"ZohoDesk:{DatasourceName}";
        }

        public override string ColumnDelimiter { get => ","; set => base.ColumnDelimiter = value; }
        public override string ParameterDelimiter { get => ":"; set => base.ParameterDelimiter = value; }
        public override string RowDelimiter { get => "\n"; set => base.RowDelimiter = value; }

        public override List<string> GetEntitiesList()
        {
            return Map.Keys.ToList();
        }

        public override List<EntityStructure> GetEntityStructure(string EntityName, bool refresh)
        {
            return GetEntityStructureAsync(EntityName, refresh).GetAwaiter().GetResult();
        }

        public override async Task<List<EntityStructure>> GetEntityStructureAsync(string EntityName, bool refresh)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Zoho Desk entity '{EntityName}'.");

            var endpoint = m.endpoint;
            using var resp = await GetAsync(endpoint).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return new List<EntityStructure>();

            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = JsonDocument.Parse(json);
            var root = GetJsonProperty(doc.RootElement, m.root.Split('.'));

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return new List<EntityStructure>();

            var sample = root[0];
            var structure = new EntityStructure
            {
                EntityName = EntityName,
                ViewID = EntityName,
                DataSourceID = DatasourceName,
                Fields = new List<EntityField>()
            };

            foreach (var prop in sample.EnumerateObject())
            {
                structure.Fields.Add(new EntityField
                {
                    fieldname = prop.Name,
                    fieldtype = MapJsonType(prop.Value.ValueKind),
                    ValueRetrievedFromParent = false,
                    AllowDBNull = true,
                    IsAutoIncrement = false,
                    IsIdentity = false,
                    IsKey = false,
                    IsUnique = false
                });
            }

            return new List<EntityStructure> { structure };
        }

        public override object GetEntity(string EntityName, List<AppFilter> Filter)
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

            using var resp = Get(endpoint, q);
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

        private string MapJsonType(JsonValueKind kind) => kind switch
        {
            JsonValueKind.String => "System.String",
            JsonValueKind.Number => "System.Decimal",
            JsonValueKind.True or JsonValueKind.False => "System.Boolean",
            JsonValueKind.Array => "System.Object[]",
            JsonValueKind.Object => "System.Object",
            _ => "System.String"
        };
    }
}
