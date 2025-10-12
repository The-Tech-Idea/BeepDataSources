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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HelpScout)]
    public class HelpScoutDataSource : WebAPIDataSource
    {
        public HelpScoutDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
        }

        private record EntityMapping(string endpoint, string root, string[] requiredFilters);

        private static readonly Dictionary<string, EntityMapping> Map = new()
        {
            ["conversations"] = new("/conversations", "_embedded.conversations", Array.Empty<string>()),
            ["customers"] = new("/customers", "_embedded.customers", Array.Empty<string>()),
            ["mailboxes"] = new("/mailboxes", "_embedded.mailboxes", Array.Empty<string>()),
            ["users"] = new("/users", "_embedded.users", Array.Empty<string>()),
            ["teams"] = new("/teams", "_embedded.teams", Array.Empty<string>()),
            ["tags"] = new("/tags", "_embedded.tags", Array.Empty<string>()),
            ["folders"] = new("/folders", "_embedded.folders", Array.Empty<string>()),
            ["workflows"] = new("/workflows", "_embedded.workflows", Array.Empty<string>()),
            ["reports"] = new("/reports", "_embedded.reports", Array.Empty<string>()),
        };

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).GetAwaiter().GetResult();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown HelpScout entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (HelpScout uses offset-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown HelpScout entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // HelpScout pagination
            q["page"] = Math.Max(1, pageNumber).ToString();

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            if (resp is null || !resp.IsSuccessStatusCode) return new PagedResult();

            var data = ExtractArray(resp, m.root);
            return new PagedResult
            {
                Data = data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 1, // HelpScout doesn't provide total count in response
                TotalRecords = data.Count()
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

        private void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrEmpty(q[r])).ToArray();
            if (missing.Length > 0)
                throw new ArgumentException($"HelpScout entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

        [CommandAttribute(
            Caption = "Get Conversations",
            Name = "GetConversations",
            ObjectType = "Conversation",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Conversation>"
        )]
        public IEnumerable<object> GetConversations()
        {
            return GetEntity("conversations", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Customers",
            Name = "GetCustomers",
            ObjectType = "Customer",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Customer>"
        )]
        public IEnumerable<object> GetCustomers()
        {
            return GetEntity("customers", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Mailboxes",
            Name = "GetMailboxes",
            ObjectType = "Mailbox",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Mailbox>"
        )]
        public IEnumerable<object> GetMailboxes()
        {
            return GetEntity("mailboxes", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Users",
            Name = "GetUsers",
            ObjectType = "User",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<User>"
        )]
        public IEnumerable<object> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Teams",
            Name = "GetTeams",
            ObjectType = "Team",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Team>"
        )]
        public IEnumerable<object> GetTeams()
        {
            return GetEntity("teams", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Tags",
            Name = "GetTags",
            ObjectType = "Tag",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Tag>"
        )]
        public IEnumerable<object> GetTags()
        {
            return GetEntity("tags", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Folders",
            Name = "GetFolders",
            ObjectType = "Folder",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Folder>"
        )]
        public IEnumerable<object> GetFolders()
        {
            return GetEntity("folders", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Workflows",
            Name = "GetWorkflows",
            ObjectType = "Workflow",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Workflow>"
        )]
        public IEnumerable<object> GetWorkflows()
        {
            return GetEntity("workflows", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Reports",
            Name = "GetReports",
            ObjectType = "Report",
            PointType = EnumPointType.Function,
            ClassName = "HelpScoutDataSource",
            misc = "ReturnType: IEnumerable<Report>"
        )]
        public IEnumerable<object> GetReports()
        {
            return GetEntity("reports", new List<AppFilter>());
        }
    }
}
