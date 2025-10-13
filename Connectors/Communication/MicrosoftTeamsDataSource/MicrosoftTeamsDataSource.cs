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

namespace TheTechIdea.Beep.Connectors.Communication.MicrosoftTeams
{
    using Models;

    /// <summary>
    /// Microsoft Teams API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Microsoft Graph API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MicrosoftTeams)]
    public class MicrosoftTeamsDataSource : WebAPIDataSource
    {
        // Supported Microsoft Teams entities -> Microsoft Graph endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string? root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["teams"] = ("v1.0/teams", null, Array.Empty<string>()),
                ["team"] = ("v1.0/teams/{team_id}", null, new[] { "team_id" }),
                ["channels"] = ("v1.0/teams/{team_id}/channels", null, new[] { "team_id" }),
                ["channel"] = ("v1.0/teams/{team_id}/channels/{channel_id}", null, new[] { "team_id", "channel_id" }),
                ["channel_messages"] = ("v1.0/teams/{team_id}/channels/{channel_id}/messages", null, new[] { "team_id", "channel_id" }),
                ["channel_message"] = ("v1.0/teams/{team_id}/channels/{channel_id}/messages/{message_id}", null, new[] { "team_id", "channel_id", "message_id" }),
                ["channel_tabs"] = ("v1.0/teams/{team_id}/channels/{channel_id}/tabs", null, new[] { "team_id", "channel_id" }),
                ["channel_members"] = ("v1.0/teams/{team_id}/channels/{channel_id}/members", null, new[] { "team_id", "channel_id" }),
                ["team_members"] = ("v1.0/teams/{team_id}/members", null, new[] { "team_id" }),
                ["team_apps"] = ("v1.0/teams/{team_id}/installedApps", null, new[] { "team_id" }),
                ["chats"] = ("v1.0/chats", null, Array.Empty<string>()),
                ["chat"] = ("v1.0/chats/{chat_id}", null, new[] { "chat_id" }),
                ["chat_messages"] = ("v1.0/chats/{chat_id}/messages", null, new[] { "chat_id" }),
                ["chat_message"] = ("v1.0/chats/{chat_id}/messages/{message_id}", null, new[] { "chat_id", "message_id" }),
                ["chat_members"] = ("v1.0/chats/{chat_id}/members", null, new[] { "chat_id" }),
                ["users"] = ("v1.0/users", null, Array.Empty<string>()),
                ["user"] = ("v1.0/users/{user_id}", null, new[] { "user_id" }),
                ["me"] = ("v1.0/me", null, Array.Empty<string>()),
                ["me_joined_teams"] = ("v1.0/me/joinedTeams", null, Array.Empty<string>()),
                ["me_chats"] = ("v1.0/me/chats", null, Array.Empty<string>()),
                ["apps"] = ("v1.0/appCatalogs/teamsApps", null, Array.Empty<string>()),
                ["app"] = ("v1.0/appCatalogs/teamsApps/{app_id}", null, new[] { "app_id" })
            };

