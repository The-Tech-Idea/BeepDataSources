# Connector Refactoring Plan - IDataSource Implementation Using WebAPIDataSource

## Overview
This document outlines the comprehensive plan to refactor all connector implementations to use `WebAPIDataSource` as the base class and implement proper `IDataSource` interface compliance. Each connector will have POCO classes for data entities and follow a consistent pattern.

## Current State Analysis
- **SocialMedia Connectors**: 12 platforms (Facebook, Twitter, Instagram, LinkedIn, Pinterest, YouTube, TikTok, Snapchat, Reddit, Buffer, Hootsuite, TikTokAds)
- **CustomerSupport Connectors**: 7 platforms (Zendesk, Freshdesk, HelpScout, ZohoDesk, Kayako, LiveAgent, Front)
- **Current Implementation**: Standalone classes implementing IDataSource directly
- **Target Implementation**: Inherit from WebAPIDataSource,WEBAPIDataConnection, and use POCO classes, implement IDataSource properly
- ** Learned Points**: FROM C:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\DataSourcesPlugins\RDBMSDataSource WHERE RDBDataSource.cs and RDBDataConnnection is.

## Architecture Changes

### 1. Base Class Structure
```csharp
public class PlatformDataSource : WebAPIDataSource
{
    // Platform-specific configuration
    // POCO classes for entities
    // Platform-specific API logic
    // IDataSource interface implementation
}
```

### 2. POCO Classes Structure
```csharp
namespace Connectors.Platform.Entities
{
    public class Ticket { /* properties */ }
    public class User { /* properties */ }
    public class Organization { /* properties */ }
    // ... other entities
}
```

### 3. Configuration Architecture (UPDATED)
**Use WebAPIConnectionProperties as Base Configuration**
```csharp
// OLD APPROACH (avoid duplication)
public class PlatformConfig
{
    public string BaseUrl { get; set; }           // Duplicate
    public int TimeoutSeconds { get; set; }      // Duplicate
    public int MaxRetries { get; set; }          // Duplicate
    public bool EnableCaching { get; set; }      // Duplicate
    // ... many more duplicates
    public string ApiKey { get; set; }           // Platform-specific
    public string PlatformSecret { get; set; }   // Platform-specific
}

// NEW APPROACH (extend WebAPIConnectionProperties)
public class PlatformDataSourceConfig : WebAPIConnectionProperties
{
    // Inherits ALL common properties:
    // - ConnectionString (BaseUrl)
    // - TimeoutMs, MaxRetries, RetryDelayMs
    // - EnableCaching, CacheExpiryMinutes
    // - Proxy settings (UseProxy, ProxyUrl, etc.)
    // - Authentication (ApiKey, UserID, Password, ClientId, etc.)
    // - Headers, Rate limiting, Pagination
    // - And many more...

    // Add only platform-specific properties:
    public string PlatformSecret { get; set; }
    public string PlatformApiVersion { get; set; }
    public List<string> PlatformPermissions { get; set; }
    public Dictionary<string, object> PlatformSettings { get; set; }
}
```

**Benefits of New Approach:**
- ✅ Eliminates code duplication
- ✅ Consistent configuration across all connectors
- ✅ Automatic inheritance of all Web API features
- ✅ Easier maintenance and updates
- ✅ Standardized authentication patterns
- ✅ Built-in proxy, caching, rate limiting support

### 4. Namespace Convention
All connectors must use the standardized namespace pattern:
```csharp
namespace TheTechIdea.Beep.{PlatformName}DataSource
{
    // Classes for this connector
}
```

Example:
- `TheTechIdea.Beep.FacebookDataSource`
- `TheTechIdea.Beep.TwitterDataSource`
- `TheTechIdea.Beep.ZendeskDataSource`

## Implementation Strategy

### Phase 1: SocialMedia Connectors (12 platforms)
1. **Facebook** - Graph API v18
2. **Twitter** - Twitter API v2
3. **Instagram** - Instagram Basic Display API
4. **LinkedIn** - LinkedIn Marketing API
5. **Pinterest** - Pinterest API v5
6. **YouTube** - YouTube Data API v3
7. **TikTok** - TikTok for Developers API
8. **Snapchat** - Snapchat Marketing API
9. **Reddit** - Reddit API
10. **Buffer** - Buffer API
11. **Hootsuite** - Hootsuite API
12. **TikTokAds** - TikTok Ads API

### Phase 2: CustomerSupport Connectors (7 platforms)
1. **Zendesk** - Zendesk REST API v2
2. **Freshdesk** - Freshdesk REST API v2
3. **HelpScout** - HelpScout REST API v2
4. **ZohoDesk** - Zoho Desk REST API v1
5. **Kayako** - Kayako REST API v1
6. **LiveAgent** - LiveAgent REST API v3
7. **Front** - Front REST API v1

## Common Implementation Pattern

### For Each Connector:
1. **Create POCO Classes** - Define entity classes in `Entities/` folder
2. **Create Configuration Class** - Platform-specific config in `Config/` folder
3. **Refactor DataSource Class** - Inherit from WebAPIDataSource
4. **Implement Platform Logic** - Override methods for platform-specific behavior
5. **Update Project File** - Add necessary NuGet packages
6. **Create Tests** - Unit tests for the implementation

### File Structure per Connector:
```
Platform/
├── Platform.csproj
├── PlatformDataSource.cs (inherits WebAPIDataSource)
├── Config/
│   └── PlatformConfig.cs
├── Entities/
│   ├── Entity1.cs
│   ├── Entity2.cs
│   └── ...
└── Tests/
    └── PlatformDataSourceTests.cs
```

## Timeline

- **Week 1-2**: SocialMedia connectors refactoring (6 platforms)
- **Week 3**: SocialMedia connectors completion (6 platforms)
- **Week 4**: CustomerSupport connectors refactoring (7 platforms)
- **Week 5**: Testing, documentation, and final integration

## Dependencies

### NuGet Packages to Add:
- Microsoft.Extensions.Http
- Microsoft.Extensions.Http.Polly
- System.Text.Json
- System.Net.Http.Json

### Project References:
- DataManagementEngineStandard
- DataManagementModelsStandard
- DMLoggerStandard

## Quality Assurance

### Testing Strategy:
1. **Unit Tests** - For each platform's data source methods
2. **Integration Tests** - End-to-end API connectivity
3. **Performance Tests** - Rate limiting and caching
4. **Error Handling Tests** - Various failure scenarios

### Documentation:
1. **API Documentation** - For each platform's entities and methods
2. **Usage Examples** - Sample code for common operations
3. **Configuration Guide** - Setup and authentication instructions

## Success Criteria

1. ✅ All connectors inherit from WebAPIDataSource
2. ✅ All connectors implement IDataSource interface properly
3. ✅ POCO classes created for all entities
4. ✅ Consistent error handling and logging
5. ✅ Rate limiting and caching implemented
6. ✅ Unit tests pass for all platforms
7. ✅ Documentation complete and accurate

---

**Last Updated**: September 8, 2025
**Version**: 1.0.0</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\ConnectorRefactoringPlan.md
