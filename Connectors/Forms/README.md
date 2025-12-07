# Forms Connectors

## Overview

The Forms connectors category provides integration with form builder and survey platforms, enabling form management, submission retrieval, response analysis, and form analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys
- **Models**: Strongly-typed POCO classes for forms, submissions, responses, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Typeform (`TypeformDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.typeform.com`  
**Authentication**: API Token

#### CommandAttribute Methods
- Form management
- Response retrieval
- Workspace operations
- Analytics and reporting

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.typeform.com",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "your_api_token"
};
```

---

### Jotform (`JotformDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.jotform.com`  
**Authentication**: API Key

#### CommandAttribute Methods
- Form operations
- Submission management
- User operations
- Report generation

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.jotform.com",
    AuthType = AuthTypeEnum.ApiKey,
    ApiKey = "your_api_key"
};
```

---

## Common Patterns

### CommandAttribute Structure

All forms connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.FormsPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetForms(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Forms connectors typically support:
- **Forms** - Form creation, management, and configuration
- **Submissions** - Form submission retrieval and processing
- **Responses** - Response data and analytics
- **Workspaces** - Organization and workspace management

## Status

All Forms connectors are **✅ Completed** and ready for use.

