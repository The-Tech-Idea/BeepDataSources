# Meeting Tools Connectors

## Overview

The Meeting Tools connectors category provides integration with meeting recording and transcription platforms, enabling meeting management, recording operations, transcription retrieval, and analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys
- **Models**: Strongly-typed POCO classes for meetings, recordings, transcriptions, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Fathom (`FathomDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.fathom.ai/v1`  
**Authentication**: API Key

#### CommandAttribute Methods
- Meeting management
- Recording operations
- Transcription retrieval
- Analytics and insights

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.fathom.ai/v1",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "your_api_key"
};
```

---

### TLDV (`TLDVDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.tldv.io/v1`  
**Authentication**: API Key

#### CommandAttribute Methods
- Meeting operations
- Recording management
- Transcription operations
- Analytics

---

## Common Patterns

### CommandAttribute Structure

All meeting tools connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.MeetingTool,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetMeetings(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Meeting tools connectors typically support:
- **Meetings** - Meeting management and scheduling
- **Recordings** - Recording operations and retrieval
- **Transcriptions** - Transcription data and text
- **Analytics** - Meeting insights and metrics

## Status

All Meeting Tools connectors are **✅ Completed** and ready for use.

