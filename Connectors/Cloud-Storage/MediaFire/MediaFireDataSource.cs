using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.MediaFire.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Connectors.MediaFire
{
    public class MediaFireDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for MediaFire API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File and folder operations
            { "files", "https://www.mediafire.com/api/2.0/folder/get_content.php" },
            { "folders", "https://www.mediafire.com/api/2.0/folder/get_info.php" },
            { "fileinfo", "https://www.mediafire.com/api/2.0/file/get_info.php" },
            { "upload", "https://www.mediafire.com/api/2.0/upload/simple.php" },
            { "download", "https://www.mediafire.com/api/2.0/file/get_links.php" },
            { "delete", "https://www.mediafire.com/api/2.0/file/delete.php" },
            { "rename", "https://www.mediafire.com/api/2.0/file/update.php" },
            { "copy", "https://www.mediafire.com/api/2.0/file/copy.php" },
            { "move", "https://www.mediafire.com/api/2.0/file/move.php" },
            { "createfolder", "https://www.mediafire.com/api/2.0/folder/create.php" },
            { "deletefolder", "https://www.mediafire.com/api/2.0/folder/delete.php" },
            { "renamefolder", "https://www.mediafire.com/api/2.0/folder/update.php" },

            // Sharing operations
            { "share", "https://www.mediafire.com/api/2.0/file/get_links.php" },
            { "sharefolder", "https://www.mediafire.com/api/2.0/folder/get_links.php" },
            { "unshare", "https://www.mediafire.com/api/2.0/file/update.php" },

            // User operations
            { "userinfo", "https://www.mediafire.com/api/2.0/user/get_info.php" },
            { "accountinfo", "https://www.mediafire.com/api/2.0/user/get_info.php" },

            // Search operations
            { "search", "https://www.mediafire.com/api/2.0/search.php" }
        };

        // Required filters for each entity
        private static readonly Dictionary<string, List<string>> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            { "fileinfo", new List<string> { "quickkey" } },
            { "download", new List<string> { "quickkey" } },
            { "delete", new List<string> { "quickkey" } },
            { "rename", new List<string> { "quickkey", "filename" } },
            { "copy", new List<string> { "quickkey", "folder_key" } },
            { "move", new List<string> { "quickkey", "folder_key" } },

            // Folder operations
            { "folders", new List<string> { "folder_key" } },
            { "files", new List<string> { "folder_key" } },
            { "createfolder", new List<string> { "foldername" } },
            { "deletefolder", new List<string> { "folder_key" } },
            { "renamefolder", new List<string> { "folder_key", "foldername" } },

            // Sharing operations
            { "share", new List<string> { "quickkey" } },
            { "sharefolder", new List<string> { "folder_key" } },
            { "unshare", new List<string> { "quickkey" } },

            // Search operations
            { "search", new List<string> { "search_text" } }
        };

        public MediaFireDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            try
            {
                return GetEntityAsync(EntityName, filter).GetAwaiter().GetResult() ?? Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error getting entity '{EntityName}': {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (!EntityEndpoints.ContainsKey(EntityName.ToLower()))
                {
                    DMEEditor.AddLogMessage("Beep", $"Entity '{EntityName}' not found in MediaFire endpoints", DateTime.Now, -1, null, Errors.Failed);
                    return Array.Empty<object>();
                }

                var endpoint = EntityEndpoints[EntityName.ToLower()];
                var parameters = new Dictionary<string, string>();

                // Add authentication token
                if (!string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Parameters))
                {
                    var paramDict = ParseParameters(Dataconnection.ConnectionProp.Parameters);
                    if (paramDict.ContainsKey("session_token"))
                    {
                        parameters["session_token"] = paramDict["session_token"];
                    }
                }

                // Add required filters
                if (Filter != null && RequiredFilters.ContainsKey(EntityName.ToLower()))
                {
                    var requiredParams = RequiredFilters[EntityName.ToLower()];
                    foreach (var param in requiredParams)
                    {
                        var filterItem = Filter.FirstOrDefault(f => f.FieldName.Equals(param, StringComparison.OrdinalIgnoreCase));
                        if (filterItem != null)
                        {
                            parameters[param] = filterItem.FilterValue?.ToString() ?? "";
                        }
                    }
                }

                // Add optional filters
                if (Filter != null)
                {
                    foreach (var filterItem in Filter)
                    {
                        if (!parameters.ContainsKey(filterItem.FieldName.ToLower()))
                        {
                            parameters[filterItem.FieldName.ToLower()] = filterItem.FilterValue?.ToString() ?? "";
                        }
                    }
                }

                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                var fullUrl = $"{endpoint}?{queryString}";

                var response = await base.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                // Parse response based on entity type
                return ParseResponse(EntityName.ToLower(), content);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error getting entity '{EntityName}': {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ParseResponse(string entityName, string content)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Check for MediaFire API error
                if (root.TryGetProperty("response", out var responseElement))
                {
                    var result = responseElement.TryGetProperty("result", out var resultProp) ? resultProp.GetString() : null;
                    if (result != null && result != "Success")
                    {
                        var message = responseElement.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : "Unknown MediaFire API error";
                        DMEEditor.AddLogMessage("Beep", $"MediaFire API error: {message}", DateTime.Now, -1, null, Errors.Failed);
                        return Array.Empty<object>();
                    }
                }

                switch (entityName)
                {
                    case "files":
                    case "folders":
                        if (root.TryGetProperty("response", out var resp) &&
                            resp.TryGetProperty("folder_content", out var folderContent))
                        {
                            var files = ExtractArray<MediaFireItem>(folderContent.GetProperty("files")) ?? new List<MediaFireItem>();
                            var folders = ExtractArray<MediaFireItem>(folderContent.GetProperty("folders")) ?? new List<MediaFireItem>();
                            return files.Concat(folders);
                        }
                        break;

                    case "fileinfo":
                        if (root.TryGetProperty("response", out resp) &&
                            resp.TryGetProperty("file_info", out var fileInfo))
                        {
                            var item = JsonSerializer.Deserialize<MediaFireItem>(fileInfo.GetRawText());
                            return item != null ? new[] { item } : Array.Empty<object>();
                        }
                        break;

                    case "userinfo":
                    case "accountinfo":
                        if (root.TryGetProperty("response", out resp) &&
                            resp.TryGetProperty("user_info", out var userInfo))
                        {
                            var user = JsonSerializer.Deserialize<MediaFireUser>(userInfo.GetRawText());
                            return user != null ? new[] { user } : Array.Empty<object>();
                        }
                        break;

                    case "search":
                        if (root.TryGetProperty("response", out resp) &&
                            resp.TryGetProperty("results", out var results))
                        {
                            return ExtractArray<MediaFireItem>(results);
                        }
                        break;

                    case "share":
                    case "sharefolder":
                        if (root.TryGetProperty("response", out resp) &&
                            resp.TryGetProperty("share_link", out var shareLink))
                        {
                            var share = JsonSerializer.Deserialize<MediaFireShare>(shareLink.GetRawText());
                            return share != null ? new[] { share } : Array.Empty<object>();
                        }
                        break;

                    default:
                        return new[] { content }; // Return raw content for unhandled entities
                }

                return Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing MediaFire response: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        private List<T> ExtractArray<T>(JsonElement element) where T : MediaFireEntityBase
        {
            try
            {
                var list = new List<T>();
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        var obj = JsonSerializer.Deserialize<T>(item.GetRawText());
                        if (obj != null)
                        {
                            list.Add(obj);
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error extracting array: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new List<T>();
            }
        }

        private Dictionary<string, string> ParseParameters(string parameters)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(parameters)) return result;

            var pairs = parameters.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            return result;
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireItem", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "List<MediaFireItem>")]
        public List<MediaFireItem> GetFiles(string folderKey = "myfiles")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_key", FilterValue = folderKey } };
            return GetEntity("files", filters).Cast<MediaFireItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireItem", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "List<MediaFireItem>")]
        public List<MediaFireItem> GetFolders(string folderKey = "myfiles")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_key", FilterValue = folderKey } };
            return GetEntity("folders", filters).Cast<MediaFireItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireItem", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "MediaFireItem")]
        public MediaFireItem? GetFileInfo(string quickKey)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "quickkey", FilterValue = quickKey } };
            return GetEntity("fileinfo", filters).Cast<MediaFireItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireUser", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "MediaFireUser")]
        public MediaFireUser? GetUserInfo()
        {
            return GetEntity("userinfo", new List<AppFilter>()).Cast<MediaFireUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireUser", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "MediaFireUser")]
        public MediaFireUser? GetAccountInfo()
        {
            return GetEntity("accountinfo", new List<AppFilter>()).Cast<MediaFireUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireItem", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "List<MediaFireItem>")]
        public List<MediaFireItem> Search(string searchText)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "search_text", FilterValue = searchText } };
            return GetEntity("search", filters).Cast<MediaFireItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireShare", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "MediaFireShare")]
        public MediaFireShare? ShareFile(string quickKey)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "quickkey", FilterValue = quickKey } };
            return GetEntity("share", filters).Cast<MediaFireShare>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MediaFire, PointType = EnumPointType.Function, ObjectType = "MediaFireShare", ClassName = "MediaFireDataSource", Showin = ShowinType.Both, misc = "MediaFireShare")]
        public MediaFireShare? ShareFolder(string folderKey)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folder_key", FilterValue = folderKey } };
            return GetEntity("sharefolder", filters).Cast<MediaFireShare>().FirstOrDefault();
        }

        [CommandAttribute(
            Name = "UploadFileAsync",
            Caption = "Upload MediaFire File",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "uploadfile.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> UploadFileAsync(MediaFireItem file)
        {
            try
            {
                var result = await PostAsync("upload_simple", file);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error uploading file: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        [CommandAttribute(
            Name = "CreateFolderAsync",
            Caption = "Create MediaFire Folder",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "createfolder.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> CreateFolderAsync(MediaFireItem folder)
        {
            try
            {
                var result = await PostAsync("folder_create", folder);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating folder: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        [CommandAttribute(
            Name = "CopyFileAsync",
            Caption = "Copy MediaFire File",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "copyfile.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> CopyFileAsync(MediaFireItem file)
        {
            try
            {
                var result = await PostAsync("file_copy", file);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error copying file: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        [CommandAttribute(
            Name = "UpdateFileAsync",
            Caption = "Update MediaFire File",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "updatefile.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> UpdateFileAsync(MediaFireItem file)
        {
            try
            {
                var result = await PatchAsync("upload_simple", file);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating file: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        [CommandAttribute(
            Name = "UpdateFolderAsync",
            Caption = "Update MediaFire Folder",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "updatefolder.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> UpdateFolderAsync(MediaFireItem folder)
        {
            try
            {
                var result = await PatchAsync("folder_create", folder);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating folder: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        [CommandAttribute(
            Name = "UpdateCopyAsync",
            Caption = "Update MediaFire Copy",
            ObjectType = "MediaFireItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.MediaFire,
            ClassType = "MediaFireDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "updatecopy.png",
            misc = "ReturnType: IEnumerable<MediaFireItem>"
        )]
        public async Task<IEnumerable<MediaFireItem>> UpdateCopyAsync(MediaFireItem file)
        {
            try
            {
                var result = await PatchAsync("file_copy", file);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<MediaFireItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<MediaFireItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating copy: {ex.Message}");
            }
            return new List<MediaFireItem>();
        }

        #endregion
    }
}
