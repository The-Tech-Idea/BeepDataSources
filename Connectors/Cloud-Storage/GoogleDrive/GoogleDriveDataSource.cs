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
using TheTechIdea.Beep.Connectors.GoogleDrive.Models;

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

            return ExtractArray(resp, EntityName);
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

        // Extracts array from response into strongly typed objects based on entity name
        private static IEnumerable<object> ExtractArray(HttpResponseMessage resp, string entityName)
        {
            var list = new List<object>();
            if (resp == null || !resp.IsSuccessStatusCode) return list;

            var json = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(json)) return list;

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return entityName.ToLowerInvariant() switch
            {
                "files" => DeserializeEntity<GoogleDriveFile>(json, opts),
                "file" => DeserializeEntity<GoogleDriveFile>(json, opts),
                "folders" => DeserializeEntity<GoogleDriveFile>(json, opts),
                "folder" => DeserializeEntity<GoogleDriveFile>(json, opts),
                "permissions" => DeserializeEntity<GoogleDrivePermission>(json, opts),
                "permission" => DeserializeEntity<GoogleDrivePermission>(json, opts),
                "revisions" => DeserializeEntity<GoogleDriveRevision>(json, opts),
                "revision" => DeserializeEntity<GoogleDriveRevision>(json, opts),
                "comments" => DeserializeEntity<GoogleDriveComment>(json, opts),
                "comment" => DeserializeEntity<GoogleDriveComment>(json, opts),
                "changes" => DeserializeEntity<GoogleDriveChange>(json, opts),
                "change" => DeserializeEntity<GoogleDriveChange>(json, opts),
                _ => DeserializeEntity<Dictionary<string, object>>(json, opts)
            };
        }

        private static List<object> DeserializeEntity<T>(string json, JsonSerializerOptions opts)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check if response has a "files" array (Google Drive API pattern)
                if (root.TryGetProperty("files", out var filesArray) && filesArray.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<object>();
                    foreach (var item in filesArray.EnumerateArray())
                    {
                        var entity = JsonSerializer.Deserialize<T>(item.GetRawText(), opts);
                        if (entity != null) list.Add(entity);
                    }
                    return list;
                }
                // Check if response has an "items" array (alternative pattern)
                else if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<object>();
                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        var entity = JsonSerializer.Deserialize<T>(item.GetRawText(), opts);
                        if (entity != null) list.Add(entity);
                    }
                    return list;
                }
                // Single object response
                else
                {
                    var entity = JsonSerializer.Deserialize<T>(json, opts);
                    return entity != null ? new List<object> { entity } : new List<object>();
                }
            }
            catch
            {
                // Fallback to dictionary if deserialization fails
                return new List<object> { JsonSerializer.Deserialize<Dictionary<string, object>>(json, opts) ?? new Dictionary<string, object>() };
            }
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveFile", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDriveFile>")]
        public List<GoogleDriveFile> GetFiles()
        {
            return GetEntity("files", new List<AppFilter>()).Cast<GoogleDriveFile>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveFile", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "GoogleDriveFile")]
        public GoogleDriveFile? GetFile(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileId", FilterValue = fileId } };
            return GetEntity("file", filters).Cast<GoogleDriveFile>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveFile", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDriveFile>")]
        public List<GoogleDriveFile> GetFolders()
        {
            return GetEntity("folders", new List<AppFilter>()).Cast<GoogleDriveFile>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveFile", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "GoogleDriveFile")]
        public GoogleDriveFile? GetFolder(string folderId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folderId", FilterValue = folderId } };
            return GetEntity("folder", filters).Cast<GoogleDriveFile>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDrivePermission", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDrivePermission>")]
        public List<GoogleDrivePermission> GetPermissions(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileId", FilterValue = fileId } };
            return GetEntity("permissions", filters).Cast<GoogleDrivePermission>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDrivePermission", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "GoogleDrivePermission")]
        public GoogleDrivePermission? GetPermission(string fileId, string permissionId)
        {
            var filters = new List<AppFilter> 
            { 
                new AppFilter { FieldName = "fileId", FilterValue = fileId },
                new AppFilter { FieldName = "permissionId", FilterValue = permissionId }
            };
            return GetEntity("permission", filters).Cast<GoogleDrivePermission>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveRevision", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDriveRevision>")]
        public List<GoogleDriveRevision> GetRevisions(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileId", FilterValue = fileId } };
            return GetEntity("revisions", filters).Cast<GoogleDriveRevision>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveRevision", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "GoogleDriveRevision")]
        public GoogleDriveRevision? GetRevision(string fileId, string revisionId)
        {
            var filters = new List<AppFilter> 
            { 
                new AppFilter { FieldName = "fileId", FilterValue = fileId },
                new AppFilter { FieldName = "revisionId", FilterValue = revisionId }
            };
            return GetEntity("revision", filters).Cast<GoogleDriveRevision>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveComment", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDriveComment>")]
        public List<GoogleDriveComment> GetComments(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileId", FilterValue = fileId } };
            return GetEntity("comments", filters).Cast<GoogleDriveComment>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveComment", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "GoogleDriveComment")]
        public GoogleDriveComment? GetComment(string fileId, string commentId)
        {
            var filters = new List<AppFilter> 
            { 
                new AppFilter { FieldName = "fileId", FilterValue = fileId },
                new AppFilter { FieldName = "commentId", FilterValue = commentId }
            };
            return GetEntity("comment", filters).Cast<GoogleDriveComment>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive, PointType = EnumPointType.Function, ObjectType = "GoogleDriveChange", ClassName = "GoogleDriveDataSource", Showin = ShowinType.Both, misc = "List<GoogleDriveChange>")]
        public List<GoogleDriveChange> GetChanges()
        {
            return GetEntity("changes", new List<AppFilter>()).Cast<GoogleDriveChange>().ToList();
        }

        [CommandAttribute(Name = "CreateFileAsync", Caption = "Create Google Drive File",
            ObjectType = "GoogleDriveFile", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive,
            ClassType = "GoogleDriveDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "googledrive.png", misc = "Create a file or folder")]
        public async Task<IEnumerable<GoogleDriveFile>> CreateFileAsync(GoogleDriveFile file, List<AppFilter> filters = null)
        {
            var result = await PostAsync("files", file, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<GoogleDriveFile>>(result);
        }

        [CommandAttribute(Name = "CreatePermissionAsync", Caption = "Create Google Drive Permission",
            ObjectType = "GoogleDrivePermission", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive,
            ClassType = "GoogleDriveDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "googledrive.png", misc = "Create a permission for a file")]
        public async Task<IEnumerable<GoogleDrivePermission>> CreatePermissionAsync(GoogleDrivePermission permission, List<AppFilter> filters = null)
        {
            var result = await PostAsync("permissions", permission, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<GoogleDrivePermission>>(result);
        }

        [CommandAttribute(Name = "CreateCommentAsync", Caption = "Create Google Drive Comment",
            ObjectType = "GoogleDriveComment", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.GoogleDrive,
            ClassType = "GoogleDriveDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "googledrive.png", misc = "Create a comment on a file")]
        public async Task<IEnumerable<GoogleDriveComment>> CreateCommentAsync(GoogleDriveComment comment, List<AppFilter> filters = null)
        {
            var result = await PostAsync("comments", comment, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<GoogleDriveComment>>(result);
        }

        #endregion
    }
}
