using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Pinterest.Models;

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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pinterest)]
    public class PinterestDataSource : WebAPIDataSource
    {
        // Fixed, supported entities
        private static readonly List<string> KnownEntities = new()
        {
            "PinterestUser",
            "PinterestBoard", 
            "PinterestPin",
            "PinterestAnalytics"
        };

    /// <summary>
    /// Constructor for PinterestDataSource
    /// </summary>
    public PinterestDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(datasourcename, logger, editor, type, errors)
    {
        // Ensure WebAPI props (Url/Auth) exist (configure outside this class)
        if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

        // Set base URL for Pinterest API
        if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            webApiProps.ConnectionString = "https://api.pinterest.com/v5";

        // Register fixed entities
        EntitiesNames = KnownEntities.ToList();
        Entities = EntitiesNames
            .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
            .ToList();
    }



    /// <summary>
    /// Get data from Pinterest API
    /// </summary>
    public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                // Build endpoint based on entity name
                string endpoint = EntityName.ToLower() switch
                {
                    "user" => "user_account",
                    "boards" => "boards",
                    "board" => $"boards/{GetFilterValue(Filter, "board_id", "")}",
                    "pins" => $"boards/{GetFilterValue(Filter, "board_id", "")}/pins",
                    "pin" => $"pins/{GetFilterValue(Filter, "pin_id", "")}",
                    "userpins" => "pins",
                    "analytics" => $"pins/{GetFilterValue(Filter, "pin_id", "")}/analytics",
                    "following" => "user_account/following",
                    "followers" => "user_account/followers",
                    _ => throw new ArgumentException($"Unsupported entity: {EntityName}")
                };

                var response = await GetAsync(endpoint);
                string json = await response.Content.ReadAsStringAsync();

                // Deserialize based on entity type
                return EntityName.ToLower() switch
                {
                    "user" => new[] { JsonSerializer.Deserialize<PinterestUser>(json) },
                    "boards" => JsonSerializer.Deserialize<PinterestResponse<PinterestBoard>>(json)?.Data ?? new List<PinterestBoard>(),
                    "board" => new[] { JsonSerializer.Deserialize<PinterestBoard>(json) },
                    "pins" or "userpins" => JsonSerializer.Deserialize<PinterestResponse<PinterestPin>>(json)?.Data ?? new List<PinterestPin>(),
                    "pin" => new[] { JsonSerializer.Deserialize<PinterestPin>(json) },
                    "analytics" => new[] { JsonSerializer.Deserialize<PinterestAnalytics>(json) },
                    "following" => JsonSerializer.Deserialize<PinterestResponse<PinterestUser>>(json)?.Data ?? new List<PinterestUser>(),
                    "followers" => JsonSerializer.Deserialize<PinterestResponse<PinterestUser>>(json)?.Data ?? new List<PinterestUser>(),
                    _ => throw new ArgumentException($"Unsupported entity: {EntityName}")
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get {EntityName} data: {ex.Message}", ex);
            }
        }

        private string GetFilterValue(List<AppFilter> filters, string fieldName, string defaultValue)
        {
            var filter = filters?.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            return filter?.FilterValue?.ToString() ?? defaultValue;
        }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var entityStructure = Entities.FirstOrDefault(e => e.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));

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
                        dataTable.Columns.Add(field.fieldname, GetFieldType(field.fieldtype));
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

