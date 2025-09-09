using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.InstagramDataSource.Config;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.InstagramDataSource
{
    /// <summary>
    /// Instagram data source implementation using WebAPIDataSource as base class
    /// Supports Instagram Graph API and Basic Display API
    /// </summary>
    public class InstagramDataSource : WebAPIDataSource
    {
        private readonly InstagramDataSourceConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly HttpClient _httpClient;
        private Dictionary<string, EntityStructure> _entityMetadata;

        /// <summary>
        /// Initializes a new instance of the InstagramDataSource class
        /// </summary>
        public InstagramDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per, InstagramDataSourceConfig config)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.IsValid())
            {
                throw new ArgumentException("Invalid Instagram configuration. Access token is required.");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Initialize HttpClient for direct API calls
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AccessToken}");
            _httpClient.Timeout = TimeSpan.FromMilliseconds(30000); // 30 seconds default

            // Initialize entity metadata
            _entityMetadata = InitializeEntityMetadata();
            EntitiesNames = _entityMetadata.Keys.ToList();
            Entities = _entityMetadata.Values.ToList();

            // Set up connection properties from config
            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                // Copy config properties to connection properties
                webApiProps.ApiKey = _config.AccessToken;
                webApiProps.ClientId = _config.AppId;
                webApiProps.ClientSecret = _config.AppSecret;
                webApiProps.UserID = _config.UserId;
                webApiProps.ConnectionString = _config.UseGraphApi ? _config.GraphApiUrl : _config.BasicDisplayUrl;
                webApiProps.TimeoutMs = 30000; // 30 seconds default
                webApiProps.MaxRetries = 3;
                webApiProps.EnableRateLimit = true;
                webApiProps.RateLimitRequestsPerMinute = 60; // Instagram rate limit
            }
        }



        /// <summary>
        /// Initialize entity metadata for Instagram entities
        /// </summary>
        private Dictionary<string, EntityStructure> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityStructure>();

            // User Profile
            metadata["user"] = new EntityStructure
            {
                EntityName = "user",
                DatasourceEntityName = "user",
                Caption = "User Profile",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "username", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "account_type", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "media_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "follows_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "followed_by_count", fieldtype = "System.Int32", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Media (Posts)
            metadata["media"] = new EntityStructure
            {
                EntityName = "media",
                DatasourceEntityName = "media",
                Caption = "Media Posts",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "media_type", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "media_url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "permalink", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "thumbnail_url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "caption", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "timestamp", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "like_count", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "comments_count", fieldtype = "System.Int32", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Stories
            metadata["stories"] = new EntityStructure
            {
                EntityName = "stories",
                DatasourceEntityName = "stories",
                Caption = "Stories",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "media_type", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "media_url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "thumbnail_url", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "timestamp", fieldtype = "System.DateTime", AllowDBNull = true },
                    new EntityField { fieldname = "expires_at", fieldtype = "System.DateTime", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Insights
            metadata["insights"] = new EntityStructure
            {
                EntityName = "insights",
                DatasourceEntityName = "insights",
                Caption = "Insights",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "media_id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "metric", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "value", fieldtype = "System.Int32", AllowDBNull = true },
                    new EntityField { fieldname = "title", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "description", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "period", fieldtype = "System.String", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "media_id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "metric", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            // Tags
            metadata["tags"] = new EntityStructure
            {
                EntityName = "tags",
                DatasourceEntityName = "tags",
                Caption = "Tags",
                Viewtype = ViewType.Table,
                Fields = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false },
                    new EntityField { fieldname = "name", fieldtype = "System.String", AllowDBNull = true },
                    new EntityField { fieldname = "media_count", fieldtype = "System.Int32", AllowDBNull = true }
                },
                PrimaryKeys = new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "System.String", IsKey = true, AllowDBNull = false }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Instagram API
        /// </summary>
        public override async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for Instagram connection");
                }

                // Test connection by getting user profile
                var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.BasicDisplayUrl;
                var testUrl = $"{baseUrl}/me?fields=id,username,account_type&access_token={_config.AccessToken}";

                // Use base class connection method
                ConnectionStatus = ConnectionState.Connecting;

                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

                    if (userInfo.TryGetProperty("id", out var userId))
                    {
                        _config.UserId = userId.GetString();
                        ConnectionStatus = ConnectionState.Open;
                        return true;
                    }
                }

                ConnectionStatus = ConnectionState.Closed;
                return false;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionState.Closed;
                throw new Exception($"Failed to connect to Instagram API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Instagram API
        /// </summary>
        public override Task<bool> DisconnectAsync()
        {
            try
            {
                // Use base class disconnect method
                ConnectionStatus = ConnectionState.Closed;
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to disconnect from Instagram API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get data from Instagram API
        /// </summary>
        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object>? parameters = null)
        {
            if (ConnectionStatus != ConnectionState.Open)
            {
                await ConnectAsync();
            }

            parameters ??= new Dictionary<string, object>();

            try
            {
                string url;
                var fields = parameters.ContainsKey("fields") ? parameters["fields"].ToString() : GetDefaultFields(entityName);

                switch (entityName.ToLower())
                {
                    case "user":
                        var baseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.BasicDisplayUrl;
                        url = $"{baseUrl}/me?fields={fields}&access_token={_config.AccessToken}";
                        break;

                    case "media":
                        var mediaLimit = parameters.ContainsKey("limit") ? parameters["limit"].ToString() : "25";
                        var mediaBaseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.BasicDisplayUrl;
                        url = $"{mediaBaseUrl}/me/media?fields={fields}&limit={mediaLimit}&access_token={_config.AccessToken}";
                        break;

                    case "stories":
                        var storiesBaseUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.BasicDisplayUrl;
                        url = $"{storiesBaseUrl}/me/stories?fields={fields}&access_token={_config.AccessToken}";
                        break;

                    case "insights":
                        var mediaId = parameters.ContainsKey("media_id") ? parameters["media_id"].ToString() : "";
                        var graphUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.GraphApiUrl;
                        var metrics = parameters.ContainsKey("metrics") ? parameters["metrics"].ToString() : "engagement,impressions,reach,saved";
                        url = $"{graphUrl}/{mediaId}/insights?metric={metrics}&access_token={_config.AccessToken}";
                        break;

                    case "tags":
                        var tagName = parameters.ContainsKey("tag_name") ? parameters["tag_name"].ToString() : "";
                        var tagsGraphUrl = Dataconnection?.ConnectionProp?.ConnectionString ?? _config.GraphApiUrl;
                        url = $"{tagsGraphUrl}/ig_hashtag_search?user_id={_config.UserId}&q={tagName}&access_token={_config.AccessToken}";
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
                    throw new Exception($"Instagram API request failed: {response.StatusCode} - {jsonContent}");
                }

                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get {entityName} data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get default fields for entity
        /// </summary>
        private string GetDefaultFields(string entityName)
        {
            return entityName.ToLower() switch
            {
                "user" => "id,username,account_type,media_count",
                "media" => "id,media_type,media_url,permalink,thumbnail_url,caption,timestamp,like_count,comments_count",
                "stories" => "id,media_type,media_url,thumbnail_url,timestamp,expires_at",
                "insights" => "value,title,description,period",
                "tags" => "id,name,media_count",
                _ => "*"
            };
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
                        dataTable.Columns.Add(field.fieldname, GetFieldType(field.Type));
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
            return new List<string> { "user", "media", "stories", "insights", "tags" };
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
        /// Insert data (not supported for Instagram API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Instagram API");
        }

        /// <summary>
        /// Update data (not supported for Instagram API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Instagram API");
        }

        /// <summary>
        /// Delete data (not supported for Instagram API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Instagram API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Instagram, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public override bool IsConnected => base.IsConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Instagram";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Instagram Data Source";

        /// <summary>
        /// Dispose resources
        /// </summary>
        public override void Dispose()
        {
            _httpClient?.Dispose();
            base.Dispose();
        }
    }
}
