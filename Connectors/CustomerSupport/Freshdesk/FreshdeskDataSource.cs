using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.Freshdesk
{
    /// <summary>
    /// Configuration class for Freshdesk data source
    /// </summary>
    public class FreshdeskConfig
    {
        /// <summary>
        /// Freshdesk domain (e.g., 'yourcompany' for yourcompany.freshdesk.com)
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Freshdesk API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API version for Freshdesk API (default: v2)
        /// </summary>
        public string ApiVersion { get; set; } = "v2";

        /// <summary>
        /// Base URL for Freshdesk API
        /// </summary>
        public string BaseUrl => $"https://{Domain}.freshdesk.com/api/{ApiVersion}";

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
    /// Freshdesk data source implementation for Beep framework
    /// Supports Freshdesk REST API v2
    /// </summary>
    public class FreshdeskDataSource : IDataSource
    {
        private readonly FreshdeskConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for FreshdeskDataSource
        /// </summary>
        /// <param name="config">Freshdesk configuration</param>
        public FreshdeskDataSource(FreshdeskConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for FreshdeskDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: Domain=xxx;ApiKey=xxx;ApiVersion=xxx</param>
        public FreshdeskDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into FreshdeskConfig
        /// </summary>
        private FreshdeskConfig ParseConnectionString(string connectionString)
        {
            var config = new FreshdeskConfig();
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
        /// Initialize entity metadata for Freshdesk entities
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
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "description_text", Type = "string", DisplayName = "Description Text" },
                    new EntityField { Name = "status", Type = "integer", DisplayName = "Status" },
                    new EntityField { Name = "priority", Type = "integer", DisplayName = "Priority" },
                    new EntityField { Name = "source", Type = "integer", DisplayName = "Source" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "requester_id", Type = "string", DisplayName = "Requester ID" },
                    new EntityField { Name = "responder_id", Type = "string", DisplayName = "Responder ID" },
                    new EntityField { Name = "company_id", Type = "string", DisplayName = "Company ID" },
                    new EntityField { Name = "group_id", Type = "string", DisplayName = "Group ID" },
                    new EntityField { Name = "product_id", Type = "string", DisplayName = "Product ID" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "cc_emails", Type = "string", DisplayName = "CC Emails" },
                    new EntityField { Name = "fwd_emails", Type = "string", DisplayName = "Forward Emails" },
                    new EntityField { Name = "reply_cc_emails", Type = "string", DisplayName = "Reply CC Emails" },
                    new EntityField { Name = "email_config_id", Type = "string", DisplayName = "Email Config ID" },
                    new EntityField { Name = "is_escalated", Type = "boolean", DisplayName = "Is Escalated" },
                    new EntityField { Name = "fr_escalated", Type = "boolean", DisplayName = "FR Escalated" },
                    new EntityField { Name = "spam", Type = "boolean", DisplayName = "Spam" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "twitter_id", Type = "string", DisplayName = "Twitter ID" },
                    new EntityField { Name = "facebook_id", Type = "string", DisplayName = "Facebook ID" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "due_by", Type = "datetime", DisplayName = "Due By" },
                    new EntityField { Name = "fr_due_by", Type = "datetime", DisplayName = "FR Due By" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" }
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
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "mobile", Type = "string", DisplayName = "Mobile" },
                    new EntityField { Name = "twitter_id", Type = "string", DisplayName = "Twitter ID" },
                    new EntityField { Name = "unique_external_id", Type = "string", DisplayName = "Unique External ID" },
                    new EntityField { Name = "other_emails", Type = "string", DisplayName = "Other Emails" },
                    new EntityField { Name = "company_id", Type = "string", DisplayName = "Company ID" },
                    new EntityField { Name = "view_all_tickets", Type = "boolean", DisplayName = "View All Tickets" },
                    new EntityField { Name = "address", Type = "string", DisplayName = "Address" },
                    new EntityField { Name = "avatar", Type = "string", DisplayName = "Avatar" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "job_title", Type = "string", DisplayName = "Job Title" },
                    new EntityField { Name = "language", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "facebook_id", Type = "string", DisplayName = "Facebook ID" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "active", Type = "boolean", DisplayName = "Active" },
                    new EntityField { Name = "deleted", Type = "boolean", DisplayName = "Deleted" },
                    new EntityField { Name = "preferred_source", Type = "string", DisplayName = "Preferred Source" },
                    new EntityField { Name = "client_manager", Type = "boolean", DisplayName = "Client Manager" },
                    new EntityField { Name = "auto_collection", Type = "boolean", DisplayName = "Auto Collection" }
                }
            };

            // Companies
            metadata["companies"] = new EntityMetadata
            {
                EntityName = "companies",
                DisplayName = "Companies",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Company ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "domains", Type = "string", DisplayName = "Domains" },
                    new EntityField { Name = "note", Type = "string", DisplayName = "Note" },
                    new EntityField { Name = "health_score", Type = "string", DisplayName = "Health Score" },
                    new EntityField { Name = "account_tier", Type = "string", DisplayName = "Account Tier" },
                    new EntityField { Name = "renewal_date", Type = "datetime", DisplayName = "Renewal Date" },
                    new EntityField { Name = "industry", Type = "string", DisplayName = "Industry" },
                    new EntityField { Name = "custom_fields", Type = "string", DisplayName = "Custom Fields" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" }
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
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "mobile", Type = "string", DisplayName = "Mobile" },
                    new EntityField { Name = "job_title", Type = "string", DisplayName = "Job Title" },
                    new EntityField { Name = "language", Type = "string", DisplayName = "Language" },
                    new EntityField { Name = "last_login_at", Type = "datetime", DisplayName = "Last Login At" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "available", Type = "boolean", DisplayName = "Available" },
                    new EntityField { Name = "available_since", Type = "datetime", DisplayName = "Available Since" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "occasional", Type = "boolean", DisplayName = "Occasional" },
                    new EntityField { Name = "signature", Type = "string", DisplayName = "Signature" },
                    new EntityField { Name = "ticket_scope", Type = "integer", DisplayName = "Ticket Scope" },
                    new EntityField { Name = "group_ids", Type = "string", DisplayName = "Group IDs" },
                    new EntityField { Name = "role_ids", Type = "string", DisplayName = "Role IDs" },
                    new EntityField { Name = "skill_ids", Type = "string", DisplayName = "Skill IDs" },
                    new EntityField { Name = "auto_collection", Type = "boolean", DisplayName = "Auto Collection" }
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
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "escalate_to", Type = "string", DisplayName = "Escalate To" },
                    new EntityField { Name = "unassigned_for", Type = "string", DisplayName = "Unassigned For" },
                    new EntityField { Name = "business_hour_id", Type = "string", DisplayName = "Business Hour ID" },
                    new EntityField { Name = "group_type", Type = "string", DisplayName = "Group Type" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "auto_ticket_assign", Type = "boolean", DisplayName = "Auto Ticket Assign" },
                    new EntityField { Name = "agent_availability_status", Type = "string", DisplayName = "Agent Availability Status" }
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
                    new EntityField { Name = "body", Type = "string", DisplayName = "Body" },
                    new EntityField { Name = "body_text", Type = "string", DisplayName = "Body Text" },
                    new EntityField { Name = "ticket_id", Type = "string", DisplayName = "Ticket ID" },
                    new EntityField { Name = "author_id", Type = "string", DisplayName = "Author ID" },
                    new EntityField { Name = "to_emails", Type = "string", DisplayName = "To Emails" },
                    new EntityField { Name = "cc_emails", Type = "string", DisplayName = "CC Emails" },
                    new EntityField { Name = "bcc_emails", Type = "string", DisplayName = "BCC Emails" },
                    new EntityField { Name = "from_email", Type = "string", DisplayName = "From Email" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "incoming", Type = "boolean", DisplayName = "Incoming" },
                    new EntityField { Name = "private", Type = "boolean", DisplayName = "Private" },
                    new EntityField { Name = "support_email", Type = "string", DisplayName = "Support Email" },
                    new EntityField { Name = "source", Type = "integer", DisplayName = "Source" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "notify_emails", Type = "string", DisplayName = "Notify Emails" }
                }
            };

            // Solutions
            metadata["solutions"] = new EntityMetadata
            {
                EntityName = "solutions",
                DisplayName = "Solutions",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Solution ID" },
                    new EntityField { Name = "title", Type = "string", DisplayName = "Title" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "description_text", Type = "string", DisplayName = "Description Text" },
                    new EntityField { Name = "folder_id", Type = "string", DisplayName = "Folder ID" },
                    new EntityField { Name = "category_id", Type = "string", DisplayName = "Category ID" },
                    new EntityField { Name = "status", Type = "integer", DisplayName = "Status" },
                    new EntityField { Name = "thumbs_up", Type = "integer", DisplayName = "Thumbs Up" },
                    new EntityField { Name = "thumbs_down", Type = "integer", DisplayName = "Thumbs Down" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "seo_data", Type = "string", DisplayName = "SEO Data" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "created_at", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "updated_at", Type = "datetime", DisplayName = "Updated At" },
                    new EntityField { Name = "recent_helpful", Type = "boolean", DisplayName = "Recent Helpful" },
                    new EntityField { Name = "modified_by", Type = "string", DisplayName = "Modified By" },
                    new EntityField { Name = "modified_at", Type = "datetime", DisplayName = "Modified At" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Freshdesk API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Domain))
                {
                    throw new InvalidOperationException("Domain is required for Freshdesk connection");
                }

                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    throw new InvalidOperationException("API Key is required for Freshdesk connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header (API key as username, 'X' as password)
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.ApiKey}:X"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                // Test connection by getting current user
                var testUrl = $"{_config.BaseUrl}/agents/me";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Freshdesk API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Freshdesk API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Freshdesk API
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
        /// Get data from Freshdesk API
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
                        var ticketRequester = parameters.ContainsKey("requester_id") ? parameters["requester_id"].ToString() : "";
                        var ticketResponder = parameters.ContainsKey("responder_id") ? parameters["responder_id"].ToString() : "";
                        var ticketCompany = parameters.ContainsKey("company_id") ? parameters["company_id"].ToString() : "";

                        if (!string.IsNullOrEmpty(ticketId))
                        {
                            url = $"{_config.BaseUrl}/tickets/{ticketId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(ticketStatus)) queryParams.Add($"status={ticketStatus}");
                            if (!string.IsNullOrEmpty(ticketPriority)) queryParams.Add($"priority={ticketPriority}");
                            if (!string.IsNullOrEmpty(ticketRequester)) queryParams.Add($"requester_id={ticketRequester}");
                            if (!string.IsNullOrEmpty(ticketResponder)) queryParams.Add($"responder_id={ticketResponder}");
                            if (!string.IsNullOrEmpty(ticketCompany)) queryParams.Add($"company_id={ticketCompany}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/tickets{queryString}";
                        }
                        break;

                    case "contacts":
                        var contactId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        var contactCompany = parameters.ContainsKey("company_id") ? parameters["company_id"].ToString() : "";
                        url = string.IsNullOrEmpty(contactId) ? $"{_config.BaseUrl}/contacts" : $"{_config.BaseUrl}/contacts/{contactId}";
                        if (!string.IsNullOrEmpty(contactCompany) && string.IsNullOrEmpty(contactId))
                        {
                            url += $"?company_id={contactCompany}";
                        }
                        break;

                    case "companies":
                        var companyId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(companyId) ? $"{_config.BaseUrl}/companies" : $"{_config.BaseUrl}/companies/{companyId}";
                        break;

                    case "agents":
                        var agentId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(agentId) ? $"{_config.BaseUrl}/agents" : $"{_config.BaseUrl}/agents/{agentId}";
                        break;

                    case "groups":
                        var groupId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(groupId) ? $"{_config.BaseUrl}/groups" : $"{_config.BaseUrl}/groups/{groupId}";
                        break;

                    case "comments":
                        var ticketIdForComments = parameters.ContainsKey("ticket_id") ? parameters["ticket_id"].ToString() : "";
                        if (string.IsNullOrEmpty(ticketIdForComments))
                        {
                            throw new ArgumentException("ticket_id parameter is required for comments");
                        }
                        url = $"{_config.BaseUrl}/tickets/{ticketIdForComments}/conversations";
                        break;

                    case "solutions":
                        var solutionId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(solutionId) ? $"{_config.BaseUrl}/solutions" : $"{_config.BaseUrl}/solutions/{solutionId}";
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
                    throw new Exception($"Freshdesk API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Freshdesk API response structure
                JsonElement dataElement;
                if (root.ValueKind == JsonValueKind.Array)
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
            return new List<string> { "tickets", "contacts", "companies", "agents", "groups", "comments", "solutions" };
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
        /// Insert data (limited support for Freshdesk API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            if (entityName.ToLower() != "tickets")
            {
                throw new NotSupportedException($"Insert operations are not supported for {entityName}");
            }

            // Implementation for creating tickets would go here
            // This is a placeholder as Freshdesk API has specific requirements for ticket creation
            throw new NotImplementedException("Ticket creation not yet implemented");
        }

        /// <summary>
        /// Update data (limited support for Freshdesk API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Freshdesk API");
        }

        /// <summary>
        /// Delete data (limited support for Freshdesk API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Freshdesk API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Freshdesk, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Freshdesk";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Freshdesk Data Source";

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
