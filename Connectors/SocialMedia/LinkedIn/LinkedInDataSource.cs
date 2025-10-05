using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DataManagementEngineStandard;
using DataManagementModelsStandard;

namespace BeepDataSources.Connectors.SocialMedia.LinkedIn
{
    /// <summary>
    /// Configuration class for LinkedIn data source
    /// </summary>
    public class LinkedInConfig
    {
        /// <summary>
        /// LinkedIn App Client ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// LinkedIn App Client Secret
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for LinkedIn API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// LinkedIn Person URN (person identifier)
        /// </summary>
        public string PersonUrn { get; set; } = string.Empty;

        /// <summary>
        /// LinkedIn Organization URN (company identifier)
        /// </summary>
        public string OrganizationUrn { get; set; } = string.Empty;

        /// <summary>
        /// API version for LinkedIn Marketing API (default: 202401)
        /// </summary>
        public string ApiVersion { get; set; } = "202401";

        /// <summary>
        /// Base URL for LinkedIn API
        /// </summary>
        public string BaseUrl => $"https://api.linkedin.com/v2";

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
    /// LinkedIn data source implementation for Beep framework
    /// Supports LinkedIn Marketing API v2
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.SOCIALMEDIA, DatasourceType = DataSourceType.WebApi)]
    public class LinkedInDataSource : WebAPIDataSource
    {
        private readonly LinkedInConfig _config;
        private readonly Dictionary<string, EntityMetadata> _entityMetadata;

        /// <summary>
        /// Constructor for LinkedInDataSource
        /// </summary>
        public LinkedInDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            _config = new LinkedInConfig();
            _entityMetadata = InitializeEntityMetadata();
            InitializeEntities();
        }

        /// <summary>
        /// Initialize entities for LinkedIn data source
        /// </summary>
        private void InitializeEntities()
        {
            foreach (var kvp in _entityMetadata)
            {
                var entityStructure = new EntityStructure
                {
                    EntityName = kvp.Value.EntityName,
                    ViewID = kvp.Value.EntityName,
                    DataSourceID = DatasourceName,
                    ViewName = kvp.Value.DisplayName,
                    Fields = kvp.Value.Fields,
                    Methods = new List<EntityMethod>(),
                    Viewtype = ViewType.Table
                };

                Entities[kvp.Value.EntityName] = entityStructure;
                EntitiesNames.Add(kvp.Value.EntityName);
            }
        }

        /// <summary>
        /// Initialize entity metadata for LinkedIn entities
        /// </summary>
        private Dictionary<string, EntityMetadata> InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Profile
            metadata["profile"] = new EntityMetadata
            {
                EntityName = "profile",
                DisplayName = "User Profile",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Profile ID" },
                    new EntityField { Name = "localizedFirstName", Type = "string", DisplayName = "First Name" },
                    new EntityField { Name = "localizedLastName", Type = "string", DisplayName = "Last Name" },
                    new EntityField { Name = "vanityName", Type = "string", DisplayName = "Vanity Name" },
                    new EntityField { Name = "profilePicture", Type = "string", DisplayName = "Profile Picture URL" },
                    new EntityField { Name = "headline", Type = "string", DisplayName = "Headline" },
                    new EntityField { Name = "publicProfileUrl", Type = "string", DisplayName = "Public Profile URL" }
                }
            };

            // Posts
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Post ID" },
                    new EntityField { Name = "author", Type = "string", DisplayName = "Author URN" },
                    new EntityField { Name = "lifecycleState", Type = "string", DisplayName = "Lifecycle State" },
                    new EntityField { Name = "visibility", Type = "string", DisplayName = "Visibility" },
                    new EntityField { Name = "createdAt", Type = "datetime", DisplayName = "Created At" },
                    new EntityField { Name = "lastModifiedAt", Type = "datetime", DisplayName = "Last Modified At" },
                    new EntityField { Name = "text", Type = "string", DisplayName = "Post Text" },
                    new EntityField { Name = "commentary", Type = "string", DisplayName = "Commentary" }
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
                    new EntityField { Name = "localizedName", Type = "string", DisplayName = "Organization Name" },
                    new EntityField { Name = "vanityName", Type = "string", DisplayName = "Vanity Name" },
                    new EntityField { Name = "logoV2", Type = "string", DisplayName = "Logo URL" },
                    new EntityField { Name = "description", Type = "string", DisplayName = "Description" },
                    new EntityField { Name = "website", Type = "string", DisplayName = "Website" },
                    new EntityField { Name = "locations", Type = "string", DisplayName = "Locations" }
                }
            };

            // Followers
            metadata["followers"] = new EntityMetadata
            {
                EntityName = "followers",
                DisplayName = "Followers",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "follower", Type = "string", IsPrimaryKey = true, DisplayName = "Follower URN" },
                    new EntityField { Name = "followedAt", Type = "datetime", DisplayName = "Followed At" },
                    new EntityField { Name = "organization", Type = "string", DisplayName = "Organization URN" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                DisplayName = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "organizationalEntity", Type = "string", IsPrimaryKey = true, DisplayName = "Organization URN" },
                    new EntityField { Name = "timeRange", Type = "string", IsPrimaryKey = true, DisplayName = "Time Range" },
                    new EntityField { Name = "followerGains", Type = "integer", DisplayName = "Follower Gains" },
                    new EntityField { Name = "impressions", Type = "integer", DisplayName = "Impressions" },
                    new EntityField { Name = "clicks", Type = "integer", DisplayName = "Clicks" },
                    new EntityField { Name = "likes", Type = "integer", DisplayName = "Likes" },
                    new EntityField { Name = "comments", Type = "integer", DisplayName = "Comments" },
                    new EntityField { Name = "shares", Type = "integer", DisplayName = "Shares" }
                }
            };

            // Campaigns
            metadata["campaigns"] = new EntityMetadata
            {
                EntityName = "campaigns",
                DisplayName = "Campaigns",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true, DisplayName = "Campaign ID" },
                    new EntityField { Name = "account", Type = "string", DisplayName = "Account URN" },
                    new EntityField { Name = "name", Type = "string", DisplayName = "Campaign Name" },
                    new EntityField { Name = "status", Type = "string", DisplayName = "Status" },
                    new EntityField { Name = "objectiveType", Type = "string", DisplayName = "Objective Type" },
                    new EntityField { Name = "budget", Type = "decimal", DisplayName = "Budget" },
                    new EntityField { Name = "runSchedule", Type = "string", DisplayName = "Run Schedule" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to LinkedIn API
        /// </summary>
        public override async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.AccessToken))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Access token is required for LinkedIn connection";
                    return false;
                }

                // Initialize HTTP client
                var handler = new HttpClientHandler();
                HttpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
                };

                // Add authorization header
                HttpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.AccessToken);

                // Test connection by getting user profile
                var testUrl = $"{_config.BaseUrl}/people/~";
                var response = await HttpClient.GetAsync(testUrl);

                if (response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Ok;
                    ErrorObject.Message = "Successfully connected to LinkedIn API";
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"LinkedIn API connection failed: {response.StatusCode} - {errorContent}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to connect to LinkedIn API: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Disconnect from LinkedIn API
        /// </summary>
        public override async Task<bool> DisconnectAsync()
        {
            if (HttpClient != null)
            {
                HttpClient.Dispose();
                HttpClient = null;
            }
            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = "Successfully disconnected from LinkedIn API";
            return true;
        }

        /// <summary>
        /// Get data from LinkedIn API
        /// </summary>
        public override async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            parameters ??= new Dictionary<string, object>();

            try
            {
                string url;
                var count = parameters.ContainsKey("count") ? parameters["count"].ToString() : "25";
                var start = parameters.ContainsKey("start") ? parameters["start"].ToString() : "0";

                switch (entityName.ToLower())
                {
                    case "profile":
                        url = $"{_config.BaseUrl}/people/~";
                        break;

                    case "posts":
                        var authorUrn = parameters.ContainsKey("author_urn") ? parameters["author_urn"].ToString() : _config.PersonUrn;
                        url = $"{_config.BaseUrl}/posts?q=author&author={authorUrn}&count={count}&start={start}";
                        break;

                    case "organizations":
                        if (!string.IsNullOrEmpty(_config.OrganizationUrn))
                        {
                            url = $"{_config.BaseUrl}/organizations/{_config.OrganizationUrn}";
                        }
                        else
                        {
                            url = $"{_config.BaseUrl}/organizations?q=owners&owners={_config.PersonUrn}";
                        }
                        break;

                    case "followers":
                        var orgUrn = parameters.ContainsKey("organization_urn") ? parameters["organization_urn"].ToString() : _config.OrganizationUrn;
                        url = $"{_config.BaseUrl}/organizationalEntityFollowerStatistics?q=organizationalEntity&organizationalEntity={orgUrn}";
                        break;

                    case "analytics":
                        var orgEntity = parameters.ContainsKey("organization_urn") ? parameters["organization_urn"].ToString() : _config.OrganizationUrn;
                        var timeRange = parameters.ContainsKey("time_range") ? parameters["time_range"].ToString() : "LAST_30_DAYS";
                        url = $"{_config.BaseUrl}/organizationalEntityFollowerStatistics?q=organizationalEntity&organizationalEntity={orgEntity}&timeIntervals.timeRange={timeRange}";
                        break;

                    case "campaigns":
                        var accountUrn = parameters.ContainsKey("account_urn") ? parameters["account_urn"].ToString() : "";
                        url = $"{_config.BaseUrl}/adCampaignsV2?q=search&search.account.values[0]={accountUrn}&count={count}&start={start}";
                        break;

                    default:
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = $"Unsupported entity: {entityName}";
                        return null;
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
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"LinkedIn API request failed: {response.StatusCode} - {jsonContent}";
                    return null;
                }

                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = $"Successfully retrieved {entityName} data";
                return ParseJsonToDataTable(jsonContent, entityName);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
                return null;
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

                // Handle different response structures
                JsonElement dataElement;
                if (root.TryGetProperty("elements", out var elementsProp))
                {
                    dataElement = elementsProp;
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
