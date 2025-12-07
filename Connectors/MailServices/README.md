# Mail Services Connectors

## Overview

The Mail Services connectors category provides integration with email service providers, enabling email sending, inbox management, message operations, and email analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0
- **Models**: Strongly-typed POCO classes for messages, folders, attachments, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Gmail (`GmailDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://gmail.googleapis.com/gmail/v1`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Message operations (send, read, delete)
- Label management
- Thread operations
- Attachment handling
- Search functionality

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://gmail.googleapis.com/gmail/v1",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://oauth2.googleapis.com/token"
};
```

---

### Outlook (`OutlookDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://graph.microsoft.com/v1.0/me/mail`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Message operations
- Folder management
- Calendar integration
- Contact operations
- Search functionality

---

### Yahoo (`YahooDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.login.yahoo.com`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Message operations
- Folder management
- Contact operations
- Calendar integration

---

## Common Patterns

### CommandAttribute Structure

All mail services connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.MailService,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetMessages(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Mail services connectors typically support:
- **Messages** - Email message operations
- **Folders** - Folder/label management
- **Attachments** - Attachment handling
- **Contacts** - Contact management
- **Threads** - Conversation threads

## Status

All Mail Services connectors are **✅ Completed** and ready for use.

