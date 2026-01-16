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
using TheTechIdea.Beep.Connectors.OneDrive.Models;

namespace TheTechIdea.Beep.Connectors.OneDrive
{
    /// <summary>
    /// OneDrive data source implementation using WebAPIDataSource as base class
    /// Supports files, folders, sharing, and drive operations
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive)]
    public class OneDriveDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Microsoft Graph API (OneDrive)
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Drive operations
            ["drives"] = "https://graph.microsoft.com/v1.0/me/drives",
            ["drive_details"] = "https://graph.microsoft.com/v1.0/me/drive",
            // Root operations
            ["root"] = "https://graph.microsoft.com/v1.0/me/drive/root",
            ["root_children"] = "https://graph.microsoft.com/v1.0/me/drive/root/children",
            // Item operations
            ["items"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}",
            ["item_children"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}/children",
            ["item_content"] = "https://graph.microsoft.com/v1.0/me/drive/items/{item_id}/content",
            // Search operations
            ["search"] = "https://graph.microsoft.com/v1.0/me/drive/search(q='{query}')",
            // Recent files
            ["recent"] = "https://graph.microsoft.com/v1.0/me/drive/recent",
            // Shared with me
            ["shared"] = "https://graph.microsoft.com/v1.0/me/drive/sharedWithMe",
            // Special folders
            ["documents"] = "https://graph.microsoft.com/v1.0/me/drive/special/documents",
            ["photos"] = "https://graph.microsoft.com/v1.0/me/drive/special/photos",
            ["cameraroll"] = "https://graph.microsoft.com/v1.0/me/drive/special/cameraroll"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // Drive operations don't require filters
            ["drives"] = Array.Empty<string>(),
            ["drive_details"] = Array.Empty<string>(),
            // Root operations don't require filters
            ["root"] = Array.Empty<string>(),
            ["root_children"] = Array.Empty<string>(),
            // Item operations require item_id
            ["items"] = new[] { "item_id" },
            ["item_children"] = new[] { "item_id" },
            ["item_content"] = new[] { "item_id" },
            // Search operations require query
            ["search"] = new[] { "query" },
            // Recent and shared don't require filters
            ["recent"] = Array.Empty<string>(),
            ["shared"] = Array.Empty<string>(),
            // Special folders don't require filters
            ["documents"] = Array.Empty<string>(),
            ["photos"] = Array.Empty<string>(),
            ["cameraroll"] = Array.Empty<string>()
        };

        public OneDriveDataSource(
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
                throw new InvalidOperationException($"Unknown OneDrive entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, "value");
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
                throw new ArgumentException($"OneDrive entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle item_id
            if (result.Contains("{item_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("item_id", out var itemId) || string.IsNullOrWhiteSpace(itemId))
                    throw new ArgumentException("Missing required 'item_id' filter for this endpoint.");
                result = result.Replace("{item_id}", Uri.EscapeDataString(itemId));
            }

            // Handle query for search
            if (result.Contains("{query}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("query", out var query) || string.IsNullOrWhiteSpace(query))
                    throw new ArgumentException("Missing required 'query' filter for this endpoint.");
                result = result.Replace("{query}", Uri.EscapeDataString(query));
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="Drive", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<Drive>")]
        public List<Drive> GetDrives()
        {
            return GetEntity("drives", new List<AppFilter>()).Cast<Drive>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="Drive", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "Drive")]
        public Drive? GetDrive()
        {
            return GetEntity("drive_details", new List<AppFilter>()).Cast<Drive>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "DriveItem")]
        public DriveItem? GetRoot()
        {
            return GetEntity("root", new List<AppFilter>()).Cast<DriveItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<DriveItem>")]
        public List<DriveItem> GetRootChildren()
        {
            return GetEntity("root_children", new List<AppFilter>()).Cast<DriveItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "DriveItem")]
        public DriveItem? GetItem(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("items", filters).Cast<DriveItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<DriveItem>")]
        public List<DriveItem> GetItemChildren(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("item_children", filters).Cast<DriveItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="Stream", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "Stream")]
        public Models.Stream? GetItemContent(string itemId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "item_id", FilterValue = itemId } };
            return GetEntity("item_content", filters).Cast<Models.Stream>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="SearchResult", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<SearchResult>")]
        public List<SearchResult> Search(string query)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "query", FilterValue = query } };
            return GetEntity("search", filters).Cast<SearchResult>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<DriveItem>")]
        public List<DriveItem> GetRecent()
        {
            return GetEntity("recent", new List<AppFilter>()).Cast<DriveItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "List<DriveItem>")]
        public List<DriveItem> GetSharedWithMe()
        {
            return GetEntity("shared", new List<AppFilter>()).Cast<DriveItem>().ToList();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "DriveItem")]
        public DriveItem? GetDocumentsFolder()
        {
            return GetEntity("documents", new List<AppFilter>()).Cast<DriveItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "DriveItem")]
        public DriveItem? GetPhotosFolder()
        {
            return GetEntity("photos", new List<AppFilter>()).Cast<DriveItem>().FirstOrDefault();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.OneDrive, PointType = EnumPointType.Function, ObjectType ="DriveItem", ClassName = "OneDriveDataSource", Showin = ShowinType.Both, misc = "DriveItem")]
        public DriveItem? GetCameraRollFolder()
        {
            return GetEntity("cameraroll", new List<AppFilter>()).Cast<DriveItem>().FirstOrDefault();
        }

        [CommandAttribute(
           Name = "CreateItemAsync",
            Caption = "Create OneDrive Item",
            ObjectType ="DriveItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.OneDrive,
            ClassType ="OneDriveDataSource",
            Showin = ShowinType.Both,
            Order = 1,
            iconimage = "createitem.png",
            misc = "ReturnType: IEnumerable<DriveItem>"
        )]
        public async Task<IEnumerable<DriveItem>> CreateItemAsync(DriveItem item)
        {
            try
            {
                var result = await PostAsync("root_children", item);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<DriveItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<DriveItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating item: {ex.Message}");
            }
            return new List<DriveItem>();
        }

        [CommandAttribute(
           Name = "UpdateItemContentAsync",
            Caption = "Update OneDrive Item Content",
            ObjectType ="DriveItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.OneDrive,
            ClassType ="OneDriveDataSource",
            Showin = ShowinType.Both,
            Order = 2,
            iconimage = "updateitem.png",
            misc = "ReturnType: IEnumerable<DriveItem>"
        )]
        public async Task<IEnumerable<DriveItem>> UpdateItemContentAsync(DriveItem item)
        {
            try
            {
                var result = await PutAsync("item_content", item);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<DriveItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<DriveItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating item content: {ex.Message}");
            }
            return new List<DriveItem>();
        }

        [CommandAttribute(
           Name = "UpdateItemAsync",
            Caption = "Update OneDrive Item",
            ObjectType ="DriveItem",
            PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector,
            DatasourceType = DataSourceType.OneDrive,
            ClassType ="OneDriveDataSource",
            Showin = ShowinType.Both,
            Order = 3,
            iconimage = "updateitem.png",
            misc = "ReturnType: IEnumerable<DriveItem>"
        )]
        public async Task<IEnumerable<DriveItem>> UpdateItemAsync(DriveItem item)
        {
            try
            {
                var result = await PatchAsync("item", item);
                var content = await result.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<IEnumerable<DriveItem>>(content, options);
                if (items != null)
                {
                    foreach (var i in items)
                    {
                        i.Attach<DriveItem>(this);
                    }
                }
                return items;
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating item: {ex.Message}");
            }
            return new List<DriveItem>();
        }

        #endregion
    }
}
