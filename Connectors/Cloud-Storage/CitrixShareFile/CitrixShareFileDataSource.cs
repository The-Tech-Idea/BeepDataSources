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

namespace BeepDM.Connectors.CloudStorage.CitrixShareFile
{
    public class CitrixShareFileConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Subdomain { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ApiControlPlane { get; set; }
    }

    public class CitrixShareFileDataSource : IDataSource
    {
        private readonly ILogger<CitrixShareFileDataSource> _logger;
        private readonly HttpClient _httpClient;
        private CitrixShareFileConfig _config;
        private bool _isConnected;
        private string _baseUrl;

        public string DataSourceName => "CitrixShareFile";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Citrix ShareFile Enterprise Cloud Storage Data Source";

        public CitrixShareFileDataSource(ILogger<CitrixShareFileDataSource> logger, HttpClient httpClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new CitrixShareFileConfig();

                // Check authentication method
                if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.Subdomain = parameters.ContainsKey("Subdomain") ? parameters["Subdomain"].ToString() : "";
                    _config.AccessToken = parameters.ContainsKey("AccessToken") ? parameters["AccessToken"].ToString() : "";
                    _config.RefreshToken = parameters.ContainsKey("RefreshToken") ? parameters["RefreshToken"].ToString() : "";
                    _config.ApiControlPlane = parameters.ContainsKey("ApiControlPlane") ? parameters["ApiControlPlane"].ToString() : "sharefile.com";
                }
                else
                {
                    throw new ArgumentException("ClientId and ClientSecret are required");
                }

                // Set base URL
                _baseUrl = $"https://{_config.Subdomain}.{_config.ApiControlPlane}/sf/v3";

                // Test connection by getting user info
                await InitializeHttpClientAsync();
                var userInfo = await GetUserInfoAsync();

                if (userInfo != null)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Citrix ShareFile API");
                    return true;
                }

