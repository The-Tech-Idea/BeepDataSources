using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.Front
{
    /// <summary>
    /// Configuration class for Front data source
    /// </summary>
    public class FrontConfig
    {
        /// <summary>
        /// Front API Token
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// API version for Front API (default: v1.0)
        /// </summary>
        public string ApiVersion { get; set; } = "v1.0";

        /// <summary>
        /// Base URL for Front API
        /// </summary>
        public string BaseUrl => $"https://api2.frontapp.com/{ApiVersion}";

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
    /// Front data source implementation for Beep framework
    /// Supports Front REST API v1.0
    /// </summary>
    public class FrontDataSource : IDataSource
    {
        private readonly FrontConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for FrontDataSource
        /// </summary>
        /// <param name="config">Front configuration</param>
        public FrontDataSource(FrontConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for FrontDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ApiToken=xxx;ApiVersion=xxx</param>
        public FrontDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into FrontConfig
        /// </summary>
        private FrontConfig ParseConnectionString(string connectionString)
        {
            var config = new FrontConfig();
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
                        case "apitoken":
                            config.ApiToken = value;
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
        /// Initialize entity metadata for Front entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Conversations
            metadata["conversations"] = new EntityMetadata
            {
                EntityName = "conversations",
                DisplayName = "Conversations",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Conversation ID" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "priority", Type = "string", DisplayName = "Priority" },
                    new EntityField { Name = "assignee", Type = "string", DisplayName = "Assignee" },
                    new EntityField { Name = "recipient", Type = "string", DisplayName = "Recipient" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "wait_time", Type = "integer", DisplayName = "Wait Time" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "is_locked", Type = "boolean", DisplayName = "Is Locked" },
                    new EntityField { Name = "is_spam", Type = "boolean", DisplayName = "Is Spam" },
                    new EntityField { Name = "is_archived", Type = "boolean", DisplayName = "Is Archived" },
                    new EntityField { Name = "channel", Type = "string", DisplayName = "Channel" },
                    new EntityField { Name = "inbox", Type = "string", DisplayName = "Inbox" },
                    new EntityField { Name = "last_message", Type = "string", DisplayName = "Last Message" },
                    new EntityField { Name = "last_message_at", Type = "datetime", DisplayName = "Last Message At" },
                    new EntityField { Name = "message_count", Type = "integer", DisplayName = "Message Count" },
                    new EntityField { Name = "unread_count", Type = "integer", DisplayName = "Unread Count" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" }
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
                    new EntityField { Name = "conversation", Type = "string", DisplayName = "Conversation ID" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "is_inbound", Type = "boolean", DisplayName = "Is Inbound" },
                    new EntityField { Name = "is_draft", Type = "boolean", DisplayName = "Is Draft" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "is_archived", Type = "boolean", DisplayName = "Is Archived" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "blurb", Type = "string", DisplayName = "Blurb" },
                    new EntityField { Name = "body", Type = "string", DisplayName = "Body" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Text" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author" },
                    new EntityField { Name = "recipients", Type = "string", DisplayName = "Recipients" },
                    new EntityField { Name = "cc", Type = "string", DisplayName = "CC" },
                    new EntityField { Name = "bcc", Type = "string", DisplayName = "BCC" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "metadata", Type = "string", DisplayName = "Metadata" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Contacts
            metadata["contacts"] = new EntityMetadata
            {
                EntityName = "contacts",
                DisplayName = "Contacts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Contact ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "first_name", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "last_name", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "avatar_url", Type = "string", DisplayName = "Avatar URL" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "is_spam", Type = "boolean", DisplayName = "Is Spam" },
                    new EntityField { Name = "links", Type = "string", DisplayName = "Links" },
                    new EntityField { Name = "groups", Type = "string", DisplayName = "Groups" },
                    new EntityField { Name = "handles", Type = "string", DisplayName = "Handles" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Inboxes
            metadata["inboxes"] = new EntityMetadata
            {
                EntityName = "inboxes",
                DisplayName = "Inboxes",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Inbox ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "address", Type = "string", DisplayName = "Address" },
                    new EntityField { Name = "send_as", Type = "string", DisplayName = "Send As" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "teammates", Type = "string", DisplayName = "Teammates" },
                    new EntityField { Name = "rules", Type = "string", DisplayName = "Rules" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Tags
            metadata["tags"] = new EntityMetadata
            {
                EntityName = "tags",
                DisplayName = "Tags",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Tag ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "color", Type = "string", DisplayName = "Color" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "highlight", Type = "string", DisplayName = "Highlight" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "child_tags", Type = "string", DisplayName = "Child Tags" },
                    new EntityField { Name = "parent_tag", Type = "string", DisplayName = "Parent Tag" }
                }
            };

            // Rules
            metadata["rules"] = new EntityMetadata
            {
                EntityName = "rules",
                DisplayName = "Rules",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Rule ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "is_enabled", Type = "boolean", DisplayName = "Is Enabled" },
                    new EntityField { Name = "is_private", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "actions", Type = "string", DisplayName = "Actions" },
                    new EntityField { Name = "conditions", Type = "string", DisplayName = "Conditions" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "folder", Type = "string", DisplayName = "Folder" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "metric", Type = "string", IsPrimaryKey = true, DisplayName = "Metric" },
                    new EntityField { Name = "value", Type = "integer", DisplayName = "Value" },
                    new EntityField { Name = "date", Type = "datetime", DisplayName = "Date" },
                    new EntityField { Name = "inbox", Type = "string", DisplayName = "Inbox" },
                    new EntityField { Name = "teammate", Type = "string", DisplayName = "Teammate" },
                    new EntityField { Name = "tag", Type = "string", DisplayName = "Tag" },
                    new EntityField { Name = "channel", Type = "string", DisplayName = "Channel" },
                    new EntityField { Name = "period", Type = "string", DisplayName = "Period" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Front API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ApiToken))
                {
                    throw new InvalidOperationException("API Token is required for Front connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiToken}");

                // Test connection by getting inboxes
                var testUrl = $"{_config.BaseUrl}/inboxes";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Front API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Front API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Front API
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
        /// Get data from Front API
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
                    case "conversations":
                        var conversationId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var conversationStatus = parameters.ContainsKey("status") ? parameters["status"].ToString() : "";
                        var conversationInbox = parameters.ContainsKey("inbox") ? parameters["inbox"].ToString() : "";

                        if (!string.IsNullOrEmpty(conversationId))
                        {
                            url = $"{_config.BaseUrl}/conversations/{conversationId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(conversationStatus)) queryParams.Add($"q[statuses][]={conversationStatus}");
                            if (!string.IsNullOrEmpty(conversationInbox)) queryParams.Add($"q[inbox_id]={conversationInbox}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/conversations{queryString}";
                        }
                        break;

                    case "messages":
                        var conversationIdForMessages = parameters.ContainsKey("conversation_id") ? parameters["conversation_id"].ToString() : "";
                        if (string.IsNullOrEmpty(conversationIdForMessages))
                        {
                            throw new ArgumentException("conversation_id parameter is required for messages");
                        }
                        url = $"{_config.BaseUrl}/conversations/{conversationIdForMessages}/messages";
                        break;

                    case "contacts":
                        var contactId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(contactId) ? $"{_config.BaseUrl}/contacts" : $"{_config.BaseUrl}/contacts/{contactId}";
                        break;

                    case "inboxes":
                        var inboxId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(inboxId) ? $"{_config.BaseUrl}/inboxes" : $"{_config.BaseUrl}/inboxes/{inboxId}";
                        break;

                    case "tags":
                        var tagId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(tagId) ? $"{_config.BaseUrl}/tags" : $"{_config.BaseUrl}/tags/{tagId}";
                        break;

                    case "rules":
                        var ruleId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(ruleId) ? $"{_config.BaseUrl}/rules" : $"{_config.BaseUrl}/rules/{ruleId}";
                        break;

                    case "analytics":
                        var metric = parameters.ContainsKey("metric") ? parameters["metric"].ToString() : "conversations_count";
                        var startDate = parameters.ContainsKey("start_date") ? parameters["start_date"].ToString() : "";
                        var endDate = parameters.ContainsKey("end_date") ? parameters["end_date"].ToString() : "";

                        var queryParams = new List<string>();
                        queryParams.Add($"metric={metric}");
                        if (!string.IsNullOrEmpty(startDate)) queryParams.Add($"start={startDate}");
                        if (!string.IsNullOrEmpty(endDate)) queryParams.Add($"end={endDate}");

                        url = $"{_config.BaseUrl}/analytics?" + string.Join("&", queryParams);
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
                    throw new Exception($"Front API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Front API response structure
                JsonElement dataElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
                }
                else if (root.TryGetProperty("_results", out var resultsElement))
                {
                    dataElement = resultsElement;
                }
                else if (root.TryGetProperty("data", out var dataProperty))
                {
                    dataElement = dataProperty;
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
            return new List<string> { "conversations", "messages", "contacts", "inboxes", "tags", "rules", "analytics" };
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
        /// Insert data (limited support for Front API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Front API");
        }

        /// <summary>
        /// Update data (limited support for Front API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Front API");
        }

        /// <summary>
        /// Delete data (limited support for Front API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Front API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Front, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Front";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Front Data Source";

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
