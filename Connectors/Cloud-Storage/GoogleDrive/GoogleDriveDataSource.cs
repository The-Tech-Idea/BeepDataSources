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

namespace TheTechIdea.Beep.Connectors.GoogleDrive
{
    /// <summary>
    /// Google Drive data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, permissions, revisions, and comments
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive)]
    public class GoogleDriveDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Google Drive API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://www.googleapis.com/drive/v3/files",
            ["file_details"] = "https://www.googleapis.com/drive/v3/files/{file_id}",
            // Folder operations
            ["folders"] = "https://www.googleapis.com/drive/v3/files?q=mimeType='application/vnd.google-apps.folder'",
            // Permission operations
            ["permissions"] = "https://www.googleapis.com/drive/v3/files/{file_id}/permissions",
            ["permission_details"] = "https://www.googleapis.com/drive/v3/files/{file_id}/permissions/{permission_id}",
            // Revision operations
            ["revisions"] = "https://www.googleapis.com/drive/v3/files/{file_id}/revisions",
            ["revision_details"] = "https://www.googleapis.com/drive/v3/files/{file_id}/revisions/{revision_id}",
            // Comment operations
            ["comments"] = "https://www.googleapis.com/drive/v3/files/{file_id}/comments",
            ["comment_details"] = "https://www.googleapis.com/drive/v3/files/{file_id}/comments/{comment_id}",
            // Changes operations
            ["changes"] = "https://www.googleapis.com/drive/v3/changes"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations don't require filters
            ["files"] = Array.Empty<string>(),
            ["file_details"] = new[] { "file_id" },
            // Folder operations don't require filters
            ["folders"] = Array.Empty<string>(),
            // Permission operations require file_id
            ["permissions"] = new[] { "file_id" },
            ["permission_details"] = new[] { "file_id", "permission_id" },
            // Revision operations require file_id
            ["revisions"] = new[] { "file_id" },
            ["revision_details"] = new[] { "file_id", "revision_id" },
            // Comment operations require file_id
            ["comments"] = new[] { "file_id" },
            ["comment_details"] = new[] { "file_id", "comment_id" },
            // Changes operations don't require filters
            ["changes"] = Array.Empty<string>()
        };

        public GoogleDriveDataSource(
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
                throw new InvalidOperationException($"Unknown Google Drive entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "files");
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
                throw new ArgumentException($"Google Drive entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle file_id
            if (result.Contains("{file_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("file_id", out var fileId) || string.IsNullOrWhiteSpace(fileId))
                    throw new ArgumentException("Missing required 'file_id' filter for this endpoint.");
                result = result.Replace("{file_id}", Uri.EscapeDataString(fileId));
            }

            // Handle permission_id
            if (result.Contains("{permission_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("permission_id", out var permissionId) || string.IsNullOrWhiteSpace(permissionId))
                    throw new ArgumentException("Missing required 'permission_id' filter for this endpoint.");
                result = result.Replace("{permission_id}", Uri.EscapeDataString(permissionId));
            }

            // Handle revision_id
            if (result.Contains("{revision_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("revision_id", out var revisionId) || string.IsNullOrWhiteSpace(revisionId))
                    throw new ArgumentException("Missing required 'revision_id' filter for this endpoint.");
                result = result.Replace("{revision_id}", Uri.EscapeDataString(revisionId));
            }

            // Handle comment_id
            if (result.Contains("{comment_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("comment_id", out var commentId) || string.IsNullOrWhiteSpace(commentId))
                    throw new ArgumentException("Missing required 'comment_id' filter for this endpoint.");
                result = result.Replace("{comment_id}", Uri.EscapeDataString(commentId));
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
