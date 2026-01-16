using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.LinkedIn.Models;

namespace BeepDataSources.Connectors.SocialMedia.LinkedIn
{
    /// <summary>
    /// Entity metadata for LinkedIn entities
    /// </summary>
    public class EntityMetadata
    {
        /// <summary>
        /// Name of the entity
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Display caption for the entity
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// List of fields in the entity
        /// </summary>
        public List<EntityField> Fields { get; set; } = new List<EntityField>();
    }

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
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LinkedIn)]
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
            EntitiesNames = _entityMetadata.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure
                {
                    EntityName = n,
                    DatasourceEntityName = n,
                    DataSourceID = DatasourceName,
                    Caption = _entityMetadata[n].Caption,
                    Fields = _entityMetadata[n].Fields,
                    Viewtype = ViewType.Table
                })
                .ToList();
        }

        private string GetBearerToken()
        {
            if (!string.IsNullOrWhiteSpace(_config.AccessToken))
                return _config.AccessToken;

            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                return webApiProps.AccessToken
                       ?? webApiProps.BearerToken
                       ?? webApiProps.OAuthAccessToken
                       ?? string.Empty;
            }

            return string.Empty;
        }

        private Dictionary<string, string> BuildAuthHeaders()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Restli-Protocol-Version"] = "2.0.0"
            };

            var token = GetBearerToken();
            if (!string.IsNullOrWhiteSpace(token))
                headers["Authorization"] = $"Bearer {token}";

            return headers;
        }

        private void HydrateConfigFromConnectionProperties()
        {
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties webApiProps)
                return;

            _config.AccessToken = webApiProps.AccessToken
                                  ?? webApiProps.BearerToken
                                  ?? webApiProps.OAuthAccessToken
                                  ?? _config.AccessToken;

            _config.ClientId = webApiProps.ClientId ?? _config.ClientId;
            _config.ClientSecret = webApiProps.ClientSecret ?? _config.ClientSecret;
            _config.ApiVersion = webApiProps.ApiVersion ?? _config.ApiVersion;

            if (webApiProps.TimeoutMs > 0)
                _config.TimeoutSeconds = Math.Max(1, webApiProps.TimeoutMs / 1000);

            if (webApiProps.MaxRetries > 0)
                _config.MaxRetries = webApiProps.MaxRetries;

            if (webApiProps.RetryDelayMs > 0)
                _config.RateLimitDelayMs = webApiProps.RetryDelayMs;
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
                Caption = "User Profile",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Profile ID" },
                    new EntityField {FieldName = "localizedFirstName", Fieldtype ="string", Caption = "First Name" },
                    new EntityField {FieldName = "localizedLastName", Fieldtype ="string", Caption = "Last Name" },
                    new EntityField {FieldName = "vanityName", Fieldtype ="string", Caption = "Vanity Name" },
                    new EntityField {FieldName = "profilePicture", Fieldtype ="string", Caption = "Profile Picture URL" },
                    new EntityField {FieldName = "headline", Fieldtype ="string", Caption = "Headline" },
                    new EntityField {FieldName = "publicProfileUrl", Fieldtype ="string", Caption = "Public Profile URL" }
                }
            };

            // Posts
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                Caption = "Posts",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Post ID" },
                    new EntityField {FieldName = "author", Fieldtype ="string", Caption = "Author URN" },
                    new EntityField {FieldName = "lifecycleState", Fieldtype ="string", Caption = "Lifecycle State" },
                    new EntityField {FieldName = "visibility", Fieldtype ="string", Caption = "Visibility" },
                    new EntityField {FieldName = "createdAt", Fieldtype ="datetime", Caption = "Created At" },
                    new EntityField {FieldName = "lastModifiedAt", Fieldtype ="datetime", Caption = "Last Modified At" },
                    new EntityField {FieldName = "text", Fieldtype ="string", Caption = "Post Text" },
                    new EntityField {FieldName = "commentary", Fieldtype ="string", Caption = "Commentary" }
                }
            };

            // Organizations
            metadata["organizations"] = new EntityMetadata
            {
                EntityName = "organizations",
                Caption = "Organizations",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Organization ID" },
                    new EntityField {FieldName = "localizedName", Fieldtype ="string", Caption = "Organization Name" },
                    new EntityField {FieldName = "vanityName", Fieldtype ="string", Caption = "Vanity Name" },
                    new EntityField {FieldName = "logoV2", Fieldtype ="string", Caption = "Logo URL" },
                    new EntityField {FieldName = "description", Fieldtype ="string", Caption = "Description" },
                    new EntityField {FieldName = "website", Fieldtype ="string", Caption = "Website" },
                    new EntityField {FieldName = "locations", Fieldtype ="string", Caption = "Locations" }
                }
            };

            // Followers
            metadata["followers"] = new EntityMetadata
            {
                EntityName = "followers",
                Caption = "Followers",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "follower", Fieldtype ="string", IsKey = true, Caption = "Follower URN" },
                    new EntityField {FieldName = "followedAt", Fieldtype ="datetime", Caption = "Followed At" },
                    new EntityField {FieldName = "organization", Fieldtype ="string", Caption = "Organization URN" }
                }
            };

            // Analytics
            metadata["analytics"] = new EntityMetadata
            {
                EntityName = "analytics",
                Caption = "Analytics",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "organizationalEntity", Fieldtype ="string", IsKey = true, Caption = "Organization URN" },
                    new EntityField {FieldName = "timeRange", Fieldtype ="string", IsKey = true, Caption = "Time Range" },
                    new EntityField {FieldName = "followerGains", Fieldtype ="integer", Caption = "Follower Gains" },
                    new EntityField {FieldName = "impressions", Fieldtype ="integer", Caption = "Impressions" },
                    new EntityField {FieldName = "clicks", Fieldtype ="integer", Caption = "Clicks" },
                    new EntityField {FieldName = "likes", Fieldtype ="integer", Caption = "Likes" },
                    new EntityField {FieldName = "comments", Fieldtype ="integer", Caption = "Comments" },
                    new EntityField {FieldName = "shares", Fieldtype ="integer", Caption = "Shares" }
                }
            };

            // Campaigns
            metadata["campaigns"] = new EntityMetadata
            {
                EntityName = "campaigns",
                Caption = "Campaigns",
                Fields = new List<EntityField>
                {
                    new EntityField {FieldName = "id", Fieldtype ="string", IsKey = true, Caption = "Campaign ID" },
                    new EntityField {FieldName = "account", Fieldtype ="string", Caption = "Account URN" },
                    new EntityField {FieldName = "name", Fieldtype ="string", Caption = "Campaign Name" },
                    new EntityField {FieldName = "status", Fieldtype ="string", Caption = "Status" },
                    new EntityField {FieldName = "objectiveType", Fieldtype ="string", Caption = "Objective Type" },
                    new EntityField {FieldName = "budget", Fieldtype ="decimal", Caption = "Budget" },
                    new EntityField {FieldName = "runSchedule", Fieldtype ="string", Caption = "Run Schedule" }
                }
            };

            return metadata;
        }

        /// <summary>
        /// Connect to LinkedIn API (convenience; Beep uses Openconnection/Closeconnection)
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                HydrateConfigFromConnectionProperties();

                if (string.IsNullOrEmpty(GetBearerToken()))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Access token is required for LinkedIn connection";
                    return false;
                }

                // Test connection by getting user profile
                var testUrl = $"{_config.BaseUrl}/people/~";
                using var response = await GetAsync(testUrl, null, BuildAuthHeaders(), default).ConfigureAwait(false);

                if (response is not null && response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Ok;
                    ErrorObject.Message = "Successfully connected to LinkedIn API";
                    ConnectionStatus = ConnectionState.Open;
                    return true;
                }
                else
                {
                    var errorContent = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : "No response";
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"LinkedIn API connection failed: {response?.StatusCode} - {errorContent}";
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
        /// Disconnect from LinkedIn API (convenience; Beep uses Openconnection/Closeconnection)
        /// </summary>
        public Task<bool> DisconnectAsync()
        {
            try
            {
                Closeconnection();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Successfully disconnected from LinkedIn API";
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Failed to disconnect from LinkedIn API: {ex.Message}";
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Get data from LinkedIn API as a DataTable
        /// </summary>
        public async Task<DataTable?> GetEntityDataTableAsync(string entityName, Dictionary<string, object>? parameters = null)
        {
            HydrateConfigFromConnectionProperties();
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

                using var response = await GetAsync(url, null, BuildAuthHeaders(), default).ConfigureAwait(false);
                var jsonContent = response != null ? await response.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;

                if (response is null || !response.IsSuccessStatusCode)
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"LinkedIn API request failed: {response?.StatusCode} - {jsonContent}";
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

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            HydrateConfigFromConnectionProperties();

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (Filter != null)
            {
                foreach (var f in Filter)
                {
                    if (!string.IsNullOrWhiteSpace(f?.FieldName))
                        parameters[f.FieldName] = f.FilterValue ?? string.Empty;
                }
            }

            var dt = await GetEntityDataTableAsync(EntityName, parameters).ConfigureAwait(false);
            if (dt == null) return Array.Empty<object>();

            var result = new List<object>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn col in dt.Columns)
                    dict[col.ColumnName] = row[col];
                result.Add(dict);
            }

            return result;
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
                        dataTable.Columns.Add(field.FieldName, GetFieldtype(field.Fieldtype));
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
        private Type GetFieldtype(string Fieldtype)
        {
            return Fieldtype.ToLower() switch
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

        // ---------------- Specific LinkedIn Methods ----------------

        /// <summary>
        /// Gets posts from LinkedIn
        /// </summary>
        [CommandAttribute(ObjectType ="LinkedInPost", PointType = EnumPointType.Function,Name = "GetPosts", Caption = "Get LinkedIn Posts", ClassName = "LinkedInDataSource", misc = "ReturnType: IEnumerable<LinkedInPost>")]
        public async Task<IEnumerable<LinkedInPost>> GetPosts(string authorUrn, int count = 10)
        {
            string endpoint = $"ugcPosts?q=authors&authors=List({authorUrn})&count={count}";
            using var response = await GetAsync($"{_config.BaseUrl}/{endpoint}", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode) return new List<LinkedInPost>();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<LinkedInResponse<LinkedInPost>>(json);
            return data?.Elements ?? new List<LinkedInPost>();
        }

        /// <summary>
        /// Gets user profile information
        /// </summary>
        [CommandAttribute(ObjectType ="LinkedInPerson", PointType = EnumPointType.Function,Name = "GetProfile", Caption = "Get LinkedIn Profile", ClassName = "LinkedInDataSource", misc = "ReturnType: IEnumerable<LinkedInPerson>")]
        public async Task<IEnumerable<LinkedInPerson>> GetProfile(string personId)
        {
            string endpoint = $"people/{personId}";
            using var response = await GetAsync($"{_config.BaseUrl}/{endpoint}", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode) return new List<LinkedInPerson>();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var profile = JsonSerializer.Deserialize<LinkedInPerson>(json);
            return profile != null ? new List<LinkedInPerson> { profile } : new List<LinkedInPerson>();
        }

        /// <summary>
        /// Gets company information
        /// </summary>
        [CommandAttribute(ObjectType ="LinkedInCompany", PointType = EnumPointType.Function,Name = "GetCompany", Caption = "Get LinkedIn Company", ClassName = "LinkedInDataSource", misc = "ReturnType: IEnumerable<LinkedInCompany>")]
        public async Task<IEnumerable<LinkedInCompany>> GetCompany(string companyId)
        {
            string endpoint = $"organizations/{companyId}";
            using var response = await GetAsync($"{_config.BaseUrl}/{endpoint}", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode) return new List<LinkedInCompany>();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var company = JsonSerializer.Deserialize<LinkedInCompany>(json);
            return company != null ? new List<LinkedInCompany> { company } : new List<LinkedInCompany>();
        }

        /// <summary>
        /// Gets company posts
        /// </summary>
        [CommandAttribute(ObjectType ="LinkedInPost", PointType = EnumPointType.Function,Name = "GetCompanyPosts", Caption = "Get LinkedIn Company Posts", ClassName = "LinkedInDataSource", misc = "ReturnType: IEnumerable<LinkedInPost>")]
        public async Task<IEnumerable<LinkedInPost>> GetCompanyPosts(string companyId, int count = 10)
        {
            string endpoint = $"ugcPosts?q=authors&authors=List(urn%3Ali%3Aorganization%3A{companyId})&count={count}";
            using var response = await GetAsync($"{_config.BaseUrl}/{endpoint}", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode) return new List<LinkedInPost>();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<LinkedInResponse<LinkedInPost>>(json);
            return data?.Elements ?? new List<LinkedInPost>();
        }

        /// <summary>
        /// Gets user connections/following
        /// </summary>
        [CommandAttribute(ObjectType ="LinkedInFollowing", PointType = EnumPointType.Function,Name = "GetFollowing", Caption = "Get LinkedIn Following", ClassName = "LinkedInDataSource", misc = "ReturnType: IEnumerable<LinkedInFollowing>")]
        public async Task<IEnumerable<LinkedInFollowing>> GetFollowing(string personId, int count = 10)
        {
            string endpoint = $"people/{personId}/following?count={count}";
            using var response = await GetAsync($"{_config.BaseUrl}/{endpoint}", null, BuildAuthHeaders(), default).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode) return new List<LinkedInFollowing>();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<LinkedInResponse<LinkedInFollowing>>(json);
            return data?.Elements ?? new List<LinkedInFollowing>();
        }

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LinkedIn, PointType = EnumPointType.Function, ObjectType ="LinkedInPost",Name = "CreatePost", Caption = "Create LinkedIn Post", ClassType ="LinkedInDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "linkedin.png", misc = "ReturnType: IEnumerable<LinkedInPost>")]
        public async Task<IEnumerable<LinkedInPost>> CreatePostAsync(LinkedInPost post)
        {
            try
            {
                using var result = await PostAsync($"{_config.BaseUrl}/posts", post, null, BuildAuthHeaders(), default).ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var createdPost = JsonSerializer.Deserialize<LinkedInPost>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<LinkedInPost> { createdPost }.Select(p => p.Attach<LinkedInPost>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating post: {ex.Message}");
            }
            return new List<LinkedInPost>();
        }

        // PUT methods for updating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.LinkedIn, PointType = EnumPointType.Function, ObjectType ="LinkedInPost",Name = "UpdatePost", Caption = "Update LinkedIn Post", ClassType ="LinkedInDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "linkedin.png", misc = "ReturnType: IEnumerable<LinkedInPost>")]
        public async Task<IEnumerable<LinkedInPost>> UpdatePostAsync(LinkedInPost post)
        {
            try
            {
                using var result = await PutAsync($"{_config.BaseUrl}/posts/{post.Id}", post, null, BuildAuthHeaders(), default).ConfigureAwait(false);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var updatedPost = JsonSerializer.Deserialize<LinkedInPost>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<LinkedInPost> { updatedPost }.Select(p => p.Attach<LinkedInPost>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating post: {ex.Message}");
            }
            return new List<LinkedInPost>();
        }
    }
}
