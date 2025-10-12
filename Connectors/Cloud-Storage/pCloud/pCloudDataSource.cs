using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.pCloud.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Connectors.pCloud
{
    public class pCloudDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for pCloud API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File and folder operations
            { "files", "https://api.pcloud.com/listfolder" },
            { "folders", "https://api.pcloud.com/listfolder" },
            { "fileinfo", "https://api.pcloud.com/stat" },
            { "upload", "https://api.pcloud.com/uploadfile" },
            { "download", "https://api.pcloud.com/download" },
            { "delete", "https://api.pcloud.com/deletefile" },
            { "rename", "https://api.pcloud.com/renamefile" },
            { "copy", "https://api.pcloud.com/copyfile" },
            { "move", "https://api.pcloud.com/renamefile" },
            { "createfolder", "https://api.pcloud.com/createfolder" },
            { "deletefolder", "https://api.pcloud.com/deletefolder" },
            { "renamefolder", "https://api.pcloud.com/renamefolder" },
            { "copyfolder", "https://api.pcloud.com/copyfolder" },
            { "movefolder", "https://api.pcloud.com/renamefolder" },

            // Sharing operations
            { "share", "https://api.pcloud.com/getfilelink" },
            { "sharefolder", "https://api.pcloud.com/getfolderlink" },
            { "unshare", "https://api.pcloud.com/deletefilelink" },
            { "unsharefolder", "https://api.pcloud.com/deletefolderlink" },

            // User operations
            { "userinfo", "https://api.pcloud.com/userinfo" },
            { "accountinfo", "https://api.pcloud.com/userinfo" },

            // Search operations
            { "search", "https://api.pcloud.com/search" },

            // Thumbnail operations
            { "thumbnail", "https://api.pcloud.com/getthumb" },

            // Public link operations
            { "publiclink", "https://api.pcloud.com/getfilepublink" },
            { "publicfolderlink", "https://api.pcloud.com/getfolderpublink" }
        };

        // Required filters for each entity
        private static readonly Dictionary<string, List<string>> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            { "fileinfo", new List<string> { "fileid" } },
            { "download", new List<string> { "fileid" } },
            { "delete", new List<string> { "fileid" } },
            { "rename", new List<string> { "fileid", "toname" } },
            { "copy", new List<string> { "fileid", "tofolderid" } },
            { "move", new List<string> { "fileid", "tofolderid" } },

            // Folder operations
            { "folders", new List<string> { "folderid" } },
            { "files", new List<string> { "folderid" } },
            { "createfolder", new List<string> { "name", "folderid" } },
            { "deletefolder", new List<string> { "folderid" } },
            { "renamefolder", new List<string> { "folderid", "toname" } },
            { "copyfolder", new List<string> { "folderid", "tofolderid" } },
            { "movefolder", new List<string> { "folderid", "tofolderid" } },

            // Sharing operations
            { "share", new List<string> { "fileid" } },
            { "sharefolder", new List<string> { "folderid" } },
            { "unshare", new List<string> { "fileid" } },
            { "unsharefolder", new List<string> { "folderid" } },

            // Search operations
            { "search", new List<string> { "query" } },

            // Thumbnail operations
            { "thumbnail", new List<string> { "fileid", "size" } },

            // Public link operations
            { "publiclink", new List<string> { "fileid" } },
            { "publicfolderlink", new List<string> { "folderid" } }
        };

        public pCloudDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
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
                    DMEEditor.AddLogMessage("Beep", $"Entity '{EntityName}' not found in pCloud endpoints", DateTime.Now, -1, null, Errors.Failed);
                    return Array.Empty<object>();
                }

                var endpoint = EntityEndpoints[EntityName.ToLower()];
                var parameters = new Dictionary<string, string>();

                // Add authentication token
                if (!string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Parameters))
                {
                    var paramDict = ParseParameters(Dataconnection.ConnectionProp.Parameters);
                    if (paramDict.ContainsKey("token"))
                    {
                        parameters["access_token"] = paramDict["token"];
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

                // Check for pCloud API error
                if (root.TryGetProperty("result", out var result) && result.GetInt32() != 0)
                {
                    var errorMsg = root.TryGetProperty("error", out var error) ? error.GetString() : "Unknown pCloud API error";
                    DMEEditor.AddLogMessage("Beep", $"pCloud API error: {errorMsg}", DateTime.Now, -1, null, Errors.Failed);
                    return Array.Empty<object>();
                }

                switch (entityName)
                {
                    case "files":
                    case "folders":
                        if (root.TryGetProperty("metadata", out var metadata))
                        {
                            return ExtractArray<pCloudItem>(metadata.GetProperty("contents"));
                        }
                        break;

                    case "fileinfo":
                        if (root.TryGetProperty("metadata", out metadata))
                        {
                            var item = JsonSerializer.Deserialize<pCloudItem>(metadata.GetRawText());
                            return item != null ? new[] { item } : Array.Empty<object>();
                        }
                        break;

                    case "userinfo":
                    case "accountinfo":
                        var user = JsonSerializer.Deserialize<pCloudUser>(root.GetRawText());
                        return user != null ? new[] { user } : Array.Empty<object>();

                    case "search":
                        if (root.TryGetProperty("files", out var files))
                        {
                            return ExtractArray<pCloudItem>(files);
                        }
                        break;

                    case "share":
                    case "sharefolder":
                        if (root.TryGetProperty("link", out var link))
                        {
                            var share = JsonSerializer.Deserialize<pCloudShare>(root.GetRawText());
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
                DMEEditor.AddLogMessage("Beep", $"Error parsing pCloud response: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        private List<T> ExtractArray<T>(JsonElement element) where T : pCloudEntityBase
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

        private string ResolveEndpoint(string entityName, Dictionary<string, object>? parameters = null)
        {
            if (!EntityEndpoints.ContainsKey(entityName.ToLower()))
            {
                throw new ArgumentException($"Entity '{entityName}' not found in endpoints");
            }

            var endpoint = EntityEndpoints[entityName.ToLower()];
            var queryParams = new List<string>();

            // Add authentication token
            var parsedParams = ParseParameters(Dataconnection?.ConnectionProp?.Parameters ?? "");
            if (parsedParams.ContainsKey("token"))
            {
                queryParams.Add($"access_token={Uri.EscapeDataString(parsedParams["token"])}");
            }

            // Add parameters
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value?.ToString() ?? "")}");
                }
            }

            return queryParams.Count > 0 ? $"{endpoint}?{string.Join("&", queryParams)}" : endpoint;
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudItem", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "List<pCloudItem>")]
        public List<pCloudItem> GetFiles(string folderId = "0")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folderid", FilterValue = folderId } };
            return GetEntity("files", filters).Cast<pCloudItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudItem", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "List<pCloudItem>")]
        public List<pCloudItem> GetFolders(string folderId = "0")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folderid", FilterValue = folderId } };
            return GetEntity("folders", filters).Cast<pCloudItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudItem", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "pCloudItem")]
        public pCloudItem? GetFileInfo(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileid", FilterValue = fileId } };
            return GetEntity("fileinfo", filters).Cast<pCloudItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudUser", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "pCloudUser")]
        public pCloudUser? GetUserInfo()
        {
            return GetEntity("userinfo", new List<AppFilter>()).Cast<pCloudUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudUser", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "pCloudUser")]
        public pCloudUser? GetAccountInfo()
        {
            return GetEntity("accountinfo", new List<AppFilter>()).Cast<pCloudUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudItem", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "List<pCloudItem>")]
        public List<pCloudItem> Search(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query } };
            return GetEntity("search", filters).Cast<pCloudItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudShare", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "pCloudShare")]
        public pCloudShare? ShareFile(string fileId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "fileid", FilterValue = fileId } };
            return GetEntity("share", filters).Cast<pCloudShare>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud, PointType = EnumPointType.Function, ObjectType = "pCloudShare", ClassName = "pCloudDataSource", Showin = ShowinType.Both, misc = "pCloudShare")]
        public pCloudShare? ShareFolder(string folderId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "folderid", FilterValue = folderId } };
            return GetEntity("sharefolder", filters).Cast<pCloudShare>().FirstOrDefault();
        }

        [CommandAttribute(Name = "UploadFileAsync", Caption = "Upload pCloud File",
            ObjectType = "pCloudItem", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud,
            ClassType = "pCloudDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "pcloud.png", misc = "Upload a file")]
        public async Task<IEnumerable<pCloudItem>> UploadFileAsync(pCloudItem file, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://api.pcloud.com/uploadfile", file, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<pCloudItem>>(result);
        }

        [CommandAttribute(Name = "CreateFolderAsync", Caption = "Create pCloud Folder",
            ObjectType = "pCloudItem", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud,
            ClassType = "pCloudDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "pcloud.png", misc = "Create a folder")]
        public async Task<IEnumerable<pCloudItem>> CreateFolderAsync(pCloudItem folder, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://api.pcloud.com/createfolder", folder, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<pCloudItem>>(result);
        }

        [CommandAttribute(Name = "CopyFileAsync", Caption = "Copy pCloud File",
            ObjectType = "pCloudItem", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.pCloud,
            ClassType = "pCloudDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "pcloud.png", misc = "Copy a file")]
        public async Task<IEnumerable<pCloudItem>> CopyFileAsync(pCloudItem file, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://api.pcloud.com/copyfile", file, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<pCloudItem>>(result);
        }

        #endregion
    }
}
