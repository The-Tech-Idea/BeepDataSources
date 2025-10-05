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
    [AddinAttribute(Category = "SocialMedia", Name = "PinterestDataSource")]
    public class PinterestDataSource : WebAPIDataSource
    {
    /// <summary>
    /// Constructor for PinterestDataSource
    /// </summary>
    public PinterestDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(datasourcename, logger, editor, type, errors)
    {
        InitializeEntities();
    }

    /// <summary>
    /// Initialize entities for Pinterest data source
    /// </summary>
    private void InitializeEntities()
    {
        // User Profile
        Entities["user"] = new EntityStructure
        {
            EntityName = "user",
            ViewID = 1,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "username", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "first_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "last_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "bio", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "counts", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "image", fieldtype = "string", ValueRetrievedFromParent = false }
            }
        };

        // Boards
        Entities["boards"] = new EntityStructure
        {
            EntityName = "boards",
            ViewID = 2,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "description", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "owner", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "privacy", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "counts", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "image", fieldtype = "string", ValueRetrievedFromParent = false }
            }
        };

        // Pins
        Entities["pins"] = new EntityStructure
        {
            EntityName = "pins",
            ViewID = 3,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "title", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "description", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "link", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "url", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "board", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "counts", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "images", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "dominant_color", fieldtype = "string", ValueRetrievedFromParent = false }
            }
        };

        // Analytics
        Entities["analytics"] = new EntityStructure
        {
            EntityName = "analytics",
            ViewID = 4,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "date", fieldtype = "datetime", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "pin_id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "impressions", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "saves", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "clicks", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "outbound_clicks", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "pin_clicks", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "closeups", fieldtype = "integer", ValueRetrievedFromParent = false }
            }
        };

        // Following
        Entities["following"] = new EntityStructure
        {
            EntityName = "following",
            ViewID = 5,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "username", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "first_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "last_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "followed_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Followers
        Entities["followers"] = new EntityStructure
        {
            EntityName = "followers",
            ViewID = 6,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "username", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "first_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "last_name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "followed_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Update EntitiesNames collection
        EntitiesNames.AddRange(Entities.Keys);
    }

    /// <summary>
    /// Connect to Pinterest API
    /// </summary>
    public override async Task<IErrorsInfo> ConnectAsync(WebAPIConnectionProperties properties)
    {
        try
        {
            if (string.IsNullOrEmpty(properties.AccessToken))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Access token is required for Pinterest connection";
                return ErrorObject;
            }

            // Test connection by getting user profile
            var testUrl = $"{properties.BaseUrl}/user_account";
            var response = await HttpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Successfully connected to Pinterest API";
                return ErrorObject;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Pinterest API connection failed: {response.StatusCode} - {errorContent}";
                return ErrorObject;
            }
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to connect to Pinterest API: {ex.Message}";
            return ErrorObject;
        }
    }

    /// <summary>
    /// Disconnect from Pinterest API
    /// </summary>
    public override async Task<IErrorsInfo> DisconnectAsync()
    {
        ErrorObject.Flag = Errors.Ok;
        ErrorObject.Message = "Successfully disconnected from Pinterest API";
        return ErrorObject;
    }

        /// <summary>
        /// Get data from Pinterest API
        public override async Task<(DataTable, ErrorObject)> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            var errorObject = new ErrorObject();
            DataTable dataTable = null;

            try
            {
                parameters ??= new Dictionary<string, object>();

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
                        errorObject.Flag = Errors.Failed;
                        errorObject.Message = $"Unsupported entity: {entityName}";
                        return (null, errorObject);
                }

                // Rate limiting delay
                if (_config.RateLimitDelayMs > 0)
                {
                    await Task.Delay(_config.RateLimitDelayMs);
                }

                var response = await HttpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    errorObject.Flag = Errors.Failed;
                    errorObject.Message = $"Pinterest API request failed: {response.StatusCode} - {jsonContent}";
                    return (null, errorObject);
                }

                dataTable = ParseJsonToDataTable(jsonContent, entityName);
                errorObject.Flag = Errors.Ok;
                errorObject.Message = $"Successfully retrieved {entityName} data";
            }
            catch (Exception ex)
            {
                errorObject.Flag = Errors.Failed;
                errorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
            }

            return (dataTable, errorObject);
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var entityStructure = Entities.ContainsKey(entityName.ToLower()) ? Entities[entityName.ToLower()] : null;

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
                if (entityStructure != null)
                {
                    foreach (var field in entityStructure.Fields)
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




















    }
}
