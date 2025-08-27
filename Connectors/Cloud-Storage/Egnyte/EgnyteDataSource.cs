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

namespace BeepDM.Connectors.CloudStorage.Egnyte
{
    public class EgnyteConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Domain { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ApiToken { get; set; }
        public bool UseApiToken { get; set; }
    }

    public class EgnyteDataSource : IDataSource
    {
        private readonly ILogger<EgnyteDataSource> _logger;
        private readonly HttpClient _httpClient;
        private EgnyteConfig _config;
        private bool _isConnected;
        private string _baseUrl;

        public string DataSourceName => "Egnyte";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Egnyte Enterprise Cloud Storage Data Source";

        public EgnyteDataSource(ILogger<EgnyteDataSource> logger, HttpClient httpClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new EgnyteConfig();

                // Check authentication method
                if (parameters.ContainsKey("ApiToken"))
                {
                    _config.ApiToken = parameters["ApiToken"].ToString();
                    _config.UseApiToken = true;
                    _config.Domain = parameters.ContainsKey("Domain") ? parameters["Domain"].ToString() : "";
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.Domain = parameters.ContainsKey("Domain") ? parameters["Domain"].ToString() : "";
                    _config.AccessToken = parameters.ContainsKey("AccessToken") ? parameters["AccessToken"].ToString() : "";
                    _config.RefreshToken = parameters.ContainsKey("RefreshToken") ? parameters["RefreshToken"].ToString() : "";
                }
                else
                {
                    throw new ArgumentException("Either ApiToken or ClientId/ClientSecret are required");
                }

                // Set base URL
                _baseUrl = $"https://{_config.Domain}.egnyte.com";

                // Test connection by getting user info
                await InitializeHttpClientAsync();
                var userInfo = await GetUserInfoAsync();

                if (userInfo != null)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Egnyte API");
                    return true;
                }

