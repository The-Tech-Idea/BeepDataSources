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
                throw new InvalidOperationException($"Unknown Chanty entity '{EntityName}'.");

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
                throw new InvalidOperationException($"Unknown Chanty entity '{EntityName}'.");

            var q = FiltersToQuery(filter);
            RequireFilters(EntityName, q, m.requiredFilters);

            // Chanty API supports pagination with limit and offset
            q["limit"] = Math.Max(1, Math.Min(pageSize, 100)).ToString(); // Chanty max is usually 100
            q["offset"] = ((pageNumber - 1) * pageSize).ToString();

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
                    throw new ArgumentException($"Chanty entity '{entity}' requires '{req}' parameter in filters.");
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

        private static List<object> ExtractArray(HttpResponseMessage resp, string? rootPath)
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