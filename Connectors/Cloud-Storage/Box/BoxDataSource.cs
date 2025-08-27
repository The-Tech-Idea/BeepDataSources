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
using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.CloudStorage.Box
{
    public class BoxConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public bool UseAccessToken { get; set; }
    }

    public class BoxDataSource : IDataSource
    {
        private readonly ILogger<BoxDataSource> _logger;
        private BoxClient _boxClient;
        private BoxConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Box";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Box Cloud Storage Data Source";

        public BoxDataSource(ILogger<BoxDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new BoxConfig();

                // Check authentication method
                if (parameters.ContainsKey("AccessToken"))
                {
                    _config.UseAccessToken = true;
                    _config.AccessToken = parameters["AccessToken"].ToString();
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret") &&
                         parameters.ContainsKey("RefreshToken"))
                {
                    _config.UseAccessToken = false;
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.RefreshToken = parameters["RefreshToken"].ToString();
                }
                else
                {
                    throw new ArgumentException("Either AccessToken or ClientId/ClientSecret/RefreshToken is required");
                }

                // Initialize the Box client
                await InitializeBoxClientAsync();

                // Test connection by getting user information
                var user = await _boxClient.UsersManager.GetCurrentUserInformationAsync();

                if (user != null)
                {
                    _isConnected = true;
                    _logger.LogInformation($"Successfully connected to Box API for user: {user.Name}");
                    return true;
                }

                _logger.LogError("Failed to connect to Box API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Box API");
                return false;
            }
        }

        private async Task InitializeBoxClientAsync()
        {
            var config = new BoxConfig(_config.ClientId ?? "dummy", _config.ClientSecret ?? "dummy");

            if (_config.UseAccessToken)
            {
                // Use Access Token authentication
                var auth = new OAuthSession(_config.AccessToken, "dummy_refresh", 3600, "bearer");
                _boxClient = new BoxClient(config, auth);
            }
            else
            {
                // Use OAuth 2.0 with refresh token
                var httpClient = new HttpClient();
                var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.box.com/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", _config.RefreshToken),
                        new KeyValuePair<string, string>("client_id", _config.ClientId),
                        new KeyValuePair<string, string>("client_secret", _config.ClientSecret)
                    })
                };

                var response = await httpClient.SendAsync(refreshRequest);
                response.EnsureSuccessStatusCode();

                var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var newAccessToken = tokenResponse.GetProperty("access_token").GetString();

                var auth = new OAuthSession(newAccessToken, "dummy_refresh", 3600, "bearer");
                _boxClient = new BoxClient(config, auth);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_boxClient != null)
                {
                    _boxClient = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from Box API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Box API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Box API");
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
                    case "users":
                        return await GetUsersAsync(parameters);
                    case "groups":
                        return await GetGroupsAsync(parameters);
                    case "metadata":
                        return await GetMetadataAsync(parameters);
                    case "webhooks":
                        return await GetWebhooksAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Box");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                string folderId = "0"; // Root folder by default
                if (parameters != null && parameters.ContainsKey("folderId"))
                {
                    folderId = parameters["folderId"].ToString();
                }

                var items = await _boxClient.FoldersManager.GetFolderItemsAsync(folderId, 1000);
                var files = items.Entries.Where(e => e.Type == "file");

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("modified_at", typeof(DateTime));
                dataTable.Columns.Add("created_by", typeof(string));
                dataTable.Columns.Add("modified_by", typeof(string));
                dataTable.Columns.Add("owned_by", typeof(string));
                dataTable.Columns.Add("shared_link", typeof(string));
                dataTable.Columns.Add("parent", typeof(string));
                dataTable.Columns.Add("item_status", typeof(string));

                // Add rows
                foreach (var file in files)
                {
                    var row = dataTable.NewRow();
                    row["id"] = file.Id;
                    row["name"] = file.Name;
                    row["size"] = file.Size;
                    row["created_at"] = file.CreatedAt;
                    row["modified_at"] = file.ModifiedAt;
                    row["created_by"] = JsonSerializer.Serialize(file.CreatedBy);
                    row["modified_by"] = JsonSerializer.Serialize(file.ModifiedBy);
                    row["owned_by"] = JsonSerializer.Serialize(file.OwnedBy);
                    row["shared_link"] = JsonSerializer.Serialize(file.SharedLink);
                    row["parent"] = JsonSerializer.Serialize(file.Parent);
                    row["item_status"] = file.ItemStatus;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from Box");
                throw;
            }
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                string folderId = "0"; // Root folder by default
                if (parameters != null && parameters.ContainsKey("folderId"))
                {
                    folderId = parameters["folderId"].ToString();
                }

                var items = await _boxClient.FoldersManager.GetFolderItemsAsync(folderId, 1000);
                var folders = items.Entries.Where(e => e.Type == "folder");

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("modified_at", typeof(DateTime));
                dataTable.Columns.Add("created_by", typeof(string));
                dataTable.Columns.Add("modified_by", typeof(string));
                dataTable.Columns.Add("owned_by", typeof(string));
                dataTable.Columns.Add("shared_link", typeof(string));
                dataTable.Columns.Add("parent", typeof(string));
                dataTable.Columns.Add("item_status", typeof(string));
                dataTable.Columns.Add("folder_upload_email", typeof(string));

                // Add rows
                foreach (var folder in folders)
                {
                    var row = dataTable.NewRow();
                    row["id"] = folder.Id;
                    row["name"] = folder.Name;
                    row["created_at"] = folder.CreatedAt;
                    row["modified_at"] = folder.ModifiedAt;
                    row["created_by"] = JsonSerializer.Serialize(folder.CreatedBy);
                    row["modified_by"] = JsonSerializer.Serialize(folder.ModifiedBy);
                    row["owned_by"] = JsonSerializer.Serialize(folder.OwnedBy);
                    row["shared_link"] = JsonSerializer.Serialize(folder.SharedLink);
                    row["parent"] = JsonSerializer.Serialize(folder.Parent);
                    row["item_status"] = folder.ItemStatus;
                    row["folder_upload_email"] = folder.FolderUploadEmail?.Access?.ToString();

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from Box");
                throw;
            }
        }

        private async Task<DataTable> GetSharedLinksAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("sharedlinks");

            try
            {
                // Get all folders and files with shared links
                var rootItems = await _boxClient.FoldersManager.GetFolderItemsAsync("0", 1000);
                var sharedItems = rootItems.Entries.Where(e => e.SharedLink != null);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("vanity_url", typeof(string));
                dataTable.Columns.Add("effective_access", typeof(string));
                dataTable.Columns.Add("effective_permission", typeof(string));
                dataTable.Columns.Add("unshared_at", typeof(DateTime));
                dataTable.Columns.Add("is_password_enabled", typeof(bool));
                dataTable.Columns.Add("password", typeof(string));

                // Add rows
                foreach (var item in sharedItems)
                {
                    var row = dataTable.NewRow();
                    row["id"] = item.Id;
                    row["name"] = item.Name;
                    row["type"] = item.Type;
                    row["url"] = item.SharedLink?.Url;
                    row["vanity_url"] = item.SharedLink?.VanityUrl;
                    row["effective_access"] = item.SharedLink?.EffectiveAccess;
                    row["effective_permission"] = item.SharedLink?.EffectivePermission;
                    row["unshared_at"] = item.SharedLink?.UnsharedAt;
                    row["is_password_enabled"] = item.SharedLink?.IsPasswordEnabled;
                    row["password"] = item.SharedLink?.Password;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared links from Box");
                throw;
            }
        }

        private async Task<DataTable> GetUsersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("users");

            try
            {
                var currentUser = await _boxClient.UsersManager.GetCurrentUserInformationAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("login", typeof(string));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("modified_at", typeof(DateTime));
                dataTable.Columns.Add("language", typeof(string));
                dataTable.Columns.Add("timezone", typeof(string));
                dataTable.Columns.Add("space_amount", typeof(long));
                dataTable.Columns.Add("space_used", typeof(long));
                dataTable.Columns.Add("max_upload_size", typeof(long));
                dataTable.Columns.Add("status", typeof(string));
                dataTable.Columns.Add("job_title", typeof(string));
                dataTable.Columns.Add("phone", typeof(string));
                dataTable.Columns.Add("address", typeof(string));
                dataTable.Columns.Add("avatar_url", typeof(string));

                // Add current user
                var row = dataTable.NewRow();
                row["id"] = currentUser.Id;
                row["name"] = currentUser.Name;
                row["login"] = currentUser.Login;
                row["created_at"] = currentUser.CreatedAt;
                row["modified_at"] = currentUser.ModifiedAt;
                row["language"] = currentUser.Language;
                row["timezone"] = currentUser.Timezone;
                row["space_amount"] = currentUser.SpaceAmount;
                row["space_used"] = currentUser.SpaceUsed;
                row["max_upload_size"] = currentUser.MaxUploadSize;
                row["status"] = currentUser.Status;
                row["job_title"] = currentUser.JobTitle;
                row["phone"] = currentUser.Phone;
                row["address"] = currentUser.Address;
                row["avatar_url"] = currentUser.AvatarUrl;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from Box");
                throw;
            }
        }

        private async Task<DataTable> GetGroupsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("groups");

            try
            {
                var groups = await _boxClient.GroupsManager.GetAllGroupsAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("modified_at", typeof(DateTime));
                dataTable.Columns.Add("provenance", typeof(string));
                dataTable.Columns.Add("external_sync_identifier", typeof(string));
                dataTable.Columns.Add("description", typeof(string));
                dataTable.Columns.Add("invitability_level", typeof(string));
                dataTable.Columns.Add("member_viewability_level", typeof(string));

                // Add rows
                foreach (var group in groups.Entries)
                {
                    var row = dataTable.NewRow();
                    row["id"] = group.Id;
                    row["name"] = group.Name;
                    row["created_at"] = group.CreatedAt;
                    row["modified_at"] = group.ModifiedAt;
                    row["provenance"] = group.Provenance;
                    row["external_sync_identifier"] = group.ExternalSyncIdentifier;
                    row["description"] = group.Description;
                    row["invitability_level"] = group.InvitabilityLevel;
                    row["member_viewability_level"] = group.MemberViewabilityLevel;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups from Box");
                throw;
            }
        }

        private async Task<DataTable> GetMetadataAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("metadata");

            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for metadata");
                }

                var itemId = parameters["itemId"].ToString();
                var metadata = await _boxClient.MetadataManager.GetAllMetadataAsync(itemId);

                // Create columns
                dataTable.Columns.Add("item_id", typeof(string));
                dataTable.Columns.Add("template", typeof(string));
                dataTable.Columns.Add("scope", typeof(string));
                dataTable.Columns.Add("metadata", typeof(string));

                // Add rows
                foreach (var meta in metadata.Entries)
                {
                    var row = dataTable.NewRow();
                    row["item_id"] = itemId;
                    row["template"] = meta.Template;
                    row["scope"] = meta.Scope;
                    row["metadata"] = JsonSerializer.Serialize(meta);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata from Box");
                throw;
            }
        }

        private async Task<DataTable> GetWebhooksAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("webhooks");

            try
            {
                var webhooks = await _boxClient.WebhooksManager.GetAllWebhooksAsync();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("target", typeof(string));
                dataTable.Columns.Add("address", typeof(string));
                dataTable.Columns.Add("triggers", typeof(string));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("created_by", typeof(string));

                // Add rows
                foreach (var webhook in webhooks.Entries)
                {
                    var row = dataTable.NewRow();
                    row["id"] = webhook.Id;
                    row["target"] = JsonSerializer.Serialize(webhook.Target);
                    row["address"] = webhook.Address;
                    row["triggers"] = JsonSerializer.Serialize(webhook.Triggers);
                    row["created_at"] = webhook.CreatedAt;
                    row["created_by"] = JsonSerializer.Serialize(webhook.CreatedBy);

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhooks from Box");
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
                new { Name = "files", Type = "File", Description = "Individual files in Box", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "folders", Type = "Folder", Description = "Directory structure in Box", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "sharedlinks", Type = "SharedLink", Description = "Public sharing URLs", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "users", Type = "User", Description = "User account information", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "groups", Type = "Group", Description = "Group information and membership", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "metadata", Type = "Metadata", Description = "Custom metadata associated with items", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "webhooks", Type = "Webhook", Description = "Webhooks for real-time notifications", Create = true, Read = true, Update = true, Delete = true }
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
                throw new InvalidOperationException("Not connected to Box API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await CreateFileAsync(data);
                    case "folders":
                        return await CreateFolderAsync(data);
                    case "sharedlinks":
                        return await CreateSharedLinkAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in Box");
                throw;
            }
        }

        private async Task<DataTable> CreateFileAsync(Dictionary<string, object> data)
        {
            // Implementation for file upload would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("File upload not yet implemented");
        }

        private async Task<DataTable> CreateFolderAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("folders");

            try
            {
                if (!data.ContainsKey("name"))
                {
                    throw new ArgumentException("Name is required for folder creation");
                }

                var name = data["name"].ToString();
                var parentId = data.ContainsKey("parentId") ? data["parentId"].ToString() : "0";

                var folderRequest = new BoxFolderRequest
                {
                    Name = name,
                    Parent = new BoxRequestEntity { Id = parentId }
                };

                var createdFolder = await _boxClient.FoldersManager.CreateAsync(folderRequest);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("created_at", typeof(DateTime));
                dataTable.Columns.Add("modified_at", typeof(DateTime));

                // Add created folder
                var row = dataTable.NewRow();
                row["id"] = createdFolder.Id;
                row["name"] = createdFolder.Name;
                row["created_at"] = createdFolder.CreatedAt;
                row["modified_at"] = createdFolder.ModifiedAt;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in Box");
                throw;
            }
        }

        private async Task<DataTable> CreateSharedLinkAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("sharedlinks");

            try
            {
                if (!data.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for shared link creation");
                }

                var itemId = data["itemId"].ToString();
                var sharedLinkRequest = new BoxSharedLinkRequest
                {
                    Access = data.ContainsKey("access") ? data["access"].ToString() : "company",
                    AllowDownload = data.ContainsKey("allowDownload") ? (bool?)data["allowDownload"] : null,
                    AllowPreview = data.ContainsKey("allowPreview") ? (bool?)data["allowPreview"] : null,
                    Password = data.ContainsKey("password") ? data["password"].ToString() : null,
                    VanityUrl = data.ContainsKey("vanityUrl") ? data["vanityUrl"].ToString() : null
                };

                var updatedItem = await _boxClient.SharedLinksManager.AddSharedLinkAsync(itemId, sharedLinkRequest);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("vanity_url", typeof(string));
                dataTable.Columns.Add("effective_access", typeof(string));

                // Add created shared link
                var row = dataTable.NewRow();
                row["id"] = updatedItem.Id;
                row["name"] = updatedItem.Name;
                row["url"] = updatedItem.SharedLink?.Url;
                row["vanity_url"] = updatedItem.SharedLink?.VanityUrl;
                row["effective_access"] = updatedItem.SharedLink?.EffectiveAccess;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shared link in Box");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Box API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await UpdateFileAsync(data);
                    case "folders":
                        return await UpdateFolderAsync(data);
                    case "sharedlinks":
                        return await UpdateSharedLinkAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Box");
                throw;
            }
        }

        private async Task<DataTable> UpdateFileAsync(Dictionary<string, object> data)
        {
            // Implementation for file update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("File update not yet implemented");
        }

        private async Task<DataTable> UpdateFolderAsync(Dictionary<string, object> data)
        {
            // Implementation for folder update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Folder update not yet implemented");
        }

        private async Task<DataTable> UpdateSharedLinkAsync(Dictionary<string, object> data)
        {
            // Implementation for shared link update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Shared link update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Box API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await DeleteFileAsync(parameters);
                    case "folders":
                        return await DeleteFolderAsync(parameters);
                    case "sharedlinks":
                        return await DeleteSharedLinkAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Box");
                throw;
            }
        }

        private async Task<bool> DeleteFileAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("fileId"))
                {
                    throw new ArgumentException("fileId is required for file deletion");
                }

                var fileId = parameters["fileId"].ToString();
                await _boxClient.FilesManager.DeleteAsync(fileId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Box");
                throw;
            }
        }

        private async Task<bool> DeleteFolderAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("folderId"))
                {
                    throw new ArgumentException("folderId is required for folder deletion");
                }

                var folderId = parameters["folderId"].ToString();
                await _boxClient.FoldersManager.DeleteAsync(folderId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder from Box");
                throw;
            }
        }

        private async Task<bool> DeleteSharedLinkAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for shared link deletion");
                }

                var itemId = parameters["itemId"].ToString();
                await _boxClient.SharedLinksManager.RemoveSharedLinkAsync(itemId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shared link from Box");
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
                    case "files":
                        var fileFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the file" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Name of the file" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the file in bytes" },
                            new { Name = "created_at", Type = "datetime", Nullable = true, Description = "Date and time of creation" },
                            new { Name = "modified_at", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "created_by", Type = "string", Nullable = true, Description = "JSON string of user who created the file" },
                            new { Name = "modified_by", Type = "string", Nullable = true, Description = "JSON string of user who last modified the file" },
                            new { Name = "owned_by", Type = "string", Nullable = true, Description = "JSON string of user who owns the file" },
                            new { Name = "shared_link", Type = "string", Nullable = true, Description = "JSON string of shared link information" },
                            new { Name = "parent", Type = "string", Nullable = true, Description = "JSON string of parent folder information" },
                            new { Name = "item_status", Type = "string", Nullable = true, Description = "Status of the item" }
                        };

                        foreach (var field in fileFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "folders":
                        var folderFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the folder" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Name of the folder" },
                            new { Name = "created_at", Type = "datetime", Nullable = true, Description = "Date and time of creation" },
                            new { Name = "modified_at", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "created_by", Type = "string", Nullable = true, Description = "JSON string of user who created the folder" },
                            new { Name = "modified_by", Type = "string", Nullable = true, Description = "JSON string of user who last modified the folder" },
                            new { Name = "owned_by", Type = "string", Nullable = true, Description = "JSON string of user who owns the folder" },
                            new { Name = "shared_link", Type = "string", Nullable = true, Description = "JSON string of shared link information" },
                            new { Name = "parent", Type = "string", Nullable = true, Description = "JSON string of parent folder information" },
                            new { Name = "item_status", Type = "string", Nullable = true, Description = "Status of the item" },
                            new { Name = "folder_upload_email", Type = "string", Nullable = true, Description = "Folder upload email access level" }
                        };

                        foreach (var field in folderFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "sharedlinks":
                        var sharedLinkFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the item" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Name of the item" },
                            new { Name = "type", Type = "string", Nullable = true, Description = "Type of the item (file/folder)" },
                            new { Name = "url", Type = "string", Nullable = true, Description = "Shared link URL" },
                            new { Name = "vanity_url", Type = "string", Nullable = true, Description = "Custom vanity URL" },
                            new { Name = "effective_access", Type = "string", Nullable = true, Description = "Effective access level" },
                            new { Name = "effective_permission", Type = "string", Nullable = true, Description = "Effective permission level" },
                            new { Name = "unshared_at", Type = "datetime", Nullable = true, Description = "Date and time when link will be unshared" },
                            new { Name = "is_password_enabled", Type = "bool", Nullable = true, Description = "Whether password is enabled" },
                            new { Name = "password", Type = "string", Nullable = true, Description = "Password for the shared link" }
                        };

                        foreach (var field in sharedLinkFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "users":
                        var userFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the user" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Display name of the user" },
                            new { Name = "login", Type = "string", Nullable = false, Description = "Email address of the user" },
                            new { Name = "created_at", Type = "datetime", Nullable = true, Description = "Date and time of account creation" },
                            new { Name = "modified_at", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "language", Type = "string", Nullable = true, Description = "User's language setting" },
                            new { Name = "timezone", Type = "string", Nullable = true, Description = "User's timezone setting" },
                            new { Name = "space_amount", Type = "long", Nullable = true, Description = "Total space allocated to user" },
                            new { Name = "space_used", Type = "long", Nullable = true, Description = "Space used by user" },
                            new { Name = "max_upload_size", Type = "long", Nullable = true, Description = "Maximum upload size for user" },
                            new { Name = "status", Type = "string", Nullable = true, Description = "Account status" },
                            new { Name = "job_title", Type = "string", Nullable = true, Description = "User's job title" },
                            new { Name = "phone", Type = "string", Nullable = true, Description = "User's phone number" },
                            new { Name = "address", Type = "string", Nullable = true, Description = "User's address" },
                            new { Name = "avatar_url", Type = "string", Nullable = true, Description = "User's avatar URL" }
                        };

                        foreach (var field in userFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "groups":
                        var groupFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the group" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Name of the group" },
                            new { Name = "created_at", Type = "datetime", Nullable = true, Description = "Date and time of group creation" },
                            new { Name = "modified_at", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "provenance", Type = "string", Nullable = true, Description = "Group provenance" },
                            new { Name = "external_sync_identifier", Type = "string", Nullable = true, Description = "External sync identifier" },
                            new { Name = "description", Type = "string", Nullable = true, Description = "Group description" },
                            new { Name = "invitability_level", Type = "string", Nullable = true, Description = "Who can invite users to the group" },
                            new { Name = "member_viewability_level", Type = "string", Nullable = true, Description = "Who can view group membership" }
                        };

                        foreach (var field in groupFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "metadata":
                        var metadataFields = new[]
                        {
                            new { Name = "item_id", Type = "string", Nullable = false, Description = "ID of the item the metadata belongs to" },
                            new { Name = "template", Type = "string", Nullable = false, Description = "Metadata template name" },
                            new { Name = "scope", Type = "string", Nullable = false, Description = "Metadata scope" },
                            new { Name = "metadata", Type = "string", Nullable = false, Description = "JSON string of metadata values" }
                        };

                        foreach (var field in metadataFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "webhooks":
                        var webhookFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the webhook" },
                            new { Name = "target", Type = "string", Nullable = false, Description = "JSON string of webhook target" },
                            new { Name = "address", Type = "string", Nullable = false, Description = "Webhook notification URL" },
                            new { Name = "triggers", Type = "string", Nullable = false, Description = "JSON array of webhook triggers" },
                            new { Name = "created_at", Type = "datetime", Nullable = true, Description = "Date and time of webhook creation" },
                            new { Name = "created_by", Type = "string", Nullable = true, Description = "JSON string of user who created the webhook" }
                        };

                        foreach (var field in webhookFields)
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
