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
using TheTechIdea.Beep.Connectors.Communication.Chanty.Models;

namespace TheTechIdea.Beep.Connectors.Communication.Chanty
{
    /// <summary>
    /// Chanty API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Chanty API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Chanty)]
    public class ChantyDataSource : WebAPIDataSource
    {
        // Supported Chanty entities -> Chanty endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["users"] = ("users", null, Array.Empty<string>()),
                ["user"] = ("users/{user_id}", null, new[] { "user_id" }),
                ["teams"] = ("teams", null, Array.Empty<string>()),
                ["team"] = ("teams/{team_id}", null, new[] { "team_id" }),
                ["team_members"] = ("teams/{team_id}/members", null, new[] { "team_id" }),
                ["channels"] = ("teams/{team_id}/channels", null, new[] { "team_id" }),
                ["channel"] = ("channels/{channel_id}", null, new[] { "channel_id" }),
                ["channel_members"] = ("channels/{channel_id}/members", null, new[] { "channel_id" }),
                ["messages"] = ("channels/{channel_id}/messages", null, new[] { "channel_id" }),
                ["message"] = ("messages/{message_id}", null, new[] { "message_id" }),
                ["message_reactions"] = ("messages/{message_id}/reactions", null, new[] { "message_id" }),
                ["message_replies"] = ("messages/{message_id}/replies", null, new[] { "message_id" }),
                ["files"] = ("channels/{channel_id}/files", null, new[] { "channel_id" }),
                ["file"] = ("files/{file_id}", null, new[] { "file_id" }),
                ["notifications"] = ("users/{user_id}/notifications", null, new[] { "user_id" }),
                ["user_settings"] = ("users/{user_id}/settings", null, new[] { "user_id" }),
                ["team_settings"] = ("teams/{team_id}/settings", null, new[] { "team_id" }),
                ["integrations"] = ("teams/{team_id}/integrations", null, new[] { "team_id" }),
                ["webhooks"] = ("teams/{team_id}/webhooks", null, new[] { "team_id" }),
                ["audit_logs"] = ("teams/{team_id}/audit-logs", null, new[] { "team_id" })
            };

        public ChantyDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
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

