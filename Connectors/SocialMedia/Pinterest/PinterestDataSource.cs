using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.Pinterest
{
    /// <summary>
    /// Configuration class for Pinterest data source
    /// </summary>
    public class PinterestConfig
    {
        /// <summary>
        /// Pinterest App ID
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Pinterest App Secret
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Pinterest API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Pinterest User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Pinterest Username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// API version for Pinterest API (default: v5)
        /// </summary>
        public string ApiVersion { get; set; } = "v5";

        /// <summary>
        /// Base URL for Pinterest API
        /// </summary>
        public string BaseUrl => $"https://api.pinterest.com/{ApiVersion}";

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Rate limit delay between requests in milliseconds
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;

        /// <summary>
        /// Page size for paginated requests
        /// </summary>
        public int PageSize { get; set; } = 25;
    }

    /// <summary>
    /// Pinterest data source implementation for Beep framework
    /// Supports Pinterest API v5
    /// </summary>
    public class PinterestDataSource : IDataSource
    {
        private readonly PinterestConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for PinterestDataSource
        /// </summary>
        /// <param name="config">Pinterest configuration</param>
        public PinterestDataSource(PinterestConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for PinterestDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: AppId=xxx;AppSecret=xxx;AccessToken=xxx;UserId=xxx</param>
        public PinterestDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into PinterestConfig
        /// </summary>
        private PinterestConfig ParseConnectionString(string connectionString)
        {
            var config = new PinterestConfig();
            var parts = connectionString.Split(';');

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    switch (key.ToLower())
                    {
                        case "appid":
                            config.AppId = value;
                            break;
                        case "appsecret":
                            config.AppSecret = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "userid":
                            config.UserId = value;
                            break;
                        case "username":
                            config.Username = value;
                            break;
                        case "apiversion":
                            config.ApiVersion = value;
                            break;
                        case "timeoutseconds":
                            if (int.TryParse(value, out var timeout))
                                config.TimeoutSeconds = timeout;
                            break;
                        case "maxretries":
                            if (int.TryParse(value, out var retries))
                                config.MaxRetries = retries;
                            break;
                        case "ratelimitdelayms":
                            if (int.TryParse(value, out var delay))
                                config.RateLimitDelayMs = delay;
                            break;
                        case "pagesize":
                            if (int.TryParse(value, out var pageSize))
                                config.PageSize = pageSize;
                            break;
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for Pinterest entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // User Profile
            metadata["user"] = new EntityMetadata
            {
                EntityName = "user",
                DisplayName = "User Profile",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "first_name", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "last_name", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "bio", Type = "string", DisplayName = "Bio" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "counts", Type = "string", DisplayName = "Counts" },
                    new EntityField { Name = "image", Type = "string", DisplayName = "Profile Image" }
                }
            };

            // Boards
            metadata["boards"] = new EntityMetadata
            {
                EntityName = "boards",
                DisplayName = "Boards",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Board ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Board Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "owner", Type = "string", DisplayName = "Owner" },
                    new EntityField { Name = "privacy", Type = "string", DisplayName = "Privacy" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "counts", Type = "string", DisplayName = "Counts" },
                    new EntityField { Name = "image", Type = "string", DisplayName = "Board Image" }
                }
            };

            // Pins
            metadata["pins"] = new EntityMetadata
            {
                EntityName = "pins",
                DisplayName = "Pins",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Pin ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "link", Type = "string", DisplayName = "Link" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "board", Type = "string", DisplayName = "Board" },
                    new EntityField { Name = "counts", Type = "string", DisplayName = "Counts" },
                    new EntityField { Name = "images", Type = "string", DisplayName = "Images" },
                    new EntityField { Name = "dominant_color", Type = "string", DisplayName = "Dominant Color" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "date", Type = "datetime", IsPrimaryKey = true, DisplayName = "Date" },
                    new EntityField { Name = "pin_id", Type = "string", IsPrimaryKey = true, DisplayName = "Pin ID" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "saves", Type = "integer", DisplayName = "Saves" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "outbound_clicks", Type = "integer", DisplayName = "Outbound Clicks" },
                    new EntityField { Name = "pin_clicks", Type = "integer", DisplayName = "Pin Clicks" },
                    new EntityField { Name = "closeups", Type = "integer", DisplayName = "Closeups" }
                }
            };

            // Following
            metadata["following"] = new EntityMetadata
            {
                EntityName = "following",
                DisplayName = "Following",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "first_name", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "last_name", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "followed_at", Type = "datetime", DisplayName = "Followed At" }
                }
            };

            // Followers
            metadata["followers"] = new EntityMetadata
            {
                EntityName = "followers",
                DisplayName = "Followers",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "username", Type = "string", DisplayName = "Username" },
                    new EntityField { Name = "first_name", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "last_name", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "followed_at", Type = "datetime", DisplayName = "Followed At" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Pinterest API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for Pinterest connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user profile
                var testUrl = $"{_config.BaseUrl}/user_account";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Pinterest API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Pinterest API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Pinterest API
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
            return true;
        }

        /// <summary>
        /// Get data from Pinterest API
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                await ConnectAsync();
            }

            parameters ??= new Dictionary<string, object>();

            try
            {
                string url;
                var pageSize = parameters.ContainsKey("page_size") ? parameters["page_size"].ToString() : _config.PageSize.ToString();

                switch (entityName.ToLower())
                {
                    case "user":
                        url = $"{_config.BaseUrl}/user_account";
                        break;

                    case "boards":
                        var userBoards = parameters.ContainsKey("user") ? parameters["user"].ToString() : _config.Username;
                        url = $"{_config.BaseUrl}/boards?page_size={pageSize}";
                        break;

                    case "board":
                        var boardId = parameters.ContainsKey("board_id") ? parameters["board_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/boards/{boardId}";
                        break;

                    case "pins":
                        var boardPins = parameters.ContainsKey("board_id") ? parameters["board_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/boards/{boardPins}/pins?page_size={pageSize}";
                        break;

                    case "pin":
                        var pinId = parameters.ContainsKey("pin_id") ? parameters["pin_id"].ToString() : "";
                        url = $"{_config.BaseUrl}/pins/{pinId}";
                        break;

                    case "userpins":
                        url = $"{_config.BaseUrl}/pins?page_size={pageSize}";
                        break;

                    case "analytics":
                        var pinAnalyticsId = parameters.ContainsKey("pin_id") ? parameters["pin_id"].ToString() : "";
                        var startDate = parameters.ContainsKey("start_date") ? parameters["start_date"].ToString() : DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
                        var endDate = parameters.ContainsKey("end_date") ? parameters["end_date"].ToString() : DateTime.Now.ToString("yyyy-MM-dd");
                        url = $"{_config.BaseUrl}/pins/{pinAnalyticsId}/analytics?start_date={startDate}&end_date={endDate}";
                        break;

                    case "following":
                        url = $"{_config.BaseUrl}/user_account/following?page_size={pageSize}";
                        break;

                    case "followers":
                        url = $"{_config.BaseUrl}/user_account/followers?page_size={pageSize}";
                        break;

                    default:
                        throw new ArgumentException($"Unsupported entity: {entityName}");
                }

                // Rate limiting delay
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }

                var response = await _httpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Pinterest API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata.ContainsKey(entityName.ToLower()) ? _entityMetadata[entityName.ToLower()] : null;

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Handle different response structures
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataProp))
                {
                    dataElement = dataProp;
                }
                else if (root.TryGetProperty("items", out var itemsProp))
                {
                    dataElement = itemsProp;
                }
                else
                {
                    // Single object response
                    dataElement = root;
                }

                // Create columns based on metadata or first object
                if (metadata != null)
                {
                    foreach (var field in metadata.Fields)
                    {
                        dataTable.Columns.Add(field.Name, GetFieldType(field.Type));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            row[property.Name] = GetJsonValue(property.Value);
                        }
                    }
                    dataTable.Rows.Add(row);
                }
                else if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (dataTable.Columns.Contains(property.Name))
                            {
                                row[property.Name] = GetJsonValue(property.Value);
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
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "long" => typeof(long),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get value from JSON element
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// Get available entities
        /// </summary>
        public List<string> GetEntities()
        {
            return new List<string> { "user", "boards", "board", "pins", "pin", "userpins", "analytics", "following", "followers" };
        }

        /// <summary>
        /// Get entity metadata
        /// </summary>
        public EntityMetadata GetEntityMetadata(string entityName)
        {
            if (_entityMetadata.ContainsKey(entityName.ToLower()))
            {
                return _entityMetadata[entityName.ToLower()];
            }
            throw new ArgumentException($"Entity '{entityName}' not found");
        }

        /// <summary>
        /// Insert data (limited support for Pinterest API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "pins")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating pins would go here
            // This is a placeholder as Pinterest API has specific requirements for pin creation
            throw new NotImplementedException("Pin creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Pinterest API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Pinterest API");
        }

        /// <summary>
        /// Delete data (limited support for Pinterest API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Pinterest API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Pinterest, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Pinterest";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Pinterest Data Source";

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
            _isConnected = false;
        }
    }
}
