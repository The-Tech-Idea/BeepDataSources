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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Front)]
    public class FrontDataSource : WebAPIDataSource
    {
        public FrontDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
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
            ["conversations"] = new("/conversations", "_results", Array.Empty<string>()),
            ["messages"] = new("/conversations/{conversationId}/messages", "_results", new[] { "conversationId" }),
            ["contacts"] = new("/contacts", "_results", Array.Empty<string>()),
            ["inboxes"] = new("/inboxes", "_results", Array.Empty<string>()),
            ["tags"] = new("/tags", "_results", Array.Empty<string>()),
            ["rules"] = new("/rules", "_results", Array.Empty<string>()),
            ["analytics"] = new("/analytics", "_results", Array.Empty<string>()),
            ["teams"] = new("/teams", "_results", Array.Empty<string>()),
        };

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).GetAwaiter().GetResult();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Front entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ResolveEndpoint(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // Paged (Front uses cursor-based pagination)
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Front entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Front pagination
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
                TotalPages = 1, // Front doesn't provide total count in response
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
                throw new ArgumentException($"Front entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Conversation>"
        )]
        public IEnumerable<object> GetConversations()
        {
            return GetEntity("conversations", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Messages",
            Name = "GetMessages",
            ObjectType = "Message",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Message>"
        )]
        public IEnumerable<object> GetMessages(string conversationId)
        {
            var filters = new List<AppFilter>
            {
                new AppFilter { FieldName = "conversationId", FilterValue = conversationId, Operator = "=" }
            };
            return GetEntity("messages", filters);
        }

        [CommandAttribute(
            Caption = "Get Contacts",
            Name = "GetContacts",
            ObjectType = "Contact",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Contact>"
        )]
        public IEnumerable<object> GetContacts()
        {
            return GetEntity("contacts", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Inboxes",
            Name = "GetInboxes",
            ObjectType = "Inbox",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Inbox>"
        )]
        public IEnumerable<object> GetInboxes()
        {
            return GetEntity("inboxes", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Tags",
            Name = "GetTags",
            ObjectType = "Tag",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Tag>"
        )]
        public IEnumerable<object> GetTags()
        {
            return GetEntity("tags", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Rules",
            Name = "GetRules",
            ObjectType = "Rule",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Rule>"
        )]
        public IEnumerable<object> GetRules()
        {
            return GetEntity("rules", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Analytics",
            Name = "GetAnalytics",
            ObjectType = "Analytics",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Analytics>"
        )]
        public IEnumerable<object> GetAnalytics()
        {
            return GetEntity("analytics", new List<AppFilter>());
        }

        [CommandAttribute(
            Caption = "Get Teams",
            Name = "GetTeams",
            ObjectType = "Team",
            PointType = EnumPointType.Function,
            ClassName = "FrontDataSource",
            misc = "ReturnType: IEnumerable<Team>"
        )]
        public IEnumerable<object> GetTeams()
        {
            return GetEntity("teams", new List<AppFilter>());
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Front, PointType = EnumPointType.Function, ObjectType = "Conversation", Name = "CreateConversation", Caption = "Create Front Conversation", ClassType = "FrontDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "front.png", misc = "ReturnType: IEnumerable<Conversation>")]
        public async Task<IEnumerable<Conversation>> CreateConversationAsync(Conversation conversation)
        {
            try
            {
                var result = await PostAsync("conversations", conversation);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdConversation = JsonSerializer.Deserialize<Conversation>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Conversation> { createdConversation }.Select(c => c.Attach<Conversation>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating conversation: {ex.Message}");
            }
            return new List<Conversation>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Front, PointType = EnumPointType.Function, ObjectType = "Conversation", Name = "UpdateConversation", Caption = "Update Front Conversation", ClassType = "FrontDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "front.png", misc = "ReturnType: IEnumerable<Conversation>")]
        public async Task<IEnumerable<Conversation>> UpdateConversationAsync(Conversation conversation)
        {
            try
            {
                var result = await PutAsync($"conversations/{conversation.Id}", conversation);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var updatedConversation = JsonSerializer.Deserialize<Conversation>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<Conversation> { updatedConversation }.Select(c => c.Attach<Conversation>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating conversation: {ex.Message}");
            }
            return new List<Conversation>();
        }
    }
}