        // ---------------- Overrides (same signatures) ----------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Chanty entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            var endpoint = ReplacePathParameters(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root ?? "data");
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        // ---------------- Helper Methods ----------------

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
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Chanty entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ReplacePathParameters(string endpoint, Dictionary<string, string> q)
        {
            var result = endpoint;
            foreach (var kvp in q)
            {
                var placeholder = $"{{{kvp.Key}}}";
                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, Uri.EscapeDataString(kvp.Value));
                }
            }
            return result;
        }

        private IEnumerable<object> ExtractArray(HttpResponseMessage resp, string? root)
        {
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<object>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var rootElement = doc.RootElement;

                if (!string.IsNullOrEmpty(root))
                {
                    if (rootElement.TryGetProperty(root, out var rootProp))
                        rootElement = rootProp;
                }

                if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    return rootElement.EnumerateArray().Select(e => (object)e.Clone()).ToList();
                }
                else if (rootElement.ValueKind == JsonValueKind.Object)
                {
                    return new List<object> { rootElement.Clone() };
                }

                return Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        // ---------------- CommandAttribute methods ----------------

        [CommandAttribute(
            ObjectType = "ChantyUser",
            PointType = EnumPointType.Function,
            Name = "GetUsers",
            Caption = "Get Users",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "chanty.png",
            misc = "ReturnType: IEnumerable<ChantyUser>"
        )]
        public async Task<IEnumerable<ChantyUser>> GetUsers()
        {
            return (await GetEntityAsync("users", new List<AppFilter>())).Cast<ChantyUser>();
        }

        [CommandAttribute(
            ObjectType = "ChantyUser",
            PointType = EnumPointType.Function,
            Name = "GetUser",
            Caption = "Get User",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "chanty.png",
            misc = "user_id|ReturnType: IEnumerable<ChantyUser>"
        )]
        public async Task<IEnumerable<ChantyUser>> GetUser(string userId)
        {
            return (await GetEntityAsync("user", new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } })).Cast<ChantyUser>();
        }

        [CommandAttribute(
            ObjectType = "ChantyTeam",
            PointType = EnumPointType.Function,
            Name = "GetTeams",
            Caption = "Get Teams",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "chanty.png",
            misc = "ReturnType: IEnumerable<ChantyTeam>"
        )]
        public async Task<IEnumerable<ChantyTeam>> GetTeams()
        {
            return (await GetEntityAsync("teams", new List<AppFilter>())).Cast<ChantyTeam>();
        }

        [CommandAttribute(
            ObjectType = "ChantyTeam",
            PointType = EnumPointType.Function,
            Name = "GetTeam",
            Caption = "Get Team",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyTeam>"
        )]
        public async Task<IEnumerable<ChantyTeam>> GetTeam(string teamId)
        {
            return (await GetEntityAsync("team", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyTeam>();
        }

        [CommandAttribute(
            ObjectType = "ChantyTeamMember",
            PointType = EnumPointType.Function,
            Name = "GetTeamMembers",
            Caption = "Get Team Members",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyTeamMember>"
        )]
        public async Task<IEnumerable<ChantyTeamMember>> GetTeamMembers(string teamId)
        {
            return (await GetEntityAsync("team_members", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyTeamMember>();
        }

        [CommandAttribute(
            ObjectType = "ChantyChannel",
            PointType = EnumPointType.Function,
            Name = "GetChannels",
            Caption = "Get Channels",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyChannel>"
        )]
        public async Task<IEnumerable<ChantyChannel>> GetChannels(string teamId)
        {
            return (await GetEntityAsync("channels", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyChannel>();
        }

        [CommandAttribute(
            ObjectType = "ChantyChannel",
            PointType = EnumPointType.Function,
            Name = "GetChannel",
            Caption = "Get Channel",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "chanty.png",
            misc = "channel_id|ReturnType: IEnumerable<ChantyChannel>"
        )]
        public async Task<IEnumerable<ChantyChannel>> GetChannel(string channelId)
        {
            return (await GetEntityAsync("channel", new List<AppFilter> { new AppFilter { FieldName = "channel_id", FilterValue = channelId } })).Cast<ChantyChannel>();
        }

        [CommandAttribute(
            ObjectType = "ChantyChannelMember",
            PointType = EnumPointType.Function,
            Name = "GetChannelMembers",
            Caption = "Get Channel Members",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "chanty.png",
            misc = "channel_id|ReturnType: IEnumerable<ChantyChannelMember>"
        )]
        public async Task<IEnumerable<ChantyChannelMember>> GetChannelMembers(string channelId)
        {
            return (await GetEntityAsync("channel_members", new List<AppFilter> { new AppFilter { FieldName = "channel_id", FilterValue = channelId } })).Cast<ChantyChannelMember>();
        }

        [CommandAttribute(
            ObjectType = "ChantyMessage",
            PointType = EnumPointType.Function,
            Name = "GetMessages",
            Caption = "Get Messages",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "chanty.png",
            misc = "channel_id|ReturnType: IEnumerable<ChantyMessage>"
        )]
        public async Task<IEnumerable<ChantyMessage>> GetMessages(string channelId)
        {
            return (await GetEntityAsync("messages", new List<AppFilter> { new AppFilter { FieldName = "channel_id", FilterValue = channelId } })).Cast<ChantyMessage>();
        }

        [CommandAttribute(
            ObjectType = "ChantyMessage",
            PointType = EnumPointType.Function,
            Name = "GetMessage",
            Caption = "Get Message",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "chanty.png",
            misc = "message_id|ReturnType: IEnumerable<ChantyMessage>"
        )]
        public async Task<IEnumerable<ChantyMessage>> GetMessage(string messageId)
        {
            return (await GetEntityAsync("message", new List<AppFilter> { new AppFilter { FieldName = "message_id", FilterValue = messageId } })).Cast<ChantyMessage>();
        }

        [CommandAttribute(
            ObjectType = "ChantyReaction",
            PointType = EnumPointType.Function,
            Name = "GetMessageReactions",
            Caption = "Get Message Reactions",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "chanty.png",
            misc = "message_id|ReturnType: IEnumerable<ChantyReaction>"
        )]
        public async Task<IEnumerable<ChantyReaction>> GetMessageReactions(string messageId)
        {
            return (await GetEntityAsync("message_reactions", new List<AppFilter> { new AppFilter { FieldName = "message_id", FilterValue = messageId } })).Cast<ChantyReaction>();
        }

        [CommandAttribute(
            ObjectType = "ChantyMessageReply",
            PointType = EnumPointType.Function,
            Name = "GetMessageReplies",
            Caption = "Get Message Replies",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "chanty.png",
            misc = "message_id|ReturnType: IEnumerable<ChantyMessageReply>"
        )]
        public async Task<IEnumerable<ChantyMessageReply>> GetMessageReplies(string messageId)
        {
            return (await GetEntityAsync("message_replies", new List<AppFilter> { new AppFilter { FieldName = "message_id", FilterValue = messageId } })).Cast<ChantyMessageReply>();
        }

        [CommandAttribute(
            ObjectType = "ChantyFile",
            PointType = EnumPointType.Function,
            Name = "GetFiles",
            Caption = "Get Files",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "chanty.png",
            misc = "channel_id|ReturnType: IEnumerable<ChantyFile>"
        )]
        public async Task<IEnumerable<ChantyFile>> GetFiles(string channelId)
        {
            return (await GetEntityAsync("files", new List<AppFilter> { new AppFilter { FieldName = "channel_id", FilterValue = channelId } })).Cast<ChantyFile>();
        }

        [CommandAttribute(
            ObjectType = "ChantyFile",
            PointType = EnumPointType.Function,
            Name = "GetFile",
            Caption = "Get File",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "chanty.png",
            misc = "file_id|ReturnType: IEnumerable<ChantyFile>"
        )]
        public async Task<IEnumerable<ChantyFile>> GetFile(string fileId)
        {
            return (await GetEntityAsync("file", new List<AppFilter> { new AppFilter { FieldName = "file_id", FilterValue = fileId } })).Cast<ChantyFile>();
        }

        [CommandAttribute(
            ObjectType = "ChantyNotification",
            PointType = EnumPointType.Function,
            Name = "GetNotifications",
            Caption = "Get Notifications",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 15,
            iconimage = "chanty.png",
            misc = "user_id|ReturnType: IEnumerable<ChantyNotification>"
        )]
        public async Task<IEnumerable<ChantyNotification>> GetNotifications(string userId)
        {
            return (await GetEntityAsync("notifications", new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } })).Cast<ChantyNotification>();
        }

        [CommandAttribute(
            ObjectType = "ChantyUserSettings",
            PointType = EnumPointType.Function,
            Name = "GetUserSettings",
            Caption = "Get User Settings",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 16,
            iconimage = "chanty.png",
            misc = "user_id|ReturnType: IEnumerable<ChantyUserSettings>"
        )]
        public async Task<IEnumerable<ChantyUserSettings>> GetUserSettings(string userId)
        {
            return (await GetEntityAsync("user_settings", new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } })).Cast<ChantyUserSettings>();
        }

        [CommandAttribute(
            ObjectType = "ChantyTeamSettings",
            PointType = EnumPointType.Function,
            Name = "GetTeamSettings",
            Caption = "Get Team Settings",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 17,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyTeamSettings>"
        )]
        public async Task<IEnumerable<ChantyTeamSettings>> GetTeamSettings(string teamId)
        {
            return (await GetEntityAsync("team_settings", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyTeamSettings>();
        }

        [CommandAttribute(
            ObjectType = "ChantyIntegration",
            PointType = EnumPointType.Function,
            Name = "GetIntegrations",
            Caption = "Get Integrations",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 18,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyIntegration>"
        )]
        public async Task<IEnumerable<ChantyIntegration>> GetIntegrations(string teamId)
        {
            return (await GetEntityAsync("integrations", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyIntegration>();
        }

        [CommandAttribute(
            ObjectType = "ChantyWebhook",
            PointType = EnumPointType.Function,
            Name = "GetWebhooks",
            Caption = "Get Webhooks",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 19,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyWebhook>"
        )]
        public async Task<IEnumerable<ChantyWebhook>> GetWebhooks(string teamId)
        {
            return (await GetEntityAsync("webhooks", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyWebhook>();
        }

        [CommandAttribute(
            ObjectType = "ChantyAuditLog",
            PointType = EnumPointType.Function,
            Name = "GetAuditLogs",
            Caption = "Get Audit Logs",
            ClassName = "ChantyDataSource",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            Showin = ShowinType.Both,
            Order = 20,
            iconimage = "chanty.png",
            misc = "team_id|ReturnType: IEnumerable<ChantyAuditLog>"
        )]
        public async Task<IEnumerable<ChantyAuditLog>> GetAuditLogs(string teamId)
        {
            return (await GetEntityAsync("audit_logs", new List<AppFilter> { new AppFilter { FieldName = "team_id", FilterValue = teamId } })).Cast<ChantyAuditLog>();
        }

        [CommandAttribute(
            Name = "CreateMessageAsync",
            Caption = "Create Chanty Message",
            ObjectType = "ChantyMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            ClassType = "ChantyDataSource",
            Showin = ShowinType.Both,
            Order = 21,
            iconimage = "createmessage.png",
            misc = "ReturnType: IEnumerable<ChantyMessage>"
        )]
        public async Task<IEnumerable<ChantyMessage>> CreateMessageAsync(ChantyMessage message)
        {
            try
            {
                var result = await PostAsync("channels/{channel_id}/messages", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<ChantyMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<ChantyMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating message: {ex.Message}");
            }
            return new List<ChantyMessage>();
        }

        [CommandAttribute(
            Name = "CreateChannelAsync",
            Caption = "Create Chanty Channel",
            ObjectType = "ChantyChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            ClassType = "ChantyDataSource",
            Showin = ShowinType.Both,
            Order = 22,
            iconimage = "createchannel.png",
            misc = "ReturnType: IEnumerable<ChantyChannel>"
        )]
        public async Task<IEnumerable<ChantyChannel>> CreateChannelAsync(ChantyChannel channel)
        {
            try
            {
                var result = await PostAsync("teams/{team_id}/channels", channel);
                var content = await result.Content.ReadAsStringAsync();
                var channels = JsonSerializer.Deserialize<IEnumerable<ChantyChannel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<ChantyChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating channel: {ex.Message}");
            }
            return new List<ChantyChannel>();
        }

        [CommandAttribute(
            Name = "UpdateMessageAsync",
            Caption = "Update Chanty Message",
            ObjectType = "ChantyMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            ClassType = "ChantyDataSource",
            Showin = ShowinType.Both,
            Order = 23,
            iconimage = "updatemessage.png",
            misc = "ReturnType: IEnumerable<ChantyMessage>"
        )]
        public async Task<IEnumerable<ChantyMessage>> UpdateMessageAsync(ChantyMessage message)
        {
            try
            {
                var result = await PutAsync("messages/{message_id}", message);
                var content = await result.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<IEnumerable<ChantyMessage>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<ChantyMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating message: {ex.Message}");
            }
            return new List<ChantyMessage>();
        }

        [CommandAttribute(
            Name = "UpdateChannelAsync",
            Caption = "Update Chanty Channel",
            ObjectType = "ChantyChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Chanty,
            ClassType = "ChantyDataSource",
            Showin = ShowinType.Both,
            Order = 24,
            iconimage = "updatechannel.png",
            misc = "ReturnType: IEnumerable<ChantyChannel>"
        )]
        public async Task<IEnumerable<ChantyChannel>> UpdateChannelAsync(ChantyChannel channel)
        {
            try
            {
                var result = await PutAsync("channels/{channel_id}", channel);
                var content = await result.Content.ReadAsStringAsync();
                var channels = JsonSerializer.Deserialize<IEnumerable<ChantyChannel>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<ChantyChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating channel: {ex.Message}");
            }
            return new List<ChantyChannel>();
        }
    }
}