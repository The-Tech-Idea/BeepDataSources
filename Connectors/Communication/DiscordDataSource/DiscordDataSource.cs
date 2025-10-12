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

namespace TheTechIdea.Beep.Connectors.Communication.Discord
{
    /// <summary>
    /// Discord API data source built on WebAPIDataSource.
    /// Configure WebAPIConnectionProperties.Url to Discord API base URL and set Authorization header.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord)]
    public class DiscordDataSource : WebAPIDataSource
    {
        // Supported Discord entities -> Discord endpoint + result root + required filters
        private static readonly Dictionary<string, (string endpoint, string root, string[] requiredFilters)> Map
            = new(StringComparer.OrdinalIgnoreCase)
            {
                ["guilds"] = ("users/@me/guilds", "", Array.Empty<string>()),
                ["channels"] = ("guilds/{guild_id}/channels", "", new[] { "guild_id" }),
                ["messages"] = ("channels/{channel_id}/messages", "", new[] { "channel_id" }),
                ["users"] = ("users/@me", "", Array.Empty<string>()),
                ["guild_members"] = ("guilds/{guild_id}/members", "", new[] { "guild_id" }),
                ["roles"] = ("guilds/{guild_id}/roles", "", new[] { "guild_id" }),
                ["emojis"] = ("guilds/{guild_id}/emojis", "", new[] { "guild_id" }),
                ["stickers"] = ("guilds/{guild_id}/stickers", "", new[] { "guild_id" }),
                ["invites"] = ("guilds/{guild_id}/invites", "", new[] { "guild_id" }),
                ["voice_states"] = ("guilds/{guild_id}/voice-states", "", new[] { "guild_id" }),
                ["webhooks"] = ("guilds/{guild_id}/webhooks", "", new[] { "guild_id" }),
                ["applications"] = ("applications/@me", "", Array.Empty<string>()),
                ["audit_logs"] = ("guilds/{guild_id}/audit-logs", "", new[] { "guild_id" }),
                ["integrations"] = ("guilds/{guild_id}/integrations", "", new[] { "guild_id" }),
                ["guild_scheduled_events"] = ("guilds/{guild_id}/scheduled-events", "", new[] { "guild_id" }),
                ["stage_instances"] = ("guilds/{guild_id}/stage-instances", "", new[] { "guild_id" }),
                ["auto_moderation_rules"] = ("guilds/{guild_id}/auto-moderation/rules", "", new[] { "guild_id" })
            };

        public DiscordDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props exist; caller configures Url/Auth outside this class.
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
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
                throw new InvalidOperationException($"Unknown Discord entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Replace path parameters in endpoint
            var endpoint = ReplacePathParameters(m.endpoint, q);

            using var resp = await GetAsync(endpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, m.root);
        }

        // paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            if (!Map.TryGetValue(EntityName, out var m))
                throw new InvalidOperationException($"Unknown Discord entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Discord API doesn't have built-in pagination like Slack, but we can implement basic paging
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Discord max is 100

            // For entities that support before/after pagination
            if (pageNumber > 1 && filter.Any(f => f.FieldName == "before" || f.FieldName == "after"))
            {
                // Keep existing pagination parameters
            }

            var endpoint = ReplacePathParameters(m.endpoint, q);

            var resp = GetAsync(endpoint, q).ConfigureAwait(false).GetAwaiter().GetResult();
            var items = ExtractArray(resp, m.root);

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
                    throw new ArgumentException($"Discord entity '{entity}' requires '{req}' parameter in filters.");
            }
        }

        private static string ReplacePathParameters(string endpoint, Dictionary<string, string> parameters)
        {
            var result = endpoint;
            foreach (var param in parameters)
            {
                result = result.Replace($"{{{param.Key}}}", param.Value);
            }
            return result;
        }

