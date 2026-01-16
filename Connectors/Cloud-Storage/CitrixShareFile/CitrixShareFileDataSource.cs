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
using TheTechIdea.Beep.Connectors.CitrixShareFile.Models;

namespace TheTechIdea.Beep.Connectors.CitrixShareFile
{
    /// <summary>
    /// Citrix ShareFile data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, users, and collaboration operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile)]
    public class CitrixShareFileDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Citrix ShareFile API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://api.sharefile.com/sf/v3/Items({item_id})",
            ["file_download"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Download",
            ["file_upload"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Upload",
            ["file_versions"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Version",
            // Folder operations
            ["folders"] = "https://api.sharefile.com/sf/v3/Items({item_id})",
            ["folder_children"] = "https://api.sharefile.com/sf/v3/Items({item_id})/Children",
            ["root_folder"] = "https://api.sharefile.com/sf/v3/Items",
            // User operations
            ["users"] = "https://api.sharefile.com/sf/v3/Users",
            ["user_info"] = "https://api.sharefile.com/sf/v3/Users({user_id})",
            ["current_user"] = "https://api.sharefile.com/sf/v3/Users/me",
            // Group operations
            ["groups"] = "https://api.sharefile.com/sf/v3/Groups",
            ["group_info"] = "https://api.sharefile.com/sf/v3/Groups({group_id})",
            // Share operations
            ["shares"] = "https://api.sharefile.com/sf/v3/Shares",
            ["share_info"] = "https://api.sharefile.com/sf/v3/Shares({share_id})",
            ["share_requests"] = "https://api.sharefile.com/sf/v3/ShareRequests",
            // Search operations
            ["search"] = "https://api.sharefile.com/sf/v3/Items/Search",
            // Favorite operations
            ["favorites"] = "https://api.sharefile.com/sf/v3/Favorites",
            // Connector operations
            ["connectors"] = "https://api.sharefile.com/sf/v3/ConnectorGroups",
            ["connector_info"] = "https://api.sharefile.com/sf/v3/ConnectorGroups({connector_id})",
            // Account operations
            ["account"] = "https://api.sharefile.com/sf/v3/Accounts",
            // Access control operations
            ["access_controls"] = "https://api.sharefile.com/sf/v3/Items({item_id})/AccessControls"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations require item_id
            ["files"] = new[] { "item_id" },
            ["file_download"] = new[] { "item_id" },
            ["file_upload"] = new[] { "item_id" },
            ["file_versions"] = new[] { "item_id" },
            // Folder operations require item_id
            ["folders"] = new[] { "item_id" },
            ["folder_children"] = new[] { "item_id" },
            ["access_controls"] = new[] { "item_id" },
            // Root folder doesn't require filters
            ["root_folder"] = Array.Empty<string>(),
            // User operations may require user_id for specific user
            ["users"] = Array.Empty<string>(),
            ["user_info"] = new[] { "user_id" },
            ["current_user"] = Array.Empty<string>(),
            // Group operations may require group_id for specific group
            ["groups"] = Array.Empty<string>(),
            ["group_info"] = new[] { "group_id" },
            // Share operations may require share_id for specific share
            ["shares"] = Array.Empty<string>(),
            ["share_info"] = new[] { "share_id" },
            ["share_requests"] = Array.Empty<string>(),
            // Search operations don't require filters (query can be optional)
            ["search"] = Array.Empty<string>(),
            // Favorites don't require filters
            ["favorites"] = Array.Empty<string>(),
            // Connector operations may require connector_id for specific connector
            ["connectors"] = Array.Empty<string>(),
            ["connector_info"] = new[] { "connector_id" },
            // Account operations don't require filters
            ["account"] = Array.Empty<string>()
        };

        public CitrixShareFileDataSource(
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

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (!EntityEndpoints.TryGetValue(EntityName.ToLower(), out var endpoint))
                {
                    DMEEditor.AddLogMessage("Beep", $"Entity '{EntityName}' not found in Citrix ShareFile endpoints", DateTime.Now, -1, null, Errors.Failed);
                    return Array.Empty<object>();
                }

                var parameters = new Dictionary<string, string>();

                // Add authentication token if available
                if (!string.IsNullOrEmpty(Dataconnection?.ConnectionProp?.Parameters))
                {
                    var paramDict = ParseParameters(Dataconnection.ConnectionProp.Parameters);
                    if (paramDict.ContainsKey("access_token"))
                    {
                        parameters["Authorization"] = $"Bearer {paramDict["access_token"]}";
                    }
                }

                // Add required filters
                if (Filter != null && RequiredFilters.TryGetValue(EntityName.ToLower(), out var requiredParams))
                {
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
                    case "folder_children":
                    case "root_folder":
                        return ExtractArray<ShareFileItem>(root);

                    case "users":
                    case "current_user":
                        return ExtractArray<ShareFileUser>(root);

                    case "groups":
                        return ExtractArray<ShareFileGroup>(root);

                    case "shares":
                    case "share_info":
                        return ExtractArray<ShareFileShare>(root);

                    case "access_controls":
                        return ExtractArray<ShareFileAccessControl>(root);

                    case "connectors":
                    case "connector_info":
                        return ExtractArray<ShareFileConnectorGroup>(root);

                    case "account":
                        return ExtractArray<ShareFileAccount>(root);

                    case "search":
                        return ExtractArray<ShareFileSearchResult>(root);

                    case "favorites":
                        return ExtractArray<ShareFileItem>(root);

                    default:
                        // For single objects, try to deserialize directly
                        try
                        {
                            var item = JsonSerializer.Deserialize<ShareFileItem>(content);
                            return item != null ? new[] { item } : Array.Empty<object>();
                        }
                        catch
                        {
                            return new[] { content }; // Return raw content for unhandled entities
                        }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error parsing Citrix ShareFile response: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return Array.Empty<object>();
            }
        }

        private List<T> ExtractArray<T>(JsonElement element) where T : CitrixShareFileEntityBase
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
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    var obj = JsonSerializer.Deserialize<T>(element.GetRawText());
                    if (obj != null)
                    {
                        list.Add(obj);
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

        private string ResolveEndpoint(string template, Dictionary<string, string> parameters)
        {
            var result = template;

            // Handle item_id
            if (result.Contains("{item_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("item_id", out var itemId) && !string.IsNullOrWhiteSpace(itemId))
                {
                    result = result.Replace("{item_id}", Uri.EscapeDataString(itemId));
                }
            }

            // Handle user_id
            if (result.Contains("{user_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("user_id", out var userId) && !string.IsNullOrWhiteSpace(userId))
                {
                    result = result.Replace("{user_id}", Uri.EscapeDataString(userId));
                }
            }

            // Handle group_id
            if (result.Contains("{group_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("group_id", out var groupId) && !string.IsNullOrWhiteSpace(groupId))
                {
                    result = result.Replace("{group_id}", Uri.EscapeDataString(groupId));
                }
            }

            // Handle share_id
            if (result.Contains("{share_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("share_id", out var shareId) && !string.IsNullOrWhiteSpace(shareId))
                {
                    result = result.Replace("{share_id}", Uri.EscapeDataString(shareId));
                }
            }

            // Handle connector_id
            if (result.Contains("{connector_id}", StringComparison.Ordinal))
            {
                if (parameters.TryGetValue("connector_id", out var connectorId) && !string.IsNullOrWhiteSpace(connectorId))
                {
                    result = result.Replace("{connector_id}", Uri.EscapeDataString(connectorId));
                }
            }

            return result;
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileItem", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileItem>")]
        public List<ShareFileItem> GetItems(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("files", filters).Cast<ShareFileItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileItem", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileItem>")]
        public List<ShareFileItem> GetFolderChildren(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("folder_children", filters).Cast<ShareFileItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileItem", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileItem>")]
        public List<ShareFileItem> GetRootFolder()
        {
            return GetEntity("root_folder", new List<AppFilter>()).Cast<ShareFileItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileUser", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileUser>")]
        public List<ShareFileUser> GetUsers()
        {
            return GetEntity("users", new List<AppFilter>()).Cast<ShareFileUser>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileUser", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "ShareFileUser")]
        public ShareFileUser? GetUserInfo(string userId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "user_id", FilterValue = userId } };
            return GetEntity("user_info", filters).Cast<ShareFileUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileUser", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "ShareFileUser")]
        public ShareFileUser? GetCurrentUser()
        {
            return GetEntity("current_user", new List<AppFilter>()).Cast<ShareFileUser>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileGroup", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileGroup>")]
        public List<ShareFileGroup> GetGroups()
        {
            return GetEntity("groups", new List<AppFilter>()).Cast<ShareFileGroup>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileGroup", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "ShareFileGroup")]
        public ShareFileGroup? GetGroupInfo(string groupId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "group_id", FilterValue = groupId } };
            return GetEntity("group_info", filters).Cast<ShareFileGroup>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileShare", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileShare>")]
        public List<ShareFileShare> GetShares()
        {
            return GetEntity("shares", new List<AppFilter>()).Cast<ShareFileShare>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileShare", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "ShareFileShare")]
        public ShareFileShare? GetShareInfo(string shareId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "share_id", FilterValue = shareId } };
            return GetEntity("share_info", filters).Cast<ShareFileShare>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileSearchResult", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileSearchResult>")]
        public List<ShareFileSearchResult> Search(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query } };
            return GetEntity("search", filters).Cast<ShareFileSearchResult>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileItem", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileItem>")]
        public List<ShareFileItem> GetFavorites()
        {
            return GetEntity("favorites", new List<AppFilter>()).Cast<ShareFileItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.CitrixShareFile, PointType = EnumPointType.Function, ObjectType ="ShareFileAccessControl", ClassName = "CitrixShareFileDataSource", Showin = ShowinType.Both, misc = "List<ShareFileAccessControl>")]
        public List<ShareFileAccessControl> GetAccessControls(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("access_controls", filters).Cast<ShareFileAccessControl>().ToList();
        }

        [CommandAttribute(
           Name = "CreateItemAsync",
            Caption = "Create Citrix ShareFile Item",
            ObjectType ="ShareFileItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "createitem.png",
            misc = "ReturnType: IEnumerable<ShareFileItem>"
        )]
        public async Task<IEnumerable<ShareFileItem>> CreateItemAsync(ShareFileItem item)
        {
            try
            {
                var result = await PostAsync("Items", item);
                var items = JsonSerializer.Deserialize<IEnumerable<ShareFileItem>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<ShareFileItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating item: {ex.Message}");
            }
            return new List<ShareFileItem>();
        }

        [CommandAttribute(
           Name = "CreateShareAsync",
            Caption = "Create Citrix ShareFile Share",
            ObjectType ="ShareFileShare",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "createshare.png",
            misc = "ReturnType: IEnumerable<ShareFileShare>"
        )]
        public async Task<IEnumerable<ShareFileShare>> CreateShareAsync(ShareFileShare share)
        {
            try
            {
                var result = await PostAsync("Shares", share);
                var shares = JsonSerializer.Deserialize<IEnumerable<ShareFileShare>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (shares != null)
                {
                    foreach (var s in shares)
                    {
                        s.Attach<ShareFileShare>(this);
                    }
                }
                return shares;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating share: {ex.Message}");
            }
            return new List<ShareFileShare>();
        }

        [CommandAttribute(
           Name = "CreateGroupAsync",
            Caption = "Create Citrix ShareFile Group",
            ObjectType ="ShareFileGroup",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "creategroup.png",
            misc = "ReturnType: IEnumerable<ShareFileGroup>"
        )]
        public async Task<IEnumerable<ShareFileGroup>> CreateGroupAsync(ShareFileGroup group)
        {
            try
            {
                var result = await PostAsync("Groups", group);
                var groups = JsonSerializer.Deserialize<IEnumerable<ShareFileGroup>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (groups != null)
                {
                    foreach (var g in groups)
                    {
                        g.Attach<ShareFileGroup>(this);
                    }
                }
                return groups;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating group: {ex.Message}");
            }
            return new List<ShareFileGroup>();
        }

        [CommandAttribute(
           Name = "UpdateItemAsync",
            Caption = "Update Citrix ShareFile Item",
            ObjectType ="ShareFileItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 4,
            iconimage = "updateitem.png",
            misc = "ReturnType: IEnumerable<ShareFileItem>"
        )]
        public async Task<IEnumerable<ShareFileItem>> UpdateItemAsync(ShareFileItem item)
        {
            try
            {
                var result = await PatchAsync("Items", item);
                var items = JsonSerializer.Deserialize<IEnumerable<ShareFileItem>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<ShareFileItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating item: {ex.Message}");
            }
            return new List<ShareFileItem>();
        }

        [CommandAttribute(
           Name = "UpdateShareAsync",
            Caption = "Update Citrix ShareFile Share",
            ObjectType ="ShareFileShare",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 5,
            iconimage = "updateshare.png",
            misc = "ReturnType: IEnumerable<ShareFileShare>"
        )]
        public async Task<IEnumerable<ShareFileShare>> UpdateShareAsync(ShareFileShare share)
        {
            try
            {
                var result = await PatchAsync("Shares", share);
                var shares = JsonSerializer.Deserialize<IEnumerable<ShareFileShare>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (shares != null)
                {
                    foreach (var s in shares)
                    {
                        s.Attach<ShareFileShare>(this);
                    }
                }
                return shares;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating share: {ex.Message}");
            }
            return new List<ShareFileShare>();
        }

        [CommandAttribute(
           Name = "UpdateGroupAsync",
            Caption = "Update Citrix ShareFile Group",
            ObjectType ="ShareFileGroup",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.CitrixShareFile,
            ClassType ="CitrixShareFileDataSource",
            Showin = ShowinType.Both,
            Order = 6,
            iconimage = "updategroup.png",
            misc = "ReturnType: IEnumerable<ShareFileGroup>"
        )]
        public async Task<IEnumerable<ShareFileGroup>> UpdateGroupAsync(ShareFileGroup group)
        {
            try
            {
                var result = await PatchAsync("Groups", group);
                var groups = JsonSerializer.Deserialize<IEnumerable<ShareFileGroup>>(await result.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (groups != null)
                {
                    foreach (var g in groups)
                    {
                        g.Attach<ShareFileGroup>(this);
                    }
                }
                return groups;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating group: {ex.Message}");
            }
            return new List<ShareFileGroup>();
        }

        #endregion
    }
}
