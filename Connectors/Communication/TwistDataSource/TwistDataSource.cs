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
using TheTechIdea.Beep.Connectors.Communication.Twist.Models;

namespace TheTechIdea.Beep.Connectors.Communication.Twist
{
    /// <summary>
    /// Twist API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Twist API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Twist)]
    public class TwistDataSource : WebAPIDataSource
    {
        // Supported Twist entities -> Twist endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["workspaces"] = ("api/v3/workspaces/get", "workspaces", Array.Empty<string>()),
                ["workspace"] = ("api/v3/workspaces/getone", "workspace", new[] { "id" }),
                ["channels"] = ("api/v3/channels/get", "channels", Array.Empty<string>()),
                ["channel"] = ("api/v3/channels/getone", "channel", new[] { "id" }),
                ["threads"] = ("api/v3/threads/get", "threads", Array.Empty<string>()),
                ["thread"] = ("api/v3/threads/getone", "thread", new[] { "id" }),
                ["messages"] = ("api/v3/messages/get", "messages", Array.Empty<string>()),
                ["message"] = ("api/v3/messages/getone", "message", new[] { "id" }),
                ["comments"] = ("api/v3/comments/get", "comments", Array.Empty<string>()),
                ["comment"] = ("api/v3/comments/getone", "comment", new[] { "id" }),
                ["users"] = ("api/v3/users/get", "users", Array.Empty<string>()),
                ["user"] = ("api/v3/users/getone", "user", new[] { "id" }),
                ["groups"] = ("api/v3/groups/get", "groups", Array.Empty<string>()),
                ["group"] = ("api/v3/groups/getone", "group", new[] { "id" }),
                ["integrations"] = ("api/v3/integrations/get", "integrations", Array.Empty<string>()),
                ["integration"] = ("api/v3/integrations/getone", "integration", new[] { "id" }),
                ["attachments"] = ("api/v3/attachments/get", "attachments", Array.Empty<string>()),
                ["attachment"] = ("api/v3/attachments/getone", "attachment", new[] { "id" }),
                ["notifications"] = ("api/v3/notifications/get", "notifications", Array.Empty<string>()),
                ["notification"] = ("api/v3/notifications/getone", "notification", new[] { "id" })
            };

