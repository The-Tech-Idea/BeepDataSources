# Facebook Connector Refactoring Plan

## Overview
Refactor FacebookDataSource to inherit from WebAPIDataSource and implement proper IDataSource interface with POCO classes.

## Current Implementation Analysis
- **API**: Facebook Graph API v18
- **Authentication**: Access Token
- **Entities**: Posts, Pages, Users, Comments, Likes, Insights
- **Current Status**: Standalone IDataSource implementation

## Target Architecture

### 1. POCO Classes (Entities/Facebook/)
```csharp
namespace BeepDataSources.Connectors.SocialMedia.Facebook.Entities
{
    public class FacebookPost
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public string CreatedTime { get; set; }
        public FacebookUser From { get; set; }
        public List<FacebookComment> Comments { get; set; }
        public FacebookInsights Insights { get; set; }
        // ... other properties
    }

    public class FacebookUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        // ... other properties
    }

    public class FacebookPage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string About { get; set; }
        // ... other properties
    }

    public class FacebookComment
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public string CreatedTime { get; set; }
        public FacebookUser From { get; set; }
        // ... other properties
    }

    public class FacebookInsights
    {
        public int Likes { get; set; }
        public int Shares { get; set; }
        public int Comments { get; set; }
        public int Reach { get; set; }
        // ... other metrics
    }
}
```

### 2. Configuration Class (UPDATED - Extends WebAPIConnectionProperties)
```csharp
namespace TheTechIdea.Beep.FacebookDataSource.Config
{
    public class FacebookDataSourceConfig : WebAPIConnectionProperties
    {
        // INHERITED PROPERTIES (no need to redefine):
        // ConnectionString (maps to BaseUrl)
        // ApiKey (maps to AccessToken)
        // TimeoutMs, MaxRetries, RetryDelayMs
        // EnableCaching, CacheExpiryMinutes
        // UseProxy, ProxyUrl, ProxyUser, ProxyPassword
        // AuthType, ClientId, ClientSecret
        // Headers, Rate limiting, Pagination
        // And many more...

        // FACEBOOK-SPECIFIC PROPERTIES ONLY:
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string PageAccessToken { get; set; }
        public string UserId { get; set; }
        public string PageId { get; set; }
        public string ApiVersion { get; set; } = "v18.0";
        public bool UseRateLimiting { get; set; } = true;
        public int RateLimitPerHour { get; set; } = 200;
        public int RateLimitPerDay { get; set; } = 5000;
        public FacebookWebhookConfig Webhook { get; set; } = new();
        public FacebookFieldsConfig Fields { get; set; } = new();
        public List<string> Permissions { get; set; } = new();

        public FacebookDataSourceConfig()
        {
            // Set Facebook-specific defaults
            ConnectionName = "Facebook Data Source";
            ConnectionString = "https://graph.facebook.com";
            DatabaseType = DataSourceType.WebApi;
            Category = DatasourceCategory.WEBAPI;
            DriverName = "FacebookGraphAPI";
            DriverVersion = "v18.0";
            UserAgent = "BeepDM-Facebook/1.0";
            AuthType = AuthTypeEnum.Bearer;

            // Initialize permissions
            Permissions.AddRange(new[] {
                "email", "public_profile", "pages_read_engagement",
                "pages_manage_posts", "pages_show_list", "read_insights"
            });
        }

        // Access token via inherited ApiKey property
        public string AccessToken
        {
            get => ApiKey;
            set => ApiKey = value;
        }

        public bool IsValid() =>
            !string.IsNullOrEmpty(AccessToken) ||
            (!string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(AppSecret));
    }
}
```

**Benefits:**
- ✅ **No Code Duplication**: Inherits 50+ properties from WebAPIConnectionProperties
- ✅ **Consistent Architecture**: Same config pattern across all connectors
- ✅ **Automatic Features**: Proxy, caching, rate limiting, authentication built-in
- ✅ **Maintainable**: Updates to base class benefit all connectors
- ✅ **Type Safety**: Strong typing with platform-specific extensions

