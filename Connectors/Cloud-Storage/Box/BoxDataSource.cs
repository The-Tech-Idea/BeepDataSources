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
using TheTechIdea.Beep.Connectors.Box.Models;

namespace TheTechIdea.Beep.Connectors.Box
{
    /// <summary>
    /// Box data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, users, groups, and metadata operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box)]
    public class BoxDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Box API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://api.box.com/2.0/folders/{folder_id}/items",
            ["file_info"] = "https://api.box.com/2.0/files/{file_id}",
            ["file_content"] = "https://api.box.com/2.0/files/{file_id}/content",
            ["file_versions"] = "https://api.box.com/2.0/files/{file_id}/versions",
            // Folder operations
            ["folders"] = "https://api.box.com/2.0/folders/{folder_id}",
            ["folder_items"] = "https://api.box.com/2.0/folders/{folder_id}/items",
            // User operations
            ["users"] = "https://api.box.com/2.0/users",
            ["user_info"] = "https://api.box.com/2.0/users/{user_id}",
            ["current_user"] = "https://api.box.com/2.0/users/me",
            // Group operations
            ["groups"] = "https://api.box.com/2.0/groups",
            ["group_info"] = "https://api.box.com/2.0/groups/{group_id}",
            // Shared link operations
            ["shared_links"] = "https://api.box.com/2.0/files/{file_id}#shared_link",
            // Metadata operations
            ["metadata"] = "https://api.box.com/2.0/files/{file_id}/metadata/global/properties",
            // Webhook operations
            ["webhooks"] = "https://api.box.com/2.0/webhooks",
            ["webhook_info"] = "https://api.box.com/2.0/webhooks/{webhook_id}",
            // Search operations
            ["search"] = "https://api.box.com/2.0/search"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require folder_id or file_id
            ["files"] = new[] { "folder_id" },
            ["file_info"] = new[] { "file_id" },
            ["file_content"] = new[] { "file_id" },
            ["file_versions"] = new[] { "file_id" },
            // Folder operations require folder_id
            ["folders"] = new[] { "folder_id" },
            ["folder_items"] = new[] { "folder_id" },
            // User operations may require user_id for specific user
            ["users"] = Array.Empty<string>(),
            ["user_info"] = new[] { "user_id" },
            ["current_user"] = Array.Empty<string>(),
            // Group operations may require group_id for specific group
            ["groups"] = Array.Empty<string>(),
            ["group_info"] = new[] { "group_id" },
            // Shared link operations require file_id
            ["shared_links"] = new[] { "file_id" },
            // Metadata operations require file_id
            ["metadata"] = new[] { "file_id" },
            // Webhook operations may require webhook_id for specific webhook
            ["webhooks"] = Array.Empty<string>(),
            ["webhook_info"] = new[] { "webhook_id" },
            // Search operations don't require filters (query can be optional)
            ["search"] = Array.Empty<string>()
        };

        public BoxDataSource(
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
                throw new InvalidOperationException($"Unknown Box entity '{EntityName}'.");

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
                throw new ArgumentException($"Box entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle folder_id
            if (result.Contains("{folder_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("folder_id", out var folderId) || string.IsNullOrWhiteSpace(folderId))
                    folderId = "0"; // Default to root folder
                result = result.Replace("{folder_id}", Uri.EscapeDataString(folderId));
            }

            // Handle file_id
            if (result.Contains("{file_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("file_id", out var fileId) || string.IsNullOrWhiteSpace(fileId))
                    throw new ArgumentException("Missing required 'file_id' filter for this endpoint.");
                result = result.Replace("{file_id}", Uri.EscapeDataString(fileId));
            }

            // Handle user_id
            if (result.Contains("{user_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("user_id", out var userId) || string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("Missing required 'user_id' filter for this endpoint.");
                result = result.Replace("{user_id}", Uri.EscapeDataString(userId));
            }

            // Handle group_id
            if (result.Contains("{group_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("group_id", out var groupId) || string.IsNullOrWhiteSpace(groupId))
                    throw new ArgumentException("Missing required 'group_id' filter for this endpoint.");
                result = result.Replace("{group_id}", Uri.EscapeDataString(groupId));
            }

            // Handle webhook_id
            if (result.Contains("{webhook_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("webhook_id", out var webhookId) || string.IsNullOrWhiteSpace(webhookId))
                    throw new ArgumentException("Missing required 'webhook_id' filter for this endpoint.");
                result = result.Replace("{webhook_id}", Uri.EscapeDataString(webhookId));
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

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxItem", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxItem>")]
        public List<BoxItem> GetFiles(string folderId = "0")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_id", FilterValue = folderId } };
            return GetEntity("files", filters).Cast<BoxItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxItem", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxItem")]
        public BoxItem? GetFile(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "file_id", FilterValue = fileId } };
            return GetEntity("file_info", filters).Cast<BoxItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxItem", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxItem>")]
        public List<BoxItem> GetFolders(string folderId = "0")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_id", FilterValue = folderId } };
            return GetEntity("folder_items", filters).Cast<BoxItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxItem", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxItem")]
        public BoxItem? GetFolder(string folderId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_id", FilterValue = folderId } };
            return GetEntity("folders", filters).Cast<BoxItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxFileVersion", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxFileVersion>")]
        public List<BoxFileVersion> GetFileVersions(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "file_id", FilterValue = fileId } };
            return GetEntity("file_versions", filters).Cast<BoxFileVersion>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxUser", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxUser>")]
        public List<BoxUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<BoxUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxUser", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxUser")]
        public BoxUser? GetUser(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } };
            return GetEntity("user_info", filters).Cast<BoxUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxUser", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxUser")]
        public BoxUser? GetCurrentUser()
        {
            return GetEntity("current_user", new List<AppFilter>()).Cast<BoxUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxGroup", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxGroup>")]
        public List<BoxGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<BoxGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxGroup", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxGroup")]
        public BoxGroup? GetGroup(string groupId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "group_id", FilterValue = groupId } };
            return GetEntity("group_info", filters).Cast<BoxGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxSharedLink", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxSharedLink")]
        public BoxSharedLink? GetSharedLink(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "file_id", FilterValue = fileId } };
            return GetEntity("shared_links", filters).Cast<BoxSharedLink>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxWebhook", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxWebhook>")]
        public List<BoxWebhook> GetWebhooks()
        {
            return GetEntity("webhooks", new List<AppFilter>()).Cast<BoxWebhook>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxWebhook", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "BoxWebhook")]
        public BoxWebhook? GetWebhook(string webhookId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "webhook_id", FilterValue = webhookId } };
            return GetEntity("webhook_info", filters).Cast<BoxWebhook>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Box, PointType = EnumPointType.Function, ObjectType = "BoxSearchResult", ClassName = "BoxDataSource", Showin = ShowinType.Both, misc = "List<BoxSearchResult>")]
        public List<BoxSearchResult> Search(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query } };
            return GetEntity("search", filters).Cast<BoxSearchResult>().ToList();
        }

        [CommandAttribute(
            Name = "CreateFolderAsync",
            Caption = "Create Box Folder",
            ObjectType = "BoxFolder",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "createfolder.png",
            misc = "ReturnType: IEnumerable<BoxFolder>"
        )]
        public async Task<IEnumerable<BoxFolder>> CreateFolderAsync(BoxFolder folder)
        {
            try
            {
                var result = await PostAsync("folders", folder);
                var folders = JsonSerializer.Deserialize<IEnumerable<BoxFolder>>(result);
                if (folders != null)
                {
                    foreach (var f in folders)
                    {
                        f.Attach<BoxFolder>(this);
                    }
                }
                return folders;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating folder: {ex.Message}");
            }
            return new List<BoxFolder>();
        }

        [CommandAttribute(
            Name = "UploadFileAsync",
            Caption = "Upload Box File",
            ObjectType = "BoxFile",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "uploadfile.png",
            misc = "ReturnType: IEnumerable<BoxFile>"
        )]
        public async Task<IEnumerable<BoxFile>> UploadFileAsync(BoxFile file)
        {
            try
            {
                var result = await PostAsync("files_content", file);
                var files = JsonSerializer.Deserialize<IEnumerable<BoxFile>>(result);
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        f.Attach<BoxFile>(this);
                    }
                }
                return files;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error uploading file: {ex.Message}");
            }
            return new List<BoxFile>();
        }

        [CommandAttribute(
            Name = "CreateWebhookAsync",
            Caption = "Create Box Webhook",
            ObjectType = "BoxWebhook",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "createwebhook.png",
            misc = "ReturnType: IEnumerable<BoxWebhook>"
        )]
        public async Task<IEnumerable<BoxWebhook>> CreateWebhookAsync(BoxWebhook webhook)
        {
            try
            {
                var result = await PostAsync("webhooks", webhook);
                var webhooks = JsonSerializer.Deserialize<IEnumerable<BoxWebhook>>(result);
                if (webhooks != null)
                {
                    foreach (var w in webhooks)
                    {
                        w.Attach<BoxWebhook>(this);
                    }
                }
                return webhooks;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating webhook: {ex.Message}");
            }
            return new List<BoxWebhook>();
        }

        [CommandAttribute(
            Name = "UpdateFolderAsync",
            Caption = "Update Box Folder",
            ObjectType = "BoxFolder",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "updatefolder.png",
            misc = "ReturnType: IEnumerable<BoxFolder>"
        )]
        public async Task<IEnumerable<BoxFolder>> UpdateFolderAsync(BoxFolder folder)
        {
            try
            {
                var result = await PutAsync("folders", folder);
                var folders = JsonSerializer.Deserialize<IEnumerable<BoxFolder>>(result);
                if (folders != null)
                {
                    foreach (var f in folders)
                    {
                        f.Attach<BoxFolder>(this);
                    }
                }
                return folders;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating folder: {ex.Message}");
            }
            return new List<BoxFolder>();
        }

        [CommandAttribute(
            Name = "UpdateFileAsync",
            Caption = "Update Box File",
            ObjectType = "BoxFile",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "updatefile.png",
            misc = "ReturnType: IEnumerable<BoxFile>"
        )]
        public async Task<IEnumerable<BoxFile>> UpdateFileAsync(BoxFile file)
        {
            try
            {
                var result = await PutAsync("files", file);
                var files = JsonSerializer.Deserialize<IEnumerable<BoxFile>>(result);
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        f.Attach<BoxFile>(this);
                    }
                }
                return files;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating file: {ex.Message}");
            }
            return new List<BoxFile>();
        }

        [CommandAttribute(
            Name = "UpdateWebhookAsync",
            Caption = "Update Box Webhook",
            ObjectType = "BoxWebhook",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.Box,
            ClassType = "BoxDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "updatewebhook.png",
            misc = "ReturnType: IEnumerable<BoxWebhook>"
        )]
        public async Task<IEnumerable<BoxWebhook>> UpdateWebhookAsync(BoxWebhook webhook)
        {
            try
            {
                var result = await PutAsync("webhooks", webhook);
                var webhooks = JsonSerializer.Deserialize<IEnumerable<BoxWebhook>>(result);
                if (webhooks != null)
                {
                    foreach (var w in webhooks)
                    {
                        w.Attach<BoxWebhook>(this);
                    }
                }
                return webhooks;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating webhook: {ex.Message}");
            }
            return new List<BoxWebhook>();
        }

        #endregion
    }
}
