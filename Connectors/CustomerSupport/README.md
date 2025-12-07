# Customer Support Connectors

## Overview

The Customer Support connectors category provides integration with help desk and customer support platforms, enabling ticket management, customer communication, knowledge base operations, and support analytics. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys or OAuth 2.0
- **Models**: Strongly-typed POCO classes for tickets, contacts, agents, articles, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Zendesk (`ZendeskDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{subdomain}.zendesk.com/api/v2`  
**Authentication**: API Token or OAuth 2.0

#### CommandAttribute Methods
- Ticket management
- User/contact operations
- Agent management
- Knowledge base articles
- Analytics and reporting

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://yourdomain.zendesk.com/api/v2",
    AuthType = AuthTypeEnum.Basic,
    UserID = "your_email/token",
    Password = "your_api_token"
};
```

---

### Freshdesk (`FreshdeskDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{domain}.freshdesk.com/api/v2`  
**Authentication**: API Key

#### CommandAttribute Methods
- Ticket operations
- Contact management
- Agent operations
- Solution articles
- Time tracking

---

### HelpScout (`HelpScoutDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.helpscout.net/v2`  
**Authentication**: API Key

#### CommandAttribute Methods
- Conversation management
- Customer operations
- Mailbox management
- Article operations
- Reporting

---

### Zoho Desk (`ZohoDeskDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://desk.zoho.com/api/v1`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Ticket management
- Contact operations
- Agent management
- Knowledge base
- Analytics

---

### Front (`FrontDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api2.frontapp.com`  
**Authentication**: API Token

#### CommandAttribute Methods
- Conversation management
- Contact operations
- Channel management
- Analytics

---

### LiveAgent (`LiveAgentDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{domain}.ladesk.com/api/v3`  
**Authentication**: API Key

#### CommandAttribute Methods
- Ticket operations
- Contact management
- Agent operations
- Chat management

---

### Kayako (`KayakoDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{domain}.kayako.com/api/v1`  
**Authentication**: API Key

#### CommandAttribute Methods
- Ticket management
- User operations
- Staff management
- Knowledge base

---

## Common Patterns

### CommandAttribute Structure

All customer support connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.SupportPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetTickets(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Customer support connectors typically support:
- **Tickets** - Ticket creation, updates, status management
- **Contacts** - Customer contact management
- **Agents** - Agent/staff management
- **Articles** - Knowledge base articles and solutions
- **Conversations** - Message threads and communications
- **Analytics** - Support metrics and reporting

## Authentication Patterns

### API Key Platforms
- Freshdesk, HelpScout, Front, LiveAgent, Kayako
- Uses API keys for authentication
- Direct key-based access

### API Token Platforms
- Zendesk (API Token), Front
- Uses API tokens for authentication

### OAuth 2.0 Platforms
- Zoho Desk
- Requires client registration and user consent

## Best Practices

1. **Rate Limiting**: Respect platform rate limits (Zendesk: 700 req/min, Freshdesk: varies)
2. **Ticket Lifecycle**: Properly manage ticket status transitions
3. **Agent Assignment**: Efficiently assign tickets to appropriate agents
4. **Knowledge Base**: Maintain and update knowledge base articles
5. **Analytics**: Track support metrics and performance
6. **Automation**: Leverage automation rules for ticket routing

## Configuration Requirements

### Zendesk
- Subdomain
- Email/API Token
- Base URL: `https://{subdomain}.zendesk.com/api/v2`

### Freshdesk
- Domain
- API Key
- Base URL: `https://{domain}.freshdesk.com/api/v2`

### HelpScout
- API Key
- Base URL: `https://api.helpscout.net/v2`

## Status

All Customer Support connectors are **✅ Completed** and ready for use. See `progress.md` for detailed implementation status.

