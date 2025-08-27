using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.CustomerSupport.Kayako
{
    /// <summary>
    /// Configuration class for Kayako data source
    /// </summary>
    public class KayakoConfig
    {
        /// <summary>
        /// Kayako Domain (e.g., 'yourcompany' for yourcompany.kayako.com)
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// Kayako API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Kayako Secret Key
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// API version for Kayako API (default: v1)
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Kayako API
        /// </summary>
        public string BaseUrl => $"https://{Domain}.kayako.com/api/{ApiVersion}";

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
    /// Kayako data source implementation for Beep framework
    /// Supports Kayako REST API v1
    /// </summary>
    public class KayakoDataSource : IDataSource
    {
        private readonly KayakoConfig _config;
        private HttpClient _httpClient;
        private bool _isConnected;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for KayakoDataSource
        /// </summary>
        /// <param name="config">Kayako configuration</param>
        public KayakoDataSource(KayakoConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Constructor for KayakoDataSource with connection string
        /// </summary>
        /// <param name="connectionString">Connection string in format: Domain=xxx;ApiKey=xxx;SecretKey=xxx;ApiVersion=xxx</param>
        public KayakoDataSource(string connectionString)
        {
            _config = ParseConnectionString(connectionString);
            _entityMetadata = InitializeEntityMetadata();
        }

        /// <summary>
        /// Parse connection string into KayakoConfig
        /// </summary>
        private KayakoConfig ParseConnectionString(string connectionString)
        {
            var config = new KayakoConfig();
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
                        case "secretkey":
                            config.SecretKey = value;
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
        /// Initialize entity metadata for Kayako entities
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
                    new EntityField { Name = "displayid", Type = "string", DisplayName = "Display ID" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "statusid", Type = "string", DisplayName = "Status ID" },
                    new EntityField { Name = "priorityid", Type = "string", DisplayName = "Priority ID" },
                    new EntityField { Name = "typeid", Type = "string", DisplayName = "Type ID" },
                    new EntityField { Name = "userid", Type = "string", DisplayName = "User ID" },
                    new EntityField { Name = "staffid", Type = "string", DisplayName = "Staff ID" },
                    new EntityField { Name = "fullname", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "contents", Type = "string", DisplayName = "Contents" },
                    new EntityField { Name = "contents_html", Type = "string", DisplayName = "Contents HTML" },
                    new EntityField { Name = "contents_text", Type = "string", DisplayName = "Contents Text" },
                    new EntityField { Name = "ipaddress", Type = "string", DisplayName = "IP Address" },
                    new EntityField { Name = "creator", Type = "string", DisplayName = "Creator" },
                    new EntityField { Name = "creationmode", Type = "string", DisplayName = "Creation Mode" },
                    new EntityField { Name = "creationtype", Type = "string", DisplayName = "Creation Type" },
                    new EntityField { Name = "isescalated", Type = "boolean", DisplayName = "Is Escalated" },
                    new EntityField { Name = "isautoresponderreceived", Type = "boolean", DisplayName = "Is Autoresponder Received" },
                    new EntityField { Name = "isautorespondersent", Type = "boolean", DisplayName = "Is Autoresponder Sent" },
                    new EntityField { Name = "isrepliesent", Type = "boolean", DisplayName = "Is Reply Sent" },
                    new EntityField { Name = "islastreplier", Type = "boolean", DisplayName = "Is Last Replier" },
                    new EntityField { Name = "isunread", Type = "boolean", DisplayName = "Is Unread" },
                    new EntityField { Name = "isanswered", Type = "boolean", DisplayName = "Is Answered" },
                    new EntityField { Name = "islocked", Type = "boolean", DisplayName = "Is Locked" },
                    new EntityField { Name = "isspam", Type = "boolean", DisplayName = "Is Spam" },
                    new EntityField { Name = "isdeleted", Type = "boolean", DisplayName = "Is Deleted" },
                    new EntityField { Name = "isresolved", Type = "boolean", DisplayName = "Is Resolved" },
                    new EntityField { Name = "isclosed", Type = "boolean", DisplayName = "Is Closed" },
                    new EntityField { Name = "isreopened", Type = "boolean", DisplayName = "Is Reopened" },
                    new EntityField { Name = "isphonecall", Type = "boolean", DisplayName = "Is Phone Call" },
                    new EntityField { Name = "isemailed", Type = "boolean", DisplayName = "Is Emailed" },
                    new EntityField { Name = "hasnotes", Type = "boolean", DisplayName = "Has Notes" },
                    new EntityField { Name = "hasattachments", Type = "boolean", DisplayName = "Has Attachments" },
                    new EntityField { Name = "hasdraft", Type = "boolean", DisplayName = "Has Draft" },
                    new EntityField { Name = "hasbilling", Type = "boolean", DisplayName = "Has Billing" },
                    new EntityField { Name = "haswork", Type = "boolean", DisplayName = "Has Work" },
                    new EntityField { Name = "hasproperties", Type = "boolean", DisplayName = "Has Properties" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "laststaffreply", Type = "datetime", DisplayName = "Last Staff Reply" },
                    new EntityField { Name = "lastuserreply", Type = "datetime", DisplayName = "Last User Reply" },
                    new EntityField { Name = "lastescalation", Type = "datetime", DisplayName = "Last Escalation" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "duetime", Type = "datetime", DisplayName = "Due Time" },
                    new EntityField { Name = "resolutionduetime", Type = "datetime", DisplayName = "Resolution Due Time" },
                    new EntityField { Name = "closedat", Type = "datetime", DisplayName = "Closed At" },
                    new EntityField { Name = "reopendat", Type = "datetime", DisplayName = "Reopened At" },
                    new EntityField { Name = "escalatedat", Type = "datetime", DisplayName = "Escalated At" },
                    new EntityField { Name = "lastreplier", Type = "string", DisplayName = "Last Replier" },
                    new EntityField { Name = "replycount", Type = "integer", DisplayName = "Reply Count" },
                    new EntityField { Name = "templategroupid", Type = "string", DisplayName = "Template Group ID" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "watchers", Type = "string", DisplayName = "Watchers" },
                    new EntityField { Name = "cc", Type = "string", DisplayName = "CC" },
                    new EntityField { Name = "bcc", Type = "string", DisplayName = "BCC" },
                    new EntityField { Name = "organizationid", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "organizationname", Type = "string", DisplayName = "Organization Name" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
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
                    new EntityField { Name = "userid", Type = "string", DisplayName = "User ID" },
                    new EntityField { Name = "fullname", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "firstname", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "lastname", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "designation", Type = "string", DisplayName = "Designation" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "isstaff", Type = "boolean", DisplayName = "Is Staff" },
                    new EntityField { Name = "isenabled", Type = "boolean", DisplayName = "Is Enabled" },
                    new EntityField { Name = "timezone", Type = "string", DisplayName = "Time Zone" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "lastvisit", Type = "datetime", DisplayName = "Last Visit" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "slaplanid", Type = "string", DisplayName = "SLA Plan ID" },
                    new EntityField { Name = "slaplanexpiry", Type = "datetime", DisplayName = "SLA Plan Expiry" },
                    new EntityField { Name = "userexpiry", Type = "datetime", DisplayName = "User Expiry" },
                    new EntityField { Name = "userrating", Type = "integer", DisplayName = "User Rating" },
                    new EntityField { Name = "isvalidated", Type = "boolean", DisplayName = "Is Validated" },
                    new EntityField { Name = "regdate", Type = "datetime", DisplayName = "Registration Date" },
                    new EntityField { Name = "organizationid", Type = "string", DisplayName = "Organization ID" },
                    new EntityField { Name = "organizationname", Type = "string", DisplayName = "Organization Name" },
                    new EntityField { Name = "salutation", Type = "string", DisplayName = "Salutation" },
                    new EntityField { Name = "role", Type = "string", DisplayName = "Role" },
                    new EntityField { Name = "signature", Type = "string", DisplayName = "Signature" },
                    new EntityField { Name = "disabledstafflogin", Type = "boolean", DisplayName = "Disabled Staff Login" },
                    new EntityField { Name = "staffgroupid", Type = "string", DisplayName = "Staff Group ID" },
                    new EntityField { Name = "staffgroupname", Type = "string", DisplayName = "Staff Group Name" },
                    new EntityField { Name = "greeting", Type = "string", DisplayName = "Greeting" },
                    new EntityField { Name = "mobile", Type = "string", DisplayName = "Mobile" },
                    new EntityField { Name = "homephone", Type = "string", DisplayName = "Home Phone" },
                    new EntityField { Name = "workphone", Type = "string", DisplayName = "Work Phone" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
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
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "organizationtype", Type = "string", DisplayName = "Organization Type" },
                    new EntityField { Name = "address", Type = "string", DisplayName = "Address" },
                    new EntityField { Name = "city", Type = "string", DisplayName = "City" },
                    new EntityField { Name = "state", Type = "string", DisplayName = "State" },
                    new EntityField { Name = "postalcode", Type = "string", DisplayName = "Postal Code" },
                    new EntityField { Name = "country", Type = "string", DisplayName = "Country" },
                    new EntityField { Name = "phone", Type = "string", DisplayName = "Phone" },
                    new EntityField { Name = "fax", Type = "string", DisplayName = "Fax" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "slaplanid", Type = "string", DisplayName = "SLA Plan ID" },
                    new EntityField { Name = "slaplanexpiry", Type = "datetime", DisplayName = "SLA Plan Expiry" },
                    new EntityField { Name = "organizationexpiry", Type = "datetime", DisplayName = "Organization Expiry" },
                    new EntityField { Name = "isenabled", Type = "boolean", DisplayName = "Is Enabled" },
                    new EntityField { Name = "usercount", Type = "integer", DisplayName = "User Count" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            // Teams
            metadata["teams"] = new EntityMetadata
            {
                EntityName = "teams",
                DisplayName = "Teams",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Team ID" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Name" },
                    new EntityField { Name = "displayname", Type = "string", DisplayName = "Display Name" },
                    new EntityField { Name = "isenabled", Type = "boolean", DisplayName = "Is Enabled" },
                    new EntityField { Name = "type", Type = "string", DisplayName = "Type" },
                    new EntityField { Name = "module", Type = "string", DisplayName = "Module" },
                    new EntityField { Name = "departmentid", Type = "string", DisplayName = "Department ID" },
                    new EntityField { Name = "creatorstaffid", Type = "string", DisplayName = "Creator Staff ID" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "lastactivity", Type = "datetime", DisplayName = "Last Activity" },
                    new EntityField { Name = "autoassignmax", Type = "integer", DisplayName = "Auto Assign Max" },
                    new EntityField { Name = "autoassignalert", Type = "integer", DisplayName = "Auto Assign Alert" },
                    new EntityField { Name = "autoassignignorestatus", Type = "string", DisplayName = "Auto Assign Ignore Status" },
                    new EntityField { Name = "autoassignignoretypes", Type = "string", DisplayName = "Auto Assign Ignore Types" },
                    new EntityField { Name = "autoassignignorepriorities", Type = "string", DisplayName = "Auto Assign Ignore Priorities" },
                    new EntityField { Name = "staffids", Type = "string", DisplayName = "Staff IDs" }
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
                    new EntityField { Name = "ticketid", Type = "string", DisplayName = "Ticket ID" },
                    new EntityField { Name = "ticketmaskid", Type = "string", DisplayName = "Ticket Mask ID" },
                    new EntityField { Name = "ticketpostid", Type = "string", DisplayName = "Ticket Post ID" },
                    new EntityField { Name = "contents", Type = "string", DisplayName = "Contents" },
                    new EntityField { Name = "contents_html", Type = "string", DisplayName = "Contents HTML" },
                    new EntityField { Name = "contents_text", Type = "string", DisplayName = "Contents Text" },
                    new EntityField { Name = "userid", Type = "string", DisplayName = "User ID" },
                    new EntityField { Name = "fullname", Type = "string", DisplayName = "Full Name" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "staffid", Type = "string", DisplayName = "Staff ID" },
                    new EntityField { Name = "isthirdparty", Type = "boolean", DisplayName = "Is Third Party" },
                    new EntityField { Name = "issurveycomment", Type = "boolean", DisplayName = "Is Survey Comment" },
                    new EntityField { Name = "isprivate", Type = "boolean", DisplayName = "Is Private" },
                    new EntityField { Name = "issurrogate", Type = "boolean", DisplayName = "Is Surrogate" },
                    new EntityField { Name = "creator", Type = "string", DisplayName = "Creator" },
                    new EntityField { Name = "creationmode", Type = "string", DisplayName = "Creation Mode" },
                    new EntityField { Name = "creationtype", Type = "string", DisplayName = "Creation Type" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "editeddateline", Type = "datetime", DisplayName = "Edited Date Line" },
                    new EntityField { Name = "editedstaffid", Type = "string", DisplayName = "Edited Staff ID" },
                    new EntityField { Name = "isedited", Type = "boolean", DisplayName = "Is Edited" },
                    new EntityField { Name = "hasattachments", Type = "boolean", DisplayName = "Has Attachments" },
                    new EntityField { Name = "attachmentcount", Type = "integer", DisplayName = "Attachment Count" },
                    new EntityField { Name = "ipaddress", Type = "string", DisplayName = "IP Address" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" }
                }
            };

            // Knowledgebase
            metadata["knowledgebase"] = new EntityMetadata
            {
                EntityName = "knowledgebase",
                DisplayName = "Knowledgebase",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Article ID" },
                    new EntityField { Name = "kbarticleid", Type = "string", DisplayName = "KB Article ID" },
                    new EntityField { Name = "creatorstaffid", Type = "string", DisplayName = "Creator Staff ID" },
                    new EntityField { Name = "creatorstaffname", Type = "string", DisplayName = "Creator Staff Name" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author" },
                    new EntityField { Name = "email", Type = "string", DisplayName = "Email" },
                    new EntityField { Name = "subject", Type = "string", DisplayName = "Subject" },
                    new EntityField { Name = "contents", Type = "string", DisplayName = "Contents" },
                    new EntityField { Name = "contents_html", Type = "string", DisplayName = "Contents HTML" },
                    new EntityField { Name = "contents_text", Type = "string", DisplayName = "Contents Text" },
                    new EntityField { Name = "ispublished", Type = "boolean", DisplayName = "Is Published" },
                    new EntityField { Name = "views", Type = "integer", DisplayName = "Views" },
                    new EntityField { Name = "dateline", Type = "datetime", DisplayName = "Date Line" },
                    new EntityField { Name = "editeddateline", Type = "datetime", DisplayName = "Edited Date Line" },
                    new EntityField { Name = "editedstaffid", Type = "string", DisplayName = "Edited Staff ID" },
                    new EntityField { Name = "isedited", Type = "boolean", DisplayName = "Is Edited" },
                    new EntityField { Name = "hasattachments", Type = "boolean", DisplayName = "Has Attachments" },
                    new EntityField { Name = "attachmentcount", Type = "integer", DisplayName = "Attachment Count" },
                    new EntityField { Name = "seokeywords", Type = "string", DisplayName = "SEO Keywords" },
                    new EntityField { Name = "seodescription", Type = "string", DisplayName = "SEO Description" },
                    new EntityField { Name = "rating", Type = "integer", DisplayName = "Rating" },
                    new EntityField { Name = "ratingcount", Type = "integer", DisplayName = "Rating Count" },
                    new EntityField { Name = "ratinghits", Type = "integer", DisplayName = "Rating Hits" },
                    new EntityField { Name = "categoryid", Type = "string", DisplayName = "Category ID" },
                    new EntityField { Name = "categoryname", Type = "string", DisplayName = "Category Name" },
                    new EntityField { Name = "articletype", Type = "string", DisplayName = "Article Type" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "isfeatured", Type = "boolean", DisplayName = "Is Featured" },
                    new EntityField { Name = "allowcomments", Type = "boolean", DisplayName = "Allow Comments" },
                    new EntityField { Name = "totalcomments", Type = "integer", DisplayName = "Total Comments" },
                    new EntityField { Name = "attachments", Type = "string", DisplayName = "Attachments" },
                    new EntityField { Name = "tags", Type = "string", DisplayName = "Tags" },
                    new EntityField { Name = "customfields", Type = "string", DisplayName = "Custom Fields" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to Kayako API
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Domain))
                {
                    throw new InvalidOperationException("Domain is required for Kayako connection");
                }

                if (string.IsNullOrEmpty(_config.ApiKey) || string.IsNullOrEmpty(_config.SecretKey))
                {
                    throw new InvalidOperationException("API Key and Secret Key are required for Kayako connection");
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authentication header
                var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.ApiKey}:{_config.SecretKey}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                // Test connection by getting current user
                var testUrl = $"{_config.BaseUrl}/Base/Staff";
                var response = await _httpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    _isConnected = true;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Kayako API connection failed: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception($"Failed to connect to Kayako API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disconnect from Kayako API
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
        /// Get data from Kayako API
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
                        var ticketDepartment = parameters.ContainsKey("department") ? parameters["department"].ToString() : "";

                        if (!string.IsNullOrEmpty(ticketId))
                        {
                            url = $"{_config.BaseUrl}/Tickets/Ticket/{ticketId}";
                        }
                        else
                        {
                            var queryParams = new List<string>();
                            if (!string.IsNullOrEmpty(ticketStatus)) queryParams.Add($"status={ticketStatus}");
                            if (!string.IsNullOrEmpty(ticketPriority)) queryParams.Add($"priority={ticketPriority}");
                            if (!string.IsNullOrEmpty(ticketDepartment)) queryParams.Add($"department={ticketDepartment}");

                            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                            url = $"{_config.BaseUrl}/Tickets/Ticket{queryString}";
                        }
                        break;

                    case "users":
                        var userId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(userId) ? $"{_config.BaseUrl}/Base/User" : $"{_config.BaseUrl}/Base/User/{userId}";
                        break;

                    case "organizations":
                        var orgId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(orgId) ? $"{_config.BaseUrl}/Base/Organization" : $"{_config.BaseUrl}/Base/Organization/{orgId}";
                        break;

                    case "teams":
                        var teamId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(teamId) ? $"{_config.BaseUrl}/Tickets/Team" : $"{_config.BaseUrl}/Tickets/Team/{teamId}";
                        break;

                    case "comments":
                        var ticketIdForComments = parameters.ContainsKey("ticket_id") ? parameters["ticket_id"].ToString() : "";
                        if (string.IsNullOrEmpty(ticketIdForComments))
                        {
                            throw new ArgumentException("ticket_id parameter is required for comments");
                        }
                        url = $"{_config.BaseUrl}/Tickets/TicketPost/ListAll/{ticketIdForComments}";
                        break;

                    case "knowledgebase":
                        var articleId = parameters.ContainsKey("id") ? parameters["id"].ToString() : "";
                        url = string.IsNullOrEmpty(articleId) ? $"{_config.BaseUrl}/Knowledgebase/Article" : $"{_config.BaseUrl}/Knowledgebase/Article/{articleId}";
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
                    throw new Exception($"Kayako API request failed: {response.StatusCode} - {jsonContent}");
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

                // Handle Kayako API response structure
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
            return new List<string> { "tickets", "users", "organizations", "teams", "comments", "knowledgebase" };
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
        /// Insert data (limited support for Kayako API)
        /// </summary>
        public async Task<int> InsertEntityAsync(string entityName, DataTable data)
        {
            throw new NotSupportedException("Insert operations are not supported for Kayako API");
        }

        /// <summary>
        /// Update data (limited support for Kayako API)
        /// </summary>
        public async Task<int> UpdateEntityAsync(string entityName, DataTable data, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Update operations are not supported for Kayako API");
        }

        /// <summary>
        /// Delete data (limited support for Kayako API)
        /// </summary>
        public async Task<int> DeleteEntityAsync(string entityName, Dictionary<string, object> filter)
        {
            throw new NotSupportedException("Delete operations are not supported for Kayako API");
        }

        /// <summary>
        /// Execute custom query
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // For Kayako, we'll treat query as entity name with parameters
            return await GetEntityAsync(query, parameters);
        }

        /// <summary>
        /// Get connection status
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Get data source type
        /// </summary>
        public string DataSourceType => "Kayako";

        /// <summary>
        /// Get data source name
        /// </summary>
        public string DataSourceName => "Kayako Data Source";

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
