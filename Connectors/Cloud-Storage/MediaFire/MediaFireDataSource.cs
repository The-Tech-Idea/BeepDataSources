using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.MediaFire
{
    /// <summary>
    /// Configuration class for MediaFire data source
    /// </summary>
    public class MediaFireConfig
    {
        /// <summary>
        /// MediaFire API Key
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// MediaFire API Secret
        /// </summary>
        public string ApiSecret { get; set; } = "";

        /// <summary>
        /// User email address
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; } = "";

        /// <summary>
        /// Application ID
        /// </summary>
        public string AppId { get; set; } = "";

        /// <summary>
        /// Session token (obtained after login)
        /// </summary>
        public string SessionToken { get; set; } = "";

        /// <summary>
        /// MediaFire service endpoint URL
        /// </summary>
        public string ServiceUrl { get; set; } = "https://www.mediafire.com/api";

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// MediaFire data source implementation for Beep framework
    /// </summary>
    public class MediaFireDataSource : IDataSource
    {
        private readonly MediaFireConfig _config;
        private HttpClient _httpClient;
        private string _sessionToken;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for MediaFireDataSource
        /// </summary>
        /// <param name="config">Configuration object</param>
        public MediaFireDataSource(object config)
        {
            _config = config as MediaFireConfig ?? new MediaFireConfig();
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Initialize entity metadata for MediaFire entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Files entity
            metadata["files"] = new EntityMetadata
            {
                EntityName = "files",
                DisplayName = "Files",
                PrimaryKey = "quickkey",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "quickkey", Type = "string", IsPrimaryKey = true, DisplayName = "Quick Key" },
                    new EntityField { Name = "filename", Type = "string", DisplayName = "File Name" },
                    new EntityField { Name = "filesize", Type = "long", DisplayName = "File Size" },
                    new EntityField { Name = "hash", Type = "string", DisplayName = "File Hash" },
                    new EntityField { Name = "created", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "modified", Type = "datetime", DisplayName = "Date Modified" },
                    new EntityField { Name = "downloads", Type = "int", DisplayName = "Download Count" },
                    new EntityField { Name = "parentfolderkey", Type = "string", DisplayName = "Parent Folder Key" },
                    new EntityField { Name = "filetype", Type = "string", DisplayName = "File Type" },
                    new EntityField { Name = "extension", Type = "string", DisplayName = "File Extension" },
                    new EntityField { Name = "mimetype", Type = "string", DisplayName = "MIME Type" },
                    new EntityField { Name = "links", Type = "string", DisplayName = "Download Links" },
                    new EntityField { Name = "onetime_download", Type = "boolean", DisplayName = "One Time Download" },
                    new EntityField { Name = "password_protected", Type = "boolean", DisplayName = "Password Protected" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" }
                }
            };

            // Folders entity
            metadata["folders"] = new EntityMetadata
            {
                EntityName = "folders",
                DisplayName = "Folders",
                PrimaryKey = "folderkey",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "folderkey", Type = "string", IsPrimaryKey = true, DisplayName = "Folder Key" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Folder Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "created", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "file_count", Type = "int", DisplayName = "File Count" },
                    new EntityField { Name = "folder_count", Type = "int", DisplayName = "Folder Count" },
                    new EntityField { Name = "total_size", Type = "long", DisplayName = "Total Size" },
                    new EntityField { Name = "parentfolderkey", Type = "string", DisplayName = "Parent Folder Key" },
                    new EntityField { Name = "privacy", Type = "string", DisplayName = "Privacy" },
                    new EntityField { Name = "shared", Type = "boolean", DisplayName = "Is Shared" },
                    new EntityField { Name = "dropbox_enabled", Type = "boolean", DisplayName = "Dropbox Enabled" },
                    new EntityField { Name = "revision", Type = "int", DisplayName = "Revision" }
                }
            };

            // Shares entity
            metadata["shares"] = new EntityMetadata
            {
                EntityName = "shares",
                DisplayName = "Shares",
                PrimaryKey = "sharekey",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "sharekey", Type = "string", IsPrimaryKey = true, DisplayName = "Share Key" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "created", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "expires", Type = "datetime", DisplayName = "Expiration Date" },
                    new EntityField { Name = "download_count", Type = "int", DisplayName = "Download Count" },
                    new EntityField { Name = "max_downloads", Type = "int", DisplayName = "Max Downloads" },
                    new EntityField { Name = "password_protected", Type = "boolean", DisplayName = "Password Protected" },
                    new EntityField { Name = "one_time_download", Type = "boolean", DisplayName = "One Time Download" },
                    new EntityField { Name = "item_count", Type = "int", DisplayName = "Item Count" },
                    new EntityField { Name = "total_size", Type = "long", DisplayName = "Total Size" },
                    new EntityField { Name = "share_type", Type = "string", DisplayName = "Share Type" },
                    new EntityField { Name = "folderkey", Type = "string", DisplayName = "Folder Key" },
                    new EntityField { Name = "quickkey", Type = "string", DisplayName = "File Quick Key" }
                }
            };

            // Contacts entity
            metadata["contacts"] = new EntityMetadata
            {
                EntityName = "contacts",
                DisplayName = "Contacts",
                PrimaryKey = "contact_key",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "contact_key", Type = "string", IsPrimaryKey = true, DisplayName = "Contact Key" },
                    new EntityField { Name = "display_name", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "first_name", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "last_name", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email Address" },
                    new EntityField { Name = "created", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "last_login", Type = "datetime", DisplayName = "Last Login" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "relationship", Type = "string", DisplayName = "Relationship" },
                    new EntityField { Name = "shared_folders", Type = "int", DisplayName = "Shared Folders Count" },
                    new EntityField { Name = "shared_files", Type = "int", DisplayName = "Shared Files Count" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to MediaFire service
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Email) || string.IsNullOrEmpty(_config.Password) ||
                    string.IsNullOrEmpty(_config.AppId))
                {
                    throw new ArgumentException("Email, Password, and AppId are required");
                }

                // Initialize HTTP client
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // If session token is already provided, use it
                if (!string.IsNullOrEmpty(_config.SessionToken))
                {
                    _sessionToken = _config.SessionToken;
                    _isConnected = true;
                    return true;
                }

                // Login to get session token
                var loginParams = new Dictionary<string, string>
                {
                    ["email"] = _config.Email,
                    ["password"] = _config.Password,
                    ["application_id"] = _config.AppId,
                    ["signature"] = GenerateSignature(_config.Email + _config.Password + _config.AppId + _config.ApiKey),
                    ["response_format"] = "json"
                };

                var loginUrl = $"{_config.ServiceUrl}/user/login.php";
                var response = await _httpClient.PostAsync(loginUrl, new FormUrlEncodedContent(loginParams));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<JsonElement>(content);

                    if (loginResponse.TryGetProperty("response", out var responseObj) &&
                        responseObj.TryGetProperty("session_token", out var sessionToken))
                    {
                        _sessionToken = sessionToken.GetString();
                        _config.SessionToken = _sessionToken;
                        _isConnected = true;
                        return true;
                    }
                }

                _isConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to MediaFire: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate signature for API requests
        /// </summary>
        private string GenerateSignature(string data)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Disconnect from MediaFire service
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
                _sessionToken = null;
                _isConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to disconnect from MediaFire: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity data from MediaFire
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to MediaFire. Call ConnectAsync first.");
            }

            if (!_entityMetadata.ContainsKey(entityName.ToLower()))
            {
                throw new ArgumentException($"Entity '{entityName}' is not supported");
            }

            try
            {
                var endpoint = GetEntityEndpoint(entityName, parameters);
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"MediaFire API request failed: {response.StatusCode} - {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                return ParseJsonToDataTable(content, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get entity '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity endpoint URL
        /// </summary>
        private string GetEntityEndpoint(string entityName, Dictionary<string, object> parameters = null)
        {
            var baseParams = new Dictionary<string, string>
            {
                ["session_token"] = _sessionToken,
                ["response_format"] = "json"
            };

            var endpoint = entityName.ToLower() switch
            {
                "files" => $"{_config.ServiceUrl}/folder/get_content.php",
                "folders" => $"{_config.ServiceUrl}/folder/get_content.php",
                "shares" => $"{_config.ServiceUrl}/share/get_content.php",
                "contacts" => $"{_config.ServiceUrl}/contact/fetch.php",
                _ => throw new ArgumentException($"Unknown entity: {entityName}")
            };

            // Add parameters
            var allParams = new Dictionary<string, string>(baseParams);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    allParams[param.Key] = param.Value?.ToString() ?? "";
                }
            }

            var queryString = string.Join("&", allParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return $"{endpoint}?{queryString}";
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata[entityName.ToLower()];

            // Add columns based on entity metadata
            foreach (var field in metadata.Fields)
            {
                dataTable.Columns.Add(field.Name, GetFieldType(field.Type));
            }

            try
            {
                var jsonDoc = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                if (jsonDoc.TryGetProperty("response", out var responseObj) &&
                    responseObj.TryGetProperty("folder_content", out var folderContent))
                {
                    // Handle folder content (files and folders)
                    if (folderContent.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var file in files.EnumerateArray())
                        {
                            var row = dataTable.NewRow();
                            foreach (var field in metadata.Fields)
                            {
                                if (file.TryGetProperty(field.Name, out var value))
                                {
                                    row[field.Name] = ParseJsonValue(value, field.Type);
                                }
                            }
                            dataTable.Rows.Add(row);
                        }
                    }

                    if (folderContent.TryGetProperty("folders", out var folders) && folders.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var folder in folders.EnumerateArray())
                        {
                            var row = dataTable.NewRow();
                            foreach (var field in metadata.Fields)
                            {
                                if (folder.TryGetProperty(field.Name, out var value))
                                {
                                    row[field.Name] = ParseJsonValue(value, field.Type);
                                }
                            }
                            dataTable.Rows.Add(row);
                        }
                    }
                }
                else if (jsonDoc.TryGetProperty("response", out var responseObj2) &&
                         responseObj2.TryGetProperty("shares", out var shares) && shares.ValueKind == JsonValueKind.Array)
                {
                    // Handle shares
                    foreach (var share in shares.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (share.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (jsonDoc.TryGetProperty("response", out var responseObj3) &&
                         responseObj3.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == JsonValueKind.Array)
                {
                    // Handle contacts
                    foreach (var contact in contacts.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (contact.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse JSON response: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Parse JSON value based on field type
        /// </summary>
        private object ParseJsonValue(JsonElement value, string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString(),
                "int" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intVal) ? intVal : 0,
                "long" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longVal) ? longVal : 0L,
                "boolean" => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False ? value.GetBoolean() : false,
                "datetime" => value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var dateVal) ? dateVal : DBNull.Value,
                "decimal" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalVal) ? decimalVal : 0m,
                "double" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var doubleVal) ? doubleVal : 0.0,
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => typeof(string),
                "int" => typeof(int),
                "long" => typeof(long),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                "decimal" => typeof(decimal),
                "double" => typeof(double),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get list of available entities
        /// </summary>
        public List<string> GetEntities()
        {
            return _entityMetadata.Keys.ToList();
        }

        /// <summary>
        /// Get metadata for a specific entity
        /// </summary>
        public EntityMetadata GetEntityMetadata(string entityName)
        {
            if (_entityMetadata.TryGetValue(entityName.ToLower(), out var metadata))
            {
                return metadata;
            }
            throw new ArgumentException($"Entity '{entityName}' not found");
        }

        /// <summary>
        /// Create a new record in the specified entity
        /// </summary>
        public async Task<bool> CreateAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to MediaFire. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetCreateEndpoint(entityName);
                var formData = new MultipartFormDataContent();

                // Add session token
                formData.Add(new StringContent(_sessionToken), "session_token");
                formData.Add(new StringContent("json"), "response_format");

                // Add data fields
                foreach (var item in data)
                {
                    formData.Add(new StringContent(item.Value?.ToString() ?? ""), item.Key);
                }

                var response = await _httpClient.PostAsync(endpoint, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get create endpoint for entity
        /// </summary>
        private string GetCreateEndpoint(string entityName)
        {
            return entityName.ToLower() switch
            {
                "files" => $"{_config.ServiceUrl}/upload/upload.php",
                "folders" => $"{_config.ServiceUrl}/folder/create.php",
                "shares" => $"{_config.ServiceUrl}/share/create.php",
                _ => throw new ArgumentException($"Create not supported for entity: {entityName}")
            };
        }

        /// <summary>
        /// Update an existing record in the specified entity
        /// </summary>
        public async Task<bool> UpdateAsync(string entityName, Dictionary<string, object> data, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to MediaFire. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetUpdateEndpoint(entityName, id);
                var formData = new MultipartFormDataContent();

                // Add session token
                formData.Add(new StringContent(_sessionToken), "session_token");
                formData.Add(new StringContent("json"), "response_format");

                // Add data fields
                foreach (var item in data)
                {
                    formData.Add(new StringContent(item.Value?.ToString() ?? ""), item.Key);
                }

                var response = await _httpClient.PostAsync(endpoint, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get update endpoint for entity
        /// </summary>
        private string GetUpdateEndpoint(string entityName, string id)
        {
            return entityName.ToLower() switch
            {
                "files" => $"{_config.ServiceUrl}/file/update.php",
                "folders" => $"{_config.ServiceUrl}/folder/update.php",
                "shares" => $"{_config.ServiceUrl}/share/update.php",
                _ => throw new ArgumentException($"Update not supported for entity: {entityName}")
            };
        }

        /// <summary>
        /// Delete a record from the specified entity
        /// </summary>
        public async Task<bool> DeleteAsync(string entityName, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to MediaFire. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetDeleteEndpoint(entityName, id);
                var formData = new MultipartFormDataContent();

                // Add session token
                formData.Add(new StringContent(_sessionToken), "session_token");
                formData.Add(new StringContent("json"), "response_format");

                var response = await _httpClient.PostAsync(endpoint, formData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete record from '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get delete endpoint for entity
        /// </summary>
        private string GetDeleteEndpoint(string entityName, string id)
        {
            return entityName.ToLower() switch
            {
                "files" => $"{_config.ServiceUrl}/file/delete.php",
                "folders" => $"{_config.ServiceUrl}/folder/delete.php",
                "shares" => $"{_config.ServiceUrl}/share/delete.php",
                _ => throw new ArgumentException($"Delete not supported for entity: {entityName}")
            };
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected()
        {
            return _isConnected;
        }

        /// <summary>
        /// Get data source configuration
        /// </summary>
        public object GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Set data source configuration
        /// </summary>
        public void SetConfig(object config)
        {
            var newConfig = config as MediaFireConfig;
            if (newConfig != null)
            {
                _config.ApiKey = newConfig.ApiKey;
                _config.ApiSecret = newConfig.ApiSecret;
                _config.Email = newConfig.Email;
                _config.Password = newConfig.Password;
                _config.AppId = newConfig.AppId;
                _config.SessionToken = newConfig.SessionToken;
                _config.ServiceUrl = newConfig.ServiceUrl;
                _config.TimeoutSeconds = newConfig.TimeoutSeconds;
                _config.MaxRetries = newConfig.MaxRetries;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}
