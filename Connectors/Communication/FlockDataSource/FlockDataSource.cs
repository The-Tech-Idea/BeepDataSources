using System;
using System.Collections.Generic;
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
using TheTechIdea.Beep.Connectors.Communication.Flock.Models;

namespace TheTechIdea.Beep.Connectors.Communication.Flock
{
    /// <summary>
    /// Flock API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Flock API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock)]
    public class FlockDataSource : WebAPIDataSource
    {
        // Supported Flock entities -> Flock endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("users.list", "users", Array.Empty<string>()),
                ["user"] = ("users.getInfo", "user", new[] { "userId" }),
                ["user_presence"] = ("users.getPresence", "presence", new[] { "userId" }),
                ["groups"] = ("groups.list", "groups", Array.Empty<string>()),
                ["group"] = ("groups.getInfo", "group", new[] { "groupId" }),
                ["group_members"] = ("groups.getMembers", "members", new[] { "groupId" }),
                ["channels"] = ("channels.list", "channels", Array.Empty<string>()),
                ["channel"] = ("channels.getInfo", "channel", new[] { "channelId" }),
                ["channel_members"] = ("channels.getMembers", "members", new[] { "channelId" }),
                ["messages"] = ("channels.getMessages", "messages", new[] { "channelId" }),
                ["message"] = ("chat.getMessage", "message", new[] { "messageId" }),
                ["message_reactions"] = ("chat.getReactions", "reactions", new[] { "messageId" }),
                ["message_replies"] = ("chat.getThreadMessages", "messages", new[] { "messageId" }),
                ["files"] = ("channels.getFiles", "files", new[] { "channelId" }),
                ["file"] = ("chat.getFile", "file", new[] { "fileId" }),
                ["contacts"] = ("contacts.list", "contacts", Array.Empty<string>()),
                ["contact"] = ("contacts.getInfo", "contact", new[] { "contactId" }),
                ["apps"] = ("apps.list", "apps", Array.Empty<string>()),
                ["app"] = ("apps.getInfo", "app", new[] { "appId" }),
                ["webhooks"] = ("webhooks.list", "webhooks", Array.Empty<string>()),
                ["webhook"] = ("webhooks.getInfo", "webhook", new[] { "webhookId" }),
                ["tokens"] = ("tokens.list", "tokens", Array.Empty<string>()),
                ["token"] = ("tokens.getInfo", "token", new[] { "tokenId" })
            };

        public FlockDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection != null && Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Keep your interface exactly
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // ---------------- overrides (same signatures) ----------------

        // sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Flock entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            using var resp = await GetAsync(m.endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root, EntityName);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Flock entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Flock API supports pagination with limit and offset
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Flock max is usually 100
            q["offset"] = ((pageNumber - 1) * pageSize).ToString();

            var resp = GetAsync(m.endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root, EntityName);

            // Basic pagination estimate
            int totalRecordsSoFar = (pageNumber - 1) * Math.Max(1, pageSize) + items.Count;