        public TwistDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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
                throw new InvalidOperationException($"Unknown Twist entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown Twist entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Twist API supports pagination with limit and offset
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Twist max is usually 100
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
                    throw new ArgumentException($"Twist entity '{entity}' requires '{req}' parameter in filters.");
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
                    var obj = DeserializeEntity(el.GetRawText(), entityName, opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                // wrap single object
                var obj = DeserializeEntity(node.GetRawText(), entityName, opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        private static object? DeserializeEntity(string json, string entityName, JsonSerializerOptions opts)
        {
            return entityName.ToLowerInvariant() switch
            {
                "workspaces" or "workspace" => JsonSerializer.Deserialize<TwistWorkspace>(json, opts),
                "channels" or "channel" => JsonSerializer.Deserialize<TwistChannel>(json, opts),
                "threads" or "thread" => JsonSerializer.Deserialize<TwistThread>(json, opts),
                "messages" or "message" => JsonSerializer.Deserialize<TwistMessage>(json, opts),
                "comments" or "comment" => JsonSerializer.Deserialize<TwistComment>(json, opts),
                "users" or "user" => JsonSerializer.Deserialize<TwistUser>(json, opts),
                "groups" or "group" => JsonSerializer.Deserialize<TwistGroup>(json, opts),
                "integrations" or "integration" => JsonSerializer.Deserialize<TwistIntegration>(json, opts),
                "attachments" or "attachment" => JsonSerializer.Deserialize<TwistAttachment>(json, opts),
                "notifications" or "notification" => JsonSerializer.Deserialize<TwistNotification>(json, opts),
                _ => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts)
            };
        }

        #region CommandAttribute Methods

        /// <summary>
        /// Gets all workspaces from Twist
        /// </summary>
        /// <returns>Enumerable of TwistWorkspace objects</returns>
        [CommandAttribute(
            ObjectType = "TwistWorkspace",
            PointType = EnumPointType.Function,
            Name = "GetWorkspaces",
            Caption = "Get Workspaces",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistWorkspace>"
        )]
        public IEnumerable<TwistWorkspace> GetWorkspaces()
        {
            return GetEntityAsync("workspaces", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistWorkspace>()
                .Select(x => x.Attach<TwistWorkspace>(this));
        }

        /// <summary>
        /// Gets a specific workspace by ID
        /// </summary>
        /// <param name="workspaceId">The workspace ID</param>
        /// <returns>TwistWorkspace object</returns>
        [CommandAttribute(
            ObjectType = "TwistWorkspace",
            PointType = EnumPointType.Function,
            Name = "GetWorkspace",
            Caption = "Get Workspace",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistWorkspace>, Filter: id"
        )]
        public IEnumerable<TwistWorkspace> GetWorkspace(int workspaceId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = workspaceId.ToString() } };
            return GetEntityAsync("workspace", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistWorkspace>()
                .Select(x => x.Attach<TwistWorkspace>(this));
        }

        /// <summary>
        /// Gets all channels from Twist
        /// </summary>
        /// <returns>Enumerable of TwistChannel objects</returns>
        [CommandAttribute(
            ObjectType = "TwistChannel",
            PointType = EnumPointType.Function,
            Name = "GetChannels",
            Caption = "Get Channels",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistChannel>"
        )]
        public IEnumerable<TwistChannel> GetChannels()
        {
            return GetEntityAsync("channels", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistChannel>()
                .Select(x => x.Attach<TwistChannel>(this));
        }

        /// <summary>
        /// Gets a specific channel by ID
        /// </summary>
        /// <param name="channelId">The channel ID</param>
        /// <returns>TwistChannel object</returns>
        [CommandAttribute(
            ObjectType = "TwistChannel",
            PointType = EnumPointType.Function,
            Name = "GetChannel",
            Caption = "Get Channel",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistChannel>, Filter: id"
        )]
        public IEnumerable<TwistChannel> GetChannel(int channelId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = channelId.ToString() } };
            return GetEntityAsync("channel", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistChannel>()
                .Select(x => x.Attach<TwistChannel>(this));
        }

        /// <summary>
        /// Gets all threads from Twist
        /// </summary>
        /// <returns>Enumerable of TwistThread objects</returns>
        [CommandAttribute(
            ObjectType = "TwistThread",
            PointType = EnumPointType.Function,
            Name = "GetThreads",
            Caption = "Get Threads",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistThread>"
        )]
        public IEnumerable<TwistThread> GetThreads()
        {
            return GetEntityAsync("threads", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistThread>()
                .Select(x => x.Attach<TwistThread>(this));
        }

        /// <summary>
        /// Gets all messages from Twist
        /// </summary>
        /// <returns>Enumerable of TwistMessage objects</returns>
        [CommandAttribute(
            ObjectType = "TwistMessage",
            PointType = EnumPointType.Function,
            Name = "GetMessages",
            Caption = "Get Messages",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistMessage>"
        )]
        public IEnumerable<TwistMessage> GetMessages()
        {
            return GetEntityAsync("messages", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistMessage>()
                .Select(x => x.Attach<TwistMessage>(this));
        }

        /// <summary>
        /// Gets all users from Twist
        /// </summary>
        /// <returns>Enumerable of TwistUser objects</returns>
        [CommandAttribute(
            ObjectType = "TwistUser",
            PointType = EnumPointType.Function,
            Name = "GetUsers",
            Caption = "Get Users",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistUser>"
        )]
        public IEnumerable<TwistUser> GetUsers()
        {
            return GetEntityAsync("users", new List<AppFilter>())
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistUser>()
                .Select(x => x.Attach<TwistUser>(this));
        }

        /// <summary>
        /// Gets a specific user by ID
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>TwistUser object</returns>
        [CommandAttribute(
            ObjectType = "TwistUser",
            PointType = EnumPointType.Function,
            Name = "GetUser",
            Caption = "Get User",
            ClassName = "TwistDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "twist.png",
            misc = "ReturnType: IEnumerable<TwistUser>, Filter: id"
        )]
        public IEnumerable<TwistUser> GetUser(int userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "id", FilterValue = userId.ToString() } };
            return GetEntityAsync("user", filters)
                .ConfigureAwait(false).GetAwaiter().GetResult()
                .Cast<TwistUser>()
                .Select(x => x.Attach<TwistUser>(this));
        }

        /// <summary>
        /// Creates a message in a Twist thread
        /// </summary>
        [CommandAttribute(
            Name = "CreateMessageAsync",
            Caption = "Create Twist Message",
            ObjectType = "TwistMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            ClassType = "TwistDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "createmessage.png",
            misc = "ReturnType: IEnumerable<TwistMessage>"
        )]
        public async Task<IEnumerable<TwistMessage>> CreateMessageAsync(TwistMessage message)
        {
            try
            {
                var result = await PostAsync("thread_messages/add", message);
                var messages = JsonSerializer.Deserialize<IEnumerable<TwistMessage>>(result);
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<TwistMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating message: {ex.Message}");
            }
            return new List<TwistMessage>();
        }

        /// <summary>
        /// Creates a channel in a Twist workspace
        /// </summary>
        [CommandAttribute(
            Name = "CreateChannelAsync",
            Caption = "Create Twist Channel",
            ObjectType = "TwistChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            ClassType = "TwistDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "createchannel.png",
            misc = "ReturnType: IEnumerable<TwistChannel>"
        )]
        public async Task<IEnumerable<TwistChannel>> CreateChannelAsync(TwistChannel channel)
        {
            try
            {
                var result = await PostAsync("channels/add", channel);
                var channels = JsonSerializer.Deserialize<IEnumerable<TwistChannel>>(result);
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<TwistChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating channel: {ex.Message}");
            }
            return new List<TwistChannel>();
        }

        /// <summary>
        /// Updates a message in a Twist thread
        /// </summary>
        [CommandAttribute(
            Name = "UpdateMessageAsync",
            Caption = "Update Twist Message",
            ObjectType = "TwistMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            ClassType = "TwistDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "updatemessage.png",
            misc = "ReturnType: IEnumerable<TwistMessage>"
        )]
        public async Task<IEnumerable<TwistMessage>> UpdateMessageAsync(TwistMessage message)
        {
            try
            {
                var result = await PutAsync("thread_messages/update", message);
                var messages = JsonSerializer.Deserialize<IEnumerable<TwistMessage>>(result);
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<TwistMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating message: {ex.Message}");
            }
            return new List<TwistMessage>();
        }

        /// <summary>
        /// Updates a channel in a Twist workspace
        /// </summary>
        [CommandAttribute(
            Name = "UpdateChannelAsync",
            Caption = "Update Twist Channel",
            ObjectType = "TwistChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Twist,
            ClassType = "TwistDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "updatechannel.png",
            misc = "ReturnType: IEnumerable<TwistChannel>"
        )]
        public async Task<IEnumerable<TwistChannel>> UpdateChannelAsync(TwistChannel channel)
        {
            try
            {
                var result = await PutAsync("channels/update", channel);
                var channels = JsonSerializer.Deserialize<IEnumerable<TwistChannel>>(result);
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<TwistChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating channel: {ex.Message}");
            }
            return new List<TwistChannel>();
        }

        #endregion
    }
}