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
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using Dropbox.Api.Users;
using Dropbox.Api.Team;
using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.CloudStorage.Dropbox
{
    public class DropboxConfig
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public bool UseAccessToken { get; set; }
    }

    public class DropboxDataSource : IDataSource
    {
        private readonly ILogger<DropboxDataSource> _logger;
        private DropboxClient _dropboxClient;
        private DropboxConfig _config;
        private bool _isConnected;

        public string DataSourceName => "Dropbox";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Dropbox Cloud Storage Data Source";

        public DropboxDataSource(ILogger<DropboxDataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new DropboxConfig();

                // Check authentication method
                if (parameters.ContainsKey("AccessToken"))
                {
                    _config.UseAccessToken = true;
                    _config.AccessToken = parameters["AccessToken"].ToString();
                }
                else if (parameters.ContainsKey("AppKey") && parameters.ContainsKey("AppSecret") &&
                         parameters.ContainsKey("RefreshToken"))
                {
                    _config.UseAccessToken = false;
                    _config.AppKey = parameters["AppKey"].ToString();
                    _config.AppSecret = parameters["AppSecret"].ToString();
                    _config.RefreshToken = parameters["RefreshToken"].ToString();
                }
                else
                {
                    throw new ArgumentException("Either AccessToken or AppKey/AppSecret/RefreshToken is required");
                }

                // Initialize the Dropbox client
                await InitializeDropboxClientAsync();

                // Test connection by getting account information
                var account = await _dropboxClient.Users.GetCurrentAccountAsync();

                if (account != null)
                {
                    _isConnected = true;
                    _logger.LogInformation($"Successfully connected to Dropbox API for user: {account.Email}");
                    return true;
                }

                _logger.LogError("Failed to connect to Dropbox API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Dropbox API");
                return false;
            }
        }

        private async Task InitializeDropboxClientAsync()
        {
            if (_config.UseAccessToken)
            {
                // Use Access Token authentication
                _dropboxClient = new DropboxClient(_config.AccessToken);
            }
            else
            {
                // Use OAuth 2.0 with refresh token
                var httpClient = new HttpClient();
                var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.dropbox.com/oauth2/token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", _config.RefreshToken),
                        new KeyValuePair<string, string>("client_id", _config.AppKey),
                        new KeyValuePair<string, string>("client_secret", _config.AppSecret)
                    })
                };

                var response = await httpClient.SendAsync(refreshRequest);
                response.EnsureSuccessStatusCode();

                var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var newAccessToken = tokenResponse.GetProperty("access_token").GetString();

                _dropboxClient = new DropboxClient(newAccessToken);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_dropboxClient != null)
                {
                    _dropboxClient.Dispose();
                    _dropboxClient = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from Dropbox API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Dropbox API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Dropbox API");
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
                    case "teams":
                        return await GetTeamsAsync(parameters);
                    case "filerequests":
                        return await GetFileRequestsAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                string path = "";
                if (parameters != null && parameters.ContainsKey("path"))
                {
                    path = parameters["path"].ToString();
                }

                var listFolderResult = await _dropboxClient.Files.ListFolderAsync(path);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path_lower", typeof(string));
                dataTable.Columns.Add("path_display", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("client_modified", typeof(DateTime));
                dataTable.Columns.Add("server_modified", typeof(DateTime));
                dataTable.Columns.Add("is_downloadable", typeof(bool));
                dataTable.Columns.Add("content_hash", typeof(string));
                dataTable.Columns.Add("shared_link", typeof(string));

                // Add rows
                foreach (var entry in listFolderResult.Entries.Where(e => e.IsFile))
                {
                    var file = entry.AsFile;
                    var row = dataTable.NewRow();
                    row["id"] = file.Id;
                    row["name"] = file.Name;
                    row["path_lower"] = file.PathLower;
                    row["path_display"] = file.PathDisplay;
                    row["size"] = file.Size;
                    row["client_modified"] = file.ClientModified;
                    row["server_modified"] = file.ServerModified;
                    row["is_downloadable"] = file.IsDownloadable;
                    row["content_hash"] = file.ContentHash;
                    row["shared_link"] = ""; // Will be populated if shared

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                string path = "";
                if (parameters != null && parameters.ContainsKey("path"))
                {
                    path = parameters["path"].ToString();
                }

                var listFolderResult = await _dropboxClient.Files.ListFolderAsync(path);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path_lower", typeof(string));
                dataTable.Columns.Add("path_display", typeof(string));
                dataTable.Columns.Add("shared_folder_id", typeof(string));

                // Add rows
                foreach (var entry in listFolderResult.Entries.Where(e => e.IsFolder))
                {
                    var folder = entry.AsFolder;
                    var row = dataTable.NewRow();
                    row["id"] = folder.Id;
                    row["name"] = folder.Name;
                    row["path_lower"] = folder.PathLower;
                    row["path_display"] = folder.PathDisplay;
                    row["shared_folder_id"] = folder.SharedFolderId;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetSharedLinksAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("sharedlinks");

            try
            {
                var listSharedLinksResult = await _dropboxClient.Sharing.ListSharedLinksAsync();

                // Create columns
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path_lower", typeof(string));
                dataTable.Columns.Add("link_permissions", typeof(string));
                dataTable.Columns.Add("client_modified", typeof(DateTime));
                dataTable.Columns.Add("server_modified", typeof(DateTime));
                dataTable.Columns.Add("expires", typeof(DateTime));

                // Add rows
                foreach (var link in listSharedLinksResult.Links)
                {
                    var row = dataTable.NewRow();
                    row["url"] = link.Url;
                    row["name"] = link.Name;
                    row["path_lower"] = link.PathLower;
                    row["link_permissions"] = JsonSerializer.Serialize(link.LinkPermissions);
                    row["client_modified"] = link.ClientModified;
                    row["server_modified"] = link.ServerModified;
                    row["expires"] = link.Expires;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared links from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetUsersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("users");

            try
            {
                var account = await _dropboxClient.Users.GetCurrentAccountAsync();

                // Create columns
                dataTable.Columns.Add("account_id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("email", typeof(string));
                dataTable.Columns.Add("email_verified", typeof(bool));
                dataTable.Columns.Add("profile_photo_url", typeof(string));
                dataTable.Columns.Add("team_member_id", typeof(string));

                // Add current user
                var row = dataTable.NewRow();
                row["account_id"] = account.AccountId;
                row["name"] = account.Name.DisplayName;
                row["email"] = account.Email;
                row["email_verified"] = account.EmailVerified;
                row["profile_photo_url"] = account.ProfilePhotoUrl;
                row["team_member_id"] = account.TeamMemberId;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetTeamsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("teams");

            try
            {
                var teamInfo = await _dropboxClient.Team.GetInfoAsync();

                // Create columns
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("team_id", typeof(string));
                dataTable.Columns.Add("num_licensed_users", typeof(int));
                dataTable.Columns.Add("num_provisioned_users", typeof(int));
                dataTable.Columns.Add("policies", typeof(string));

                // Add team info
                var row = dataTable.NewRow();
                row["name"] = teamInfo.Name;
                row["team_id"] = teamInfo.Id;
                row["num_licensed_users"] = teamInfo.NumLicensedUsers;
                row["num_provisioned_users"] = teamInfo.NumProvisionedUsers;
                row["policies"] = JsonSerializer.Serialize(teamInfo.Policies);

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teams from Dropbox");
                throw;
            }
        }

        private async Task<DataTable> GetFileRequestsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("filerequests");

            try
            {
                var fileRequests = await _dropboxClient.FileRequests.ListV2Async();

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("title", typeof(string));
                dataTable.Columns.Add("destination", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));
                dataTable.Columns.Add("deadline", typeof(DateTime));
                dataTable.Columns.Add("description", typeof(string));
                dataTable.Columns.Add("is_open", typeof(bool));

                // Add rows
                foreach (var request in fileRequests.FileRequests)
                {
                    var row = dataTable.NewRow();
                    row["id"] = request.Id;
                    row["title"] = request.Title;
                    row["destination"] = request.Destination;
                    row["created"] = request.Created;
                    row["deadline"] = request.Deadline;
                    row["description"] = request.Description;
                    row["is_open"] = request.IsOpen;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file requests from Dropbox");
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
                new { Name = "files", Type = "File", Description = "Individual files in Dropbox", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "folders", Type = "Folder", Description = "Directory structure in Dropbox", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "sharedlinks", Type = "SharedLink", Description = "Public sharing URLs", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "users", Type = "User", Description = "User account information", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "teams", Type = "Team", Description = "Team information and policies", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "filerequests", Type = "FileRequest", Description = "File request collections", Create = true, Read = true, Update = true, Delete = true }
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
                throw new InvalidOperationException("Not connected to Dropbox API");
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
                    case "filerequests":
                        return await CreateFileRequestAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in Dropbox");
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
                if (!data.ContainsKey("path"))
                {
                    throw new ArgumentException("Path is required for folder creation");
                }

                var path = data["path"].ToString();
                var createFolderResult = await _dropboxClient.Files.CreateFolderV2Async(path);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path_lower", typeof(string));
                dataTable.Columns.Add("path_display", typeof(string));

                // Add created folder
                var row = dataTable.NewRow();
                row["id"] = createFolderResult.Metadata.Id;
                row["name"] = createFolderResult.Metadata.Name;
                row["path_lower"] = createFolderResult.Metadata.PathLower;
                row["path_display"] = createFolderResult.Metadata.PathDisplay;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in Dropbox");
                throw;
            }
        }

        private async Task<DataTable> CreateSharedLinkAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("sharedlinks");

            try
            {
                if (!data.ContainsKey("path"))
                {
                    throw new ArgumentException("Path is required for shared link creation");
                }

                var path = data["path"].ToString();
                var createSharedLinkArg = new CreateSharedLinkArg(path);
                var sharedLink = await _dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(createSharedLinkArg);

                // Create columns
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path_lower", typeof(string));

                // Add created shared link
                var row = dataTable.NewRow();
                row["url"] = sharedLink.Url;
                row["name"] = sharedLink.Name;
                row["path_lower"] = sharedLink.PathLower;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shared link in Dropbox");
                throw;
            }
        }

        private async Task<DataTable> CreateFileRequestAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("filerequests");

            try
            {
                if (!data.ContainsKey("title") || !data.ContainsKey("destination"))
                {
                    throw new ArgumentException("Title and destination are required for file request creation");
                }

                var title = data["title"].ToString();
                var destination = data["destination"].ToString();
                var description = data.ContainsKey("description") ? data["description"].ToString() : "";
                var deadline = data.ContainsKey("deadline") ? (DateTime?)data["deadline"] : null;

                var createFileRequestArgs = new CreateFileRequestArgs(title, destination)
                {
                    Description = description,
                    Deadline = deadline
                };

                var fileRequest = await _dropboxClient.FileRequests.CreateAsync(createFileRequestArgs);

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("title", typeof(string));
                dataTable.Columns.Add("destination", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));
                dataTable.Columns.Add("deadline", typeof(DateTime));
                dataTable.Columns.Add("description", typeof(string));

                // Add created file request
                var row = dataTable.NewRow();
                row["id"] = fileRequest.Id;
                row["title"] = fileRequest.Title;
                row["destination"] = fileRequest.Destination;
                row["created"] = fileRequest.Created;
                row["deadline"] = fileRequest.Deadline;
                row["description"] = fileRequest.Description;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file request in Dropbox");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Dropbox API");
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
                    case "filerequests":
                        return await UpdateFileRequestAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Dropbox");
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

        private async Task<DataTable> UpdateFileRequestAsync(Dictionary<string, object> data)
        {
            // Implementation for file request update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("File request update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Dropbox API");
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
                    case "filerequests":
                        return await DeleteFileRequestAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Dropbox");
                throw;
            }
        }

        private async Task<bool> DeleteFileAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("path"))
                {
                    throw new ArgumentException("Path is required for file deletion");
                }

                var path = parameters["path"].ToString();
                await _dropboxClient.Files.DeleteV2Async(path);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Dropbox");
                throw;
            }
        }

        private async Task<bool> DeleteFolderAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("path"))
                {
                    throw new ArgumentException("Path is required for folder deletion");
                }

                var path = parameters["path"].ToString();
                await _dropboxClient.Files.DeleteV2Async(path);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder from Dropbox");
                throw;
            }
        }

        private async Task<bool> DeleteSharedLinkAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("url"))
                {
                    throw new ArgumentException("URL is required for shared link deletion");
                }

                var url = parameters["url"].ToString();
                await _dropboxClient.Sharing.RevokeSharedLinkAsync(url);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shared link from Dropbox");
                throw;
            }
        }

        private async Task<bool> DeleteFileRequestAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("id"))
                {
                    throw new ArgumentException("ID is required for file request deletion");
                }

                var id = parameters["id"].ToString();
                await _dropboxClient.FileRequests.DeleteAsync(id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file request from Dropbox");
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
                            new { Name = "path_lower", Type = "string", Nullable = false, Description = "Lowercase path of the file" },
                            new { Name = "path_display", Type = "string", Nullable = false, Description = "Display path of the file" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the file in bytes" },
                            new { Name = "client_modified", Type = "datetime", Nullable = true, Description = "Client modification time" },
                            new { Name = "server_modified", Type = "datetime", Nullable = true, Description = "Server modification time" },
                            new { Name = "is_downloadable", Type = "bool", Nullable = true, Description = "Whether the file is downloadable" },
                            new { Name = "content_hash", Type = "string", Nullable = true, Description = "Content hash of the file" },
                            new { Name = "shared_link", Type = "string", Nullable = true, Description = "Shared link URL if available" }
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
                            new { Name = "path_lower", Type = "string", Nullable = false, Description = "Lowercase path of the folder" },
                            new { Name = "path_display", Type = "string", Nullable = false, Description = "Display path of the folder" },
                            new { Name = "shared_folder_id", Type = "string", Nullable = true, Description = "Shared folder ID if applicable" }
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
                            new { Name = "url", Type = "string", Nullable = false, Description = "Shared link URL" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Name associated with the link" },
                            new { Name = "path_lower", Type = "string", Nullable = true, Description = "Lowercase path of the linked item" },
                            new { Name = "link_permissions", Type = "string", Nullable = true, Description = "JSON string of link permissions" },
                            new { Name = "client_modified", Type = "datetime", Nullable = true, Description = "Client modification time" },
                            new { Name = "server_modified", Type = "datetime", Nullable = true, Description = "Server modification time" },
                            new { Name = "expires", Type = "datetime", Nullable = true, Description = "Expiration date of the link" }
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
                            new { Name = "account_id", Type = "string", Nullable = false, Description = "Unique account identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Display name of the user" },
                            new { Name = "email", Type = "string", Nullable = false, Description = "Email address of the user" },
                            new { Name = "email_verified", Type = "bool", Nullable = false, Description = "Whether email is verified" },
                            new { Name = "profile_photo_url", Type = "string", Nullable = true, Description = "Profile photo URL" },
                            new { Name = "team_member_id", Type = "string", Nullable = true, Description = "Team member ID if applicable" }
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

                    case "teams":
                        var teamFields = new[]
                        {
                            new { Name = "name", Type = "string", Nullable = false, Description = "Name of the team" },
                            new { Name = "team_id", Type = "string", Nullable = false, Description = "Unique team identifier" },
                            new { Name = "num_licensed_users", Type = "int", Nullable = false, Description = "Number of licensed users" },
                            new { Name = "num_provisioned_users", Type = "int", Nullable = false, Description = "Number of provisioned users" },
                            new { Name = "policies", Type = "string", Nullable = true, Description = "JSON string of team policies" }
                        };

                        foreach (var field in teamFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "filerequests":
                        var fileRequestFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique identifier for the file request" },
                            new { Name = "title", Type = "string", Nullable = false, Description = "Title of the file request" },
                            new { Name = "destination", Type = "string", Nullable = false, Description = "Destination path for uploaded files" },
                            new { Name = "created", Type = "datetime", Nullable = false, Description = "Creation date of the file request" },
                            new { Name = "deadline", Type = "datetime", Nullable = true, Description = "Deadline for the file request" },
                            new { Name = "description", Type = "string", Nullable = true, Description = "Description of the file request" },
                            new { Name = "is_open", Type = "bool", Nullable = false, Description = "Whether the file request is open" }
                        };

                        foreach (var field in fileRequestFields)
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
