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

namespace BeepDataSources.iCloud
{
    /// <summary>
    /// Configuration class for iCloud data source
    /// </summary>
    public class iCloudConfig
    {
        /// <summary>
        /// Apple ID email address
        /// </summary>
        public string AppleId { get; set; } = "";

        /// <summary>
        /// App-specific password for authentication
        /// </summary>
        public string AppSpecificPassword { get; set; } = "";

        /// <summary>
        /// iCloud service endpoint URL
        /// </summary>
        public string ServiceUrl { get; set; } = "https://www.icloud.com";

        /// <summary>
        /// Client ID for the application
        /// </summary>
        public string ClientId { get; set; } = "";

        /// <summary>
        /// Client secret for the application
        /// </summary>
        public string ClientSecret { get; set; } = "";

        /// <summary>
        /// Two-factor authentication code (if required)
        /// </summary>
        public string TwoFactorCode { get; set; } = "";

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
    /// iCloud data source implementation for Beep framework
    /// </summary>
    public class iCloudDataSource : IDataSource
    {
        private readonly iCloudConfig _config;
        private HttpClient _httpClient;
        private string _sessionToken;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for iCloudDataSource
        /// </summary>
        /// <param name="config">Configuration object</param>
        public iCloudDataSource(object config)
        {
            _config = config as iCloudConfig ?? new iCloudConfig();
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Initialize entity metadata for iCloud entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Files entity
            metadata["files"] = new EntityMetadata
            {
                EntityName = "files",
                DisplayName = "Files",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "File ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "File Name" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "File Type" },
                    new EntityField { Name = "size", Type = "long", DisplayName = "File Size" },
                    new EntityField { Name = "dateCreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "dateModified", Type = "datetime", DisplayName = "Date Modified" },
                    new EntityField { Name = "dateLastOpened", Type = "datetime", DisplayName = "Date Last Opened" },
                    new EntityField { Name = "parentId", Type = "string", DisplayName = "Parent Folder ID" },
                    new EntityField { Name = "isFolder", Type = "boolean", DisplayName = "Is Folder" },
                    new EntityField { Name = "extension", Type = "string", DisplayName = "File Extension" },
                    new EntityField { Name = "mimeType", Type = "string", DisplayName = "MIME Type" },
                    new EntityField { Name = "etag", Type = "string", DisplayName = "ETag" },
                    new EntityField { Name = "shareType", Type = "string", DisplayName = "Share Type" },
                    new EntityField { Name = "shareUrl", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "isShared", Type = "boolean", DisplayName = "Is Shared" },
                    new EntityField { Name = "downloadUrl", Type = "string", DisplayName = "Download URL" },
                    new EntityField { Name = "thumbnailUrl", Type = "string", DisplayName = "Thumbnail URL" }
                }
            };

            // Folders entity
            metadata["folders"] = new EntityMetadata
            {
                EntityName = "folders",
                DisplayName = "Folders",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Folder ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Folder Name" },
                    new EntityField { Name = "parentId", Type = "string", DisplayName = "Parent Folder ID" },
                    new EntityField { Name = "dateCreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "dateModified", Type = "datetime", DisplayName = "Date Modified" },
                    new EntityField { Name = "itemCount", Type = "int", DisplayName = "Item Count" },
                    new EntityField { Name = "shareType", Type = "string", DisplayName = "Share Type" },
                    new EntityField { Name = "shareUrl", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "isShared", Type = "boolean", DisplayName = "Is Shared" },
                    new EntityField { Name = "etag", Type = "string", DisplayName = "ETag" }
                }
            };

            // Shares entity
            metadata["shares"] = new EntityMetadata
            {
                EntityName = "shares",
                DisplayName = "Shares",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Share ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Share Name" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "Share URL" },
                    new EntityField { Name = "createdBy", Type = "string", DisplayName = "Created By" },
                    new EntityField { Name = "dateCreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "dateModified", Type = "datetime", DisplayName = "Date Modified" },
                    new EntityField { Name = "expirationDate", Type = "datetime", DisplayName = "Expiration Date" },
                    new EntityField { Name = "isPasswordProtected", Type = "boolean", DisplayName = "Is Password Protected" },
                    new EntityField { Name = "downloadCount", Type = "int", DisplayName = "Download Count" },
                    new EntityField { Name = "maxDownloads", Type = "int", DisplayName = "Max Downloads" },
                    new EntityField { Name = "itemCount", Type = "int", DisplayName = "Item Count" },
                    new EntityField { Name = "totalSize", Type = "long", DisplayName = "Total Size" }
                }
            };

            // Devices entity
            metadata["devices"] = new EntityMetadata
            {
                EntityName = "devices",
                DisplayName = "Devices",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Device ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Device Name" },
                    new EntityField { Name = "model", Type = "string", DisplayName = "Device Model" },
                    new EntityField { Name = "osVersion", Type = "string", DisplayName = "OS Version" },
                    new EntityField { Name = "serialNumber", Type = "string", DisplayName = "Serial Number" },
                    new EntityField { Name = "isActive", Type = "boolean", DisplayName = "Is Active" },
                    new EntityField { Name = "lastSeen", Type = "datetime", DisplayName = "Last Seen" },
                    new EntityField { Name = "deviceClass", Type = "string", DisplayName = "Device Class" },
                    new EntityField { Name = "backupEnabled", Type = "boolean", DisplayName = "Backup Enabled" },
                    new EntityField { Name = "storageUsed", Type = "long", DisplayName = "Storage Used" },
                    new EntityField { Name = "storageAvailable", Type = "long", DisplayName = "Storage Available" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to iCloud service
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AppleId) || string.IsNullOrEmpty(_config.AppSpecificPassword))
                {
                    throw new ArgumentException("Apple ID and App-Specific Password are required");
                }

                // Initialize HTTP client
                _httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Set up authentication headers
                var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.AppleId}:{_config.AppSpecificPassword}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

