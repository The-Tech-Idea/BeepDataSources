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

namespace BeepDM.Connectors.CloudStorage.pCloud
{
    public class pCloudConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ApiToken { get; set; }
        public bool UseApiToken { get; set; }
    }

    public class pCloudDataSource : IDataSource
    {
        private readonly ILogger<pCloudDataSource> _logger;
        private readonly HttpClient _httpClient;
        private pCloudConfig _config;
        private bool _isConnected;
        private const string BaseUrl = "https://api.pcloud.com";

        public string DataSourceName => "pCloud";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "pCloud Cloud Storage Data Source";

        public pCloudDataSource(ILogger<pCloudDataSource> logger, HttpClient httpClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new pCloudConfig();

                // Check authentication method
                if (parameters.ContainsKey("ApiToken"))
                {
                    _config.ApiToken = parameters["ApiToken"].ToString();
                    _config.UseApiToken = true;
                }
                else if (parameters.ContainsKey("ClientId") && parameters.ContainsKey("ClientSecret"))
                {
                    _config.ClientId = parameters["ClientId"].ToString();
                    _config.ClientSecret = parameters["ClientSecret"].ToString();
                    _config.AccessToken = parameters.ContainsKey("AccessToken") ? parameters["AccessToken"].ToString() : "";
                    _config.RefreshToken = parameters.ContainsKey("RefreshToken") ? parameters["RefreshToken"].ToString() : "";
                }
                else
                {
                    throw new ArgumentException("Either ApiToken or ClientId/ClientSecret are required");
                }

                // Test connection by getting user info
                await InitializeHttpClientAsync();
                var userInfo = await GetUserInfoAsync();

                if (userInfo != null)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to pCloud API");
                    return true;
                }

                _logger.LogError("Failed to connect to pCloud API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to pCloud API");
                return false;
            }
        }

        private async Task InitializeHttpClientAsync()
        {
            _httpClient.DefaultRequestHeaders.Clear();

            if (_config.UseApiToken)
            {
                // API token is used in query parameters, not headers
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
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/userinfo?access_token={_config.ApiToken}"
                    : $"{BaseUrl}/userinfo";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                return jsonDoc.RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info from pCloud");
                throw;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _isConnected = false;
                _logger.LogInformation("Disconnected from pCloud API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from pCloud API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to pCloud API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await GetFilesAsync(parameters);
                    case "folders":
                        return await GetFoldersAsync(parameters);
                    case "shares":
                        return await GetSharesAsync(parameters);
                    case "users":
                        return await GetUsersAsync(parameters);
                    case "thumbnails":
                        return await GetThumbnailsAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from pCloud");
                throw;
            }
        }

        private async Task<DataTable> GetFilesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("files");

            try
            {
                var folderId = parameters.ContainsKey("folderId") ? Convert.ToInt64(parameters["folderId"]) : 0L;
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/listfolder?folderid={folderId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/listfolder?folderid={folderId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("created", typeof(DateTime));
                dataTable.Columns.Add("modified", typeof(DateTime));
                dataTable.Columns.Add("isfolder", typeof(bool));
                dataTable.Columns.Add("parentfolderid", typeof(long));
                dataTable.Columns.Add("hash", typeof(string));
                dataTable.Columns.Add("category", typeof(int));
                dataTable.Columns.Add("contenttype", typeof(string));

                // Add files
                if (root.TryGetProperty("metadata", out var metadata))
                {
                    if (metadata.TryGetProperty("contents", out var contents))
                    {
                        foreach (var item in contents.EnumerateArray())
                        {
                            if (!item.GetProperty("isfolder").GetBoolean()) // Only files
                            {
                                var row = dataTable.NewRow();
                                row["id"] = item.GetProperty("id").GetInt64();
                                row["name"] = item.GetProperty("name").GetString();
                                row["path"] = item.GetProperty("path").GetString();
                                row["size"] = item.TryGetProperty("size", out var size) ? size.GetInt64() : 0L;
                                row["created"] = item.TryGetProperty("created", out var created) ? DateTimeOffset.FromUnixTimeSeconds(created.GetString() == "false" ? 0 : long.Parse(created.GetString())).DateTime : DateTime.MinValue;
                                row["modified"] = item.TryGetProperty("modified", out var modified) ? DateTimeOffset.FromUnixTimeSeconds(modified.GetString() == "false" ? 0 : long.Parse(modified.GetString())).DateTime : DateTime.MinValue;
                                row["isfolder"] = item.GetProperty("isfolder").GetBoolean();
                                row["parentfolderid"] = item.GetProperty("parentfolderid").GetInt64();
                                row["hash"] = item.TryGetProperty("hash", out var hash) ? hash.GetString() : "";
                                row["category"] = item.TryGetProperty("category", out var category) ? category.GetInt32() : 0;
                                row["contenttype"] = item.TryGetProperty("contenttype", out var contenttype) ? contenttype.GetString() : "";

                                dataTable.Rows.Add(row);
                            }
                        }
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files from pCloud");
                throw;
            }
        }

        private async Task<DataTable> GetFoldersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("folders");

            try
            {
                var folderId = parameters.ContainsKey("folderId") ? Convert.ToInt64(parameters["folderId"]) : 0L;
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/listfolder?folderid={folderId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/listfolder?folderid={folderId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));
                dataTable.Columns.Add("modified", typeof(DateTime));
                dataTable.Columns.Add("isfolder", typeof(bool));
                dataTable.Columns.Add("parentfolderid", typeof(long));
                dataTable.Columns.Add("folderid", typeof(long));

                // Add folders
                if (root.TryGetProperty("metadata", out var metadata))
                {
                    // Add current folder
                    var row = dataTable.NewRow();
                    row["id"] = metadata.GetProperty("id").GetInt64();
                    row["name"] = metadata.GetProperty("name").GetString();
                    row["path"] = metadata.GetProperty("path").GetString();
                    row["created"] = metadata.TryGetProperty("created", out var created) ? DateTimeOffset.FromUnixTimeSeconds(created.GetString() == "false" ? 0 : long.Parse(created.GetString())).DateTime : DateTime.MinValue;
                    row["modified"] = metadata.TryGetProperty("modified", out var modified) ? DateTimeOffset.FromUnixTimeSeconds(modified.GetString() == "false" ? 0 : long.Parse(modified.GetString())).DateTime : DateTime.MinValue;
                    row["isfolder"] = metadata.GetProperty("isfolder").GetBoolean();
                    row["parentfolderid"] = metadata.GetProperty("parentfolderid").GetInt64();
                    row["folderid"] = metadata.GetProperty("folderid").GetInt64();

                    dataTable.Rows.Add(row);

                    // Add subfolders
                    if (metadata.TryGetProperty("contents", out var contents))
                    {
                        foreach (var item in contents.EnumerateArray())
                        {
                            if (item.GetProperty("isfolder").GetBoolean()) // Only folders
                            {
                                var subRow = dataTable.NewRow();
                                subRow["id"] = item.GetProperty("id").GetInt64();
                                subRow["name"] = item.GetProperty("name").GetString();
                                subRow["path"] = item.GetProperty("path").GetString();
                                subRow["created"] = item.TryGetProperty("created", out var subCreated) ? DateTimeOffset.FromUnixTimeSeconds(subCreated.GetString() == "false" ? 0 : long.Parse(subCreated.GetString())).DateTime : DateTime.MinValue;
                                subRow["modified"] = item.TryGetProperty("modified", out var subModified) ? DateTimeOffset.FromUnixTimeSeconds(subModified.GetString() == "false" ? 0 : long.Parse(subModified.GetString())).DateTime : DateTime.MinValue;
                                subRow["isfolder"] = item.GetProperty("isfolder").GetBoolean();
                                subRow["parentfolderid"] = item.GetProperty("parentfolderid").GetInt64();
                                subRow["folderid"] = item.GetProperty("folderid").GetInt64();

                                dataTable.Rows.Add(subRow);
                            }
                        }
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders from pCloud");
                throw;
            }
        }

        private async Task<DataTable> GetSharesAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("shares");

            try
            {
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/listshares?access_token={_config.ApiToken}"
                    : $"{BaseUrl}/listshares";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("link", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));
                dataTable.Columns.Add("modified", typeof(DateTime));
                dataTable.Columns.Add("downloads", typeof(int));
                dataTable.Columns.Add("maxdownloads", typeof(int));
                dataTable.Columns.Add("expires", typeof(DateTime));
                dataTable.Columns.Add("publicupload", typeof(bool));
                dataTable.Columns.Add("publicuploadwritefolder", typeof(long));

                // Add shares
                if (root.TryGetProperty("shares", out var sharesArray))
                {
                    foreach (var share in sharesArray.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        row["id"] = share.GetProperty("id").GetInt64();
                        row["name"] = share.TryGetProperty("name", out var name) ? name.GetString() : "";
                        row["link"] = share.TryGetProperty("link", out var link) ? link.GetString() : "";
                        row["created"] = share.TryGetProperty("created", out var created) ? DateTimeOffset.FromUnixTimeSeconds(created.GetString() == "false" ? 0 : long.Parse(created.GetString())).DateTime : DateTime.MinValue;
                        row["modified"] = share.TryGetProperty("modified", out var modified) ? DateTimeOffset.FromUnixTimeSeconds(modified.GetString() == "false" ? 0 : long.Parse(modified.GetString())).DateTime : DateTime.MinValue;
                        row["downloads"] = share.TryGetProperty("downloads", out var downloads) ? downloads.GetInt32() : 0;
                        row["maxdownloads"] = share.TryGetProperty("maxdownloads", out var maxdownloads) ? maxdownloads.GetInt32() : 0;
                        row["expires"] = share.TryGetProperty("expires", out var expires) ? DateTimeOffset.FromUnixTimeSeconds(expires.GetString() == "false" ? 0 : long.Parse(expires.GetString())).DateTime : DateTime.MaxValue;
                        row["publicupload"] = share.TryGetProperty("publicupload", out var publicupload) ? publicupload.GetBoolean() : false;
                        row["publicuploadwritefolder"] = share.TryGetProperty("publicuploadwritefolder", out var publicuploadwritefolder) ? publicuploadwritefolder.GetInt64() : 0L;

                        dataTable.Rows.Add(row);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares from pCloud");
                throw;
            }
        }

        private async Task<DataTable> GetUsersAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("users");

            try
            {
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/userinfo?access_token={_config.ApiToken}"
                    : $"{BaseUrl}/userinfo";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("userid", typeof(long));
                dataTable.Columns.Add("email", typeof(string));
                dataTable.Columns.Add("email_verified", typeof(bool));
                dataTable.Columns.Add("quota", typeof(long));
                dataTable.Columns.Add("usedquota", typeof(long));
                dataTable.Columns.Add("language", typeof(string));
                dataTable.Columns.Add("plan", typeof(string));
                dataTable.Columns.Add("premium", typeof(bool));
                dataTable.Columns.Add("business", typeof(bool));

                // Add user info
                if (root.TryGetProperty("userid", out var userid))
                {
                    var row = dataTable.NewRow();
                    row["userid"] = userid.GetInt64();
                    row["email"] = root.TryGetProperty("email", out var email) ? email.GetString() : "";
                    row["email_verified"] = root.TryGetProperty("email_verified", out var emailVerified) ? emailVerified.GetBoolean() : false;
                    row["quota"] = root.TryGetProperty("quota", out var quota) ? quota.GetInt64() : 0L;
                    row["usedquota"] = root.TryGetProperty("usedquota", out var usedquota) ? usedquota.GetInt64() : 0L;
                    row["language"] = root.TryGetProperty("language", out var language) ? language.GetString() : "";
                    row["plan"] = root.TryGetProperty("plan", out var plan) ? plan.GetString() : "";
                    row["premium"] = root.TryGetProperty("premium", out var premium) ? premium.GetBoolean() : false;
                    row["business"] = root.TryGetProperty("business", out var business) ? business.GetBoolean() : false;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from pCloud");
                throw;
            }
        }

        private async Task<DataTable> GetThumbnailsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("thumbnails");

            try
            {
                if (!parameters.ContainsKey("fileId"))
                {
                    throw new ArgumentException("fileId is required for thumbnails");
                }

                var fileId = Convert.ToInt64(parameters["fileId"]);
                var size = parameters.ContainsKey("size") ? parameters["size"].ToString() : "256x256";
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/getthumbnaillink?fileid={fileId}&size={size}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/getthumbnaillink?fileid={fileId}&size={size}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("fileid", typeof(long));
                dataTable.Columns.Add("size", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("expires", typeof(DateTime));

                // Add thumbnail info
                var row = dataTable.NewRow();
                row["fileid"] = fileId;
                row["size"] = size;
                row["url"] = root.TryGetProperty("url", out var thumbnailUrl) ? thumbnailUrl.GetString() : "";
                row["expires"] = root.TryGetProperty("expires", out var expires) ? DateTimeOffset.FromUnixTimeSeconds(expires.GetString() == "false" ? 0 : long.Parse(expires.GetString())).DateTime : DateTime.MinValue;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thumbnails from pCloud");
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
                new { Name = "files", Type = "File", Description = "Files stored in pCloud", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "folders", Type = "Folder", Description = "Directory structure and folders", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "shares", Type = "Share", Description = "Public sharing links", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "users", Type = "User", Description = "User account information", Create = false, Read = true, Update = false, Delete = false },
                new { Name = "thumbnails", Type = "Thumbnail", Description = "File thumbnail images", Create = false, Read = true, Update = false, Delete = false }
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
                throw new InvalidOperationException("Not connected to pCloud API");
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
                _logger.LogError(ex, $"Error creating entity {entityName} in pCloud");
                throw;
            }
        }

        private async Task<DataTable> CreateFolderAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("folders");

            try
            {
                if (!data.ContainsKey("name") || !data.ContainsKey("parentFolderId"))
                {
                    throw new ArgumentException("name and parentFolderId are required for folder creation");
                }

                var name = data["name"].ToString();
                var parentFolderId = Convert.ToInt64(data["parentFolderId"]);
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/createfolder?name={Uri.EscapeDataString(name)}&folderid={parentFolderId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/createfolder?name={Uri.EscapeDataString(name)}&folderid={parentFolderId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("name", typeof(string));
                dataTable.Columns.Add("path", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));

                // Add created folder
                if (root.TryGetProperty("metadata", out var metadata))
                {
                    var row = dataTable.NewRow();
                    row["id"] = metadata.GetProperty("id").GetInt64();
                    row["name"] = metadata.GetProperty("name").GetString();
                    row["path"] = metadata.GetProperty("path").GetString();
                    row["created"] = metadata.TryGetProperty("created", out var created) ? DateTimeOffset.FromUnixTimeSeconds(created.GetString() == "false" ? 0 : long.Parse(created.GetString())).DateTime : DateTime.UtcNow;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder in pCloud");
                throw;
            }
        }

        private async Task<DataTable> CreateShareAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("shares");

            try
            {
                if (!data.ContainsKey("folderId"))
                {
                    throw new ArgumentException("folderId is required for share creation");
                }

                var folderId = Convert.ToInt64(data["folderId"]);
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/getsharelink?folderid={folderId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/getsharelink?folderid={folderId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Create columns
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("link", typeof(string));
                dataTable.Columns.Add("created", typeof(DateTime));

                // Add created share
                var row = dataTable.NewRow();
                row["id"] = folderId;
                row["link"] = root.TryGetProperty("link", out var link) ? link.GetString() : "";
                row["created"] = DateTime.UtcNow;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share in pCloud");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to pCloud API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await UpdateFileAsync(data);
                    case "folders":
                        return await UpdateFolderAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in pCloud");
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

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to pCloud API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "files":
                        return await DeleteFileAsync(parameters);
                    case "folders":
                        return await DeleteFolderAsync(parameters);
                    case "shares":
                        return await DeleteShareAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from pCloud");
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

                var fileId = Convert.ToInt64(parameters["fileId"]);
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/deletefile?fileid={fileId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/deletefile?fileid={fileId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from pCloud");
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

                var folderId = Convert.ToInt64(parameters["folderId"]);
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/deletefolder?folderid={folderId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/deletefolder?folderid={folderId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder from pCloud");
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

                var shareId = Convert.ToInt64(parameters["shareId"]);
                var url = _config.UseApiToken
                    ? $"{BaseUrl}/deleteshare?shareid={shareId}&access_token={_config.ApiToken}"
                    : $"{BaseUrl}/deleteshare?shareid={shareId}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share from pCloud");
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
                            new { Name = "id", Type = "long", Nullable = false, Description = "Unique file identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "File name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to file" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "File size in bytes" },
                            new { Name = "created", Type = "datetime", Nullable = true, Description = "Creation timestamp" },
                            new { Name = "modified", Type = "datetime", Nullable = true, Description = "Last modification timestamp" },
                            new { Name = "isfolder", Type = "bool", Nullable = false, Description = "Whether this is a folder" },
                            new { Name = "parentfolderid", Type = "long", Nullable = false, Description = "Parent folder identifier" },
                            new { Name = "hash", Type = "string", Nullable = true, Description = "File hash" },
                            new { Name = "category", Type = "int", Nullable = true, Description = "File category" },
                            new { Name = "contenttype", Type = "string", Nullable = true, Description = "Content type" }
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
                            new { Name = "id", Type = "long", Nullable = false, Description = "Unique folder identifier" },
                            new { Name = "name", Type = "string", Nullable = false, Description = "Folder name" },
                            new { Name = "path", Type = "string", Nullable = false, Description = "Full path to folder" },
                            new { Name = "created", Type = "datetime", Nullable = true, Description = "Creation timestamp" },
                            new { Name = "modified", Type = "datetime", Nullable = true, Description = "Last modification timestamp" },
                            new { Name = "isfolder", Type = "bool", Nullable = false, Description = "Whether this is a folder" },
                            new { Name = "parentfolderid", Type = "long", Nullable = false, Description = "Parent folder identifier" },
                            new { Name = "folderid", Type = "long", Nullable = false, Description = "Folder identifier" }
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

                    case "shares":
                        var shareFields = new[]
                        {
                            new { Name = "id", Type = "long", Nullable = false, Description = "Share identifier" },
                            new { Name = "name", Type = "string", Nullable = true, Description = "Share name" },
                            new { Name = "link", Type = "string", Nullable = false, Description = "Public share link" },
                            new { Name = "created", Type = "datetime", Nullable = true, Description = "Creation timestamp" },
                            new { Name = "modified", Type = "datetime", Nullable = true, Description = "Last modification timestamp" },
                            new { Name = "downloads", Type = "int", Nullable = true, Description = "Number of downloads" },
                            new { Name = "maxdownloads", Type = "int", Nullable = true, Description = "Maximum downloads allowed" },
                            new { Name = "expires", Type = "datetime", Nullable = true, Description = "Expiration date" },
                            new { Name = "publicupload", Type = "bool", Nullable = true, Description = "Whether public upload is enabled" },
                            new { Name = "publicuploadwritefolder", Type = "long", Nullable = true, Description = "Public upload folder identifier" }
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
                            new { Name = "userid", Type = "long", Nullable = false, Description = "User identifier" },
                            new { Name = "email", Type = "string", Nullable = false, Description = "Email address" },
                            new { Name = "email_verified", Type = "bool", Nullable = false, Description = "Whether email is verified" },
                            new { Name = "quota", Type = "long", Nullable = true, Description = "Storage quota in bytes" },
                            new { Name = "usedquota", Type = "long", Nullable = true, Description = "Used storage in bytes" },
                            new { Name = "language", Type = "string", Nullable = true, Description = "User language" },
                            new { Name = "plan", Type = "string", Nullable = true, Description = "Subscription plan" },
                            new { Name = "premium", Type = "bool", Nullable = false, Description = "Whether user has premium account" },
                            new { Name = "business", Type = "bool", Nullable = false, Description = "Whether user has business account" }
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

                    case "thumbnails":
                        var thumbnailFields = new[]
                        {
                            new { Name = "fileid", Type = "long", Nullable = false, Description = "File identifier" },
                            new { Name = "size", Type = "string", Nullable = false, Description = "Thumbnail size" },
                            new { Name = "url", Type = "string", Nullable = false, Description = "Thumbnail URL" },
                            new { Name = "expires", Type = "datetime", Nullable = true, Description = "URL expiration date" }
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
