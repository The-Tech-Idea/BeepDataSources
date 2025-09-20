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
    }
}