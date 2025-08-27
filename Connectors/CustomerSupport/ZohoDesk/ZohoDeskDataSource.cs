using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.ZohoDesk
{
    /// <summary>
    /// Configuration class for Zoho Desk data source
    /// </summary>
    public class ZohoDeskConfig
    {
        /// <summary>
        /// Zoho Desk Portal Name
        /// </summary>
        public string PortalName { get; set; } = string.Empty;

        /// <summary>
        /// Zoho Desk Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Zoho Desk Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Zoho Desk Access Token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Zoho Desk Refresh Token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// API version for Zoho Desk API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Zoho Desk API
        /// </summary>
        public string BaseUrl => $"https://desk.zoho.com/api/{ApiVersion}";

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
    /// Zoho Desk data source implementation for Beep framework
    /// Supports Zoho Desk REST API v1
    /// </summary>
    public class ZohoDeskDataSource : IDataSource
    {
        private readonly ZohoDeskConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for ZohoDeskDataSource
        /// </summary>
        /// <param name="config">Zoho Desk configuration</param>
        public ZohoDeskDataSource(ZohoDeskConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for ZohoDeskDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: PortalName=xxx;ClientId=xxx;ClientSecret=xxx;AccessToken=xxx;RefreshToken=xxx</param>
        public ZohoDeskDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into ZohoDeskConfig
        /// </summary>
        private ZohoDeskConfig ParseConnectionString(string connectionString)
        {
            var config = new ZohoDeskConfig();
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
                        case "portalname":
                            config.PortalName = value;
                            break;
                        case "clientid":
                            config.ClientId = value;
                            break;
                        case "clientsecret":
                            config.ClientSecret = value;
                            break;
                        case "accesstoken":
                            config.AccessToken = value;
                            break;
                        case "refreshtoken":
                            config.RefreshToken = value;
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
        /// Initialize entity metadata for Zoho Desk entities
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
                    new EntityField { Name = "ticketNumber", Type = "string", DisplayName = "Ticket Number" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "priority", Type = "string", DisplayName = "Priority" },
                    new EntityField { Name = "classification", Type = "string", DisplayName = "Classification" },
                    new EntityField { Name = "channel", Type = "string", DisplayName = "Channel" },
                    new EntityField { Name = "assigneeId", Type = "string", DisplayName = "Assignee ID" },
                    new EntityField { Name = "departmentId", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "contactId", Type = "string", DisplayName = "Contact ID" },
                    new EntityField { Name = "accountId", Type = "string", DisplayName = "Account ID" },
                    new EntityField { Name = "productId", Type = "string", DisplayName = "Product ID" },
                    new EntityField { Name = "layoutId", Type = "string", DisplayName = "Layout ID" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "closedTime", Type = "datetime", DisplayName = "Closed Time" },
                    new EntityField { Name = "dueDate", Type = "datetime", DisplayName = "Due Date" },
                    new EntityField { Name = "responseDueDate", Type = "datetime", DisplayName = "Response Due Date" },
                    new EntityField { Name = "isOverdue", Type = "boolean", DisplayName = "Is Overdue" },
                    new EntityField { Name = "isSpam", Type = "boolean", DisplayName = "Is Spam" },
                    new EntityField { Name = "isArchived", Type = "boolean", DisplayName = "Is Archived" },
                    new EntityField { Name = "isDeleted", Type = "boolean", DisplayName = "Is Deleted" },
                    new EntityField { Name = "threadCount", Type = "integer", DisplayName = "Thread Count" },
                    new EntityField { Name = "commentCount", Type = "integer", DisplayName = "Comment Count" },
                    new EntityField { Name = "taskCount", Type = "integer", DisplayName = "Task Count" },
                    new EntityField { Name = "approvalCount", Type = "integer", DisplayName = "Approval Count" },
                    new EntityField { Name = "timeEntryCount", Type = "integer", DisplayName = "Time Entry Count" },
                    new EntityField { Name = "customerResponseTime", Type = "datetime", DisplayName = "Customer Response Time" },
                    new EntityField { Name = "firstReplyTime", Type = "datetime", DisplayName = "First Reply Time" },
                    new EntityField { Name = "lastActivity", Type = "datetime", DisplayName = "Last Activity" }
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
                    new EntityField { Name = "firstName", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastName", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "fullName", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "secondaryEmail", Type = "string", DisplayName = "Secondary Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "mobile", Type = "string", DisplayName = "Mobile" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "accountId", Type = "string", DisplayName = "Account ID" },
                    new EntityField { Name = "ownerId", Type = "string", DisplayName = "Owner ID" },
                    new EntityField { Name = "photoURL", Type = "string", DisplayName = "Photo URL" },
                    new EntityField { Name = "timeZone", Type = "string", DisplayName = "Time Zone" },
                    new EntityField { Name = "language", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "locale", Type = "string", DisplayName = "Locale" },
                    new EntityField { Name = "about", Type = "string", DisplayName = "About" },
                    new EntityField { Name = "facebook", Type = "string", DisplayName = "Facebook" },
                    new EntityField { Name = "twitter", Type = "string", DisplayName = "Twitter" },
                    new EntityField { Name = "street", Type = "string", DisplayName = "Street" },
                    new EntityField { Name = "city", Type = "string", DisplayName = "City" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "zip", Type = "string", DisplayName = "Zip" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "lastLoginTime", Type = "datetime", DisplayName = "Last Login Time" },
                    new EntityField { Name = "isConfirmed", Type = "boolean", DisplayName = "Is Confirmed" },
                    new EntityField { Name = "isDeleted", Type = "boolean", DisplayName = "Is Deleted" },
                    new EntityField { Name = "zohoCRMContact", Type = "string", DisplayName = "Zoho CRM Contact" },
                    new EntityField { Name = "layoutId", Type = "string", DisplayName = "Layout ID" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Accounts
            metadata["accounts"] = new EntityMetadata
            {
                EntityName = "accounts",
                DisplayName = "Accounts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Account ID" },
                    new EntityField { Name = "accountName", Type = "string", DisplayName = "Account Name" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "industry", Type = "string", DisplayName = "Industry" },
                    new EntityField { Name = "annualRevenue", Type = "decimal", DisplayName = "Annual Revenue" },
                    new EntityField { Name = "numberOfEmployees", Type = "integer", DisplayName = "Number of Employees" },
                    new EntityField { Name = "street", Type = "string", DisplayName = "Street" },
                    new EntityField { Name = "city", Type = "string", DisplayName = "City" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "zip", Type = "string", DisplayName = "Zip" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "ownerId", Type = "string", DisplayName = "Owner ID" },
                    new EntityField { Name = "layoutId", Type = "string", DisplayName = "Layout ID" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "zohoCRMAccount", Type = "string", DisplayName = "Zoho CRM Account" }
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
                    new EntityField { Name = "firstName", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastName", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "fullName", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "mobile", Type = "string", DisplayName = "Mobile" },
                    new EntityField { Name = "photoURL", Type = "string", DisplayName = "Photo URL" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "roleId", Type = "string", DisplayName = "Role ID" },
                    new EntityField { Name = "signature", Type = "string", DisplayName = "Signature" },
                    new EntityField { Name = "zuid", Type = "string", DisplayName = "ZUID" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "lastLoginTime", Type = "datetime", DisplayName = "Last Login Time" },
                    new EntityField { Name = "isConfirmed", Type = "boolean", DisplayName = "Is Confirmed" },
                    new EntityField { Name = "isDeleted", Type = "boolean", DisplayName = "Is Deleted" },
                    new EntityField { Name = "isOnline", Type = "boolean", DisplayName = "Is Online" },
                    new EntityField { Name = "countryLocale", Type = "string", DisplayName = "Country Locale" },
                    new EntityField { Name = "language", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "timeZone", Type = "string", DisplayName = "Time Zone" },
                    new EntityField { Name = "locale", Type = "string", DisplayName = "Locale" },
                    new EntityField { Name = "aboutMe", Type = "string", DisplayName = "About Me" },
                    new EntityField { Name = "reportingTo", Type = "string", DisplayName = "Reporting To" },
                    new EntityField { Name = "departmentId", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "associatedDepartmentIds", Type = "string", DisplayName = "Associated Department IDs" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" }
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
                    new EntityField { Name = "isVisibleInCustomerPortal", Type = "boolean", DisplayName = "Is Visible In Customer Portal" },
                    new EntityField { Name = "isDefault", Type = "boolean", DisplayName = "Is Default" },
                    new EntityField { Name = "layoutId", Type = "string", DisplayName = "Layout ID" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "associatedAgentIds", Type = "string", DisplayName = "Associated Agent IDs" },
                    new EntityField { Name = "headId", Type = "string", DisplayName = "Head ID" },
                    new EntityField { Name = "chatStatus", Type = "string", DisplayName = "Chat Status" },
                    new EntityField { Name = "isEnabled", Type = "boolean", DisplayName = "Is Enabled" }
                }
            };

            // Comments
            metadata["comments"] = new EntityMetadata
            {
                EntityName = "comments",
                DisplayName = "Comments",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Comment ID" },
                    new EntityField { Name = "content", Type = "string", DisplayName = "Content" },
                    new EntityField { Name = "contentType", Type = "string", DisplayName = "Content Type" },
                    new EntityField { Name = "isPublic", Type = "boolean", DisplayName = "Is Public" },
                    new EntityField { Name = "authorId", Type = "string", DisplayName = "Author ID" },
                    new EntityField { Name = "authorType", Type = "string", DisplayName = "Author Type" },
                    new EntityField { Name = "ticketId", Type = "string", DisplayName = "Ticket ID" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "mentions", Type = "string", DisplayName = "Mentions" },
                    new EntityField { Name = "isDeleted", Type = "boolean", DisplayName = "Is Deleted" }
                }
            };

            // Tasks
            metadata["tasks"] = new EntityMetadata
            {
                EntityName = "tasks",
                DisplayName = "Tasks",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Task ID" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "priority", Type = "string", DisplayName = "Priority" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "ownerId", Type = "string", DisplayName = "Owner ID" },
                    new EntityField { Name = "assigneeId", Type = "string", DisplayName = "Assignee ID" },
                    new EntityField { Name = "departmentId", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "ticketId", Type = "string", DisplayName = "Ticket ID" },
                    new EntityField { Name = "dueDate", Type = "datetime", DisplayName = "Due Date" },
                    new EntityField { Name = "completedTime", Type = "datetime", DisplayName = "Completed Time" },
                    new EntityField { Name = "createdTime", Type = "datetime", DisplayName = "Created Time" },
                    new EntityField { Name = "modifiedTime", Type = "datetime", DisplayName = "Modified Time" },
                    new EntityField { Name = "percentage", Type = "integer", DisplayName = "Percentage" },
                    new EntityField { Name = "isDeleted", Type = "boolean", DisplayName = "Is Deleted" },
                    new EntityField { Name = "customFields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Zoho Desk API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.PortalName))
                {
                    throw new InvalidOperationException("Portal name is required for Zoho Desk connection");
                }

                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    throw new InvalidOperationException("Access token is required for Zoho Desk connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", _config.AccessToken);

                // Test connection by getting current user
                var testUrl = $"{_config.BaseUrl}/myinfo";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Zoho Desk API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Zoho Desk API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Zoho Desk API
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
        /// Get data from Zoho Desk API
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
                        var ticketPriority = parameters.ContainsKey("priority") ? parameters["priority"].ToString() : "";
                        var ticketDepartment = parameters.ContainsKey("departmentId") ? parameters["departmentId"].ToString() : "";

                        if (!string.IsNullOrEmpty(ticketId))
                        {
                            url = $"{_config.BaseUrl}/tickets/{ticketId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(ticketStatus)) queryParams.Add($"status={ticketStatus}");
                            if (!string.IsNullOrEmpty(ticketPriority)) queryParams.Add($"priority={ticketPriority}");
                            if (!string.IsNullOrEmpty(ticketDepartment)) queryParams.Add($"departmentId={ticketDepartment}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/tickets{queryString}";
                        }
                        break;

                    case "contacts":
                        var contactId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(contactId) ? $"{_config.BaseUrl}/contacts" : $"{_config.BaseUrl}/contacts/{contactId}";
                        break;

                    case "accounts":
                        var accountId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(accountId) ? $"{_config.BaseUrl}/accounts" : $"{_config.BaseUrl}/accounts/{accountId}";
                        break;

                    case "agents":
                        var agentId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(agentId) ? $"{_config.BaseUrl}/agents" : $"{_config.BaseUrl}/agents/{agentId}";
                        break;

                    case "departments":
                        var departmentId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(departmentId) ? $"{_config.BaseUrl}/departments" : $"{_config.BaseUrl}/departments/{departmentId}";
                        break;

                    case "comments":
                        var ticketIdForComments = parameters.ContainsKey("ticket_id") ? parameters["ticket_id"].ToString() : "";
                        if (string.IsNullOrEmpty(ticketIdForComments))
                        {
                            throw new ArgumentException("ticket_id parameter is required for comments");
                        }
                        url = $"{_config.BaseUrl}/tickets/{ticketIdForComments}/comments";
                        break;

                    case "tasks":
                        var taskId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var taskTicket = parameters.ContainsKey("ticket_id") ? parameters["ticket_id"].ToString() : "";
                        url = string.IsNullOrEmpty(taskId) ? $"{_config.BaseUrl}/tasks" : $"{_config.BaseUrl}/tasks/{taskId}";
                        if (!string.IsNullOrEmpty(taskTicket) && string.IsNullOrEmpty(taskId))
                        {
                            url += $"?ticketId={taskTicket}";
                        }
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
                    throw new Exception($"Zoho Desk API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Zoho Desk API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("data", out var dataArray))
                {
                    dataElement = dataArray;
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
            return new List<string> { "tickets", "contacts", "accounts", "agents", "departments", "comments", "tasks" };
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
        /// Insert data (limited support for Zoho Desk API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Zoho Desk API");
        }

        /// <summary>
        /// Update data (limited support for Zoho Desk API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Zoho Desk API");
        }

        /// <summary>
        /// Delete data (limited support for Zoho Desk API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Zoho Desk API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Zoho Desk, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "ZohoDesk";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Zoho Desk Data Source";

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
