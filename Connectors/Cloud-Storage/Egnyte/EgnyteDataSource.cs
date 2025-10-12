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
using TheTechIdea.Beep.Connectors.Egnyte.Models;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Connectors.Egnyte
{
    public class EgnyteDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Egnyte API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            { "files", "https://apidemo.egnyte.com/pubapi/v1/fs/{path}" },
            { "file_info", "https://apidemo.egnyte.com/pubapi/v1/fs/{path}" },
            { "file_content", "https://apidemo.egnyte.com/pubapi/v1/fs-content/{path}" },
            { "file_versions", "https://apidemo.egnyte.com/pubapi/v1/fs-versions/{path}" },
            // Folder operations
            { "folders", "https://apidemo.egnyte.com/pubapi/v1/fs/{path}" },
            { "folder_items", "https://apidemo.egnyte.com/pubapi/v1/fs/{path}" },
            // User operations
            { "users", "https://apidemo.egnyte.com/pubapi/v1/users" },
            { "user_info", "https://apidemo.egnyte.com/pubapi/v1/users/{user_id}" },
            { "current_user", "https://apidemo.egnyte.com/pubapi/v1/userinfo" },
            // Group operations
            { "groups", "https://apidemo.egnyte.com/pubapi/v1/groups" },
            { "group_info", "https://apidemo.egnyte.com/pubapi/v1/groups/{group_id}" },
            // Search operations
            { "search", "https://apidemo.egnyte.com/pubapi/v1/search" },
            // Link operations
            { "links", "https://apidemo.egnyte.com/pubapi/v1/links" },
            { "link_info", "https://apidemo.egnyte.com/pubapi/v1/links/{link_id}" },
            // Audit operations
            { "audit", "https://apidemo.egnyte.com/pubapi/v1/audit" },
            // Permission operations
            { "permissions", "https://apidemo.egnyte.com/pubapi/v1/perms/{path}" }
        };

        // Required filters for each entity
        private static readonly Dictionary<string, List<string>> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require path
            { "files", new List<string> { "path" } },
            { "file_info", new List<string> { "path" } },
            { "file_content", new List<string> { "path" } },
            { "file_versions", new List<string> { "path" } },
            // Folder operations require path
            { "folders", new List<string> { "path" } },
            { "folder_items", new List<string> { "path" } },
            // User operations may require user_id for specific user
            { "users", new List<string>() },
            { "user_info", new List<string> { "user_id" } },
            { "current_user", new List<string>() },
            // Group operations may require group_id for specific group
            { "groups", new List<string>() },
            { "group_info", new List<string> { "group_id" } },
            // Search operations don't require filters (query can be optional)
            { "search", new List<string>() },
            // Link operations may require link_id for specific link
            { "links", new List<string>() },
            { "link_info", new List<string> { "link_id" } },
            // Audit operations don't require filters
            { "audit", new List<string>() },
            // Permission operations require path
            { "permissions", new List<string> { "path" } }
        };

        public EgnyteDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
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
                    DMEEditor.AddLogMessage("Beep", $"Entity '{EntityName}' not found in Egnyte endpoints", DateTime.Now, -1, null, Errors.Failed);
                    return Array.Empty<object>();
                }

                var endpoint = EntityEndpoints[EntityName.ToLower()];
                var parameters = new Dictionary<string, string>();

                // Add authentication token
                if (!string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Parameters))
                {
                    var paramDict = ParseParameters(Dataconnection.ConnectionProp.Parameters);
                    if (paramDict.ContainsKey("access_token"))
                    {
                        parameters["access_token"] = paramDict["access_token"];
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

                var resolvedEndpoint = ResolveEndpoint(endpoint, parameters);

                var response = await base.GetAsync(resolvedEndpoint);
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

                switch (entityName)
                {
                    case "files":
                    case "folders":
                    case "folder_items":
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return ExtractArray<EgnyteItem>(root);
                        }
                        else if (root.ValueKind == JsonValueKind.Object)
                        {
                            var item = JsonSerializer.Deserialize<EgnyteItem>(content);
                            return item != null ? new[] { item } : Array.Empty<object>();
                        }
                        break;

                    case "file_info":
                        var fileInfo = JsonSerializer.Deserialize<EgnyteItem>(content);
                        return fileInfo != null ? new[] { fileInfo } : Array.Empty<object>();

                    case "file_versions":
                        if (root.TryGetProperty("versions", out var versions))
                        {
                            return ExtractArray<EgnyteFileVersion>(versions);
                        }
                        break;

                    case "users":
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return ExtractArray<EgnyteUser>(root);
                        }
                        break;

                    case "user_info":
                    case "current_user":
                        var user = JsonSerializer.Deserialize<EgnyteUserInfo>(content);
                        return user != null ? new[] { user } : Array.Empty<object>();

                    case "groups":
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return ExtractArray<EgnyteGroup>(root);
                        }
                        break;

                    case "group_info":
                        var group = JsonSerializer.Deserialize<EgnyteGroup>(content);
                        return group != null ? new[] { group } : Array.Empty<object>();

                    case "search":
                        var searchResult = JsonSerializer.Deserialize<EgnyteSearchResult>(content);
                        return searchResult != null ? new[] { searchResult } : Array.Empty<object>();

                    case "links":
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return ExtractArray<EgnyteLink>(root);
                        }
                        break;

                    case "link_info":
                        var link = JsonSerializer.Deserialize<EgnyteLink>(content);
                        return link != null ? new[] { link } : Array.Empty<object>();

                    case "audit":
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            return ExtractArray<EgnyteAuditEvent>(root);
                        }
                        break;

                    case "permissions":
                        var permissions = JsonSerializer.Deserialize<EgnytePermissions>(content);
                        return permissions != null ? new[] { permissions } : Array.Empty<object>();

                    default:
                        return new[] { content }; // Return raw content for unhandled entities
                }

                return Array.Empty<object>();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Egnyte response: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        private List<T> ExtractArray<T>(JsonElement element) where T : EgnyteEntityBase
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

        private string ResolveEndpoint(string template, Dictionary<string, string> parameters)
        {
            var result = template;

            // Handle path
            if (result.Contains("{path}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("path", out var path) && !string.IsNullOrWhiteSpace(path))
                {
                    result = result.Replace("{path}", Uri.EscapeDataString(path));
                }
                else
                {
                    result = result.Replace("{path}", "Shared"); // Default to root shared folder
                }
            }

            // Handle user_id
            if (result.Contains("{user_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("user_id", out var userId) && !string.IsNullOrWhiteSpace(userId))
                {
                    result = result.Replace("{user_id}", Uri.EscapeDataString(userId));
                }
                else
                {
                    throw new ArgumentException("Missing required 'user_id' parameter for this endpoint.");
                }
            }

            // Handle group_id
            if (result.Contains("{group_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("group_id", out var groupId) && !string.IsNullOrWhiteSpace(groupId))
                {
                    result = result.Replace("{group_id}", Uri.EscapeDataString(groupId));
                }
                else
                {
                    throw new ArgumentException("Missing required 'group_id' parameter for this endpoint.");
                }
            }

            // Handle link_id
            if (result.Contains("{link_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("link_id", out var linkId) && !string.IsNullOrWhiteSpace(linkId))
                {
                    result = result.Replace("{link_id}", Uri.EscapeDataString(linkId));
                }
                else
                {
                    throw new ArgumentException("Missing required 'link_id' parameter for this endpoint.");
                }
            }

            return result;
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteItem", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteItem>")]
        public List<EgnyteItem> GetFiles(string path = "/Shared")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("files", filters).Cast<EgnyteItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteItem", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteItem")]
        public EgnyteItem? GetFileInfo(string path)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("file_info", filters).Cast<EgnyteItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteItem", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteItem>")]
        public List<EgnyteItem> GetFolders(string path = "/Shared")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("folders", filters).Cast<EgnyteItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteItem", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteItem>")]
        public List<EgnyteItem> GetFolderItems(string path = "/Shared")
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("folder_items", filters).Cast<EgnyteItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteUser", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteUser>")]
        public List<EgnyteUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<EgnyteUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteUserInfo", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteUserInfo")]
        public EgnyteUserInfo? GetUserInfo(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } };
            return GetEntity("user_info", filters).Cast<EgnyteUserInfo>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteUserInfo", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteUserInfo")]
        public EgnyteUserInfo? GetCurrentUser()
        {
            return GetEntity("current_user", new List<AppFilter>()).Cast<EgnyteUserInfo>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteGroup", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteGroup>")]
        public List<EgnyteGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<EgnyteGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteGroup", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteGroup")]
        public EgnyteGroup? GetGroupInfo(string groupId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "group_id", FilterValue = groupId } };
            return GetEntity("group_info", filters).Cast<EgnyteGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteSearchResult", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteSearchResult")]
        public EgnyteSearchResult? Search(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query } };
            return GetEntity("search", filters).Cast<EgnyteSearchResult>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteLink", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteLink>")]
        public List<EgnyteLink> GetLinks()
        {
            return GetEntity("links", new List<AppFilter>()).Cast<EgnyteLink>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteLink", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnyteLink")]
        public EgnyteLink? GetLinkInfo(string linkId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "link_id", FilterValue = linkId } };
            return GetEntity("link_info", filters).Cast<EgnyteLink>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteAuditEvent", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteAuditEvent>")]
        public List<EgnyteAuditEvent> GetAuditEvents()
        {
            return GetEntity("audit", new List<AppFilter>()).Cast<EgnyteAuditEvent>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnytePermissions", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "EgnytePermissions")]
        public EgnytePermissions? GetPermissions(string path)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("permissions", filters).Cast<EgnytePermissions>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte, PointType = EnumPointType.Function, ObjectType = "EgnyteFileVersion", ClassName = "EgnyteDataSource", Showin = ShowinType.Both, misc = "List<EgnyteFileVersion>")]
        public List<EgnyteFileVersion> GetFileVersions(string path)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "path", FilterValue = path } };
            return GetEntity("file_versions", filters).Cast<EgnyteFileVersion>().ToList();
        }

        [CommandAttribute(Name = "CreateFolderAsync", Caption = "Create Egnyte Folder",
            ObjectType = "EgnyteFolder", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte,
            ClassType = "EgnyteDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "egnyte.png", misc = "Create a folder")]
        public async Task<IEnumerable<EgnyteFolder>> CreateFolderAsync(EgnyteFolder folder, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://apidemo.egnyte.com/pubapi/v1/fs/{path}", folder, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<EgnyteFolder>>(result);
        }

        [CommandAttribute(Name = "UploadFileAsync", Caption = "Upload Egnyte File",
            ObjectType = "EgnyteFile", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte,
            ClassType = "EgnyteDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "egnyte.png", misc = "Upload a file")]
        public async Task<IEnumerable<EgnyteFile>> UploadFileAsync(EgnyteFile file, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://apidemo.egnyte.com/pubapi/v1/fs-content/{path}", file, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<EgnyteFile>>(result);
        }

        [CommandAttribute(Name = "CreateLinkAsync", Caption = "Create Egnyte Link",
            ObjectType = "EgnyteLink", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Egnyte,
            ClassType = "EgnyteDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "egnyte.png", misc = "Create a link")]
        public async Task<IEnumerable<EgnyteLink>> CreateLinkAsync(EgnyteLink link, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://apidemo.egnyte.com/pubapi/v1/links", link, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<EgnyteLink>>(result);
        }

        #endregion
    }
}
