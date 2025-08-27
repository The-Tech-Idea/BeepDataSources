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
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;

namespace BeepDM.Connectors.CloudStorage.OneDrive
{
    public class OneDriveConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string AccessToken { get; set; }
        public bool UseAccessToken { get; set; }
    }

    public class OneDriveDataSource : IDataSource
    {
        private readonly ILogger<OneDriveDataSource> _logger;
        private GraphServiceClient _graphClient;
        private OneDriveConfig _config;
        private bool _isConnected;

        public string DataSourceName => "OneDrive";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Microsoft OneDrive Cloud Storage Data Source";

        public OneDriveDataSource(ILogger<OneDriveDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new OneDriveConfig();

                // Check authentication method
                if (parameters.ContainsKey("AccessToken"))
                {
                    _config.UseAccessToken = true;
                    _config.AccessToken = parameters["AccessToken"].ToString();
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret") &&
                         parameters.ContainsKey("TenantId"))
                {
                    _config.UseAccessToken = false;
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.TenantId = parameters["TenantId"].ToString();
                }
                else
                {
                    throw new ArgumentException("Either AccessToken or ClientId/ClientSecret/TenantId is required");
                }

                // Initialize the Graph client
                await InitializeGraphClientAsync();

                // Test connection by getting user information
                var user = await _graphClient.Me.GetAsync();

                if (user != null)
                {
                    _isConnected = true;
                    _logger.LogInformation($"Successfully connected to Microsoft Graph API for user: {user.DisplayName}");
                    return true;
                }

                _logger.LogError("Failed to connect to Microsoft Graph API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Microsoft Graph API");
                return false;
            }
        }

        private async Task InitializeGraphClientAsync()
        {
            if (_config.UseAccessToken)
            {
                // Use Access Token authentication
                var authProvider = new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);
                    return Task.CompletedTask;
                });

                _graphClient = new GraphServiceClient(authProvider);
            }
            else
            {
                // Use OAuth 2.0 with client credentials
                var confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create(_config.ClientId)
                    .WithClientSecret(_config.ClientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{_config.TenantId}"))
                    .Build();

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var authenticationResult = await confidentialClientApplication
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                var authProvider = new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
                    return Task.CompletedTask;
                });

                _graphClient = new GraphServiceClient(authProvider);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_graphClient != null)
                {
                    _graphClient = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from Microsoft Graph API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Microsoft Graph API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Graph API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "driveitems":
                        return await GetDriveItemsAsync(parameters);
                    case "drives":
                        return await GetDrivesAsync(parameters);
                    case "shareditems":
                        return await GetSharedItemsAsync(parameters);
                    case "permissions":
                        return await GetPermissionsAsync(parameters);
                    case "versions":
                        return await GetVersionsAsync(parameters);
                    case "thumbnails":
                        return await GetThumbnailsAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetDriveItemsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("driveitems");

            try
            {
                string path = "";
                if (parameters != null && parameters.ContainsKey("path"))
                {
                    path = parameters["path"].ToString();
                }

                IDriveItemChildrenCollectionPage children;
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    children = await _graphClient.Me.Drive.Root.Children.GetAsync();
                }
                else
                {
                    var item = await _graphClient.Me.Drive.Root.ItemWithPath(path).GetAsync();
                    if (item.Folder != null)
                    {
                        children = await _graphClient.Me.Drive.Items[item.Id].Children.GetAsync();
                    }
                    else
                    {
                        // Single item
                        children = new DriveItemChildrenCollectionPage
                        {
                            new List<DriveItem> { item }
                        };
                    }
                }

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("webUrl", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("createdDateTime", typeof(DateTime));
                dataTable.Columns.Add("lastModifiedDateTime", typeof(DateTime));
                dataTable.Columns.Add("downloadUrl", typeof(string));
                dataTable.Columns.Add("fileType", typeof(string));
                dataTable.Columns.Add("folder_childCount", typeof(int));
                dataTable.Columns.Add("shared_scope", typeof(string));
                dataTable.Columns.Add("parentReference", typeof(string));

                // Add rows
                foreach (var item in children)
                {
                    var row = dataTable.NewRow();
                    row["id"] = item.Id;
                    row["name"] = item.Name;
                    row["webUrl"] = item.WebUrl;
                    row["size"] = item.Size;
                    row["createdDateTime"] = item.CreatedDateTime;
                    row["lastModifiedDateTime"] = item.LastModifiedDateTime;
                    row["downloadUrl"] = item.AdditionalData?.ContainsKey("@microsoft.graph.downloadUrl") == true ?
                        item.AdditionalData["@microsoft.graph.downloadUrl"].ToString() : "";
                    row["fileType"] = item.File?.MimeType ?? "";
                    row["folder_childCount"] = item.Folder?.ChildCount ?? 0;
                    row["shared_scope"] = item.Shared?.Scope ?? "";
                    row["parentReference"] = JsonSerializer.Serialize(item.ParentReference);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drive items from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetDrivesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("drives");

            try
            {
                var drives = await _graphClient.Me.Drives.GetAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("description", typeof(string));
                dataTable.Columns.Add("webUrl", typeof(string));
                dataTable.Columns.Add("driveType", typeof(string));
                dataTable.Columns.Add("createdDateTime", typeof(DateTime));
                dataTable.Columns.Add("lastModifiedDateTime", typeof(DateTime));
                dataTable.Columns.Add("quota_total", typeof(long));
                dataTable.Columns.Add("quota_used", typeof(long));
                dataTable.Columns.Add("quota_remaining", typeof(long));
                dataTable.Columns.Add("quota_deleted", typeof(long));

                // Add rows
                foreach (var drive in drives.Value)
                {
                    var row = dataTable.NewRow();
                    row["id"] = drive.Id;
                    row["name"] = drive.Name;
                    row["description"] = drive.Description;
                    row["webUrl"] = drive.WebUrl;
                    row["driveType"] = drive.DriveType;
                    row["createdDateTime"] = drive.CreatedDateTime;
                    row["lastModifiedDateTime"] = drive.LastModifiedDateTime;
                    row["quota_total"] = drive.Quota?.Total ?? 0;
                    row["quota_used"] = drive.Quota?.Used ?? 0;
                    row["quota_remaining"] = drive.Quota?.Remaining ?? 0;
                    row["quota_deleted"] = drive.Quota?.Deleted ?? 0;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting drives from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetSharedItemsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("shareditems");

            try
            {
                var sharedItems = await _graphClient.Me.Drive.SharedWithMe.GetAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("webUrl", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("createdDateTime", typeof(DateTime));
                dataTable.Columns.Add("lastModifiedDateTime", typeof(DateTime));
                dataTable.Columns.Add("sharedBy", typeof(string));
                dataTable.Columns.Add("sharedDateTime", typeof(DateTime));
                dataTable.Columns.Add("remoteItem", typeof(string));

                // Add rows
                foreach (var item in sharedItems.Value)
                {
                    var row = dataTable.NewRow();
                    row["id"] = item.Id;
                    row["name"] = item.Name;
                    row["webUrl"] = item.WebUrl;
                    row["size"] = item.Size;
                    row["createdDateTime"] = item.CreatedDateTime;
                    row["lastModifiedDateTime"] = item.LastModifiedDateTime;
                    row["sharedBy"] = JsonSerializer.Serialize(item.SharedBy);
                    row["sharedDateTime"] = item.SharedDateTime;
                    row["remoteItem"] = JsonSerializer.Serialize(item.RemoteItem);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared items from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetPermissionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("permissions");

            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for permissions");
                }

                var itemId = parameters["itemId"].ToString();
                var permissions = await _graphClient.Me.Drive.Items[itemId].Permissions.GetAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("roles", typeof(string));
                dataTable.Columns.Add("grantedTo", typeof(string));
                dataTable.Columns.Add("grantedToIdentities", typeof(string));
                dataTable.Columns.Add("inheritedFrom", typeof(string));
                dataTable.Columns.Add("link_scope", typeof(string));
                dataTable.Columns.Add("link_type", typeof(string));
                dataTable.Columns.Add("link_webUrl", typeof(string));

                // Add rows
                foreach (var permission in permissions.Value)
                {
                    var row = dataTable.NewRow();
                    row["id"] = permission.Id;
                    row["roles"] = JsonSerializer.Serialize(permission.Roles);
                    row["grantedTo"] = JsonSerializer.Serialize(permission.GrantedTo);
                    row["grantedToIdentities"] = JsonSerializer.Serialize(permission.GrantedToIdentities);
                    row["inheritedFrom"] = JsonSerializer.Serialize(permission.InheritedFrom);
                    row["link_scope"] = permission.Link?.Scope ?? "";
                    row["link_type"] = permission.Link?.Type ?? "";
                    row["link_webUrl"] = permission.Link?.WebUrl ?? "";

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetVersionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("versions");

            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for versions");
                }

                var itemId = parameters["itemId"].ToString();
                var versions = await _graphClient.Me.Drive.Items[itemId].Versions.GetAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("lastModifiedDateTime", typeof(DateTime));
                dataTable.Columns.Add("lastModifiedBy", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("publication", typeof(string));

                // Add rows
                foreach (var version in versions.Value)
                {
                    var row = dataTable.NewRow();
                    row["id"] = version.Id;
                    row["lastModifiedDateTime"] = version.LastModifiedDateTime;
                    row["lastModifiedBy"] = JsonSerializer.Serialize(version.LastModifiedBy);
                    row["size"] = version.Size;
                    row["publication"] = JsonSerializer.Serialize(version.Publication);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions from OneDrive");
                throw;
            }
        }

        private async Task<DataTable> GetThumbnailsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("thumbnails");

            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for thumbnails");
                }

                var itemId = parameters["itemId"].ToString();
                var thumbnails = await _graphClient.Me.Drive.Items[itemId].Thumbnails.GetAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("small_url", typeof(string));
                dataTable.Columns.Add("small_width", typeof(int));
                dataTable.Columns.Add("small_height", typeof(int));
                dataTable.Columns.Add("medium_url", typeof(string));
                dataTable.Columns.Add("medium_width", typeof(int));
                dataTable.Columns.Add("medium_height", typeof(int));
                dataTable.Columns.Add("large_url", typeof(string));
                dataTable.Columns.Add("large_width", typeof(int));
                dataTable.Columns.Add("large_height", typeof(int));

                // Add rows
                foreach (var thumbnailSet in thumbnails.Value)
                {
                    var row = dataTable.NewRow();
                    row["id"] = thumbnailSet.Id;
                    row["small_url"] = thumbnailSet.Small?.Url ?? "";
                    row["small_width"] = thumbnailSet.Small?.Width ?? 0;
                    row["small_height"] = thumbnailSet.Small?.Height ?? 0;
                    row["medium_url"] = thumbnailSet.Medium?.Url ?? "";
                    row["medium_width"] = thumbnailSet.Medium?.Width ?? 0;
                    row["medium_height"] = thumbnailSet.Medium?.Height ?? 0;
                    row["large_url"] = thumbnailSet.Large?.Url ?? "";
                    row["large_width"] = thumbnailSet.Large?.Width ?? 0;
                    row["large_height"] = thumbnailSet.Large?.Height ?? 0;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thumbnails from OneDrive");
                throw;
            }
        }

        public async Task<DataTable> GetEntitiesAsync()
        {
            var dataTable = new DataTable("entities");

            // Create columns
            dataTable.Columns.Add("entity_name", typeof(string));
            dataTable.Columns.Add("entity_type", typeof(string));
            dataTable.Columns.Add("description", typeof(string));
            dataTable.Columns.Add("supports_create", typeof(bool));
            dataTable.Columns.Add("supports_read", typeof(bool));
            dataTable.Columns.Add("supports_update", typeof(bool));
            dataTable.Columns.Add("supports_delete", typeof(bool));

            // Add entity definitions
            var entities = new[]
            {
                new { Name = "driveitems", Type = "DriveItem", Description = "Files and folders in OneDrive", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "drives", Type = "Drive", Description = "OneDrive storage containers", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "shareditems", Type = "SharedItem", Description = "Items shared with the user", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "permissions", Type = "Permission", Description = "Sharing permissions for items", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "versions", Type = "Version", Description = "Version history of files", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "thumbnails", Type = "Thumbnail", Description = "Thumbnail images for files", Create = false, Read = true, Update = false, Delete = false }
            };

            foreach (var entity in entities)
            {
                var row = dataTable.NewRow();
                row["entity_name"] = entity.Name;
                row["entity_type"] = entity.Type;
                row["description"] = entity.Description;
                row["supports_create"] = entity.Create;
                row["supports_read"] = entity.Read;
                row["supports_update"] = entity.Update;
                row["supports_delete"] = entity.Delete;

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public async Task<DataTable> CreateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Graph API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "driveitems":
                        return await CreateDriveItemAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in OneDrive");
                throw;
            }
        }

        private async Task<DataTable> CreateDriveItemAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("driveitems");

            try
            {
                if (!data.ContainsKey("name"))
                {
                    throw new ArgumentException("Name is required for drive item creation");
                }

                var name = data["name"].ToString();
                var parentId = data.ContainsKey("parentId") ? data["parentId"].ToString() : null;

                DriveItem newItem;
                if (data.ContainsKey("folder"))
                {
                    // Create folder
                    var folder = new Folder();
                    var driveItem = new DriveItem
                    {
                        Name = name,
                        Folder = folder
                    };

                    if (parentId != null)
                    {
                        newItem = await _graphClient.Me.Drive.Items[parentId].Children.PostAsync(driveItem);
                    }
                    else
                    {
                        newItem = await _graphClient.Me.Drive.Root.Children.PostAsync(driveItem);
                    }
                }
                else
                {
                    // File upload would require stream handling - placeholder for now
                    throw new NotImplementedException("File upload not yet implemented");
                }

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("webUrl", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("createdDateTime", typeof(DateTime));

                // Add created item
                var row = dataTable.NewRow();
                row["id"] = newItem.Id;
                row["name"] = newItem.Name;
                row["webUrl"] = newItem.WebUrl;
                row["size"] = newItem.Size;
                row["createdDateTime"] = newItem.CreatedDateTime;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating drive item in OneDrive");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Graph API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "driveitems":
                        return await UpdateDriveItemAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in OneDrive");
                throw;
            }
        }

        private async Task<DataTable> UpdateDriveItemAsync(Dictionary<string, object> data)
        {
            // Implementation for drive item update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Drive item update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Microsoft Graph API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "driveitems":
                        return await DeleteDriveItemAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from OneDrive");
                throw;
            }
        }

        private async Task<bool> DeleteDriveItemAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for drive item deletion");
                }

                var itemId = parameters["itemId"].ToString();
                await _graphClient.Me.Drive.Items[itemId].DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting drive item from OneDrive");
                throw;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // Implementation for custom query execution would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Custom query execution not yet implemented");
        }

        public async Task<DataTable> GetEntityMetadataAsync(string entityName)
        {
            var dataTable = new DataTable("metadata");

            // Create columns
            dataTable.Columns.Add("field_name", typeof(string));
            dataTable.Columns.Add("field_type", typeof(string));
            dataTable.Columns.Add("is_nullable", typeof(bool));
            dataTable.Columns.Add("description", typeof(string));

            try
            {
                switch (entityName.ToLower())
                {
                    case "driveitems":
                        var driveItemFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the drive item" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Name of the drive item" },
                            new { Name = "webUrl", Type = "string", Nullable = true, Description = "Web URL to access the item" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the item in bytes" },
                            new { Name = "createdDateTime", Type = "datetime", Nullable = true, Description = "Date and time of creation" },
                            new { Name = "lastModifiedDateTime", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "downloadUrl", Type = "string", Nullable = true, Description = "Direct download URL for files" },
                            new { Name = "fileType", Type = "string", Nullable = true, Description = "MIME type for files" },
                            new { Name = "folder_childCount", Type = "int", Nullable = true, Description = "Number of children for folders" },
                            new { Name = "shared_scope", Type = "string", Nullable = true, Description = "Sharing scope if shared" },
                            new { Name = "parentReference", Type = "string", Nullable = true, Description = "JSON string of parent reference" }
                        };

                        foreach (var field in driveItemFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "drives":
                        var driveFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the drive" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Display name of the drive" },
                            new { Name = "description", Type = "string", Nullable = true, Description = "Description of the drive" },
                            new { Name = "webUrl", Type = "string", Nullable = true, Description = "Web URL to access the drive" },
                            new { Name = "driveType", Type = "string", Nullable = true, Description = "Type of the drive" },
                            new { Name = "createdDateTime", Type = "datetime", Nullable = true, Description = "Date and time of creation" },
                            new { Name = "lastModifiedDateTime", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "quota_total", Type = "long", Nullable = true, Description = "Total quota in bytes" },
                            new { Name = "quota_used", Type = "long", Nullable = true, Description = "Used quota in bytes" },
                            new { Name = "quota_remaining", Type = "long", Nullable = true, Description = "Remaining quota in bytes" },
                            new { Name = "quota_deleted", Type = "long", Nullable = true, Description = "Deleted quota in bytes" }
                        };

                        foreach (var field in driveFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "shareditems":
                        var sharedItemFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the shared item" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Name of the shared item" },
                            new { Name = "webUrl", Type = "string", Nullable = true, Description = "Web URL to access the item" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the item in bytes" },
                            new { Name = "createdDateTime", Type = "datetime", Nullable = true, Description = "Date and time of creation" },
                            new { Name = "lastModifiedDateTime", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "sharedBy", Type = "string", Nullable = true, Description = "JSON string of user who shared the item" },
                            new { Name = "sharedDateTime", Type = "datetime", Nullable = true, Description = "Date and time when item was shared" },
                            new { Name = "remoteItem", Type = "string", Nullable = true, Description = "JSON string of remote item information" }
                        };

                        foreach (var field in sharedItemFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "permissions":
                        var permissionFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the permission" },
                            new { Name = "roles", Type = "string", Nullable = true, Description = "JSON array of permission roles" },
                            new { Name = "grantedTo", Type = "string", Nullable = true, Description = "JSON string of user granted access" },
                            new { Name = "grantedToIdentities", Type = "string", Nullable = true, Description = "JSON string of identities granted access" },
                            new { Name = "inheritedFrom", Type = "string", Nullable = true, Description = "JSON string of inherited permission source" },
                            new { Name = "link_scope", Type = "string", Nullable = true, Description = "Sharing link scope" },
                            new { Name = "link_type", Type = "string", Nullable = true, Description = "Sharing link type" },
                            new { Name = "link_webUrl", Type = "string", Nullable = true, Description = "Sharing link web URL" }
                        };

                        foreach (var field in permissionFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "versions":
                        var versionFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the version" },
                            new { Name = "lastModifiedDateTime", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "lastModifiedBy", Type = "string", Nullable = true, Description = "JSON string of user who made the modification" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the version in bytes" },
                            new { Name = "publication", Type = "string", Nullable = true, Description = "JSON string of publication information" }
                        };

                        foreach (var field in versionFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "thumbnails":
                        var thumbnailFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the thumbnail set" },
                            new { Name = "small_url", Type = "string", Nullable = true, Description = "Small thumbnail URL" },
                            new { Name = "small_width", Type = "int", Nullable = true, Description = "Small thumbnail width" },
                            new { Name = "small_height", Type = "int", Nullable = true, Description = "Small thumbnail height" },
                            new { Name = "medium_url", Type = "string", Nullable = true, Description = "Medium thumbnail URL" },
                            new { Name = "medium_width", Type = "int", Nullable = true, Description = "Medium thumbnail width" },
                            new { Name = "medium_height", Type = "int", Nullable = true, Description = "Medium thumbnail height" },
                            new { Name = "large_url", Type = "string", Nullable = true, Description = "Large thumbnail URL" },
                            new { Name = "large_width", Type = "int", Nullable = true, Description = "Large thumbnail width" },
                            new { Name = "large_height", Type = "int", Nullable = true, Description = "Large thumbnail height" }
                        };

                        foreach (var field in thumbnailFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting metadata for entity {entityName}");
                throw;
            }
        }
    }
}
