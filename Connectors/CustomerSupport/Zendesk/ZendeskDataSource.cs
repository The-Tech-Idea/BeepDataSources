using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.Zendesk
{
    /// <summary>
    /// Configuration class for Zendesk data source
    /// </summary>
    public class ZendeskConfig
    {
        /// <summary>
        /// Zendesk subdomain (e.g., 'yourcompany' for yourcompany.zendesk.com)
        /// </summary>
        public string Subdomain { get; set; } = string.Empty;

        /// <summary>
        /// Zendesk API Token
        /// </summary>
        public string ApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Zendesk Username (for Basic Auth)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Zendesk Password (for Basic Auth)
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// API version for Zendesk API (default: v2)
        /// </summary>
        public string ApiVersion { get; set; } = "v2";

        /// <summary>
        /// Base URL for Zendesk API
        /// </summary>
        public string BaseUrl => $"https://{Subdomain}.zendesk.com/api/{ApiVersion}";

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
    /// Zendesk data source implementation for Beep framework
    /// Supports Zendesk REST API v2
    /// </summary>
    public class ZendeskDataSource : IDataSource
    {
        private readonly ZendeskConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for ZendeskDataSource
        /// </summary>
        /// <param name="config">Zendesk configuration</param>
        public ZendeskDataSource(ZendeskConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for ZendeskDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: Subdomain=xxx;ApiToken=xxx;Username=xxx;Password=xxx</param>
        public ZendeskDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into ZendeskConfig
        /// </summary>
        private ZendeskConfig ParseConnectionString(string connectionString)
        {
            var config = new ZendeskConfig();
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
                        case "subdomain":
                            config.Subdomain = value;
                            break;
                        case "apitoken":
                            config.ApiToken = value;
                            break;
                        case "username":
                            config.Username = value;
                            break;
                        case "password":
                            config.Password = value;
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
        /// Initialize entity metadata for Zendesk entities
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
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "external_id", Type = "string", DisplayName = "External ID" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "raw_subject", Type = "string", DisplayName = "Raw Subject" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "priority", Type = "string", DisplayName = "Priority" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "recipient", Type = "string", DisplayName = "Recipient" },
                    new EntityField { Name = "requester_id", Type = "string", DisplayName = "Requester ID" },
                    new EntityField { Name = "submitter_id", Type = "string", DisplayName = "Submitter ID" },
                    new EntityField { Name = "assignee_id", Type = "string", DisplayName = "Assignee ID" },
                    new EntityField { Name = "organization_id", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "group_id", Type = "string", DisplayName = "Group ID" },
                    new EntityField { Name = "collaborator_ids", Type = "string", DisplayName = "Collaborator IDs" },
                    new EntityField { Name = "follower_ids", Type = "string", DisplayName = "Follower IDs" },
                    new EntityField { Name = "email_cc_ids", Type = "string", DisplayName = "Email CC IDs" },
                    new EntityField { Name = "forum_topic_id", Type = "string", DisplayName = "Forum Topic ID" },
                    new EntityField { Name = "problem_id", Type = "string", DisplayName = "Problem ID" },
                    new EntityField { Name = "has_incidents", Type = "boolean", DisplayName = "Has Incidents" },
                    new EntityField { Name = "is_public", Type = "boolean", DisplayName = "Is Public" },
                    new EntityField { Name = "due_at", Type = "datetime", DisplayName = "Due At" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "satisfaction_rating", Type = "string", DisplayName = "Satisfaction Rating" },
                    new EntityField { Name = "sharing_agreement_ids", Type = "string", DisplayName = "Sharing Agreement IDs" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
                }
            };

            // Users
            metadata["users"] = new EntityMetadata
            {
                EntityName = "users",
                DisplayName = "Users",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "User ID" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "time_zone", Type = "string", DisplayName = "Time Zone" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "shared_phone_number", Type = "boolean", DisplayName = "Shared Phone Number" },
                    new EntityField { Name = "photo", Type = "string", DisplayName = "Photo" },
                    new EntityField { Name = "locale_id", Type = "integer", DisplayName = "Locale ID" },
                    new EntityField { Name = "locale", Type = "string", DisplayName = "Locale" },
                    new EntityField { Name = "organization_id", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "role", Type = "string", DisplayName = "Role" },
                    new EntityField { Name = "verified", Type = "boolean", DisplayName = "Verified" },
                    new EntityField { Name = "external_id", Type = "string", DisplayName = "External ID" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "alias", Type = "string", DisplayName = "Alias" },
                    new EntityField { Name = "active", Type = "boolean", DisplayName = "Active" },
                    new EntityField { Name = "shared", Type = "boolean", DisplayName = "Shared" },
                    new EntityField { Name = "shared_agent", Type = "boolean", DisplayName = "Shared Agent" },
                    new EntityField { Name = "last_login_at", Type = "datetime", DisplayName = "Last Login At" },
                    new EntityField { Name = "two_factor_auth_enabled", Type = "boolean", DisplayName = "Two Factor Auth Enabled" },
                    new EntityField { Name = "signature", Type = "string", DisplayName = "Signature" },
                    new EntityField { Name = "details", Type = "string", DisplayName = "Details" },
                    new EntityField { Name = "notes", Type = "string", DisplayName = "Notes" },
                    new EntityField { Name = "role_type", Type = "integer", DisplayName = "Role Type" },
                    new EntityField { Name = "custom_role_id", Type = "string", DisplayName = "Custom Role ID" },
                    new EntityField { Name = "moderator", Type = "boolean", DisplayName = "Moderator" },
                    new EntityField { Name = "ticket_restriction", Type = "string", DisplayName = "Ticket Restriction" },
                    new EntityField { Name = "only_private_comments", Type = "boolean", DisplayName = "Only Private Comments" },
                    new EntityField { Name = "restricted_agent", Type = "boolean", DisplayName = "Restricted Agent" },
                    new EntityField { Name = "suspended", Type = "boolean", DisplayName = "Suspended" },
                    new EntityField { Name = "chat_only", Type = "boolean", DisplayName = "Chat Only" },
                    new EntityField { Name = "default_group_id", Type = "string", DisplayName = "Default Group ID" },
                    new EntityField { Name = "report_csv", Type = "boolean", DisplayName = "Report CSV" },
                    new EntityField { Name = "user_fields", Type = "string", DisplayName = "User Fields" }
                }
            };

            // Organizations
            metadata["organizations"] = new EntityMetadata
            {
                EntityName = "organizations",
                DisplayName = "Organizations",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Organization ID" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "external_id", Type = "string", DisplayName = "External ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "domain_names", Type = "string", DisplayName = "Domain Names" },
                    new EntityField { Name = "details", Type = "string", DisplayName = "Details" },
                    new EntityField { Name = "notes", Type = "string", DisplayName = "Notes" },
                    new EntityField { Name = "group_id", Type = "string", DisplayName = "Group ID" },
                    new EntityField { Name = "shared_tickets", Type = "boolean", DisplayName = "Shared Tickets" },
                    new EntityField { Name = "shared_comments", Type = "boolean", DisplayName = "Shared Comments" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "organization_fields", Type = "string", DisplayName = "Organization Fields" }
                }
            };

            // Groups
            metadata["groups"] = new EntityMetadata
            {
                EntityName = "groups",
                DisplayName = "Groups",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Group ID" },
                    new EntityField { Name = "url", Type = "string", DisplayName = "URL" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "default", Type = "boolean", DisplayName = "Default" },
                    new EntityField { Name = "deleted", Type = "boolean", DisplayName = "Deleted" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
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
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "author_id", Type = "string", DisplayName = "Author ID" },
                    new EntityField { Name = "body", Type = "string", DisplayName = "Body" },
                    new EntityField { Name = "html_body", Type = "string", DisplayName = "HTML Body" },
                    new EntityField { Name = "plain_body", Type = "string", DisplayName = "Plain Body" },
                    new EntityField { Name = "public", Type = "boolean", DisplayName = "Public" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "audit_id", Type = "string", DisplayName = "Audit ID" },
                    new EntityField { Name = "via", Type = "string", DisplayName = "Via" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "metadata", Type = "string", DisplayName = "Metadata" }
                }
            };

            // Macros
            metadata["macros"] = new EntityMetadata
            {
                EntityName = "macros",
                DisplayName = "Macros",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Macro ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "active", Type = "boolean", DisplayName = "Active" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "actions", Type = "string", DisplayName = "Actions" },
                    new EntityField { Name = "restriction", Type = "string", DisplayName = "Restriction" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" }
                }
            };

            // Views
            metadata["views"] = new EntityMetadata
            {
                EntityName = "views",
                DisplayName = "Views",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "View ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "active", Type = "boolean", DisplayName = "Active" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "position", Type = "integer", DisplayName = "Position" },
                    new EntityField { Name = "execution", Type = "string", DisplayName = "Execution" },
                    new EntityField { Name = "conditions", Type = "string", DisplayName = "Conditions" },
                    new EntityField { Name = "restriction", Type = "string", DisplayName = "Restriction" },
                    new EntityField { Name = "watchable", Type = "boolean", DisplayName = "Watchable" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Zendesk API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Subdomain))
                {
                    throw new InvalidOperationException("Subdomain is required for Zendesk connection");
                }

                if (string.IsNullOrEmpty(_config.ApiToken) && (string.IsNullOrEmpty(_config.Username) || string.IsNullOrEmpty(_config.Password)))
                {
                    throw new InvalidOperationException("Either API token or username/password is required for Zendesk connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                if (!string.IsNullOrEmpty(_config.ApiToken))
                {
                    var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.Username}/token:{_config.ApiToken}"));
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                }
                else
                {
                    var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                }

                // Test connection by getting current user
                var testUrl = $"{_config.BaseUrl}/users/me.json";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Zendesk API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Zendesk API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Zendesk API
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
        /// Get data from Zendesk API
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
                        var ticketAssignee = parameters.ContainsKey("assignee_id") ? parameters["assignee_id"].ToString() : "";
                        var ticketRequester = parameters.ContainsKey("requester_id") ? parameters["requester_id"].ToString() : "";
                        var ticketOrganization = parameters.ContainsKey("organization_id") ? parameters["organization_id"].ToString() : "";

                        if (!string.IsNullOrEmpty(ticketId))
                        {
                            url = $"{_config.BaseUrl}/tickets/{ticketId}.json";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(ticketStatus)) queryParams.Add($"status={ticketStatus}");
                            if (!string.IsNullOrEmpty(ticketAssignee)) queryParams.Add($"assignee_id={ticketAssignee}");
                            if (!string.IsNullOrEmpty(ticketRequester)) queryParams.Add($"requester_id={ticketRequester}");
                            if (!string.IsNullOrEmpty(ticketOrganization)) queryParams.Add($"organization_id={ticketOrganization}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/tickets.json{queryString}";
                        }
                        break;

                    case "users":
                        var userId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var userRole = parameters.ContainsKey("role") ? parameters["role"].ToString() : "";

                        if (!string.IsNullOrEmpty(userId))
                        {
                            url = $"{_config.BaseUrl}/users/{userId}.json";
                        }
                        else
                        {
                            var queryString = string.IsNullOrEmpty(userRole) ? "" : $"?role={userRole}";
                            url = $"{_config.BaseUrl}/users.json{queryString}";
                        }
                        break;

                    case "organizations":
                        var orgId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(orgId) ? $"{_config.BaseUrl}/organizations.json" : $"{_config.BaseUrl}/organizations/{orgId}.json";
                        break;

                    case "groups":
                        var groupId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(groupId) ? $"{_config.BaseUrl}/groups.json" : $"{_config.BaseUrl}/groups/{groupId}.json";
                        break;

                    case "comments":
                        var ticketIdForComments = parameters.ContainsKey("ticket_id") ? parameters["ticket_id"].ToString() : "";
                        if (string.IsNullOrEmpty(ticketIdForComments))
                        {
                            throw new ArgumentException("ticket_id parameter is required for comments");
                        }
                        url = $"{_config.BaseUrl}/tickets/{ticketIdForComments}/comments.json";
                        break;

                    case "macros":
                        var macroId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(macroId) ? $"{_config.BaseUrl}/macros.json" : $"{_config.BaseUrl}/macros/{macroId}.json";
                        break;

                    case "views":
                        var viewId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(viewId) ? $"{_config.BaseUrl}/views.json" : $"{_config.BaseUrl}/views/{viewId}.json";
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
                    throw new Exception($"Zendesk API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Zendesk API response structure
                JsonElement dataElement;
                if (root.TryGetProperty(entityName, out var entityArray))
                {
                    dataElement = entityArray;
                }
                else if (root.TryGetProperty(entityName.Substring(0, entityName.Length - 1), out var singleEntity))
                {
                    dataElement = singleEntity;
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
            return new List<string> { "tickets", "users", "organizations", "groups", "comments", "macros", "views" };
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
        /// Insert data (limited support for Zendesk API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "tickets")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating tickets would go here
            // This is a placeholder as Zendesk API has specific requirements for ticket creation
            throw new NotImplementedException("Ticket creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Zendesk API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Zendesk API");
        }

        /// <summary>
        /// Delete data (limited support for Zendesk API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Zendesk API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Zendesk, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Zendesk";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Zendesk Data Source";

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