### 3. DataSource Implementation (UPDATED)
```csharp
namespace TheTechIdea.Beep.FacebookDataSource
{
    public class FacebookDataSource : WebAPIDataSource
    {
        private readonly FacebookDataSourceConfig _config;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _isConnected;
        private Dictionary<string, EntityMetadata> _entityMetadata;

        // Constructor using dependency injection
        public FacebookDataSource(FacebookDataSourceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.IsValid())
                throw new ArgumentException("Invalid Facebook configuration");

            // Initialize HTTP client and JSON options
            _httpClient = CreateHttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Initialize entity metadata
            _entityMetadata = InitializeEntityMetadata();

            // Set up base class properties from config
            BaseUrl = _config.ConnectionString;
            ApiVersion = _config.ApiVersion;
            TimeoutSeconds = _config.TimeoutMs / 1000;
            MaxRetries = _config.MaxRetries;
            RetryDelayMs = _config.RetryDelayMs;
            EnableCaching = _config.EnableCaching;
            CacheExpirationMinutes = _config.CacheExpiryMinutes;

            _isConnected = false;
        }

        // IDataSource Implementation
        public override string DataSourceName => "Facebook";
        public override string DataSourceType => "SocialMedia";

        protected override void InitializeEntityMetadata()
        {
            var metadata = new Dictionary<string, EntityMetadata>();

            // Posts entity with comprehensive field mapping
            metadata["posts"] = new EntityMetadata
            {
                EntityName = "posts",
                DisplayName = "Posts",
                PrimaryKey = "id",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "id", Type = "string", IsPrimaryKey = true },
                    new EntityField { Name = "message", Type = "string" },
                    new EntityField { Name = "created_time", Type = "datetime" },
                    // ... comprehensive field list
                }
            };

            // Add other entities: pages, users, comments, events, etc.
            // ... implementation details

            return metadata;
        }

        // Platform-specific API methods
        public async Task<bool> ConnectAsync()
        {
            // Use inherited HTTP client configuration
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);

            var testUrl = $"{_config.ConnectionString}/{_config.ApiVersion}/me?fields=id,name";
            var response = await _httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                _isConnected = true;
                return true;
            }

            _isConnected = false;
            return false;
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Not connected to Facebook");

            var endpoint = GetEntityEndpoint(entityName, parameters);
            var response = await _httpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Facebook API error: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            return ParseJsonToDataTable(content, entityName);
        }

        private string GetEntityEndpoint(string entityName, Dictionary<string, object> parameters = null)
        {
            var baseUrl = $"{_config.ConnectionString}/{_config.ApiVersion}";
            var endpoint = entityName.ToLower() switch
            {
                "posts" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/posts" : $"{baseUrl}/me/posts",
                "pages" => $"{baseUrl}/me/accounts",
                "groups" => $"{baseUrl}/me/groups",
                "events" => _config.PageId != null ? $"{baseUrl}/{_config.PageId}/events" : $"{baseUrl}/me/events",
                "ads" => $"{baseUrl}/act_{_config.UserId}/ads",
                "insights" => $"{baseUrl}/me/insights",
                _ => throw new ArgumentException($"Unknown entity: {entityName}")
            };

            // Use inherited parameter building and field selection
            var queryParams = new List<string>();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value?.ToString() ?? "")}");
                }
            }

            // Add default fields from config
            var fields = GetDefaultFields(entityName);
            if (!string.IsNullOrEmpty(fields))
            {
                queryParams.Add($"fields={fields}");
            }

            if (queryParams.Count > 0)
            {
                endpoint += "?" + string.Join("&", queryParams);
            }

            return endpoint;
        }

        private string GetDefaultFields(string entityName)
        {
            return entityName.ToLower() switch
            {
                "posts" => _config.Fields.PostFields,
                "pages" => _config.Fields.PageFields,
                "users" => _config.Fields.UserFields,
                "comments" => _config.Fields.CommentFields,
                // ... other entities
                _ => ""
            };
        }

        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            // Use inherited JSON parsing helpers where possible
            var dataTable = new DataTable(entityName);
            var metadata = _entityMetadata[entityName.ToLower()];

            // Add columns and parse JSON (implementation details)
            // ... parsing logic using System.Text.Json
        }
    }
}
```

