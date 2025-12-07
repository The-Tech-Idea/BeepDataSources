# Business Intelligence Connectors

## Overview

The Business Intelligence connectors category provides integration with BI and analytics platforms, enabling dashboard management, report operations, data visualization, and analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0
- **Models**: Strongly-typed POCO classes for dashboards, reports, datasets, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Power BI (`PowerBIDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.powerbi.com/v1.0/myorg`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Dashboard operations
- Report management
- Dataset operations
- Workspace management
- Data refresh operations

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.powerbi.com/v1.0/myorg",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token"
};
```

---

### Tableau (`TableauDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{server}/api/{version}`  
**Authentication**: Username/Password or Personal Access Token

#### CommandAttribute Methods
- Workbook operations
- View management
- Site operations
- Data source management
- User operations

---

## Common Patterns

### CommandAttribute Structure

All BI connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.BIPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetDashboards(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

BI connectors typically support:
- **Dashboards** - Dashboard management and operations
- **Reports** - Report creation and management
- **Datasets** - Data source and dataset operations
- **Workspaces** - Workspace and site management

## Status

All Business Intelligence connectors are **✅ Completed** and ready for use.