        private static List<object> ExtractArray(HttpResponseMessage resp, string rootPath)
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
                    var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), opts);
                    if (obj != null) list.Add(obj);
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                // wrap single object
                var obj = JsonSerializer.Deserialize<Dictionary<string, object>>(node.GetRawText(), opts);
                if (obj != null) list.Add(obj);
            }

            return list;
        }

        // CommandAttribute methods for Discord API
        [CommandAttribute(Name = "GetGuilds", Caption = "Get Discord Guilds", ObjectType = "DiscordGuild", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord, ClassType = "DiscordGuild", Showin = ShowinType.Both, Order = 1, iconimage = "guild.png")]
        public async Task<IEnumerable<DiscordGuild>> GetGuilds(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("guilds", filters);
            return result.Cast<DiscordGuild>();
        }

        [CommandAttribute(Name = "GetChannels", Caption = "Get Discord Channels", ObjectType = "DiscordChannel", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord, ClassType = "DiscordChannel", Showin = ShowinType.Both, Order = 2, iconimage = "channel.png")]
        public async Task<IEnumerable<DiscordChannel>> GetChannels(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("channels", filters);
            return result.Cast<DiscordChannel>();
        }

        [CommandAttribute(Name = "GetMessages", Caption = "Get Discord Messages", ObjectType = "DiscordMessage", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord, ClassType = "DiscordMessage", Showin = ShowinType.Both, Order = 3, iconimage = "message.png")]
        public async Task<IEnumerable<DiscordMessage>> GetMessages(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("messages", filters);
            return result.Cast<DiscordMessage>();
        }

        [CommandAttribute(Name = "GetUsers", Caption = "Get Discord Users", ObjectType = "DiscordUser", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord, ClassType = "DiscordUser", Showin = ShowinType.Both, Order = 4, iconimage = "user.png")]
        public async Task<IEnumerable<DiscordUser>> GetUsers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("users", filters);
            return result.Cast<DiscordUser>();
        }

        [CommandAttribute(Name = "GetGuildMembers", Caption = "Get Discord Guild Members", ObjectType = "DiscordGuildMember", PointType = EnumPointType.Function, Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Discord, ClassType = "DiscordGuildMember", Showin = ShowinType.Both, Order = 5, iconimage = "member.png")]
        public async Task<IEnumerable<DiscordGuildMember>> GetGuildMembers(List<AppFilter> filters = null)
        {
            var result = await GetEntityAsync("guild_members", filters);
            return result.Cast<DiscordGuildMember>();
        }

        /// <summary>
        /// Creates a message in a Discord channel
        /// </summary>
        [CommandAttribute(
            Name = "CreateMessage",
            Caption = "Create Discord Message",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Discord,
            PointType = EnumPointType.Function,
            ObjectType = "DiscordMessage",
            ClassType = "DiscordDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "message.png",
            misc = "ReturnType: IEnumerable<DiscordMessage>"
        )]
        public async Task<IEnumerable<DiscordMessage>> CreateMessageAsync(string channelId, DiscordMessage message)
        {
            var url = $"https://discord.com/api/v10/channels/{channelId}/messages";
            var response = await PostAsync(url, message);
            var json = await response.Content.ReadAsStringAsync();
            var createdMessage = JsonSerializer.Deserialize<DiscordMessage>(json);
            return createdMessage != null ? new[] { createdMessage } : Array.Empty<DiscordMessage>();
        }

        /// <summary>
        /// Creates a channel in a Discord guild
        /// </summary>
        [CommandAttribute(
            Name = "CreateChannel",
            Caption = "Create Discord Channel",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Discord,
            PointType = EnumPointType.Function,
            ObjectType = "DiscordChannel",
            ClassType = "DiscordDataSource",
            Showin = ShowinType.Both,
            Order = 7,
            iconimage = "channel.png",
            misc = "ReturnType: IEnumerable<DiscordChannel>"
        )]
        public async Task<IEnumerable<DiscordChannel>> CreateChannelAsync(string guildId, DiscordChannel channel)
        {
            var url = $"https://discord.com/api/v10/guilds/{guildId}/channels";
            var response = await PostAsync(url, channel);
            var json = await response.Content.ReadAsStringAsync();
            var createdChannel = JsonSerializer.Deserialize<DiscordChannel>(json);
            return createdChannel != null ? new[] { createdChannel } : Array.Empty<DiscordChannel>();
        }

        /// <summary>
        /// Creates a role in a Discord guild
        /// </summary>
        [CommandAttribute(
            Name = "CreateRole",
            Caption = "Create Discord Role",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Discord,
            PointType = EnumPointType.Function,
            ObjectType = "DiscordRole",
            ClassType = "DiscordDataSource",
            Showin = ShowinType.Both,
            Order = 8,
            iconimage = "role.png",
            misc = "ReturnType: IEnumerable<DiscordRole>"
        )]
        public async Task<IEnumerable<DiscordRole>> CreateRoleAsync(string guildId, DiscordRole role)
        {
            var url = $"https://discord.com/api/v10/guilds/{guildId}/roles";
            var response = await PostAsync(url, role);
            var json = await response.Content.ReadAsStringAsync();
            var createdRole = JsonSerializer.Deserialize<DiscordRole>(json);
            return createdRole != null ? new[] { createdRole } : Array.Empty<DiscordRole>();
        }

        /// <summary>
        /// Updates a role in a Discord guild
        /// </summary>
        [CommandAttribute(
            Name = "UpdateRole",
            Caption = "Update Discord Role",
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Discord,
            PointType = EnumPointType.Function,
            ObjectType = "DiscordRole",
            ClassType = "DiscordDataSource",
            Showin = ShowinType.Both,
            Order = 9,
            iconimage = "role.png",
            misc = "ReturnType: IEnumerable<DiscordRole>"
        )]
        public async Task<IEnumerable<DiscordRole>> UpdateRoleAsync(string guildId, string roleId, DiscordRole role)
        {
            var url = $"https://discord.com/api/v10/guilds/{guildId}/roles/{roleId}";
            var response = await PatchAsync(url, role);
            var json = await response.Content.ReadAsStringAsync();
            var updatedRole = JsonSerializer.Deserialize<DiscordRole>(json);
            return updatedRole != null ? new[] { updatedRole } : Array.Empty<DiscordRole>();
        }
    }
}
