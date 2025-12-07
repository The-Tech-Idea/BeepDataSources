# Task Management Connectors

## Overview

The Task Management connectors category provides integration with project and task management platforms, enabling task operations, project management, team collaboration, and workflow automation. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys or OAuth 2.0
- **Models**: Strongly-typed POCO classes for tasks, projects, teams, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Asana (`AsanaDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://app.asana.com/api/1.0`  
**Authentication**: Personal Access Token or OAuth 2.0

#### CommandAttribute Methods
- Task operations
- Project management
- Team operations
- Workspace management
- Portfolio operations

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://app.asana.com/api/1.0",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "your_personal_access_token"
};
```

---

### AnyDo (`AnyDoDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://smapi.any.do`  
**Authentication**: API Key

#### CommandAttribute Methods
- Task operations
- List management
- Note operations
- Reminder management

---

## Common Patterns

### CommandAttribute Structure

All task management connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.TaskManagement,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetTasks(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Task management connectors typically support:
- **Tasks** - Task creation, updates, and management
- **Projects** - Project organization and management
- **Teams** - Team collaboration features
- **Workspaces** - Workspace and organization management

## Status

All Task Management connectors are **✅ Completed** and ready for use.