                _logger.LogError("Failed to connect to Egnyte API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Egnyte API");
                return false;
            }
        }

        private async Task InitializeHttpClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (_config.UseApiToken)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiToken}");
            }
            else if (!string.IsNullOrEmpty(_config.AccessToken))
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
                var response = await _httpClient.GetAsync($"{_baseUrl}/pubapi/v1/userinfo");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info from Egnyte");
                throw;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _isConnected = false;
                _logger.LogInformation("Disconnected from Egnyte API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Egnyte API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Egnyte API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await GetFilesAsync(parameters);
                    case "folders":
                        return await GetFoldersAsync(parameters);
                    case "links":
                        return await GetLinksAsync(parameters);
                    case "users":
                        return await GetUsersAsync(parameters);
                    case "groups":
                        return await GetGroupsAsync(parameters);
                    case "permissions":
                        return await GetPermissionsAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : "/Shared";
                var listFilesRequest = new
                {
                    path = path,
                    list_content = true
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/fs/listFolder", listFilesRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("last_modified", typeof(DateTime));
                dataTable.Columns.Add("is_folder", typeof(bool));
                dataTable.Columns.Add("uploaded_by", typeof(string));
                dataTable.Columns.Add("checksum", typeof(string));
                dataTable.Columns.Add("entry_id", typeof(string));

                // Add files
                if (root.TryGetProperty("files", out var filesArray))
                {
                    foreach (var file in filesArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["name"] = file.GetProperty("name").GetString();
                        row["path"] = file.GetProperty("path").GetString();
                        row["size"] = file.GetProperty("size").GetInt64();
                        row["last_modified"] = DateTime.Parse(file.GetProperty("last_modified").GetString());
                        row["is_folder"] = file.GetProperty("is_folder").GetBoolean();
                        row["uploaded_by"] = file.TryGetProperty("uploaded_by", out var uploadedBy) ? uploadedBy.GetString() : "";
                        row["checksum"] = file.TryGetProperty("checksum", out var checksum) ? checksum.GetString() : "";
                        row["entry_id"] = file.TryGetProperty("entry_id", out var entryId) ? entryId.GetString() : "";

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : "/Shared";
                var listFoldersRequest = new
                {
                    path = path,
                    list_content = false
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/fs/listFolder", listFoldersRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("folder_id", typeof(string));
                dataTable.Columns.Add("parent_id", typeof(string));
                dataTable.Columns.Add("created_by", typeof(string));
                dataTable.Columns.Add("created_date", typeof(DateTime));
                dataTable.Columns.Add("last_modified", typeof(DateTime));

                // Add folder info
                if (root.TryGetProperty("folder_id", out var folderId))
                {
                    var row = dataTable.NewRow();
                    row["name"] = root.GetProperty("name").GetString();
                    row["path"] = root.GetProperty("path").GetString();
                    row["folder_id"] = folderId.GetString();
                    row["parent_id"] = root.TryGetProperty("parent_id", out var parentId) ? parentId.GetString() : "";
                    row["created_by"] = root.TryGetProperty("created_by", out var createdBy) ? createdBy.GetString() : "";
                    row["created_date"] = root.TryGetProperty("created", out var created) ? DateTime.Parse(created.GetString()) : DateTime.MinValue;
                    row["last_modified"] = root.TryGetProperty("last_modified", out var lastModified) ? DateTime.Parse(lastModified.GetString()) : DateTime.MinValue;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetLinksAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("links");

            try
            {
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : "/Shared";
                var listLinksRequest = new
                {
                    path = path
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/links", listLinksRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("created_by", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("expiry_date", typeof(DateTime));
                dataTable.Columns.Add("recipients", typeof(string));

                // Add links
                if (root.TryGetProperty("links", out var linksArray))
                {
                    foreach (var link in linksArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = link.GetProperty("id").GetString();
                        row["name"] = link.TryGetProperty("name", out var name) ? name.GetString() : "";
                        row["path"] = link.GetProperty("path").GetString();
                        row["url"] = link.GetProperty("url").GetString();
                        row["type"] = link.GetProperty("type").GetString();
                        row["created_by"] = link.GetProperty("created_by").GetString();
                        row["creation_date"] = DateTime.Parse(link.GetProperty("creation_date").GetString());
                        row["expiry_date"] = link.TryGetProperty("expiry_date", out var expiry) ? DateTime.Parse(expiry.GetString()) : DateTime.MaxValue;
                        row["recipients"] = link.TryGetProperty("recipients", out var recipients) ? string.Join(",", recipients.EnumerateArray().Select(r => r.GetString())) : "";

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting links from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetUsersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("users");

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/pubapi/v2/users");
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
                dataTable.Columns.Add("user_type", typeof(string));
                dataTable.Columns.Add("active", typeof(bool));
                dataTable.Columns.Add("locked", typeof(bool));

                // Add users
                if (root.TryGetProperty("resources", out var usersArray))
                {
                    foreach (var user in usersArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = user.GetProperty("id").GetString();
                        row["username"] = user.GetProperty("userName").GetString();
                        row["email"] = user.GetProperty("email").GetString();
                        row["first_name"] = user.GetProperty("firstName").GetString();
                        row["last_name"] = user.GetProperty("lastName").GetString();
                        row["user_type"] = user.GetProperty("userType").GetString();
                        row["active"] = user.GetProperty("active").GetBoolean();
                        row["locked"] = user.GetProperty("locked").GetBoolean();

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetGroupsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("groups");

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/pubapi/v2/groups");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("display_name", typeof(string));
                dataTable.Columns.Add("group_type", typeof(string));
                dataTable.Columns.Add("member_count", typeof(int));

                // Add groups
                if (root.TryGetProperty("resources", out var groupsArray))
                {
                    foreach (var group in groupsArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = group.GetProperty("id").GetString();
                        row["name"] = group.GetProperty("name").GetString();
                        row["display_name"] = group.GetProperty("displayName").GetString();
                        row["group_type"] = group.GetProperty("groupType").GetString();
                        row["member_count"] = group.TryGetProperty("memberCount", out var count) ? count.GetInt32() : 0;

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups from Egnyte");
                throw;
            }
        }

        private async Task<DataTable> GetPermissionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("permissions");

            try
            {
                if (!parameters.ContainsKey("path"))
                {
                    throw new ArgumentException("path is required for permissions");
                }

                var path = parameters["path"].ToString();
                var response = await _httpClient.GetAsync($"{_baseUrl}/pubapi/v2/perms{path}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("user_id", typeof(string));
                dataTable.Columns.Add("group_id", typeof(string));
                dataTable.Columns.Add("permission", typeof(string));
                dataTable.Columns.Add("inherited", typeof(bool));

                // Add permissions
                if (root.TryGetProperty("users", out var users))
                {
                    foreach (var user in users.EnumerateObject())
                    {
                        var row = dataTable.NewRow();
                        row["path"] = path;
                        row["user_id"] = user.Name;
                        row["group_id"] = "";
                        row["permission"] = user.Value.GetString();
                        row["inherited"] = false;

                        dataTable.Rows.Add(row);
                    }
                }

                if (root.TryGetProperty("groups", out var groups))
                {
                    foreach (var group in groups.EnumerateObject())
                    {
                        var row = dataTable.NewRow();
                        row["path"] = path;
                        row["user_id"] = "";
                        row["group_id"] = group.Name;
                        row["permission"] = group.Value.GetString();
                        row["inherited"] = false;

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions from Egnyte");
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
                new { Name = "files", Type = "File", Description = "Files and documents stored in Egnyte", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "folders", Type = "Folder", Description = "Directory structure and folders", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "links", Type = "Link", Description = "Public sharing links and URLs", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "users", Type = "User", Description = "User accounts and profiles", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "groups", Type = "Group", Description = "User groups and memberships", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "permissions", Type = "Permission", Description = "Access permissions and rights", Create = true, Read = true, Update = true, Delete = true }
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
                throw new InvalidOperationException("Not connected to Egnyte API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "folders":
                        return await CreateFolderAsync(data);
                    case "links":
                        return await CreateLinkAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in Egnyte");
                throw;
            }
        }

        private async Task<DataTable> CreateFolderAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("folders");

            try
            {
                if (!data.ContainsKey("path"))
                {
                    throw new ArgumentException("path is required for folder creation");
                }

                var path = data["path"].ToString();
                var createFolderRequest = new
                {
                    action = "add_folder",
                    path = path
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/fs", createFolderRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("folder_id", typeof(string));
                dataTable.Columns.Add("created_date", typeof(DateTime));

                // Add created folder
                var row = dataTable.NewRow();
                row["path"] = path;
                row["folder_id"] = root.TryGetProperty("folder_id", out var folderId) ? folderId.GetString() : "";
                row["created_date"] = DateTime.UtcNow;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in Egnyte");
                throw;
            }
        }

        private async Task<DataTable> CreateLinkAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("links");

            try
            {
                if (!data.ContainsKey("path"))
                {
                    throw new ArgumentException("path is required for link creation");
                }

                var path = data["path"].ToString();
                var linkType = data.ContainsKey("type") ? data["type"].ToString() : "file";
                var createLinkRequest = new
                {
                    path = path,
                    type = linkType,
                    accessibility = "anyone"
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/links", createLinkRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("type", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));

                // Add created link
                var row = dataTable.NewRow();
                row["id"] = root.GetProperty("id").GetString();
                row["path"] = path;
                row["url"] = root.GetProperty("url").GetString();
                row["type"] = linkType;
                row["creation_date"] = DateTime.UtcNow;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating link in Egnyte");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Egnyte API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await UpdateFileAsync(data);
                    case "folders":
                        return await UpdateFolderAsync(data);
                    case "permissions":
                        return await UpdatePermissionsAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Egnyte");
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

        private async Task<DataTable> UpdatePermissionsAsync(Dictionary<string, object> data)
        {
            // Implementation for permissions update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Permissions update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Egnyte API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await DeleteFileAsync(parameters);
                    case "folders":
                        return await DeleteFolderAsync(parameters);
                    case "links":
                        return await DeleteLinkAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Egnyte");
                throw;
            }
        }

        private async Task<bool> DeleteFileAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("path"))
                {
                    throw new ArgumentException("path is required for file deletion");
                }

                var path = parameters["path"].ToString();
                var deleteRequest = new
                {
                    action = "delete"
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/fs{path}", deleteRequest);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Egnyte");
                throw;
            }
        }

        private async Task<bool> DeleteFolderAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("path"))
                {
                    throw new ArgumentException("path is required for folder deletion");
                }

                var path = parameters["path"].ToString();
                var deleteRequest = new
                {
                    action = "delete"
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/pubapi/v1/fs{path}", deleteRequest);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder from Egnyte");
                throw;
            }
        }

        private async Task<bool> DeleteLinkAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("id"))
                {
                    throw new ArgumentException("id is required for link deletion");
                }

                var linkId = parameters["id"].ToString();
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/pubapi/v1/links/{linkId}");
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting link from Egnyte");
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
                            new { Name = "name", Type = "string", Nullable = false, Description = "File name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to the file" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "File size in bytes" },
                            new { Name = "last_modified", Type = "datetime", Nullable = true, Description = "Last modification date" },
                            new { Name = "is_folder", Type = "bool", Nullable = true, Description = "Whether this is a folder" },
                            new { Name = "uploaded_by", Type = "string", Nullable = true, Description = "User who uploaded the file" },
                            new { Name = "checksum", Type = "string", Nullable = true, Description = "File checksum" },
                            new { Name = "entry_id", Type = "string", Nullable = true, Description = "Unique entry identifier" }
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
                            new { Name = "name", Type = "string", Nullable = false, Description = "Folder name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to the folder" },
                            new { Name = "folder_id", Type = "string", Nullable = false, Description = "Unique folder identifier" },
                            new { Name = "parent_id", Type = "string", Nullable = true, Description = "Parent folder identifier" },
                            new { Name = "created_by", Type = "string", Nullable = true, Description = "User who created the folder" },
                            new { Name = "created_date", Type = "datetime", Nullable = true, Description = "Creation date" },
                            new { Name = "last_modified", Type = "datetime", Nullable = true, Description = "Last modification date" }
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

                    case "links":
                        var linkFields = new[]
                        {
                            new { Name = "id", Type = "string", Nullable = false, Description = "Link identifier" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Link name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Path to the linked item" },
                            new { Name = "url", Type = "string", Nullable = false, Description = "Public URL" },
                            new { Name = "type", Type = "string", Nullable = false, Description = "Link type (file/folder)" },
                            new { Name = "created_by", Type = "string", Nullable = false, Description = "User who created the link" },
                            new { Name = "creation_date", Type = "datetime", Nullable = false, Description = "Link creation date" },
                            new { Name = "expiry_date", Type = "datetime", Nullable = true, Description = "Link expiration date" },
                            new { Name = "recipients", Type = "string", Nullable = true, Description = "Link recipients" }
                        };

                        foreach (var field in linkFields)
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
                            new { Name = "user_type", Type = "string", Nullable = false, Description = "User type" },
                            new { Name = "active", Type = "bool", Nullable = false, Description = "Whether user is active" },
                            new { Name = "locked", Type = "bool", Nullable = false, Description = "Whether user is locked" }
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
                            new { Name = "display_name", Type = "string", Nullable = false, Description = "Display name" },
                            new { Name = "group_type", Type = "string", Nullable = false, Description = "Group type" },
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

                    case "permissions":
                        var permissionFields = new[]
                        {
                            new { Name = "path", Type = "string", Nullable = false, Description = "Path to the item" },
                            new { Name = "user_id", Type = "string", Nullable = true, Description = "User identifier" },
                            new { Name = "group_id", Type = "string", Nullable = true, Description = "Group identifier" },
                            new { Name = "permission", Type = "string", Nullable = false, Description = "Permission level" },
                            new { Name = "inherited", Type = "bool", Nullable = false, Description = "Whether permission is inherited" }
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