                // Test connection by getting service info
                var response = await _httpClient.GetAsync($"{_config.ServiceUrl}/setup/ws/1/accountLogin");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<JsonElement>(content);

                    if (loginResponse.TryGetProperty("webservices", out var webservices) &&
                        webservices.TryGetProperty("drivews", out var drivews) &&
                        drivews.TryGetProperty("url", out var url))
                    {
                        _sessionToken = url.GetString();
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
                throw new Exception($"Failed to connect to iCloud: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from iCloud service
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
                throw new Exception($"Failed to disconnect from iCloud: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get entity data from iCloud
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to iCloud. Call ConnectAsync first.");
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
                    throw new Exception($"iCloud API request failed: {response.StatusCode} - {response.ReasonPhrase}");
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
            var baseUrl = _sessionToken ?? $"{_config.ServiceUrl}/drivews/1";
            var endpoint = entityName.ToLower() switch
            {
                "files" => $"{baseUrl}/retrieveItems",
                "folders" => $"{baseUrl}/retrieveItems",
                "shares" => $"{baseUrl}/retrieveShares",
                "devices" => $"{baseUrl}/retrieveDevices",
                _ => throw new ArgumentException($"Unknown entity: {entityName}")
            };

            if (parameters != null && parameters.Count > 0)
            {
                var queryParams = new List<string>();
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value?.ToString() ?? "")}");
                }
                endpoint += "?" + string.Join("&", queryParams);
            }

            return endpoint;
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

                if (jsonDoc.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (item.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (jsonDoc.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in jsonDoc.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var field in metadata.Fields)
                        {
                            if (item.TryGetProperty(field.Name, out var value))
                            {
                                row[field.Name] = ParseJsonValue(value, field.Type);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
                else if (jsonDoc.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var field in metadata.Fields)
                    {
                        if (jsonDoc.TryGetProperty(field.Name, out var value))
                        {
                            row[field.Name] = ParseJsonValue(value, field.Type);
                        }
                    }
                    dataTable.Rows.Add(row);
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
                throw new InvalidOperationException("Not connected to iCloud. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = GetEntityEndpoint(entityName);
                var jsonData = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Update an existing record in the specified entity
        /// </summary>
        public async Task<bool> UpdateAsync(string entityName, Dictionary<string, object> data, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to iCloud. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = $"{GetEntityEndpoint(entityName)}/{id}";
                var jsonData = JsonSerializer.Serialize(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update record in '{entityName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete a record from the specified entity
        /// </summary>
        public async Task<bool> DeleteAsync(string entityName, string id)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to iCloud. Call ConnectAsync first.");
            }

            try
            {
                var endpoint = $"{GetEntityEndpoint(entityName)}/{id}";
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete record from '{entityName}': {ex.Message}", ex);
            }
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
            var newConfig = config as iCloudConfig;
            if (newConfig != null)
            {
                _config.AppleId = newConfig.AppleId;
                _config.AppSpecificPassword = newConfig.AppSpecificPassword;
                _config.ServiceUrl = newConfig.ServiceUrl;
                _config.ClientId = newConfig.ClientId;
                _config.ClientSecret = newConfig.ClientSecret;
                _config.TwoFactorCode = newConfig.TwoFactorCode;
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
