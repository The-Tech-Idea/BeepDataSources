using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.iCloud.Models;

namespace TheTechIdea.Beep.Connectors.iCloud
{
    /// <summary>
    /// iCloud data source implementation using WebAPIDataSource as base class
    /// Supports iCloud Drive files, folders, shares, and device information
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.iCloud)]
    public class iCloudDataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for iCloud API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = "https://icloud.com/api/files",
            ["file"] = "https://icloud.com/api/files/{file_id}",
            ["file_content"] = "https://icloud.com/api/files/{file_id}/content",

            // Folder operations
            ["folders"] = "https://icloud.com/api/folders",
            ["folder"] = "https://icloud.com/api/folders/{folder_id}",
            ["folder_children"] = "https://icloud.com/api/folders/{folder_id}/children",

            // Share operations
            ["shares"] = "https://icloud.com/api/shares",
            ["share"] = "https://icloud.com/api/shares/{share_id}",

            // Device operations
            ["devices"] = "https://icloud.com/api/devices",
            ["device"] = "https://icloud.com/api/devices/{device_id}"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // File operations
            ["files"] = Array.Empty<string>(),
            ["file"] = new[] { "file_id" },
            ["file_content"] = new[] { "file_id" },

            // Folder operations
            ["folders"] = Array.Empty<string>(),
            ["folder"] = new[] { "folder_id" },
            ["folder_children"] = new[] { "folder_id" },

            // Share operations
            ["shares"] = Array.Empty<string>(),
            ["share"] = new[] { "share_id" },

            // Device operations
            ["devices"] = Array.Empty<string>(),
            ["device"] = new[] { "device_id" }
        };

        public iCloudDataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
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
                throw new InvalidOperationException($"Unknown iCloud entity '{EntityName}'.");

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
                throw new ArgumentException($"iCloud entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
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

            // Handle folder_id
            if (result.Contains("{folder_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("folder_id", out var folderId) || string.IsNullOrWhiteSpace(folderId))
                    throw new ArgumentException("Missing required 'folder_id' filter for this endpoint.");
                result = result.Replace("{folder_id}", Uri.EscapeDataString(folderId));
            }

            // Handle share_id
            if (result.Contains("{share_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("share_id", out var shareId) || string.IsNullOrWhiteSpace(shareId))
                    throw new ArgumentException("Missing required 'share_id' filter for this endpoint.");
                result = result.Replace("{share_id}", Uri.EscapeDataString(shareId));
            }

            // Handle device_id
            if (result.Contains("{device_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("device_id", out var deviceId) || string.IsNullOrWhiteSpace(deviceId))
                    throw new ArgumentException("Missing required 'device_id' filter for this endpoint.");
                result = result.Replace("{device_id}", Uri.EscapeDataString(deviceId));
            }

            return result;
        }

        private IEnumerable<object> ExtractArray(HttpResponseMessage resp, string entityName)
        {
            try
            {
                var content = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(content)) return Array.Empty<object>();

                return entityName.ToLowerInvariant() switch
                {
                    "files" => ExtractFiles(content),
                    "folders" => ExtractFolders(content),
                    "shares" => ExtractShares(content),
                    "devices" => ExtractDevices(content),
                    _ => ExtractGeneric(content, entityName)
                };
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error extracting array for {entityName}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractFiles(string content)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                var files = new List<iCloudFile>();

                if (doc.RootElement.TryGetProperty("files", out var filesArray))
                {
                    foreach (var file in filesArray.EnumerateArray())
                    {
                        var icloudFile = new iCloudFile
                        {
                            Id = file.GetProperty("id").GetString(),
                            Name = file.GetProperty("name").GetString(),
                            Type = file.GetProperty("type").GetString(),
                            Size = file.GetProperty("size").GetInt64(),
                            DateCreated = file.GetProperty("dateCreated").GetDateTime(),
                            DateModified = file.GetProperty("dateModified").GetDateTime(),
                            ETag = file.GetProperty("etag").GetString(),
                            Extension = file.GetProperty("extension").GetString()
                        };
                        files.Add(icloudFile);
                    }
                }

                return files;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractFolders(string content)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                var folders = new List<iCloudFolder>();

                if (doc.RootElement.TryGetProperty("folders", out var foldersArray))
                {
                    foreach (var folder in foldersArray.EnumerateArray())
                    {
                        var icloudFolder = new iCloudFolder
                        {
                            Id = folder.GetProperty("id").GetString(),
                            Name = folder.GetProperty("name").GetString(),
                            Type = folder.GetProperty("type").GetString(),
                            DateCreated = folder.GetProperty("dateCreated").GetDateTime(),
                            DateModified = folder.GetProperty("dateModified").GetDateTime(),
                            ChildCount = folder.GetProperty("childCount").GetInt32(),
                            ParentId = folder.GetProperty("parentId").GetString()
                        };
                        folders.Add(icloudFolder);
                    }
                }

                return folders;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractShares(string content)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                var shares = new List<iCloudShare>();

                if (doc.RootElement.TryGetProperty("shares", out var sharesArray))
                {
                    foreach (var share in sharesArray.EnumerateArray())
                    {
                        var icloudShare = new iCloudShare
                        {
                            Id = share.GetProperty("id").GetString(),
                            Name = share.GetProperty("name").GetString(),
                            Url = share.GetProperty("url").GetString(),
                            Owner = share.GetProperty("owner").GetString(),
                            Permissions = share.GetProperty("permissions").GetString(),
                            DateShared = share.GetProperty("dateShared").GetDateTime(),
                            ExpirationDate = share.GetProperty("expirationDate").GetDateTime()
                        };
                        shares.Add(icloudShare);
                    }
                }

                return shares;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractDevices(string content)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                var devices = new List<iCloudDevice>();

                if (doc.RootElement.TryGetProperty("devices", out var devicesArray))
                {
                    foreach (var device in devicesArray.EnumerateArray())
                    {
                        var icloudDevice = new iCloudDevice
                        {
                            Id = device.GetProperty("id").GetString(),
                            Name = device.GetProperty("name").GetString(),
                            Model = device.GetProperty("model").GetString(),
                            OsVersion = device.GetProperty("osVersion").GetString(),
                            LastSeen = device.GetProperty("lastSeen").GetDateTime(),
                            IsActive = device.GetProperty("isActive").GetBoolean(),
                            DeviceClass = device.GetProperty("deviceClass").GetString()
                        };
                        devices.Add(icloudDevice);
                    }
                }

                return devices;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractGeneric(string content, string entityName)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                return new object[] { doc.RootElement };
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        // -------------------- CommandAttribute Methods --------------------

        [CommandAttribute(
            ObjectType = "iCloudFile",
            PointType = EnumPointType.Function,
            Name = "GetFiles",
            Caption = "Get All Files",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudFile>"
        )]
        public IEnumerable<iCloudFile> GetFiles()
        {
            return GetEntity("files", new List<AppFilter>()).Cast<iCloudFile>();
        }

        [CommandAttribute(
            ObjectType = "iCloudFile",
            PointType = EnumPointType.Function,
            Name = "GetFile",
            Caption = "Get File Details",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudFile>"
        )]
        public IEnumerable<iCloudFile> GetFile(AppFilter fileIdFilter)
        {
            return GetEntity("file", new List<AppFilter> { fileIdFilter }).Cast<iCloudFile>();
        }

        [CommandAttribute(
            ObjectType = "iCloudFolder",
            PointType = EnumPointType.Function,
            Name = "GetFolders",
            Caption = "Get All Folders",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudFolder>"
        )]
        public IEnumerable<iCloudFolder> GetFolders()
        {
            return GetEntity("folders", new List<AppFilter>()).Cast<iCloudFolder>();
        }

        [CommandAttribute(
            ObjectType = "iCloudFolder",
            PointType = EnumPointType.Function,
            Name = "GetFolder",
            Caption = "Get Folder Details",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudFolder>"
        )]
        public IEnumerable<iCloudFolder> GetFolder(AppFilter folderIdFilter)
        {
            return GetEntity("folder", new List<AppFilter> { folderIdFilter }).Cast<iCloudFolder>();
        }

        [CommandAttribute(
            ObjectType = "iCloudFolder",
            PointType = EnumPointType.Function,
            Name = "GetFolderChildren",
            Caption = "Get Folder Children",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudFile>"
        )]
        public IEnumerable<iCloudFile> GetFolderChildren(AppFilter folderIdFilter)
        {
            return GetEntity("folder_children", new List<AppFilter> { folderIdFilter }).Cast<iCloudFile>();
        }

        [CommandAttribute(
            ObjectType = "iCloudShare",
            PointType = EnumPointType.Function,
            Name = "GetShares",
            Caption = "Get All Shares",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudShare>"
        )]
        public IEnumerable<iCloudShare> GetShares()
        {
            return GetEntity("shares", new List<AppFilter>()).Cast<iCloudShare>();
        }

        [CommandAttribute(
            ObjectType = "iCloudShare",
            PointType = EnumPointType.Function,
            Name = "GetShare",
            Caption = "Get Share Details",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudShare>"
        )]
        public IEnumerable<iCloudShare> GetShare(AppFilter shareIdFilter)
        {
            return GetEntity("share", new List<AppFilter> { shareIdFilter }).Cast<iCloudShare>();
        }

        [CommandAttribute(
            ObjectType = "iCloudDevice",
            PointType = EnumPointType.Function,
            Name = "GetDevices",
            Caption = "Get All Devices",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudDevice>"
        )]
        public IEnumerable<iCloudDevice> GetDevices()
        {
            return GetEntity("devices", new List<AppFilter>()).Cast<iCloudDevice>();
        }

        [CommandAttribute(
            ObjectType = "iCloudDevice",
            PointType = EnumPointType.Function,
            Name = "GetDevice",
            Caption = "Get Device Details",
            ClassName = "iCloudDataSource",
            misc = "ReturnType: IEnumerable<iCloudDevice>"
        )]
        public IEnumerable<iCloudDevice> GetDevice(AppFilter deviceIdFilter)
        {
            return GetEntity("device", new List<AppFilter> { deviceIdFilter }).Cast<iCloudDevice>();
        }

        [CommandAttribute(Name = "UploadFileAsync", Caption = "Upload iCloud File",
            ObjectType = "iCloudFile", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.iCloud,
            ClassType = "iCloudDataSource", Showin = ShowinType.Both, Order = 1,
            iconimage = "icloud.png", misc = "Upload a file")]
        public async Task<IEnumerable<iCloudFile>> UploadFileAsync(iCloudFile file, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://icloud.com/api/files", file, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<iCloudFile>>(result);
        }

        [CommandAttribute(Name = "CreateFolderAsync", Caption = "Create iCloud Folder",
            ObjectType = "iCloudFolder", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.iCloud,
            ClassType = "iCloudDataSource", Showin = ShowinType.Both, Order = 2,
            iconimage = "icloud.png", misc = "Create a folder")]
        public async Task<IEnumerable<iCloudFolder>> CreateFolderAsync(iCloudFolder folder, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://icloud.com/api/folders", folder, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<iCloudFolder>>(result);
        }

        [CommandAttribute(Name = "CreateShareAsync", Caption = "Create iCloud Share",
            ObjectType = "iCloudShare", PointType = EnumPointType.Function,
            Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.iCloud,
            ClassType = "iCloudDataSource", Showin = ShowinType.Both, Order = 3,
            iconimage = "icloud.png", misc = "Create a share")]
        public async Task<IEnumerable<iCloudShare>> CreateShareAsync(iCloudShare share, List<AppFilter> filters = null)
        {
            var result = await PostAsync("https://icloud.com/api/shares", share, filters ?? new List<AppFilter>());
            return JsonSerializer.Deserialize<IEnumerable<iCloudShare>>(result);
        }
    }
}
