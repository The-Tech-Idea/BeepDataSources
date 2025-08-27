using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.HelpScout
{
    /// <summary>
    /// Configuration class for HelpScout data source
    /// </summary>
    public class HelpScoutConfig
    {
        /// <summary>
        /// HelpScout API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API version for HelpScout API (default: v2)
        /// </summary>
        public string ApiVersion { get; set; } = "v2";

        /// <summary>
        /// Base URL for HelpScout API
        /// </summary>
        public string BaseUrl => $"https://api.helpscout.net/{ApiVersion}";

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
    /// HelpScout data source implementation for Beep framework
    /// Supports HelpScout REST API v2
    /// </summary>
    public class HelpScoutDataSource : IDataSource
    {
        private readonly HelpScoutConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for HelpScoutDataSource
        /// </summary>
        /// <param name="config">HelpScout configuration</param>
        public HelpScoutDataSource(HelpScoutConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for HelpScoutDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: ApiKey=xxx;ApiVersion=xxx</param>
        public HelpScoutDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into HelpScoutConfig
        /// </summary>
        private HelpScoutConfig ParseConnectionString(string connectionString)
        {
            var config = new HelpScoutConfig();
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
        /// Initialize entity metadata for HelpScout entities
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
                    new EntityField { Name = "number", Type = "string", DisplayName = "Number" },
                    new EntityField { Name = "threads", Type = "string", DisplayName = "Threads" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "folder", Type = "string", DisplayName = "Folder" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "preview", Type = "string", DisplayName = "Preview" },
                    new EntityField { Name = "customer", Type = "string", DisplayName = "Customer" },
                    new EntityField { Name = "customerWaitingSince", Type = "string", DisplayName = "Customer Waiting Since" },
                    new EntityField { Name = "source", Type = "string", DisplayName = "Source" },
                    new EntityField { Name = "owner", Type = "string", DisplayName = "Owner" },
                    new EntityField { Name = "mailbox", Type = "string", DisplayName = "Mailbox" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "cc", Type = "string", DisplayName = "CC" },
                    new EntityField { Name = "bcc", Type = "string", DisplayName = "BCC" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" },
                    new EntityField { Name = "closedAt", Type = "datetime", DisplayName = "Closed At" },
                    new EntityField { Name = "closedBy", Type = "string", DisplayName = "Closed By" },
                    new EntityField { Name = "userModifiedAt", Type = "datetime", DisplayName = "User Modified At" },
                    new EntityField { Name = "primaryCustomer", Type = "string", DisplayName = "Primary Customer" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" }
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
                    new EntityField { Name = "firstName", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastName", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "fullName", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "company", Type = "string", DisplayName = "Company" },
                    new EntityField { Name = "jobTitle", Type = "string", DisplayName = "Job Title" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "background", Type = "string", DisplayName = "Background" },
                    new EntityField { Name = "address", Type = "string", DisplayName = "Address" },
                    new EntityField { Name = "socialProfiles", Type = "string", DisplayName = "Social Profiles" },
                    new EntityField { Name = "chats", Type = "string", DisplayName = "Chats" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "gender", Type = "string", DisplayName = "Gender" },
                    new EntityField { Name = "age", Type = "string", DisplayName = "Age" },
                    new EntityField { Name = "organization", Type = "string", DisplayName = "Organization" },
                    new EntityField { Name = "photoUrl", Type = "string", DisplayName = "Photo URL" },
                    new EntityField { Name = "photoType", Type = "string", DisplayName = "Photo Type" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" }
                }
            };

            // Mailboxes
            metadata["mailboxes"] = new EntityMetadata
            {
                EntityName = "mailboxes",
                DisplayName = "Mailboxes",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Mailbox ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "slug", Type = "string", DisplayName = "Slug" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "folders", Type = "string", DisplayName = "Folders" },
                    new EntityField { Name = "userFields", Type = "string", DisplayName = "User Fields" }
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
                    new EntityField { Name = "slug", Type = "string", DisplayName = "Slug" },
                    new EntityField { Name = "color", Type = "string", DisplayName = "Color" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" },
                    new EntityField { Name = "ticketCount", Type = "integer", DisplayName = "Ticket Count" }
                }
            };

            // Workflows
            metadata["workflows"] = new EntityMetadata
            {
                EntityName = "workflows",
                DisplayName = "Workflows",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Workflow ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "order", Type = "integer", DisplayName = "Order" },
                    new EntityField { Name = "actions", Type = "string", DisplayName = "Actions" },
                    new EntityField { Name = "conditions", Type = "string", DisplayName = "Conditions" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" },
                    new EntityField { Name = "mailboxId", Type = "string", DisplayName = "Mailbox ID" }
                }
            };

            // Reports
            metadata["reports"] = new EntityMetadata
            {
                EntityName = "reports",
                DisplayName = "Reports",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Report ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "parameters", Type = "string", DisplayName = "Parameters" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "modifiedAt", Type = "datetime", DisplayName = "Modified At" },
                    new EntityField { Name = "mailboxId", Type = "string", DisplayName = "Mailbox ID" },
                    new EntityField { Name = "userId", Type = "string", DisplayName = "User ID" },
                    new EntityField { Name = "isPublic", Type = "boolean", DisplayName = "Is Public" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to HelpScout API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("API Key is required for HelpScout connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);

                // Test connection by getting mailboxes
                var testUrl = $"{_config.BaseUrl}/mailboxes";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HelpScout API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to HelpScout API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from HelpScout API
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
        /// Get data from HelpScout API
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
                        var conversationMailbox = parameters.ContainsKey("mailbox") ? parameters["mailbox"].ToString() : "";
                        var conversationTag = parameters.ContainsKey("tag") ? parameters["tag"].ToString() : "";

                        if (!string.IsNullOrEmpty(conversationId))
                        {
                            url = $"{_config.BaseUrl}/conversations/{conversationId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(conversationStatus)) queryParams.Add($"status={conversationStatus}");
                            if (!string.IsNullOrEmpty(conversationMailbox)) queryParams.Add($"mailbox={conversationMailbox}");
                            if (!string.IsNullOrEmpty(conversationTag)) queryParams.Add($"tag={conversationTag}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/conversations{queryString}";
                        }
                        break;

                    case "customers":
                        var customerId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var customerEmail = parameters.ContainsKey("email") ? parameters["email"].ToString() : "";
                        url = string.IsNullOrEmpty(customerId) ? $"{_config.BaseUrl}/customers" : $"{_config.BaseUrl}/customers/{customerId}";
                        if (!string.IsNullOrEmpty(customerEmail) && string.IsNullOrEmpty(customerId))
                        {
                            url += $"?email={customerEmail}";
                        }
                        break;

                    case "mailboxes":
                        var mailboxId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(mailboxId) ? $"{_config.BaseUrl}/mailboxes" : $"{_config.BaseUrl}/mailboxes/{mailboxId}";
                        break;

                    case "tags":
                        var tagId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(tagId) ? $"{_config.BaseUrl}/tags" : $"{_config.BaseUrl}/tags/{tagId}";
                        break;

                    case "workflows":
                        var workflowId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var workflowMailbox = parameters.ContainsKey("mailbox") ? parameters["mailbox"].ToString() : "";
                        url = string.IsNullOrEmpty(workflowId) ? $"{_config.BaseUrl}/workflows" : $"{_config.BaseUrl}/workflows/{workflowId}";
                        if (!string.IsNullOrEmpty(workflowMailbox) && string.IsNullOrEmpty(workflowId))
                        {
                            url += $"?mailbox={workflowMailbox}";
                        }
                        break;

                    case "reports":
                        var reportId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(reportId) ? $"{_config.BaseUrl}/reports" : $"{_config.BaseUrl}/reports/{reportId}";
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
                    throw new Exception($"HelpScout API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle HelpScout API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("_embedded", out var embedded))
                {
                    if (embedded.TryGetProperty(entityName, out var entityArray))
                    {
                        dataElement = entityArray;
                    }
                    else
                    {
                        dataElement = embedded;
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    dataElement = root;
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
            return new List<string> { "conversations", "customers", "mailboxes", "tags", "workflows", "reports" };
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
        /// Insert data (limited support for HelpScout API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for HelpScout API");
        }

        /// <summary>
        /// Update data (limited support for HelpScout API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for HelpScout API");
        }

        /// <summary>
        /// Delete data (limited support for HelpScout API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for HelpScout API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For HelpScout, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "HelpScout";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "HelpScout Data Source";

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
