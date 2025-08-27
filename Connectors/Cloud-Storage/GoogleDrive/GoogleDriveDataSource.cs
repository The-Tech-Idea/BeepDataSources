using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BeepDM.DataManagementModelsStandard;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.CloudStorage.GoogleDrive
{
    public class GoogleDriveConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RefreshToken { get; set; }
        public string ServiceAccountKeyPath { get; set; }
        public string ApplicationName { get; set; } = "Beep Data Connectors";
        public bool UseServiceAccount { get; set; }
    }

    public class GoogleDriveDataSource : IDataSource
    {
        private readonly ILogger<GoogleDriveDataSource> _logger;
        private DriveService _driveService;
        private GoogleDriveConfig _config;
        private bool _isConnected;

        public string DataSourceName => "GoogleDrive";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Google Drive Cloud Storage Data Source";

        public GoogleDriveDataSource(ILogger<GoogleDriveDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new GoogleDriveConfig();

                // Check if using service account or OAuth 2.0
                if (parameters.ContainsKey("ServiceAccountKeyPath"))
                {
                    _config.UseServiceAccount = true;
                    _config.ServiceAccountKeyPath = parameters["ServiceAccountKeyPath"].ToString();
                    _config.ApplicationName = parameters.ContainsKey("ApplicationName") ?
                        parameters["ApplicationName"].ToString() : "Beep Data Connectors";
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    _config.UseServiceAccount = false;
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.RefreshToken = parameters.ContainsKey("RefreshToken") ?
                        parameters["RefreshToken"].ToString() : null;
                }
                else
                {
                    throw new ArgumentException("Either ServiceAccountKeyPath or ClientId/ClientSecret is required");
                }

                // Initialize the Drive service
                await InitializeDriveServiceAsync();

                // Test connection by getting about information
                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "user(displayName, emailAddress), storageQuota(limit, usage)";
                var about = await aboutRequest.ExecuteAsync();

                if (about != null && about.User != null)
                {
                    _isConnected = true;
                    _logger.LogInformation($"Successfully connected to Google Drive API for user: {about.User.EmailAddress}");
                    return true;
                }

                _logger.LogError("Failed to connect to Google Drive API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Google Drive API");
                return false;
            }
        }

        private async Task InitializeDriveServiceAsync()
        {
            if (_config.UseServiceAccount)
            {
                // Use Service Account authentication
                var credential = GoogleCredential.FromFile(_config.ServiceAccountKeyPath)
                    .CreateScoped(DriveService.Scope.Drive);

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _config.ApplicationName,
                });
            }
            else
            {
                // Use OAuth 2.0 authentication
                var credential = await GetOAuthCredentialAsync();

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _config.ApplicationName,
                });
            }
        }

        private async Task<GoogleCredential> GetOAuthCredentialAsync()
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                new[] { DriveService.Scope.Drive },
                "user",
                System.Threading.CancellationToken.None);

            return credential;
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_driveService != null)
                {
                    _driveService.Dispose();
                    _driveService = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from Google Drive API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Google Drive API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Drive API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await GetFilesAsync(parameters);
                    case "folders":
                        return await GetFoldersAsync(parameters);
                    case "sharedlinks":
                        return await GetSharedLinksAsync(parameters);
                    case "permissions":
                        return await GetPermissionsAsync(parameters);
                    case "revisions":
                        return await GetRevisionsAsync(parameters);
                    case "changes":
                        return await GetChangesAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Google Drive");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                var request = _driveService.Files.List();
                request.Fields = "files(id, name, mimeType, size, createdTime, modifiedTime, owners, parents, webViewLink, webContentLink, thumbnailLink, shared, sharingUser, permissions, properties)";

                // Apply filters
                if (parameters != null)
                {
                    if (parameters.ContainsKey("parentId"))
                    {
                        request.Q = $"'{parameters["parentId"]}' in parents";
                    }
                    if (parameters.ContainsKey("mimeType"))
                    {
                        request.Q = $"mimeType = '{parameters["mimeType"]}'";
                    }
                    if (parameters.ContainsKey("name"))
                    {
                        request.Q = $"name contains '{parameters["name"]}'";
                    }
                    if (parameters.ContainsKey("trashed"))
                    {
                        request.Q = $"trashed = {parameters["trashed"].ToString().ToLower()}";
                    }
                }

                var files = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("mimeType", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("createdTime", typeof(DateTime));
                dataTable.Columns.Add("modifiedTime", typeof(DateTime));
                dataTable.Columns.Add("owners", typeof(string));
                dataTable.Columns.Add("parents", typeof(string));
                dataTable.Columns.Add("webViewLink", typeof(string));
                dataTable.Columns.Add("webContentLink", typeof(string));
                dataTable.Columns.Add("thumbnailLink", typeof(string));
                dataTable.Columns.Add("shared", typeof(bool));
                dataTable.Columns.Add("sharingUser", typeof(string));
                dataTable.Columns.Add("permissions", typeof(string));
                dataTable.Columns.Add("properties", typeof(string));

                // Add rows
                foreach (var file in files.Files)
                {
                    var row = dataTable.NewRow();
                    row["id"] = file.Id;
                    row["name"] = file.Name;
                    row["mimeType"] = file.MimeType;
                    row["size"] = string.IsNullOrEmpty(file.Size) ? 0L : long.Parse(file.Size);
                    row["createdTime"] = file.CreatedTime;
                    row["modifiedTime"] = file.ModifiedTime;
                    row["owners"] = file.Owners != null ? string.Join(", ", file.Owners.Select(o => o.EmailAddress)) : null;
                    row["parents"] = file.Parents != null ? string.Join(", ", file.Parents) : null;
                    row["webViewLink"] = file.WebViewLink;
                    row["webContentLink"] = file.WebContentLink;
                    row["thumbnailLink"] = file.ThumbnailLink;
                    row["shared"] = file.Shared;
                    row["sharingUser"] = file.SharingUser?.EmailAddress;
                    row["permissions"] = file.Permissions != null ? JsonSerializer.Serialize(file.Permissions) : null;
                    row["properties"] = file.Properties != null ? JsonSerializer.Serialize(file.Properties) : null;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from Google Drive");
            }

            return dataTable;
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                var request = _driveService.Files.List();
                request.Q = "mimeType = 'application/vnd.google-apps.folder'";
                request.Fields = "files(id, name, createdTime, modifiedTime, owners, parents, webViewLink, shared, sharingUser, permissions)";

                // Apply filters
                if (parameters != null)
                {
                    if (parameters.ContainsKey("parentId"))
                    {
                        request.Q += $" and '{parameters["parentId"]}' in parents";
                    }
                    if (parameters.ContainsKey("name"))
                    {
                        request.Q += $" and name contains '{parameters["name"]}'";
                    }
                }

                var folders = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("createdTime", typeof(DateTime));
                dataTable.Columns.Add("modifiedTime", typeof(DateTime));
                dataTable.Columns.Add("owners", typeof(string));
                dataTable.Columns.Add("parents", typeof(string));
                dataTable.Columns.Add("webViewLink", typeof(string));
                dataTable.Columns.Add("shared", typeof(bool));
                dataTable.Columns.Add("sharingUser", typeof(string));
                dataTable.Columns.Add("permissions", typeof(string));

                // Add rows
                foreach (var folder in folders.Files)
                {
                    var row = dataTable.NewRow();
                    row["id"] = folder.Id;
                    row["name"] = folder.Name;
                    row["createdTime"] = folder.CreatedTime;
                    row["modifiedTime"] = folder.ModifiedTime;
                    row["owners"] = folder.Owners != null ? string.Join(", ", folder.Owners.Select(o => o.EmailAddress)) : null;
                    row["parents"] = folder.Parents != null ? string.Join(", ", folder.Parents) : null;
                    row["webViewLink"] = folder.WebViewLink;
                    row["shared"] = folder.Shared;
                    row["sharingUser"] = folder.SharingUser?.EmailAddress;
                    row["permissions"] = folder.Permissions != null ? JsonSerializer.Serialize(folder.Permissions) : null;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from Google Drive");
            }

            return dataTable;
        }

        private async Task<DataTable> GetSharedLinksAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("sharedlinks");

            try
            {
                var request = _driveService.Files.List();
                request.Q = "shared = true";
                request.Fields = "files(id, name, mimeType, webViewLink, webContentLink, thumbnailLink, sharingUser, permissions)";

                var files = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("mimeType", typeof(string));
                dataTable.Columns.Add("webViewLink", typeof(string));
                dataTable.Columns.Add("webContentLink", typeof(string));
                dataTable.Columns.Add("thumbnailLink", typeof(string));
                dataTable.Columns.Add("sharingUser", typeof(string));
                dataTable.Columns.Add("permissions", typeof(string));

                // Add rows
                foreach (var file in files.Files)
                {
                    var row = dataTable.NewRow();
                    row["id"] = file.Id;
                    row["name"] = file.Name;
                    row["mimeType"] = file.MimeType;
                    row["webViewLink"] = file.WebViewLink;
                    row["webContentLink"] = file.WebContentLink;
                    row["thumbnailLink"] = file.ThumbnailLink;
                    row["sharingUser"] = file.SharingUser?.EmailAddress;
                    row["permissions"] = file.Permissions != null ? JsonSerializer.Serialize(file.Permissions) : null;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared links from Google Drive");
            }

            return dataTable;
        }

        private async Task<DataTable> GetPermissionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("permissions");

            try
            {
                if (!parameters.ContainsKey("fileId"))
                {
                    throw new ArgumentException("fileId parameter is required for permissions");
                }

                var fileId = parameters["fileId"].ToString();
                var request = _driveService.Permissions.List(fileId);
                var permissions = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("emailAddress", typeof(string));
                dataTable.Columns.Add("domain", typeof(string));
                dataTable.Columns.Add("role", typeof(string));
                dataTable.Columns.Add("displayName", typeof(string));
                dataTable.Columns.Add("allowFileDiscovery", typeof(bool));
                dataTable.Columns.Add("deleted", typeof(bool));
                dataTable.Columns.Add("pendingOwner", typeof(bool));

                // Add rows
                foreach (var permission in permissions.Permissions)
                {
                    var row = dataTable.NewRow();
                    row["id"] = permission.Id;
                    row["type"] = permission.Type;
                    row["emailAddress"] = permission.EmailAddress;
                    row["domain"] = permission.Domain;
                    row["role"] = permission.Role;
                    row["displayName"] = permission.DisplayName;
                    row["allowFileDiscovery"] = permission.AllowFileDiscovery;
                    row["deleted"] = permission.Deleted;
                    row["pendingOwner"] = permission.PendingOwner;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions from Google Drive");
            }

            return dataTable;
        }

        private async Task<DataTable> GetRevisionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("revisions");

            try
            {
                if (!parameters.ContainsKey("fileId"))
                {
                    throw new ArgumentException("fileId parameter is required for revisions");
                }

                var fileId = parameters["fileId"].ToString();
                var request = _driveService.Revisions.List(fileId);
                var revisions = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("mimeType", typeof(string));
                dataTable.Columns.Add("modifiedTime", typeof(DateTime));
                dataTable.Columns.Add("keepForever", typeof(bool));
                dataTable.Columns.Add("published", typeof(bool));
                dataTable.Columns.Add("publishAuto", typeof(bool));
                dataTable.Columns.Add("publishedOutsideDomain", typeof(bool));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("lastModifyingUser", typeof(string));

                // Add rows
                foreach (var revision in revisions.Revisions)
                {
                    var row = dataTable.NewRow();
                    row["id"] = revision.Id;
                    row["mimeType"] = revision.MimeType;
                    row["modifiedTime"] = revision.ModifiedTime;
                    row["keepForever"] = revision.KeepForever;
                    row["published"] = revision.Published;
                    row["publishAuto"] = revision.PublishAuto;
                    row["publishedOutsideDomain"] = revision.PublishedOutsideDomain;
                    row["size"] = string.IsNullOrEmpty(revision.Size) ? 0L : long.Parse(revision.Size);
                    row["lastModifyingUser"] = revision.LastModifyingUser?.EmailAddress;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revisions from Google Drive");
            }

            return dataTable;
        }

        private async Task<DataTable> GetChangesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("changes");

            try
            {
                var request = _driveService.Changes.List();
                request.Fields = "changes(file(id, name, mimeType, modifiedTime), changeType, time)";

                // Apply filters
                if (parameters != null && parameters.ContainsKey("pageToken"))
                {
                    request.PageToken = parameters["pageToken"].ToString();
                }

                var changes = await request.ExecuteAsync();

                // Create columns
                dataTable.Columns.Add("fileId", typeof(string));
                dataTable.Columns.Add("fileName", typeof(string));
                dataTable.Columns.Add("mimeType", typeof(string));
                dataTable.Columns.Add("modifiedTime", typeof(DateTime));
                dataTable.Columns.Add("changeType", typeof(string));
                dataTable.Columns.Add("time", typeof(DateTime));

                // Add rows
                foreach (var change in changes.Changes)
                {
                    var row = dataTable.NewRow();
                    row["fileId"] = change.File?.Id;
                    row["fileName"] = change.File?.Name;
                    row["mimeType"] = change.File?.MimeType;
                    row["modifiedTime"] = change.File?.ModifiedTime;
                    row["changeType"] = change.ChangeType;
                    row["time"] = change.Time;

                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting changes from Google Drive");
            }

            return dataTable;
        }

        public async Task<bool> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Drive API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await UpdateFilesAsync(data, parameters);
                    case "folders":
                        return await UpdateFoldersAsync(data, parameters);
                    case "permissions":
                        return await UpdatePermissionsAsync(data, parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Google Drive");
                return false;
            }
        }

        private async Task<bool> UpdateFilesAsync(DataTable data, Dictionary<string, object> parameters = null)
        {
            foreach (DataRow row in data.Rows)
            {
                try
                {
                    var fileId = row["id"]?.ToString();
                    if (string.IsNullOrEmpty(fileId))
                    {
                        _logger.LogError("File ID is required for update");
                        continue;
                    }

                    var file = new Google.Apis.Drive.v3.Data.File();
                    if (row["name"] != DBNull.Value)
                    {
                        file.Name = row["name"].ToString();
                    }

                    var updateRequest = _driveService.Files.Update(file, fileId);
                    await updateRequest.ExecuteAsync();

                    _logger.LogInformation($"Updated file {fileId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating file");
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> UpdateFoldersAsync(DataTable data, Dictionary<string, object> parameters = null)
        {
            foreach (DataRow row in data.Rows)
            {
                try
                {
                    var folderId = row["id"]?.ToString();
                    if (string.IsNullOrEmpty(folderId))
                    {
                        _logger.LogError("Folder ID is required for update");
                        continue;
                    }

                    var folder = new Google.Apis.Drive.v3.Data.File();
                    folder.MimeType = "application/vnd.google-apps.folder";
                    if (row["name"] != DBNull.Value)
                    {
                        folder.Name = row["name"].ToString();
                    }

                    var updateRequest = _driveService.Files.Update(folder, folderId);
                    await updateRequest.ExecuteAsync();

                    _logger.LogInformation($"Updated folder {folderId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating folder");
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> UpdatePermissionsAsync(DataTable data, Dictionary<string, object> parameters = null)
        {
            if (!parameters.ContainsKey("fileId"))
            {
                throw new ArgumentException("fileId parameter is required for permissions update");
            }

            var fileId = parameters["fileId"].ToString();

            foreach (DataRow row in data.Rows)
            {
                try
                {
                    var permissionId = row["id"]?.ToString();
                    if (string.IsNullOrEmpty(permissionId))
                    {
                        _logger.LogError("Permission ID is required for update");
                        continue;
                    }

                    var permission = new Permission();
                    if (row["role"] != DBNull.Value)
                    {
                        permission.Role = row["role"].ToString();
                    }

                    var updateRequest = _driveService.Permissions.Update(permission, fileId, permissionId);
                    await updateRequest.ExecuteAsync();

                    _logger.LogInformation($"Updated permission {permissionId} for file {fileId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating permission");
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Google Drive API");
            }

            if (!parameters.ContainsKey("id"))
            {
                throw new ArgumentException("id parameter is required for deletion");
            }

            try
            {
                var id = parameters["id"].ToString();

                switch (entityName.ToLower())
                {
                    case "files":
                    case "folders":
                        var deleteRequest = _driveService.Files.Delete(id);
                        await deleteRequest.ExecuteAsync();
                        break;
                    case "permissions":
                        if (!parameters.ContainsKey("fileId"))
                        {
                            throw new ArgumentException("fileId parameter is required for permission deletion");
                        }
                        var fileId = parameters["fileId"].ToString();
                        var deletePermissionRequest = _driveService.Permissions.Delete(fileId, id);
                        await deletePermissionRequest.ExecuteAsync();
                        break;
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }

                _logger.LogInformation($"Deleted {entityName} with id {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Google Drive");
                return false;
            }
        }

        public async Task<List<string>> GetEntitiesAsync()
        {
            return new List<string>
            {
                "files",
                "folders",
                "sharedlinks",
                "permissions",
                "revisions",
                "changes"
            };
        }

        public async Task<DataTable> GetEntityMetadataAsync(string entityName)
        {
            var metadata = new DataTable("Metadata");
            metadata.Columns.Add("ColumnName", typeof(string));
            metadata.Columns.Add("DataType", typeof(string));
            metadata.Columns.Add("IsNullable", typeof(bool));
            metadata.Columns.Add("Description", typeof(string));

            // Add common metadata fields based on entity type
            switch (entityName.ToLower())
            {
                case "files":
                    metadata.Rows.Add("id", "string", false, "Unique file identifier");
                    metadata.Rows.Add("name", "string", false, "File name");
                    metadata.Rows.Add("mimeType", "string", false, "MIME type of the file");
                    metadata.Rows.Add("size", "long", true, "Size of the file in bytes");
                    metadata.Rows.Add("createdTime", "datetime", true, "File creation time");
                    metadata.Rows.Add("modifiedTime", "datetime", true, "File modification time");
                    metadata.Rows.Add("owners", "string", true, "List of file owners");
                    metadata.Rows.Add("parents", "string", true, "List of parent folder IDs");
                    metadata.Rows.Add("webViewLink", "string", true, "Link for viewing the file");
                    metadata.Rows.Add("webContentLink", "string", true, "Link for downloading the file");
                    metadata.Rows.Add("thumbnailLink", "string", true, "Link to file thumbnail");
                    metadata.Rows.Add("shared", "boolean", true, "Whether the file is shared");
                    metadata.Rows.Add("sharingUser", "string", true, "User who shared the file");
                    metadata.Rows.Add("permissions", "string", true, "File permissions in JSON format");
                    metadata.Rows.Add("properties", "string", true, "Custom properties in JSON format");
                    break;

                case "folders":
                    metadata.Rows.Add("id", "string", false, "Unique folder identifier");
                    metadata.Rows.Add("name", "string", false, "Folder name");
                    metadata.Rows.Add("createdTime", "datetime", true, "Folder creation time");
                    metadata.Rows.Add("modifiedTime", "datetime", true, "Folder modification time");
                    metadata.Rows.Add("owners", "string", true, "List of folder owners");
                    metadata.Rows.Add("parents", "string", true, "List of parent folder IDs");
                    metadata.Rows.Add("webViewLink", "string", true, "Link for viewing the folder");
                    metadata.Rows.Add("shared", "boolean", true, "Whether the folder is shared");
                    metadata.Rows.Add("sharingUser", "string", true, "User who shared the folder");
                    metadata.Rows.Add("permissions", "string", true, "Folder permissions in JSON format");
                    break;

                case "sharedlinks":
                    metadata.Rows.Add("id", "string", false, "Unique file identifier");
                    metadata.Rows.Add("name", "string", false, "File name");
                    metadata.Rows.Add("mimeType", "string", false, "MIME type of the file");
                    metadata.Rows.Add("webViewLink", "string", true, "Link for viewing the file");
                    metadata.Rows.Add("webContentLink", "string", true, "Link for downloading the file");
                    metadata.Rows.Add("thumbnailLink", "string", true, "Link to file thumbnail");
                    metadata.Rows.Add("sharingUser", "string", true, "User who shared the file");
                    metadata.Rows.Add("permissions", "string", true, "File permissions in JSON format");
                    break;

                case "permissions":
                    metadata.Rows.Add("id", "string", false, "Unique permission identifier");
                    metadata.Rows.Add("type", "string", false, "Type of permission (user, group, domain, anyone)");
                    metadata.Rows.Add("emailAddress", "string", true, "Email address for user or group");
                    metadata.Rows.Add("domain", "string", true, "Domain name for domain permissions");
                    metadata.Rows.Add("role", "string", false, "Permission role (owner, writer, reader)");
                    metadata.Rows.Add("displayName", "string", true, "Display name of the user or group");
                    metadata.Rows.Add("allowFileDiscovery", "boolean", true, "Whether to allow file discovery");
                    metadata.Rows.Add("deleted", "boolean", true, "Whether the permission is deleted");
                    metadata.Rows.Add("pendingOwner", "boolean", true, "Whether the user is a pending owner");
                    break;

                case "revisions":
                    metadata.Rows.Add("id", "string", false, "Unique revision identifier");
                    metadata.Rows.Add("mimeType", "string", true, "MIME type of the revision");
                    metadata.Rows.Add("modifiedTime", "datetime", true, "Revision modification time");
                    metadata.Rows.Add("keepForever", "boolean", true, "Whether to keep the revision forever");
                    metadata.Rows.Add("published", "boolean", true, "Whether the revision is published");
                    metadata.Rows.Add("publishAuto", "boolean", true, "Whether to auto-publish the revision");
                    metadata.Rows.Add("publishedOutsideDomain", "boolean", true, "Whether published outside domain");
                    metadata.Rows.Add("size", "long", true, "Size of the revision in bytes");
                    metadata.Rows.Add("lastModifyingUser", "string", true, "Last user who modified the revision");
                    break;

                case "changes":
                    metadata.Rows.Add("fileId", "string", true, "ID of the changed file");
                    metadata.Rows.Add("fileName", "string", true, "Name of the changed file");
                    metadata.Rows.Add("mimeType", "string", true, "MIME type of the changed file");
                    metadata.Rows.Add("modifiedTime", "datetime", true, "Modification time of the file");
                    metadata.Rows.Add("changeType", "string", false, "Type of change (file, drive)");
                    metadata.Rows.Add("time", "datetime", false, "Time when the change occurred");
                    break;

                default:
                    metadata.Rows.Add("id", "string", false, "Unique identifier");
                    metadata.Rows.Add("name", "string", true, "Name");
                    metadata.Rows.Add("created", "datetime", true, "Creation timestamp");
                    metadata.Rows.Add("updated", "datetime", true, "Last update timestamp");
                    break;
            }

            return metadata;
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!_isConnected)
            {
                return false;
            }

            try
            {
                var aboutRequest = _driveService.About.Get();
                aboutRequest.Fields = "user(displayName)";
                var about = await aboutRequest.ExecuteAsync();
                return about != null && about.User != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetConnectionInfoAsync()
        {
            return new Dictionary<string, object>
            {
                ["DataSourceName"] = DataSourceName,
                ["DataSourceType"] = DataSourceType,
                ["Version"] = Version,
                ["IsConnected"] = _isConnected,
                ["UseServiceAccount"] = _config?.UseServiceAccount ?? false,
                ["ApplicationName"] = _config?.ApplicationName ?? "Not specified",
                ["HasClientId"] = !string.IsNullOrEmpty(_config?.ClientId),
                ["HasClientSecret"] = !string.IsNullOrEmpty(_config?.ClientSecret),
                ["HasRefreshToken"] = !string.IsNullOrEmpty(_config?.RefreshToken),
                ["HasServiceAccountKey"] = !string.IsNullOrEmpty(_config?.ServiceAccountKeyPath)
            };
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
