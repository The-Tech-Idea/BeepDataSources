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
using TheTechIdea.Beep.Connectors.Communication.RocketChat.Models;

namespace TheTechIdea.Beep.Connectors.Communication.RocketChat
{
    /// <summary>
    /// Rocket.Chat API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Rocket.Chat API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat)]
    public class RocketChatDataSource : WebAPIDataSource
    {
        // Supported Rocket.Chat entities -> Rocket.Chat endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("api/v1/users.list", "users", Array.Empty<string>()),
                ["user"] = ("api/v1/users.info", "user", new[] { "userId" }),
                ["channels"] = ("api/v1/channels.list", "channels", Array.Empty<string>()),
                ["channel"] = ("api/v1/channels.info", "channel", new[] { "roomId" }),
                ["channel_members"] = ("api/v1/channels.members", "members", new[] { "roomId" }),
                ["channel_messages"] = ("api/v1/channels.messages", "messages", new[] { "roomId" }),
                ["groups"] = ("api/v1/groups.list", "groups", Array.Empty<string>()),
                ["group"] = ("api/v1/groups.info", "group", new[] { "roomId" }),
                ["group_members"] = ("api/v1/groups.members", "members", new[] { "roomId" }),
                ["group_messages"] = ("api/v1/groups.messages", "messages", new[] { "roomId" }),
                ["im_list"] = ("api/v1/im.list", "ims", Array.Empty<string>()),
                ["im_messages"] = ("api/v1/im.messages", "messages", new[] { "roomId" }),
                ["im_history"] = ("api/v1/im.history", "messages", new[] { "roomId" }),
                ["rooms"] = ("api/v1/rooms.get", "rooms", Array.Empty<string>()),
                ["room"] = ("api/v1/rooms.info", "room", new[] { "roomId" }),
                ["subscriptions"] = ("api/v1/subscriptions.get", "subscriptions", Array.Empty<string>()),
                ["roles"] = ("api/v1/roles.list", "roles", Array.Empty<string>()),
                ["permissions"] = ("api/v1/permissions.list", "permissions", Array.Empty<string>()),
                ["settings"] = ("api/v1/settings", "settings", Array.Empty<string>()),
                ["statistics"] = ("api/v1/statistics", "statistics", Array.Empty<string>()),
                ["integrations"] = ("api/v1/integrations.list", "integrations", Array.Empty<string>()),
                ["webhooks"] = ("api/v1/integrations.list", "integrations", Array.Empty<string>())
            };