        public MicrosoftTeamsDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection != null && Dataconnection.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register entities from Map
            EntitiesNames = Map.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures as base) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Microsoft Teams entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, requiredFilters);

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, root ?? "value");
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var mapping))
                throw new InvalidOperationException($"Unknown Microsoft Teams entity '{EntityName}'.");

            var (endpoint, root, requiredFilters) = mapping;
            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, requiredFilters);

            // Microsoft Graph uses $top and $skip for pagination
            q["$top"] = Math.Max(1, Math.Min(pageSize, 999)).ToString();
            if (pageNumber > 1)
                q["$skip"] = ((pageNumber - 1) * pageSize).ToString();

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = GetAsync(resolvedEndpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            if (resp is null || !resp.IsSuccessStatusCode)
                return new PagedResult { Data = Array.Empty<object>() };

            var items = ExtractArray(resp, root ?? "value");

            // Microsoft Graph doesn't provide total count in all cases, so we estimate
            return new PagedResult
            {
                Data = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = items.Count, // Conservative estimate
                TotalPages = 1, // Unknown without @odata.count
                HasPreviousPage = pageNumber > 1,
                HasNextPage = items.Count >= pageSize // Assume more if we got full page
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

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Microsoft Teams entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            // Substitute {param} from filters if present
            var result = template;
            foreach (var param in new[] { "team_id", "channel_id", "message_id", "chat_id", "user_id", "app_id" })
            {
                if (result.Contains($"{{{param}}}", StringComparison.Ordinal))
                {
                    if (!q.TryGetValue(param, out var value) || string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException($"Missing required '{param}' filter for this endpoint.");
                    result = result.Replace($"{{{param}}}", Uri.EscapeDataString(value));
                }
            }
            return result;
        }

        // Extracts array from response using the specified root path
        private static List<object> ExtractArray(HttpResponseMessage resp, string root)
        {
            var list = new List<object>();
            if (resp == null) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);

            JsonElement node = doc.RootElement;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!node.TryGetProperty(root, out node))
                    return list; // no data -> empty
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (node.ValueKind == JsonValueKind.Array)
            {
                list.Capacity = node.GetArrayLength();
                foreach (var el in node.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // =======================================================
        // CommandAttribute Methods
        // =======================================================

        [CommandAttribute(
            Name = "GetTeams",
            Caption = "Get Microsoft Teams",
            ObjectType = "TeamsTeam",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "teams.png",
            misc = "ReturnType: IEnumerable<TeamsTeam>"
        )]
        public async Task<IEnumerable<TeamsTeam>> GetTeams(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("teams", filters ?? new List<AppFilter>());
            return result.Cast<TeamsTeam>().Select(t => t.Attach<TeamsTeam>(this));
        }

        [CommandAttribute(
            Name = "GetTeam",
            Caption = "Get Microsoft Team by ID",
            ObjectType = "TeamsTeam",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "team.png",
            misc = "ReturnType: IEnumerable<TeamsTeam>"
        )]
        public async Task<IEnumerable<TeamsTeam>> GetTeam(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("team", filters ?? new List<AppFilter>());
            return result.Cast<TeamsTeam>().Select(t => t.Attach<TeamsTeam>(this));
        }

        [CommandAttribute(
            Name = "GetChannels",
            Caption = "Get Microsoft Teams Channels",
            ObjectType = "TeamsChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "channels.png",
            misc = "ReturnType: IEnumerable<TeamsChannel>"
        )]
        public async Task<IEnumerable<TeamsChannel>> GetChannels(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channels", filters ?? new List<AppFilter>());
            return result.Cast<TeamsChannel>().Select(c => c.Attach<TeamsChannel>(this));
        }

        [CommandAttribute(
            Name = "GetChannel",
            Caption = "Get Microsoft Teams Channel by ID",
            ObjectType = "TeamsChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "channel.png",
            misc = "ReturnType: IEnumerable<TeamsChannel>"
        )]
        public async Task<IEnumerable<TeamsChannel>> GetChannel(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channel", filters ?? new List<AppFilter>());
            return result.Cast<TeamsChannel>().Select(c => c.Attach<TeamsChannel>(this));
        }

        [CommandAttribute(
            Name = "GetChannelMessages",
            Caption = "Get Microsoft Teams Channel Messages",
            ObjectType = "TeamsMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "messages.png",
            misc = "ReturnType: IEnumerable<TeamsMessage>"
        )]
        public async Task<IEnumerable<TeamsMessage>> GetChannelMessages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channel_messages", filters ?? new List<AppFilter>());
            return result.Cast<TeamsMessage>().Select(m => m.Attach<TeamsMessage>(this));
        }

        [CommandAttribute(
            Name = "GetChannelMessage",
            Caption = "Get Microsoft Teams Channel Message by ID",
            ObjectType = "TeamsMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "message.png",
            misc = "ReturnType: IEnumerable<TeamsMessage>"
        )]
        public async Task<IEnumerable<TeamsMessage>> GetChannelMessage(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channel_message", filters ?? new List<AppFilter>());
            return result.Cast<TeamsMessage>().Select(m => m.Attach<TeamsMessage>(this));
        }

        [CommandAttribute(
            Name = "GetTeamMembers",
            Caption = "Get Microsoft Teams Team Members",
            ObjectType = "TeamsTeamMember",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "members.png",
            misc = "ReturnType: IEnumerable<TeamsTeamMember>"
        )]
        public async Task<IEnumerable<TeamsTeamMember>> GetTeamMembers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("team_members", filters ?? new List<AppFilter>());
            return result.Cast<TeamsTeamMember>().Select(m => m.Attach<TeamsTeamMember>(this));
        }

        [CommandAttribute(
            Name = "GetChats",
            Caption = "Get Microsoft Teams Chats",
            ObjectType = "TeamsChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "chats.png",
            misc = "ReturnType: IEnumerable<TeamsChat>"
        )]
        public async Task<IEnumerable<TeamsChat>> GetChats(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("chats", filters ?? new List<AppFilter>());
            return result.Cast<TeamsChat>().Select(c => c.Attach<TeamsChat>(this));
        }

        [CommandAttribute(
            Name = "GetChat",
            Caption = "Get Microsoft Teams Chat by ID",
            ObjectType = "TeamsChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "chat.png",
            misc = "ReturnType: IEnumerable<TeamsChat>"
        )]
        public async Task<IEnumerable<TeamsChat>> GetChat(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("chat", filters ?? new List<AppFilter>());
            return result.Cast<TeamsChat>().Select(c => c.Attach<TeamsChat>(this));
        }

        [CommandAttribute(
            Name = "GetChatMessages",
            Caption = "Get Microsoft Teams Chat Messages",
            ObjectType = "TeamsMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "chatmessages.png",
            misc = "ReturnType: IEnumerable<TeamsMessage>"
        )]
        public async Task<IEnumerable<TeamsMessage>> GetChatMessages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("chat_messages", filters ?? new List<AppFilter>());
            return result.Cast<TeamsMessage>().Select(m => m.Attach<TeamsMessage>(this));
        }

        [CommandAttribute(
            Name = "GetUsers",
            Caption = "Get Microsoft Teams Users",
            ObjectType = "TeamsUser",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "users.png",
            misc = "ReturnType: IEnumerable<TeamsUser>"
        )]
        public async Task<IEnumerable<TeamsUser>> GetUsers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("users", filters ?? new List<AppFilter>());
            return result.Cast<TeamsUser>().Select(u => u.Attach<TeamsUser>(this));
        }

        [CommandAttribute(
            Name = "GetMe",
            Caption = "Get Current Microsoft Teams User",
            ObjectType = "TeamsMe",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "me.png",
            misc = "ReturnType: IEnumerable<TeamsMe>"
        )]
        public async Task<IEnumerable<TeamsMe>> GetMe(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("me", filters ?? new List<AppFilter>());
            return result.Cast<TeamsMe>().Select(m => m.Attach<TeamsMe>(this));
        }

        [CommandAttribute(
            Name = "GetMyJoinedTeams",
            Caption = "Get My Joined Microsoft Teams",
            ObjectType = "TeamsJoinedTeam",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "myteams.png",
            misc = "ReturnType: IEnumerable<TeamsJoinedTeam>"
        )]
        public async Task<IEnumerable<TeamsJoinedTeam>> GetMyJoinedTeams(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("me_joined_teams", filters ?? new List<AppFilter>());
            return result.Cast<TeamsJoinedTeam>().Select(t => t.Attach<TeamsJoinedTeam>(this));
        }

        [CommandAttribute(
            Name = "GetMyChats",
            Caption = "Get My Microsoft Teams Chats",
            ObjectType = "TeamsMeChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "mychats.png",
            misc = "ReturnType: IEnumerable<TeamsMeChat>"
        )]
        public async Task<IEnumerable<TeamsMeChat>> GetMyChats(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("me_chats", filters ?? new List<AppFilter>());
            return result.Cast<TeamsMeChat>().Select(c => c.Attach<TeamsMeChat>(this));
        }

        [CommandAttribute(
            Name = "GetApps",
            Caption = "Get Microsoft Teams Apps",
            ObjectType = "TeamsApp",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 15,
            iconimage = "apps.png",
            misc = "ReturnType: IEnumerable<TeamsApp>"
        )]
        public async Task<IEnumerable<TeamsApp>> GetApps(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("apps", filters ?? new List<AppFilter>());
            return result.Cast<TeamsApp>().Select(a => a.Attach<TeamsApp>(this));
        }

        [CommandAttribute(
            Name = "CreateMessageAsync",
            Caption = "Create Microsoft Teams Message",
            ObjectType = "TeamsMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "microsoftteams.png",
            misc = "ReturnType: IEnumerable<TeamsMessage>"
        )]
        public async Task<IEnumerable<TeamsMessage>> CreateMessageAsync(TeamsMessage message)
        {
            try
            {
                var result = await PostAsync("teams/{team_id}/channels/{channel_id}/messages", message);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdMessage = JsonSerializer.Deserialize<TeamsMessage>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<TeamsMessage> { createdMessage }.Select(m => m.Attach<TeamsMessage>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating message: {ex.Message}");
            }
            return new List<TeamsMessage>();
        }

        [CommandAttribute(
            Name = "CreateChannelAsync",
            Caption = "Create Microsoft Teams Channel",
            ObjectType = "TeamsChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "microsoftteams.png",
            misc = "ReturnType: IEnumerable<TeamsChannel>"
        )]
        public async Task<IEnumerable<TeamsChannel>> CreateChannelAsync(TeamsChannel channel)
        {
            try
            {
                var result = await PostAsync("teams/{team_id}/channels", channel);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdChannel = JsonSerializer.Deserialize<TeamsChannel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<TeamsChannel> { createdChannel }.Select(c => c.Attach<TeamsChannel>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating channel: {ex.Message}");
            }
            return new List<TeamsChannel>();
        }

        [CommandAttribute(
            Name = "CreateTeamAsync",
            Caption = "Create Microsoft Teams Team",
            ObjectType = "TeamsTeam",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "microsoftteams.png",
            misc = "ReturnType: IEnumerable<TeamsTeam>"
        )]
        public async Task<IEnumerable<TeamsTeam>> CreateTeamAsync(TeamsTeam team)
        {
            try
            {
                var result = await PostAsync("teams", team);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdTeam = JsonSerializer.Deserialize<TeamsTeam>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<TeamsTeam> { createdTeam }.Select(t => t.Attach<TeamsTeam>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating team: {ex.Message}");
            }
            return new List<TeamsTeam>();
        }

        [CommandAttribute(
            Name = "UpdateTeamAsync",
            Caption = "Update Microsoft Teams Team",
            ObjectType = "TeamsTeam",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "updateteam.png",
            misc = "ReturnType: IEnumerable<TeamsTeam>"
        )]
        public async Task<IEnumerable<TeamsTeam>> UpdateTeamAsync(TeamsTeam team)
        {
            try
            {
                var result = await PutAsync("teams/{team_id}", team);
                var teams = JsonSerializer.Deserialize<IEnumerable<TeamsTeam>>(result);
                if (teams != null)
                {
                    foreach (var t in teams)
                    {
                        t.Attach<TeamsTeam>(this);
                    }
                }
                return teams;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating team: {ex.Message}");
            }
            return new List<TeamsTeam>();
        }

        [CommandAttribute(
            Name = "CreateChatAsync",
            Caption = "Create Microsoft Teams Chat",
            ObjectType = "TeamsChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 10,
            iconimage = "createchat.png",
            misc = "ReturnType: IEnumerable<TeamsChat>"
        )]
        public async Task<IEnumerable<TeamsChat>> CreateChatAsync(TeamsChat chat)
        {
            try
            {
                var result = await PostAsync("chats", chat);
                var chats = JsonSerializer.Deserialize<IEnumerable<TeamsChat>>(result);
                if (chats != null)
                {
                    foreach (var c in chats)
                    {
                        c.Attach<TeamsChat>(this);
                    }
                }
                return chats;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating chat: {ex.Message}");
            }
            return new List<TeamsChat>();
        }

        [CommandAttribute(
            Name = "UpdateChatAsync",
            Caption = "Update Microsoft Teams Chat",
            ObjectType = "TeamsChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 11,
            iconimage = "updatechat.png",
            misc = "ReturnType: IEnumerable<TeamsChat>"
        )]
        [CommandAttribute(
            Name = "UpdateChannelAsync",
            Caption = "Update Microsoft Teams Channel",
            ObjectType = "TeamsChannel",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 12,
            iconimage = "updatechannel.png",
            misc = "ReturnType: IEnumerable<TeamsChannel>"
        )]
        public async Task<IEnumerable<TeamsChannel>> UpdateChannelAsync(TeamsChannel channel)
        {
            try
            {
                var result = await PutAsync("teams/{team_id}/channels/{channel_id}", channel);
                var channels = JsonSerializer.Deserialize<IEnumerable<TeamsChannel>>(result);
                if (channels != null)
                {
                    foreach (var c in channels)
                    {
                        c.Attach<TeamsChannel>(this);
                    }
                }
                return channels;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating channel: {ex.Message}");
            }
            return new List<TeamsChannel>();
        }

        [CommandAttribute(
            Name = "UpdateMessageAsync",
            Caption = "Update Microsoft Teams Message",
            ObjectType = "TeamsMessage",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 13,
            iconimage = "updatemessage.png",
            misc = "ReturnType: IEnumerable<TeamsMessage>"
        )]
        public async Task<IEnumerable<TeamsMessage>> UpdateMessageAsync(TeamsMessage message)
        {
            try
            {
                var result = await PutAsync("teams/{team_id}/channels/{channel_id}/messages/{message_id}", message);
                var messages = JsonSerializer.Deserialize<IEnumerable<TeamsMessage>>(result);
                if (messages != null)
                {
                    foreach (var m in messages)
                    {
                        m.Attach<TeamsMessage>(this);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating message: {ex.Message}");
            }
            return new List<TeamsMessage>();
        }

        [CommandAttribute(
            Name = "UpdateChatAsync",
            Caption = "Update Microsoft Teams Chat",
            ObjectType = "TeamsChat",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MicrosoftTeams,
            ClassType = "MicrosoftTeamsDataSource",
            Showin = ShowinType.Both,
            Order = 14,
            iconimage = "updatechat.png",
            misc = "ReturnType: IEnumerable<TeamsChat>"
        )]
        public async Task<IEnumerable<TeamsChat>> UpdateChatAsync(TeamsChat chat)
        {
            try
            {
                var result = await PutAsync("chats/{chat_id}", chat);
                var chats = JsonSerializer.Deserialize<IEnumerable<TeamsChat>>(result);
                if (chats != null)
                {
                    foreach (var c in chats)
                    {
                        c.Attach<TeamsChat>(this);
                    }
                }
                return chats;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating chat: {ex.Message}");
            }
            return new List<TeamsChat>();
        }
    }
}