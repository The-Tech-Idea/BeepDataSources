using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Dropbox
{
    /// <summary>
    /// Dropbox data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, and team operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Dropbox)]
    public class DropboxDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Dropbox API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://api.dropboxapi.com/2/files/list_folder",
            ["file_details"] = "https://api.dropboxapi.com/2/files/get_metadata",
            ["file_download"] = "https://content.dropboxapi.com/2/files/download",
            // Folder operations
            ["folders"] = "https://api.dropboxapi.com/2/files/list_folder",
            ["folder_details"] = "https://api.dropboxapi.com/2/files/get_metadata",
            // Sharing operations
            ["shared_links"] = "https://api.dropboxapi.com/2/sharing/list_shared_links",
            ["shared_folders"] = "https://api.dropboxapi.com/2/sharing/list_folders",
            // Account operations
            ["account_info"] = "https://api.dropboxapi.com/2/users/get_current_account",
            ["space_usage"] = "https://api.dropboxapi.com/2/users/get_space_usage",
            // Team operations (if applicable)
            ["team_members"] = "https://api.dropboxapi.com/2/team/members/list",
            ["team_info"] = "https://api.dropboxapi.com/2/team/get_info"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require path
            ["files"] = new[] { "path" },
            ["file_details"] = new[] { "path" },
            ["file_download"] = new[] { "path" },
            // Folder operations require path
            ["folders"] = new[] { "path" },
            ["folder_details"] = new[] { "path" },
            // Sharing operations don't require filters
            ["shared_links"] = Array.Empty<string>(),
            ["shared_folders"] = Array.Empty<string>(),
            // Account operations don't require filters
            ["account_info"] = Array.Empty<string>(),
            ["space_usage"] = Array.Empty<string>(),
            // Team operations don't require filters
            ["team_members"] = Array.Empty<string>(),
            ["team_info"] = Array.Empty<string>()
        };

        public DropboxDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Dropbox entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "entries");
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
                throw new ArgumentException($"Dropbox entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle path parameter
            if (result.Contains("{path}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Missing required 'path' filter for this endpoint.");
                result = result.Replace("{path}", Uri.EscapeDataString(path));
            }

            return result;
        }

        // Extracts array from response into a List<object> (Dictionary<string,object> per item).
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
                    return list; // no root -> empty
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
