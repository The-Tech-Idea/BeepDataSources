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
    }
}