                _logger.LogError("Failed to connect to Citrix ShareFile API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Citrix ShareFile API");
                return false;
            }
        }

        private async Task InitializeHttpClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            if (!string.IsNullOrEmpty(_config.AccessToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
            }
            else
            {
                // Need to implement OAuth flow here
                throw new NotImplementedException("OAuth 2.0 flow not yet implemented");
            }
        }

        private async Task<JsonElement> GetUserInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Users");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                return jsonDoc.RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info from Citrix ShareFile");
                throw;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _isConnected = false;
                _logger.LogInformation("Disconnected from Citrix ShareFile API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Citrix ShareFile API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Citrix ShareFile API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "items":
                        return await GetItemsAsync(parameters);
                    case "shares":
                        return await GetSharesAsync(parameters);
                    case "users":
                        return await GetUsersAsync(parameters);
                    case "groups":
                        return await GetGroupsAsync(parameters);
                    case "folders":
                        return await GetFoldersAsync(parameters);
                    case "files":
                        return await GetFilesAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetItemsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("items");

            try
            {
                var itemId = parameters.ContainsKey("itemId") ? parameters["itemId"].ToString() : "";
                var url = string.IsNullOrEmpty(itemId) ? $"{_baseUrl}/Items" : $"{_baseUrl}/Items({itemId})";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("file_name", typeof(string));
                dataTable.Columns.Add("display_name", typeof(string));
                dataTable.Columns.Add("description", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("modification_date", typeof(DateTime));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("parent_id", typeof(string));
                dataTable.Columns.Add("creator_id", typeof(string));

                // Handle both single item and collection
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in root.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = item.GetProperty("Id").GetString();
                        row["name"] = item.TryGetProperty("Name", out var name) ? name.GetString() : "";
                        row["file_name"] = item.TryGetProperty("FileName", out var fileName) ? fileName.GetString() : "";
                        row["display_name"] = item.TryGetProperty("DisplayName", out var displayName) ? displayName.GetString() : "";
                        row["description"] = item.TryGetProperty("Description", out var description) ? description.GetString() : "";
                        row["path"] = item.TryGetProperty("Path", out var path) ? path.GetString() : "";
                        row["size"] = item.TryGetProperty("Size", out var size) ? size.GetInt64() : 0L;
                        row["creation_date"] = item.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                        row["modification_date"] = item.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                        row["type"] = item.TryGetProperty("Type", out var type) ? type.GetString() : "";
                        row["parent_id"] = item.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                        row["creator_id"] = item.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";

                        dataTable.Rows.Add(row);
                    }
                }
                else
                {
                    var row = dataTable.NewRow();
                    row["id"] = root.GetProperty("Id").GetString();
                    row["name"] = root.TryGetProperty("Name", out var name) ? name.GetString() : "";
                    row["file_name"] = root.TryGetProperty("FileName", out var fileName) ? fileName.GetString() : "";
                    row["display_name"] = root.TryGetProperty("DisplayName", out var displayName) ? displayName.GetString() : "";
                    row["description"] = root.TryGetProperty("Description", out var description) ? description.GetString() : "";
                    row["path"] = root.TryGetProperty("Path", out var path) ? path.GetString() : "";
                    row["size"] = root.TryGetProperty("Size", out var size) ? size.GetInt64() : 0L;
                    row["creation_date"] = root.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                    row["modification_date"] = root.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                    row["type"] = root.TryGetProperty("Type", out var type) ? type.GetString() : "";
                    row["parent_id"] = root.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                    row["creator_id"] = root.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetSharesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("shares");

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Shares");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("expiration_date", typeof(DateTime));
                dataTable.Columns.Add("max_downloads", typeof(int));
                dataTable.Columns.Add("download_count", typeof(int));
                dataTable.Columns.Add("require_login", typeof(bool));
                dataTable.Columns.Add("creator_id", typeof(string));

                // Add shares
                if (root.TryGetProperty("value", out var sharesArray))
                {
                    foreach (var share in sharesArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = share.GetProperty("Id").GetString();
                        row["name"] = share.TryGetProperty("Name", out var name) ? name.GetString() : "";
                        row["url"] = share.TryGetProperty("Url", out var url) ? url.GetString() : "";
                        row["type"] = share.TryGetProperty("Type", out var type) ? type.GetString() : "";
                        row["creation_date"] = share.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                        row["expiration_date"] = share.TryGetProperty("ExpirationDate", out var expirationDate) ? DateTime.Parse(expirationDate.GetString()) : DateTime.MaxValue;
                        row["max_downloads"] = share.TryGetProperty("MaxDownloads", out var maxDownloads) ? maxDownloads.GetInt32() : -1;
                        row["download_count"] = share.TryGetProperty("DownloadCount", out var downloadCount) ? downloadCount.GetInt32() : 0;
                        row["require_login"] = share.TryGetProperty("RequireLogin", out var requireLogin) ? requireLogin.GetBoolean() : false;
                        row["creator_id"] = share.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetUsersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("users");

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Users");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("username", typeof(string));
                dataTable.Columns.Add("email", typeof(string));
                dataTable.Columns.Add("first_name", typeof(string));
                dataTable.Columns.Add("last_name", typeof(string));
                dataTable.Columns.Add("full_name", typeof(string));
                dataTable.Columns.Add("company", typeof(string));
                dataTable.Columns.Add("is_active", typeof(bool));
                dataTable.Columns.Add("is_employee", typeof(bool));
                dataTable.Columns.Add("creation_date", typeof(DateTime));

                // Add users
                if (root.TryGetProperty("value", out var usersArray))
                {
                    foreach (var user in usersArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = user.GetProperty("Id").GetString();
                        row["username"] = user.TryGetProperty("Username", out var username) ? username.GetString() : "";
                        row["email"] = user.TryGetProperty("Email", out var email) ? email.GetString() : "";
                        row["first_name"] = user.TryGetProperty("FirstName", out var firstName) ? firstName.GetString() : "";
                        row["last_name"] = user.TryGetProperty("LastName", out var lastName) ? lastName.GetString() : "";
                        row["full_name"] = user.TryGetProperty("FullName", out var fullName) ? fullName.GetString() : "";
                        row["company"] = user.TryGetProperty("Company", out var company) ? company.GetString() : "";
                        row["is_active"] = user.TryGetProperty("IsActive", out var isActive) ? isActive.GetBoolean() : true;
                        row["is_employee"] = user.TryGetProperty("IsEmployee", out var isEmployee) ? isEmployee.GetBoolean() : true;
                        row["creation_date"] = user.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetGroupsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("groups");

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Groups");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("description", typeof(string));
                dataTable.Columns.Add("is_shared", typeof(bool));
                dataTable.Columns.Add("owner_id", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("member_count", typeof(int));

                // Add groups
                if (root.TryGetProperty("value", out var groupsArray))
                {
                    foreach (var group in groupsArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = group.GetProperty("Id").GetString();
                        row["name"] = group.TryGetProperty("Name", out var name) ? name.GetString() : "";
                        row["description"] = group.TryGetProperty("Description", out var description) ? description.GetString() : "";
                        row["is_shared"] = group.TryGetProperty("IsShared", out var isShared) ? isShared.GetBoolean() : false;
                        row["owner_id"] = group.TryGetProperty("Owner", out var owner) && owner.TryGetProperty("Id", out var ownerId) ? ownerId.GetString() : "";
                        row["creation_date"] = group.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                        row["member_count"] = group.TryGetProperty("Count", out var count) ? count.GetInt32() : 0;

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                var folderId = parameters.ContainsKey("folderId") ? parameters["folderId"].ToString() : "";
                var url = string.IsNullOrEmpty(folderId) ? $"{_baseUrl}/Items" : $"{_baseUrl}/Items({folderId})";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("parent_id", typeof(string));
                dataTable.Columns.Add("creator_id", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("modification_date", typeof(DateTime));
                dataTable.Columns.Add("file_count", typeof(int));
                dataTable.Columns.Add("subfolder_count", typeof(int));

                // Handle both single folder and collection
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var folder in root.EnumerateArray())
                    {
                        if (folder.TryGetProperty("Type", out var type) && type.GetString() == "Folder")
                        {
                            var row = dataTable.NewRow();
                            row["id"] = folder.GetProperty("Id").GetString();
                            row["name"] = folder.TryGetProperty("Name", out var name) ? name.GetString() : "";
                            row["path"] = folder.TryGetProperty("Path", out var path) ? path.GetString() : "";
                            row["parent_id"] = folder.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                            row["creator_id"] = folder.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";
                            row["creation_date"] = folder.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                            row["modification_date"] = folder.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                            row["file_count"] = folder.TryGetProperty("FileCount", out var fileCount) ? fileCount.GetInt32() : 0;
                            row["subfolder_count"] = folder.TryGetProperty("SubfolderCount", out var subfolderCount) ? subfolderCount.GetInt32() : 0;

                            dataTable.Rows.Add(row);
                        }
                    }
                }
                else if (root.TryGetProperty("Type", out var singleType) && singleType.GetString() == "Folder")
                {
                    var row = dataTable.NewRow();
                    row["id"] = root.GetProperty("Id").GetString();
                    row["name"] = root.TryGetProperty("Name", out var name) ? name.GetString() : "";
                    row["path"] = root.TryGetProperty("Path", out var path) ? path.GetString() : "";
                    row["parent_id"] = root.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                    row["creator_id"] = root.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";
                    row["creation_date"] = root.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                    row["modification_date"] = root.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                    row["file_count"] = root.TryGetProperty("FileCount", out var fileCount) ? fileCount.GetInt32() : 0;
                    row["subfolder_count"] = root.TryGetProperty("SubfolderCount", out var subfolderCount) ? subfolderCount.GetInt32() : 0;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                var folderId = parameters.ContainsKey("folderId") ? parameters["folderId"].ToString() : "";
                var url = string.IsNullOrEmpty(folderId) ? $"{_baseUrl}/Items" : $"{_baseUrl}/Items({folderId})/Children";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("file_name", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("parent_id", typeof(string));
                dataTable.Columns.Add("creator_id", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("modification_date", typeof(DateTime));
                dataTable.Columns.Add("hash", typeof(string));
                dataTable.Columns.Add("mime_type", typeof(string));

                // Handle both single file and collection
                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var file in root.EnumerateArray())
                    {
                        if (file.TryGetProperty("Type", out var type) && type.GetString() == "File")
                        {
                            var row = dataTable.NewRow();
                            row["id"] = file.GetProperty("Id").GetString();
                            row["name"] = file.TryGetProperty("Name", out var name) ? name.GetString() : "";
                            row["file_name"] = file.TryGetProperty("FileName", out var fileName) ? fileName.GetString() : "";
                            row["size"] = file.TryGetProperty("Size", out var size) ? size.GetInt64() : 0L;
                            row["path"] = file.TryGetProperty("Path", out var path) ? path.GetString() : "";
                            row["parent_id"] = file.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                            row["creator_id"] = file.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";
                            row["creation_date"] = file.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                            row["modification_date"] = file.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                            row["hash"] = file.TryGetProperty("Hash", out var hash) ? hash.GetString() : "";
                            row["mime_type"] = file.TryGetProperty("MimeType", out var mimeType) ? mimeType.GetString() : "";

                            dataTable.Rows.Add(row);
                        }
                    }
                }
                else if (root.TryGetProperty("Type", out var singleType) && singleType.GetString() == "File")
                {
                    var row = dataTable.NewRow();
                    row["id"] = root.GetProperty("Id").GetString();
                    row["name"] = root.TryGetProperty("Name", out var name) ? name.GetString() : "";
                    row["file_name"] = root.TryGetProperty("FileName", out var fileName) ? fileName.GetString() : "";
                    row["size"] = root.TryGetProperty("Size", out var size) ? size.GetInt64() : 0L;
                    row["path"] = root.TryGetProperty("Path", out var path) ? path.GetString() : "";
                    row["parent_id"] = root.TryGetProperty("Parent", out var parent) && parent.TryGetProperty("Id", out var parentId) ? parentId.GetString() : "";
                    row["creator_id"] = root.TryGetProperty("Creator", out var creator) && creator.TryGetProperty("Id", out var creatorId) ? creatorId.GetString() : "";
                    row["creation_date"] = root.TryGetProperty("CreationDate", out var creationDate) ? DateTime.Parse(creationDate.GetString()) : DateTime.MinValue;
                    row["modification_date"] = root.TryGetProperty("ModificationDate", out var modificationDate) ? DateTime.Parse(modificationDate.GetString()) : DateTime.MinValue;
                    row["hash"] = root.TryGetProperty("Hash", out var hash) ? hash.GetString() : "";
                    row["mime_type"] = root.TryGetProperty("MimeType", out var mimeType) ? mimeType.GetString() : "";

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from Citrix ShareFile");
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
                new { Name = "items", Type = "Item", Description = "Files and folders in ShareFile", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "shares", Type = "Share", Description = "Public sharing links and URLs", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "users", Type = "User", Description = "User accounts and profiles", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "groups", Type = "Group", Description = "User groups and memberships", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "folders", Type = "Folder", Description = "Directory structure and folders", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "files", Type = "File", Description = "Individual files with metadata", Create = true, Read = true, Update = true, Delete = true }
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
                throw new InvalidOperationException("Not connected to Citrix ShareFile API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "folders":
                        return await CreateFolderAsync(data);
                    case "shares":
                        return await CreateShareAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> CreateFolderAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("folders");

            try
            {
                if (!data.ContainsKey("parentId") || !data.ContainsKey("name"))
                {
                    throw new ArgumentException("parentId and name are required for folder creation");
                }

                var parentId = data["parentId"].ToString();
                var name = data["name"].ToString();
                var description = data.ContainsKey("description") ? data["description"].ToString() : "";

                var createFolderRequest = new
                {
                    Name = name,
                    Description = description
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/Items({parentId})/Folder", createFolderRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));

                // Add created folder
                var row = dataTable.NewRow();
                row["id"] = root.GetProperty("Id").GetString();
                row["name"] = root.TryGetProperty("Name", out var folderName) ? folderName.GetString() : name;
                row["path"] = root.TryGetProperty("Path", out var path) ? path.GetString() : "";
                row["creation_date"] = DateTime.UtcNow;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> CreateShareAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("shares");

            try
            {
                if (!data.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for share creation");
                }

                var itemId = data["itemId"].ToString();
                var shareType = data.ContainsKey("shareType") ? data["shareType"].ToString() : "Send";
                var requireLogin = data.ContainsKey("requireLogin") ? Convert.ToBoolean(data["requireLogin"]) : false;
                var maxDownloads = data.ContainsKey("maxDownloads") ? Convert.ToInt32(data["maxDownloads"]) : -1;
                var expirationDays = data.ContainsKey("expirationDays") ? Convert.ToInt32(data["expirationDays"]) : 30;

                var createShareRequest = new
                {
                    ShareType = shareType,
                    RequireLogin = requireLogin,
                    MaxDownloads = maxDownloads,
                    ExpirationDate = DateTime.UtcNow.AddDays(expirationDays).ToString("O")
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/Items({itemId})/Share", createShareRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));

                // Add created share
                var row = dataTable.NewRow();
                row["id"] = root.GetProperty("Id").GetString();
                row["url"] = root.TryGetProperty("Url", out var url) ? url.GetString() : "";
                row["type"] = root.TryGetProperty("Type", out var type) ? type.GetString() : shareType;
                row["creation_date"] = DateTime.UtcNow;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share in Citrix ShareFile");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Citrix ShareFile API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "items":
                        return await UpdateItemAsync(data);
                    case "folders":
                        return await UpdateFolderAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Citrix ShareFile");
                throw;
            }
        }

        private async Task<DataTable> UpdateItemAsync(Dictionary<string, object> data)
        {
            // Implementation for item update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Item update not yet implemented");
        }

        private async Task<DataTable> UpdateFolderAsync(Dictionary<string, object> data)
        {
            // Implementation for folder update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Folder update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Citrix ShareFile API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "items":
                        return await DeleteItemAsync(parameters);
                    case "shares":
                        return await DeleteShareAsync(parameters);
                    case "folders":
                        return await DeleteFolderAsync(parameters);
                    case "files":
                        return await DeleteFileAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Citrix ShareFile");
                throw;
            }
        }

        private async Task<bool> DeleteItemAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("itemId"))
                {
                    throw new ArgumentException("itemId is required for item deletion");
                }

                var itemId = parameters["itemId"].ToString();
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/Items({itemId})");
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item from Citrix ShareFile");
                throw;
            }
        }

        private async Task<bool> DeleteShareAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("shareId"))
                {
                    throw new ArgumentException("shareId is required for share deletion");
                }

                var shareId = parameters["shareId"].ToString();
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/Shares({shareId})");
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share from Citrix ShareFile");
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
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/Items({folderId})");
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder from Citrix ShareFile");
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
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/Items({fileId})");
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Citrix ShareFile");
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
                    case "items":
                        var itemFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Unique item identifier" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Item name" },
                            new { Name = "file_name", Type = "string", Nullable = true, Description = "File name" },
                            new { Name = "display_name", Type = "string", Nullable = true, Description = "Display name" },
                            new { Name = "description", Type = "string", Nullable = true, Description = "Item description" },
                            new { Name = "path", Type = "string", Nullable = true, Description = "Full path to the item" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Item size in bytes" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Creation date" },
                            new { Name = "modification_date", Type = "datetime", Nullable = true, Description = "Last modification date" },
                            new { Name = "type", Type = "string", Nullable = true, Description = "Item type (File/Folder)" },
                            new { Name = "parent_id", Type = "string", Nullable = true, Description = "Parent item identifier" },
                            new { Name = "creator_id", Type = "string", Nullable = true, Description = "Creator identifier" }
                        };

                        foreach (var field in itemFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "shares":
                        var shareFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Share identifier" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Share name" },
                            new { Name = "url", Type = "string", Nullable = false, Description = "Public URL" },
                            new { Name = "type", Type = "string", Nullable = false, Description = "Share type" },
                            new { Name = "creation_date", Type = "datetime", Nullable = false, Description = "Creation date" },
                            new { Name = "expiration_date", Type = "datetime", Nullable = true, Description = "Expiration date" },
                            new { Name = "max_downloads", Type = "int", Nullable = true, Description = "Maximum downloads allowed" },
                            new { Name = "download_count", Type = "int", Nullable = true, Description = "Current download count" },
                            new { Name = "require_login", Type = "bool", Nullable = true, Description = "Whether login is required" },
                            new { Name = "creator_id", Type = "string", Nullable = true, Description = "Creator identifier" }
                        };

                        foreach (var field in shareFields)
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
                            new { Name = "id", Type = "string", Nullable = false, Description = "User identifier" },
                            new { Name = "username", Type = "string", Nullable = false, Description = "Username" },
                            new { Name = "email", Type = "string", Nullable = false, Description = "Email address" },
                            new { Name = "first_name", Type = "string", Nullable = false, Description = "First name" },
                            new { Name = "last_name", Type = "string", Nullable = false, Description = "Last name" },
                            new { Name = "full_name", Type = "string", Nullable = false, Description = "Full name" },
                            new { Name = "company", Type = "string", Nullable = true, Description = "Company name" },
                            new { Name = "is_active", Type = "bool", Nullable = false, Description = "Whether user is active" },
                            new { Name = "is_employee", Type = "bool", Nullable = false, Description = "Whether user is employee" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Creation date" }
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
                            new { Name = "id", Type = "string", Nullable = false, Description = "Group identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Group name" },
                            new { Name = "description", Type = "string", Nullable = true, Description = "Group description" },
                            new { Name = "is_shared", Type = "bool", Nullable = false, Description = "Whether group is shared" },
                            new { Name = "owner_id", Type = "string", Nullable = true, Description = "Owner identifier" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Creation date" },
                            new { Name = "member_count", Type = "int", Nullable = true, Description = "Number of members" }
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

                    case "folders":
                        var folderFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Folder identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Folder name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to folder" },
                            new { Name = "parent_id", Type = "string", Nullable = true, Description = "Parent folder identifier" },
                            new { Name = "creator_id", Type = "string", Nullable = true, Description = "Creator identifier" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Creation date" },
                            new { Name = "modification_date", Type = "datetime", Nullable = true, Description = "Last modification date" },
                            new { Name = "file_count", Type = "int", Nullable = true, Description = "Number of files" },
                            new { Name = "subfolder_count", Type = "int", Nullable = true, Description = "Number of subfolders" }
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

                    case "files":
                        var fileFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "File identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "File name" },
                            new { Name = "file_name", Type = "string", Nullable = false, Description = "Actual file name" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "File size in bytes" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to file" },
                            new { Name = "parent_id", Type = "string", Nullable = true, Description = "Parent folder identifier" },
                            new { Name = "creator_id", Type = "string", Nullable = true, Description = "Creator identifier" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Creation date" },
                            new { Name = "modification_date", Type = "datetime", Nullable = true, Description = "Last modification date" },
                            new { Name = "hash", Type = "string", Nullable = true, Description = "File hash" },
                            new { Name = "mime_type", Type = "string", Nullable = true, Description = "MIME type" }
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
