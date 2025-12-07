# Content Management Connectors

## Overview

The Content Management connectors category provides integration with content management systems (CMS), enabling content operations, page management, media handling, and content publishing. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys or OAuth 2.0
- **Models**: Strongly-typed POCO classes for content, pages, posts, media, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### WordPress (`WordPressDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `{site_url}/wp-json/wp/v2`  
**Authentication**: Application Password or OAuth 2.0

#### CommandAttribute Methods
- Post operations
- Page management
- Media operations
- User management
- Category/tag operations

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://yoursite.com/wp-json/wp/v2",
    AuthType = AuthTypeEnum.Basic,
    UserID = "your_username",
    Password = "your_application_password"
};
```

---

### Contentful (`ContentfulDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.contentful.com/spaces/{space_id}`  
**Authentication**: Access Token

#### CommandAttribute Methods
- Content operations
- Entry management
- Asset operations
- Space management
- Environment operations

---

### Drupal (`DrupalDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `{site_url}/jsonapi`  
**Authentication**: API Key or OAuth 2.0

#### CommandAttribute Methods
- Node operations
- Content type management
- User operations
- Taxonomy operations

---

### Joomla (`JoomlaDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `{site_url}/api/index.php/v1`  
**Authentication**: API Token

#### CommandAttribute Methods
- Article operations
- Category management
- User operations
- Media management

---

## Common Patterns

### CommandAttribute Structure

All CMS connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.CMSPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetPosts(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

CMS connectors typically support:
- **Posts/Articles** - Content creation and management
- **Pages** - Page operations
- **Media** - Media file management
- **Categories/Tags** - Taxonomy operations
- **Users** - User management

## Status

All Content Management connectors are **✅ Completed** and ready for use.