// ---------------- Specific Pinterest Methods ----------------

        /// <summary>
        /// Gets user profile information
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestUser", PointType = EnumPointType.Function, Name = "GetUser", Caption = "Get Pinterest User", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestUser>")]
        public async Task<IEnumerable<PinterestUser>> GetUser(string userId)
        {
            string endpoint = $"users/{userId}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<PinterestUser>(json);
            return user != null ? new List<PinterestUser> { user } : new List<PinterestUser>();
        }

        /// <summary>
        /// Gets user boards
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestBoard", PointType = EnumPointType.Function, Name = "GetBoards", Caption = "Get Pinterest Boards", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestBoard>")]
        public async Task<IEnumerable<PinterestBoard>> GetBoards(string userId, int pageSize = 25)
        {
            string endpoint = $"users/{userId}/boards?page_size={pageSize}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var boardsResponse = JsonSerializer.Deserialize<PinterestResponse<PinterestBoard>>(json);
            return boardsResponse?.Data ?? new List<PinterestBoard>();
        }

        /// <summary>
        /// Gets pins from a board
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestPin", PointType = EnumPointType.Function, Name = "GetBoardPins", Caption = "Get Pinterest Board Pins", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestPin>")]
        public async Task<IEnumerable<PinterestPin>> GetBoardPins(string boardId, int pageSize = 25)
        {
            string endpoint = $"boards/{boardId}/pins?page_size={pageSize}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var pinsResponse = JsonSerializer.Deserialize<PinterestResponse<PinterestPin>>(json);
            return pinsResponse?.Data ?? new List<PinterestPin>();
        }

        /// <summary>
        /// Gets user pins
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestPin", PointType = EnumPointType.Function, Name = "GetUserPins", Caption = "Get Pinterest User Pins", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestPin>")]
        public async Task<IEnumerable<PinterestPin>> GetUserPins(string userId, int pageSize = 25)
        {
            string endpoint = $"users/{userId}/pins?page_size={pageSize}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var pinsResponse = JsonSerializer.Deserialize<PinterestResponse<PinterestPin>>(json);
            return pinsResponse?.Data ?? new List<PinterestPin>();
        }

        /// <summary>
        /// Gets pin details
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestPin", PointType = EnumPointType.Function, Name = "GetPin", Caption = "Get Pinterest Pin Details", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestPin>")]
        public async Task<IEnumerable<PinterestPin>> GetPin(string pinId)
        {
            string endpoint = $"pins/{pinId}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var pin = JsonSerializer.Deserialize<PinterestPin>(json);
            return pin != null ? new List<PinterestPin> { pin } : new List<PinterestPin>();
        }

        /// <summary>
        /// Gets user analytics
        /// </summary>
        [CommandAttribute(ObjectType = "PinterestAnalytics", PointType = EnumPointType.Function, Name = "GetAnalytics", Caption = "Get Pinterest User Analytics", ClassName = "PinterestDataSource", misc = "ReturnType: IEnumerable<PinterestAnalytics>")]
        public async Task<IEnumerable<PinterestAnalytics>> GetAnalytics(string userId, string startDate, string endDate)
        {
            string endpoint = $"users/{userId}/analytics?start_date={startDate}&end_date={endDate}";
            var response = await GetAsync(endpoint);
            string json = await response.Content.ReadAsStringAsync();
            var analytics = JsonSerializer.Deserialize<PinterestAnalytics>(json);
            return analytics != null ? new List<PinterestAnalytics> { analytics } : new List<PinterestAnalytics>();
        }

        // POST methods for creating entities
        [CommandAttribute(ObjectType = "PinterestPin", PointType = EnumPointType.Function, Name = "CreatePin", Caption = "Create Pinterest Pin", ClassName = "PinterestDataSource", misc = "ReturnType: PinterestPin")]
        public async Task<PinterestPin> CreatePinAsync(PinterestPin pin)
        {
            string endpoint = "pins";
            var response = await PostAsync(endpoint, pin);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PinterestPin>(json);
        }

        // PUT methods for updating entities
        [CommandAttribute(ObjectType = "PinterestPin", PointType = EnumPointType.Function, Name = "UpdatePin", Caption = "Update Pinterest Pin", ClassName = "PinterestDataSource", misc = "ReturnType: PinterestPin")]
        public async Task<PinterestPin> UpdatePinAsync(string pinId, PinterestPin pin)
        {
            string endpoint = $"pins/{pinId}";
            var response = await PutAsync(endpoint, pin);
            if (response == null) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PinterestPin>(json);
        }
    }
}