        public RocketChatDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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
                throw new InvalidOperationException($"Unknown Rocket.Chat entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown Rocket.Chat entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Rocket.Chat API supports pagination with count and offset
            q["count"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Rocket.Chat max is usually 100
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
                    throw new ArgumentException($"Rocket.Chat entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath, string entityName)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            // Rocket.Chat API always returns { success: bool, ... }
            if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.False)
            {
                // Optionally, pick up "error" for diagnostics; here we just return empty.
                return list;
            }

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
                "users" => JsonSerializer.Deserialize<RocketChatUser>(json, opts),
                "user" => JsonSerializer.Deserialize<RocketChatUser>(json, opts),
                "channels" => JsonSerializer.Deserialize<RocketChatChannel>(json, opts),
                "channel" => JsonSerializer.Deserialize<RocketChatChannel>(json, opts),
                "channel_members" => JsonSerializer.Deserialize<RocketChatUser>(json, opts),
                "channel_messages" => JsonSerializer.Deserialize<RocketChatMessage>(json, opts),
                "groups" => JsonSerializer.Deserialize<RocketChatGroup>(json, opts),
                "group" => JsonSerializer.Deserialize<RocketChatGroup>(json, opts),
                "group_members" => JsonSerializer.Deserialize<RocketChatUser>(json, opts),
                "group_messages" => JsonSerializer.Deserialize<RocketChatMessage>(json, opts),
                "im_list" => JsonSerializer.Deserialize<RocketChatIm>(json, opts),
                "im_messages" => JsonSerializer.Deserialize<RocketChatMessage>(json, opts),
                "im_history" => JsonSerializer.Deserialize<RocketChatMessage>(json, opts),
                "rooms" => JsonSerializer.Deserialize<RocketChatRoom>(json, opts),
                "room" => JsonSerializer.Deserialize<RocketChatRoom>(json, opts),
                "subscriptions" => JsonSerializer.Deserialize<RocketChatSubscription>(json, opts),
                "roles" => JsonSerializer.Deserialize<RocketChatRole>(json, opts),
                "permissions" => JsonSerializer.Deserialize<RocketChatPermission>(json, opts),
                "settings" => JsonSerializer.Deserialize<RocketChatSetting>(json, opts),
                "statistics" => JsonSerializer.Deserialize<RocketChatStatistics>(json, opts),
                "integrations" => JsonSerializer.Deserialize<RocketChatIntegration>(json, opts),
                "webhooks" => JsonSerializer.Deserialize<RocketChatWebhook>(json, opts),
                _ => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts)
            };
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatUser", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatUser>")]
        public List<RocketChatUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<RocketChatUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatUser", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "RocketChatUser")]
        public RocketChatUser? GetUser(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "userId", FilterValue = userId } };
            return GetEntity("user", filters).Cast<RocketChatUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatChannel", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatChannel>")]
        public List<RocketChatChannel> GetChannels()
        {
            return GetEntity("channels", new List<AppFilter>()).Cast<RocketChatChannel>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatChannel", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "RocketChatChannel")]
        public RocketChatChannel? GetChannel(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("channel", filters).Cast<RocketChatChannel>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatUser", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatUser>")]
        public List<RocketChatUser> GetChannelMembers(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("channel_members", filters).Cast<RocketChatUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatMessage", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatMessage>")]
        public List<RocketChatMessage> GetChannelMessages(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("channel_messages", filters).Cast<RocketChatMessage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatGroup", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatGroup>")]
        public List<RocketChatGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<RocketChatGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatGroup", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "RocketChatGroup")]
        public RocketChatGroup? GetGroup(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("group", filters).Cast<RocketChatGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatUser", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatUser>")]
        public List<RocketChatUser> GetGroupMembers(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("group_members", filters).Cast<RocketChatUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatMessage", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatMessage>")]
        public List<RocketChatMessage> GetGroupMessages(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("group_messages", filters).Cast<RocketChatMessage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatIm", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatIm>")]
        public List<RocketChatIm> GetImList()
        {
            return GetEntity("im_list", new List<AppFilter>()).Cast<RocketChatIm>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatMessage", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatMessage>")]
        public List<RocketChatMessage> GetImMessages(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("im_messages", filters).Cast<RocketChatMessage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatMessage", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatMessage>")]
        public List<RocketChatMessage> GetImHistory(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("im_history", filters).Cast<RocketChatMessage>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatRoom", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatRoom>")]
        public List<RocketChatRoom> GetRooms()
        {
            return GetEntity("rooms", new List<AppFilter>()).Cast<RocketChatRoom>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatRoom", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "RocketChatRoom")]
        public RocketChatRoom? GetRoom(string roomId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "roomId", FilterValue = roomId } };
            return GetEntity("room", filters).Cast<RocketChatRoom>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatSubscription", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatSubscription>")]
        public List<RocketChatSubscription> GetSubscriptions()
        {
            return GetEntity("subscriptions", new List<AppFilter>()).Cast<RocketChatSubscription>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatRole", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatRole>")]
        public List<RocketChatRole> GetRoles()
        {
            return GetEntity("roles", new List<AppFilter>()).Cast<RocketChatRole>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatPermission", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatPermission>")]
        public List<RocketChatPermission> GetPermissions()
        {
            return GetEntity("permissions", new List<AppFilter>()).Cast<RocketChatPermission>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.RocketChat, PointType = EnumPointType.Function, ObjectType = "RocketChatSetting", ClassName = "RocketChatDataSource", Showin = ShowinType.Both, misc = "List<RocketChatSetting>")]
        public List<RocketChatSetting> GetSettings()
        {
            return GetEntity("settings", new List<AppFilter>()).Cast<RocketChatSetting>().ToList();
        }

        [CommandAttribute(
            Name = "CreateMessageAsync",
            Caption = "Create Rocket.Chat Message",
            ObjectType = "RocketChatMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "createmessage.png",
            misc = "ReturnType: IEnumerable<RocketChatMessage>"
        )]
        public async Task<IEnumerable<RocketChatMessage>> CreateMessageAsync(RocketChatMessage message)
        {
            try
            {
                var result = await PostAsync("chat.postMessage", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<RocketChatMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<RocketChatMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating message: {ex.Message}");
            }
            return new List<RocketChatMessage>();
        }

        [CommandAttribute(
            Name = "CreateChannelAsync",
            Caption = "Create Rocket.Chat Channel",
            ObjectType = "RocketChatChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "createchannel.png",
            misc = "ReturnType: IEnumerable<RocketChatChannel>"
        )]
        public async Task<IEnumerable<RocketChatChannel>> CreateChannelAsync(RocketChatChannel channel)
        {
            try
            {
                var result = await PostAsync("channels.create", channel);
                var content = await result.Content.ReadAsStringAsync();
                var channels = JsonSerializer.Deserialize<IEnumerable<RocketChatChannel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<RocketChatChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating channel: {ex.Message}");
            }
            return new List<RocketChatChannel>();
        }

        [CommandAttribute(
            Name = "UpdateMessageAsync",
            Caption = "Update Rocket.Chat Message",
            ObjectType = "RocketChatMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "updatemessage.png",
            misc = "ReturnType: IEnumerable<RocketChatMessage>"
        )]
        public async Task<IEnumerable<RocketChatMessage>> UpdateMessageAsync(RocketChatMessage message)
        {
            try
            {
                var result = await PutAsync("chat.update", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<RocketChatMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<RocketChatMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating message: {ex.Message}");
            }
            return new List<RocketChatMessage>();
        }

        [CommandAttribute(
            Name = "UpdateChannelAsync",
            Caption = "Update Rocket.Chat Channel",
            ObjectType = "RocketChatChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "updatechannel.png",
            misc = "ReturnType: IEnumerable<RocketChatChannel>"
        )]
        public async Task<IEnumerable<RocketChatChannel>> UpdateChannelAsync(RocketChatChannel channel)
        {
            try
            {
                var result = await PutAsync("channels.setDescription", channel);
                var content = await result.Content.ReadAsStringAsync();
                var channels = JsonSerializer.Deserialize<IEnumerable<RocketChatChannel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<RocketChatChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating channel: {ex.Message}");
            }
            return new List<RocketChatChannel>();
        }

        [CommandAttribute(
            Name = "GetStatistics",
            Caption = "Get Rocket.Chat Statistics",
            ObjectType = "RocketChatStatistics",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "statistics.png",
            misc = "ReturnType: RocketChatStatistics"
        )]
        public RocketChatStatistics? GetStatistics()
        {
            return GetEntity("statistics", new List<AppFilter>()).Cast<RocketChatStatistics>().FirstOrDefault();
        }

        [CommandAttribute(
            Name = "GetIntegrations",
            Caption = "Get Rocket.Chat Integrations",
            ObjectType = "RocketChatIntegration",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "integrations.png",
            misc = "ReturnType: IEnumerable<RocketChatIntegration>"
        )]
        public List<RocketChatIntegration> GetIntegrations()
        {
            return GetEntity("integrations", new List<AppFilter>()).Cast<RocketChatIntegration>().ToList();
        }

        [CommandAttribute(
            Name = "GetWebhooks",
            Caption = "Get Rocket.Chat Webhooks",
            ObjectType = "RocketChatWebhook",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.RocketChat,
            ClassType = "RocketChatDataSource",
            Showin = ShowinType.Both,
            Order = 15,
            iconimage = "webhooks.png",
            misc = "ReturnType: IEnumerable<RocketChatWebhook>"
        )]
        public List<RocketChatWebhook> GetWebhooks()
        {
            return GetEntity("webhooks", new List<AppFilter>()).Cast<RocketChatWebhook>().ToList();
        }

        #endregion
    }
}