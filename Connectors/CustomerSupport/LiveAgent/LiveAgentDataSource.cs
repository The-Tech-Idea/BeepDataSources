using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.LiveAgent
{
    /// <summary>
    /// Configuration class for LiveAgent data source
    /// </summary>
    public class LiveAgentConfig
    {
        /// <summary>
        /// LiveAgent Domain (e.g., 'yourcompany' for yourcompany.ladesk.com)
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// LiveAgent API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API version for LiveAgent API (default: v3)
        /// </summary>
        public string ApiVersion { get; set; } = "v3";

        /// <summary>
        /// Base URL for LiveAgent API
        /// </summary>
        public string BaseUrl => $"https://{Domain}.ladesk.com/api/{ApiVersion}";

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
    }

    /// <summary>
    /// LiveAgent data source implementation for Beep framework
    /// Supports LiveAgent REST API v3
    /// </summary>
    public class LiveAgentDataSource : IDataSource
    {
        private readonly LiveAgentConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for LiveAgentDataSource
        /// </summary>
        /// <param name="config">LiveAgent configuration</param>
        public LiveAgentDataSource(LiveAgentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for LiveAgentDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: Domain=xxx;ApiKey=xxx;ApiVersion=xxx</param>
        public LiveAgentDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into LiveAgentConfig
        /// </summary>
        private LiveAgentConfig ParseConnectionString(string connectionString)
        {
            var config = new LiveAgentConfig();
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
                        case "domain":
                            config.Domain = value;
                            break;
                        case "apikey":
                            config.ApiKey = value;
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
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Initialize entity metadata for LiveAgent entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Tickets
            metadata["tickets"] = new EntityMetadata
            {
                EntityName = "tickets",
                DisplayName = "Tickets",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Ticket ID" },
                    new EntityField { Name = "code", Type = "string", DisplayName = "Ticket Code" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "priority", Type = "string", DisplayName = "Priority" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "departmentname", Type = "string", DisplayName = "Department Name" },
                    new EntityField { Name = "agentid", Type = "string", DisplayName = "Agent ID" },
                    new EntityField { Name = "agentname", Type = "string", DisplayName = "Agent Name" },
                    new EntityField { Name = "customerid", Type = "string", DisplayName = "Customer ID" },
                    new EntityField { Name = "customername", Type = "string", DisplayName = "Customer Name" },
                    new EntityField { Name = "customeremail", Type = "string", DisplayName = "Customer Email" },
                    new EntityField { Name = "channeltype", Type = "string", DisplayName = "Channel Type" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datelastactivity", Type = "datetime", DisplayName = "Date Last Activity" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "dateclosed", Type = "datetime", DisplayName = "Date Closed" },
                    new EntityField { Name = "lastmessage", Type = "string", DisplayName = "Last Message" },
                    new EntityField { Name = "lastmessagedate", Type = "datetime", DisplayName = "Last Message Date" },
                    new EntityField { Name = "lastmessageauthor", Type = "string", DisplayName = "Last Message Author" },
                    new EntityField { Name = "messagecount", Type = "integer", DisplayName = "Message Count" },
                    new EntityField { Name = "unreadmessagecount", Type = "integer", DisplayName = "Unread Message Count" },
                    new EntityField { Name = "customerwaiting", Type = "boolean", DisplayName = "Customer Waiting" },
                    new EntityField { Name = "customerwaitingtime", Type = "integer", DisplayName = "Customer Waiting Time" },
                    new EntityField { Name = "customerwaitingstarttime", Type = "datetime", DisplayName = "Customer Waiting Start Time" },
                    new EntityField { Name = "sla", Type = "string", DisplayName = "SLA" },
                    new EntityField { Name = "slaviolation", Type = "boolean", DisplayName = "SLA Violation" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Chats
            metadata["chats"] = new EntityMetadata
            {
                EntityName = "chats",
                DisplayName = "Chats",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Chat ID" },
                    new EntityField { Name = "code", Type = "string", DisplayName = "Chat Code" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "channeltype", Type = "string", DisplayName = "Channel Type" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "departmentname", Type = "string", DisplayName = "Department Name" },
                    new EntityField { Name = "agentid", Type = "string", DisplayName = "Agent ID" },
                    new EntityField { Name = "agentname", Type = "string", DisplayName = "Agent Name" },
                    new EntityField { Name = "customerid", Type = "string", DisplayName = "Customer ID" },
                    new EntityField { Name = "customername", Type = "string", DisplayName = "Customer Name" },
                    new EntityField { Name = "customeremail", Type = "string", DisplayName = "Customer Email" },
                    new EntityField { Name = "customerip", Type = "string", DisplayName = "Customer IP" },
                    new EntityField { Name = "customeruseragent", Type = "string", DisplayName = "Customer User Agent" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "datefinished", Type = "datetime", DisplayName = "Date Finished" },
                    new EntityField { Name = "waitingtime", Type = "integer", DisplayName = "Waiting Time" },
                    new EntityField { Name = "chattime", Type = "integer", DisplayName = "Chat Time" },
                    new EntityField { Name = "messagecount", Type = "integer", DisplayName = "Message Count" },
                    new EntityField { Name = "rating", Type = "integer", DisplayName = "Rating" },
                    new EntityField { Name = "ratingcomment", Type = "string", DisplayName = "Rating Comment" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Calls
            metadata["calls"] = new EntityMetadata
            {
                EntityName = "calls",
                DisplayName = "Calls",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Call ID" },
                    new EntityField { Name = "code", Type = "string", DisplayName = "Call Code" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "direction", Type = "string", DisplayName = "Direction" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "departmentname", Type = "string", DisplayName = "Department Name" },
                    new EntityField { Name = "agentid", Type = "string", DisplayName = "Agent ID" },
                    new EntityField { Name = "agentname", Type = "string", DisplayName = "Agent Name" },
                    new EntityField { Name = "customerid", Type = "string", DisplayName = "Customer ID" },
                    new EntityField { Name = "customername", Type = "string", DisplayName = "Customer Name" },
                    new EntityField { Name = "customerphone", Type = "string", DisplayName = "Customer Phone" },
                    new EntityField { Name = "callerid", Type = "string", DisplayName = "Caller ID" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "dateanswered", Type = "datetime", DisplayName = "Date Answered" },
                    new EntityField { Name = "datefinished", Type = "datetime", DisplayName = "Date Finished" },
                    new EntityField { Name = "waitingtime", Type = "integer", DisplayName = "Waiting Time" },
                    new EntityField { Name = "calltime", Type = "integer", DisplayName = "Call Time" },
                    new EntityField { Name = "recordingurl", Type = "string", DisplayName = "Recording URL" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Customers
            metadata["customers"] = new EntityMetadata
            {
                EntityName = "customers",
                DisplayName = "Customers",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Customer ID" },
                    new EntityField { Name = "firstname", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastname", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "fullname", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "company", Type = "string", DisplayName = "Company" },
                    new EntityField { Name = "city", Type = "string", DisplayName = "City" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "gender", Type = "string", DisplayName = "Gender" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "avatar", Type = "string", DisplayName = "Avatar" },
                    new EntityField { Name = "socialnetworks", Type = "string", DisplayName = "Social Networks" },
                    new EntityField { Name = "groups", Type = "string", DisplayName = "Groups" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Agents
            metadata["agents"] = new EntityMetadata
            {
                EntityName = "agents",
                DisplayName = "Agents",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Agent ID" },
                    new EntityField { Name = "firstname", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastname", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "fullname", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "departmentname", Type = "string", DisplayName = "Department Name" },
                    new EntityField { Name = "role", Type = "string", DisplayName = "Role" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "avatar", Type = "string", DisplayName = "Avatar" },
                    new EntityField { Name = "permissions", Type = "string", DisplayName = "Permissions" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Departments
            metadata["departments"] = new EntityMetadata
            {
                EntityName = "departments",
                DisplayName = "Departments",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Department ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "parentdepartmentid", Type = "string", DisplayName = "Parent Department ID" },
                    new EntityField { Name = "parentdepartmentname", Type = "string", DisplayName = "Parent Department Name" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "agentcount", Type = "integer", DisplayName = "Agent Count" },
                    new EntityField { Name = "onlineagentcount", Type = "integer", DisplayName = "Online Agent Count" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Messages
            metadata["messages"] = new EntityMetadata
            {
                EntityName = "messages",
                DisplayName = "Messages",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Message ID" },
                    new EntityField { Name = "conversationid", Type = "string", DisplayName = "Conversation ID" },
                    new EntityField { Name = "authorid", Type = "string", DisplayName = "Author ID" },
                    new EntityField { Name = "authorname", Type = "string", DisplayName = "Author Name" },
                    new EntityField { Name = "authoremail", Type = "string", DisplayName = "Author Email" },
                    new EntityField { Name = "authortype", Type = "string", DisplayName = "Author Type" },
                    new EntityField { Name = "channeltype", Type = "string", DisplayName = "Channel Type" },
                    new EntityField { Name = "messagetype", Type = "string", DisplayName = "Message Type" },
                    new EntityField { Name = "content", Type = "string", DisplayName = "Content" },
                    new EntityField { Name = "content_html", Type = "string", DisplayName = "Content HTML" },
                    new EntityField { Name = "content_text", Type = "string", DisplayName = "Content Text" },
                    new EntityField { Name = "datecreated", Type = "datetime", DisplayName = "Date Created" },
                    new EntityField { Name = "datechanged", Type = "datetime", DisplayName = "Date Changed" },
                    new EntityField { Name = "isprivate", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "isread", Type = "boolean", DisplayName = "Is Read" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to LiveAgent API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Domain))
                {
                    throw new InvalidOperationException("Domain is required for LiveAgent connection");
                }

                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("API Key is required for LiveAgent connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                _httpClient.DefaultRequestHeaders.Add("apikey", _config.ApiKey);

                // Test connection by getting departments
                var testUrl = $"{_config.BaseUrl}/departments";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"LiveAgent API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to LiveAgent API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from LiveAgent API
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
        /// Get data from LiveAgent API
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

                switch (entityName.ToLower())
                {
                    case "tickets":
                        var ticketId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var ticketStatus = parameters.ContainsKey("status") ? parameters["status"].ToString() : "";
                        var ticketDepartment = parameters.ContainsKey("department") ? parameters["department"].ToString() : "";

                        if (!string.IsNullOrEmpty(ticketId))
                        {
                            url = $"{_config.BaseUrl}/tickets/{ticketId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(ticketStatus)) queryParams.Add($"status={ticketStatus}");
                            if (!string.IsNullOrEmpty(ticketDepartment)) queryParams.Add($"departmentid={ticketDepartment}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/tickets{queryString}";
                        }
                        break;

                    case "chats":
                        var chatId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(chatId) ? $"{_config.BaseUrl}/chats" : $"{_config.BaseUrl}/chats/{chatId}";
                        break;

                    case "calls":
                        var callId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(callId) ? $"{_config.BaseUrl}/calls" : $"{_config.BaseUrl}/calls/{callId}";
                        break;

                    case "customers":
                        var customerId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(customerId) ? $"{_config.BaseUrl}/customers" : $"{_config.BaseUrl}/customers/{customerId}";
                        break;

                    case "agents":
                        var agentId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(agentId) ? $"{_config.BaseUrl}/agents" : $"{_config.BaseUrl}/agents/{agentId}";
                        break;

                    case "departments":
                        var departmentId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(departmentId) ? $"{_config.BaseUrl}/departments" : $"{_config.BaseUrl}/departments/{departmentId}";
                        break;

                    case "messages":
                        var conversationId = parameters.ContainsKey("conversation_id") ? parameters["conversation_id"].ToString() : "";
                        if (string.IsNullOrEmpty(conversationId))
                        {
                            throw new ArgumentException("conversation_id parameter is required for messages");
                        }
                        url = $"{_config.BaseUrl}/conversations/{conversationId}/messages";
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
                    throw new Exception($"LiveAgent API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle LiveAgent API response structure
                JsonElement dataElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
                }
                else if (root.TryGetProperty("response", out var responseElement))
                {
                    dataElement = responseElement;
                }
                else
                {
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
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Array)
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
                else if (dataElement.ValueKind == JsonValueKind.Object)
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
            return new List<string> { "tickets", "chats", "calls", "customers", "agents", "departments", "messages" };
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
        /// Insert data (limited support for LiveAgent API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for LiveAgent API");
        }

        /// <summary>
        /// Update data (limited support for LiveAgent API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for LiveAgent API");
        }

        /// <summary>
        /// Delete data (limited support for LiveAgent API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for LiveAgent API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For LiveAgent, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "LiveAgent";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "LiveAgent Data Source";

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