            return new PagedResult
            {
                Data = items,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecordsSoFar,
                TotalPages = pageNumber, // Can't determine total pages without count
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count == pageSize // Assume more if we got full page
            };
        }

        // -------------------------- helpers --------------------------

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
            foreach (var req in required)
            {
                if (!q.ContainsKey(req) || string.IsNullOrWhiteSpace(q[req]))
                    throw new ArgumentException($"Flock entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath, string entityName)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                foreach (var part in rootPath.Split('.'))
                {
                    if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(part, out node))
                        return list; // path not found
                }
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (node.ValueKind == JsonValueKind.Array)
            {
                list.Capacity = node.GetArrayLength();
                foreach (var el in node.EnumerateArray())
                {
                    var entity = DeserializeEntity(el.GetRawText(), entityName, opts);
                    if (entity != null) list.Add(entity);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                // wrap single object
                var entity = DeserializeEntity(node.GetRawText(), entityName, opts);
                if (entity != null) list.Add(entity);
            }

            return list;
        }

        private static object? DeserializeEntity(string json, string entityName, JsonSerializerOptions opts)
        {
            return entityName.ToLowerInvariant() switch
            {
                "users" => JsonSerializer.Deserialize<FlockUser>(json, opts),
                "user" => JsonSerializer.Deserialize<FlockUser>(json, opts),
                "user_presence" => JsonSerializer.Deserialize<FlockUserPresence>(json, opts),
                "groups" => JsonSerializer.Deserialize<FlockGroup>(json, opts),
                "group" => JsonSerializer.Deserialize<FlockGroup>(json, opts),
                "group_members" => JsonSerializer.Deserialize<FlockGroupMember>(json, opts),
                "channels" => JsonSerializer.Deserialize<FlockChannel>(json, opts),
                "channel" => JsonSerializer.Deserialize<FlockChannel>(json, opts),
                "channel_members" => JsonSerializer.Deserialize<FlockChannelMember>(json, opts),
                "messages" => JsonSerializer.Deserialize<FlockMessage>(json, opts),
                "message" => JsonSerializer.Deserialize<FlockMessage>(json, opts),
                "message_reactions" => JsonSerializer.Deserialize<FlockMessageReaction>(json, opts),
                "message_replies" => JsonSerializer.Deserialize<FlockMessageReply>(json, opts),
                "files" => JsonSerializer.Deserialize<FlockFile>(json, opts),
                "file" => JsonSerializer.Deserialize<FlockFile>(json, opts),
                "contacts" => JsonSerializer.Deserialize<FlockContact>(json, opts),
                "contact" => JsonSerializer.Deserialize<FlockContact>(json, opts),
                "apps" => JsonSerializer.Deserialize<FlockApp>(json, opts),
                "app" => JsonSerializer.Deserialize<FlockApp>(json, opts),
                "webhooks" => JsonSerializer.Deserialize<FlockWebhook>(json, opts),
                "webhook" => JsonSerializer.Deserialize<FlockWebhook>(json, opts),
                "tokens" => JsonSerializer.Deserialize<FlockToken>(json, opts),
                "token" => JsonSerializer.Deserialize<FlockToken>(json, opts),
                _ => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts)
            };
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockUser", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockUser>")]
        public List<FlockUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<FlockUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockUser", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockUser")]
        public FlockUser? GetUser(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "userId", FilterValue = userId } };
            return GetEntity("user", filters).Cast<FlockUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockUserPresence", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockUserPresence")]
        public FlockUserPresence? GetUserPresence(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "userId", FilterValue = userId } };
            return GetEntity("user_presence", filters).Cast<FlockUserPresence>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockGroup", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockGroup>")]
        public List<FlockGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<FlockGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockGroup", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockGroup")]
        public FlockGroup? GetGroup(string groupId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "groupId", FilterValue = groupId } };
            return GetEntity("group", filters).Cast<FlockGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockGroupMember", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockGroupMember>")]
        public List<FlockGroupMember> GetGroupMembers(string groupId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "groupId", FilterValue = groupId } };
            return GetEntity("group_members", filters).Cast<FlockGroupMember>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockChannel", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockChannel>")]
        public List<FlockChannel> GetChannels()
        {
            return GetEntity("channels", new List<AppFilter>()).Cast<FlockChannel>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockChannel", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockChannel")]
        public FlockChannel? GetChannel(string channelId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "channelId", FilterValue = channelId } };
            return GetEntity("channel", filters).Cast<FlockChannel>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockChannelMember", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockChannelMember>")]
        public List<FlockChannelMember> GetChannelMembers(string channelId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "channelId", FilterValue = channelId } };
            return GetEntity("channel_members", filters).Cast<FlockChannelMember>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockMessage", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockMessage>")]
        public List<FlockMessage> GetMessages(string channelId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "channelId", FilterValue = channelId } };
            return GetEntity("messages", filters).Cast<FlockMessage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockMessage", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockMessage")]
        public FlockMessage? GetMessage(string messageId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "messageId", FilterValue = messageId } };
            return GetEntity("message", filters).Cast<FlockMessage>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockMessageReaction", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockMessageReaction>")]
        public List<FlockMessageReaction> GetMessageReactions(string messageId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "messageId", FilterValue = messageId } };
            return GetEntity("message_reactions", filters).Cast<FlockMessageReaction>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockMessageReply", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockMessageReply>")]
        public List<FlockMessageReply> GetMessageReplies(string messageId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "messageId", FilterValue = messageId } };
            return GetEntity("message_replies", filters).Cast<FlockMessageReply>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockFile", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockFile>")]
        public List<FlockFile> GetFiles(string channelId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "channelId", FilterValue = channelId } };
            return GetEntity("files", filters).Cast<FlockFile>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockFile", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockFile")]
        public FlockFile? GetFile(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileId", FilterValue = fileId } };
            return GetEntity("file", filters).Cast<FlockFile>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockContact", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockContact>")]
        public List<FlockContact> GetContacts()
        {
            return GetEntity("contacts", new List<AppFilter>()).Cast<FlockContact>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockContact", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockContact")]
        public FlockContact? GetContact(string contactId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "contactId", FilterValue = contactId } };
            return GetEntity("contact", filters).Cast<FlockContact>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockApp", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockApp>")]
        public List<FlockApp> GetApps()
        {
            return GetEntity("apps", new List<AppFilter>()).Cast<FlockApp>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockApp", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockApp")]
        public FlockApp? GetApp(string appId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "appId", FilterValue = appId } };
            return GetEntity("app", filters).Cast<FlockApp>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockWebhook", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockWebhook>")]
        public List<FlockWebhook> GetWebhooks()
        {
            return GetEntity("webhooks", new List<AppFilter>()).Cast<FlockWebhook>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockWebhook", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockWebhook")]
        public FlockWebhook? GetWebhook(string webhookId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "webhookId", FilterValue = webhookId } };
            return GetEntity("webhook", filters).Cast<FlockWebhook>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockToken", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "List<FlockToken>")]
        public List<FlockToken> GetTokens()
        {
            return GetEntity("tokens", new List<AppFilter>()).Cast<FlockToken>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock, PointType = EnumPointType.Function, ObjectType = "FlockToken", ClassName = "FlockDataSource", Showin = ShowinType.Both, misc = "FlockToken")]
        public FlockToken? GetToken(string tokenId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "tokenId", FilterValue = tokenId } };
            return GetEntity("token", filters).Cast<FlockToken>().FirstOrDefault();
        }

        [CommandAttribute(Name = "SendMessageAsync", Caption = "Send Flock Message",
            ObjectType = "FlockMessage", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock,
            ClassType = "FlockDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "flock.png", misc = "Send a message")]
        public async Task<IEnumerable<FlockMessage>> SendMessageAsync(FlockMessage message, List<AppFilter> filters = null)
        {
            var result = await PostAsync("chat.sendMessage", message, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<FlockMessage>>(result);
        }

        [CommandAttribute(Name = "CreateGroupAsync", Caption = "Create Flock Group",
            ObjectType = "FlockGroup", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock,
            ClassType = "FlockDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "flock.png", misc = "Create a group")]
        public async Task<IEnumerable<FlockGroup>> CreateGroupAsync(FlockGroup group, List<AppFilter> filters = null)
        {
            var result = await PostAsync("groups.create", group, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<FlockGroup>>(result);
        }

        [CommandAttribute(Name = "CreateChannelAsync", Caption = "Create Flock Channel",
            ObjectType = "FlockChannel", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Flock,
            ClassType = "FlockDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "flock.png", misc = "Create a channel")]
        public async Task<IEnumerable<FlockChannel>> CreateChannelAsync(FlockChannel channel, List<AppFilter> filters = null)
        {
            var result = await PostAsync("channels.create", channel, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<FlockChannel>>(result);
        }

        #endregion
    }
}