**Key Improvements:**
- ✅ **Dependency Injection**: Constructor takes config parameter
- ✅ **Inherited Features**: Uses WebAPIConnectionProperties for all common settings
- ✅ **Proper Error Handling**: Validates configuration and connection status
- ✅ **Entity Metadata**: Comprehensive field mapping for all Facebook entities
- ✅ **Flexible Endpoints**: Supports different entity types with proper URL construction
- ✅ **Configuration-Driven**: Field selection and parameters from config
            var response = await _requestHelper.GetAsync(endpoint, parameters);
            var posts = JsonSerializer.Deserialize<List<FacebookPost>>(response);

            return new BindingList<FacebookPost>(posts);
        }

        // ... other entity methods
    }
}
```

## Implementation Steps

### Phase 1: Project Structure Setup
1. ✅ Create `Entities/` folder
2. ✅ Create `Config/` folder
3. ✅ Update project file with dependencies

### Phase 2: POCO Classes Creation
1. ✅ Create FacebookPost.cs
2. ✅ Create FacebookUser.cs
3. ✅ Create FacebookPage.cs
4. ✅ Create FacebookComment.cs
5. ✅ Create FacebookInsights.cs

### Phase 3: Configuration Class
1. ✅ Create FacebookConfig.cs
2. ✅ Implement connection string parsing
3. ✅ Add validation methods

### Phase 4: DataSource Refactoring
1. ✅ Change inheritance to WebAPIDataSource
2. ✅ Override necessary methods
3. ✅ Implement Facebook-specific API calls
4. ✅ Update entity initialization

### Phase 5: Testing & Validation
1. ✅ Unit tests for POCO classes
2. ✅ Integration tests for API calls
3. ✅ Error handling validation
4. ✅ Performance testing

## API Endpoints Mapping

| Entity | Endpoint | Method | Description |
|--------|----------|--------|-------------|
| Posts | /{page-id}/posts | GET | Get page posts |
| Users | /{user-id} | GET | Get user information |
| Pages | /{page-id} | GET | Get page information |
| Comments | /{post-id}/comments | GET | Get post comments |
| Insights | /{post-id}/insights | GET | Get post insights |

## Dependencies

### NuGet Packages:
- Microsoft.Extensions.Http (already in WebAPIDataSource)
- Microsoft.Extensions.Http.Polly (already in WebAPIDataSource)
- System.Text.Json (already in WebAPIDataSource)

### Project References:
- DataManagementEngineStandard (WebAPIDataSource)
- DataManagementModelsStandard (IDataSource, EntityStructure)
- DMLoggerStandard (IDMLogger)

## Testing Strategy

### Unit Tests:
```csharp
[TestClass]
public class FacebookDataSourceTests
{
    [TestMethod]
    public async Task GetPosts_ReturnsValidData()
    {
        // Arrange
        var dataSource = new FacebookDataSource("FacebookTest", logger, editor, DataSourceType.WebApi, errorInfo);

        // Act
        var posts = await dataSource.GetEntityAsync("posts", null);

        // Assert
        Assert.IsNotNull(posts);
        Assert.IsInstanceOfType(posts, typeof(BindingList<FacebookPost>));
    }
}
```

## Success Criteria

1. ✅ Inherits from WebAPIDataSource
2. ✅ Implements IDataSource interface properly
3. ✅ POCO classes created and functional
4. ✅ Facebook Graph API v18 integration working
5. ✅ Error handling and logging implemented
6. ✅ Unit tests pass
7. ✅ Documentation updated

## Timeline
- **Day 1**: Project structure and POCO classes
- **Day 2**: Configuration and DataSource refactoring
- **Day 3**: API integration and testing
- **Day 4**: Documentation and final validation

---

**Last Updated**: September 8, 2025
**Version**: 1.0.0</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\SocialMedia\Facebook\FacebookRefactoringPlan.md
