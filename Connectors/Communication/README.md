# Communication Connectors

## Overview

The Communication connectors category provides integration with team communication, messaging, and collaboration platforms. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0, API Keys, or Bot Tokens
- **Models**: Strongly-typed POCO classes for channels, messages, users, files, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Slack (`SlackDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://slack.com/api`  
**Authentication**: OAuth 2.0 (Bot Token or User Token)

#### CommandAttribute Methods

**Read Operations:**
- `GetChannels(List<AppFilter> filters)` - Get Slack channels
- `GetMessages(List<AppFilter> filters)` - Get messages from channels
- `GetUsers(List<AppFilter> filters)` - Get Slack users

#### Entities Supported
- channels, messages, users, files, reactions, teams, groups, im, mpim, bots, apps, auth, conversations, pins, reminders, search, stars, team, usergroups

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://slack.com/api",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "xoxb-your-bot-token"
};
```

---

### Microsoft Teams (`MicrosoftTeamsDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://graph.microsoft.com/v1.0`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Team management
- Channel operations
- Message retrieval
- User management

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://graph.microsoft.com/v1.0",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token"
};
```

---

### Discord (`DiscordDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://discord.com/api/v10`  
**Authentication**: Bot Token

#### CommandAttribute Methods
- Channel management
- Message operations
- User/guild management
- Webhook operations

---

### Telegram (`TelegramDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.telegram.org/bot{token}`  
**Authentication**: Bot Token

#### CommandAttribute Methods
- Message sending/receiving
- Chat management
- User operations
- File operations

---

### WhatsApp Business (`WhatsAppBusinessDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://graph.facebook.com/v18.0`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Message sending
- Contact management
- Template management
- Webhook configuration

---

### Zoom (`ZoomDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.zoom.us/v2`  
**Authentication**: OAuth 2.0 or JWT

#### CommandAttribute Methods
- Meeting management
- User operations
- Webinar operations
- Recording management

---

### Google Chat (`GoogleChatDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://chat.googleapis.com/v1`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Space management
- Message operations
- Member management

---

### Rocket.Chat (`RocketChatDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `{your-instance}/api/v1`  
**Authentication**: API Key or OAuth 2.0

#### CommandAttribute Methods
- Channel operations
- Message management
- User management

---

### Twist (`TwistDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.twist.com/api/v3`  
**Authentication**: API Token

#### CommandAttribute Methods
- Thread management
- Comment operations
- Workspace management

---

### Chanty (`ChantyDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.chanty.com/v1`  
**Authentication**: API Key

#### CommandAttribute Methods
- Team communication
- Message operations
- User management

---

### Flock (`FlockDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.flock.com/v1`  
**Authentication**: Bot Token

#### CommandAttribute Methods
- Channel management
- Message operations
- User operations

---

## Common Patterns

### CommandAttribute Structure

All connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Name = "MethodName",
    Caption = "User-Friendly Description",
    ObjectType = "ModelClassName",
    PointType = EnumPointType.Function,
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.Platform,
    ClassType = "DataSourceClassName",
    Showin = ShowinType.Both,
    Order = 1,
    iconimage = "icon.png"
)]
public async Task<IEnumerable<ModelClass>> MethodName(List<AppFilter> filters = null)
{
    // Implementation
}
```

### Entity Mapping Pattern

Connectors like Slack use entity-to-endpoint mapping:

```csharp
private static readonly Dictionary<string, (string endpoint, string root, ...)> Map
    = new(StringComparer.OrdinalIgnoreCase)
    {
        ["channels"] = ("conversations.list", "channels", Empty()),
        ["messages"] = ("conversations.history", "messages", Empty()),
        ["users"] = ("users.list", "members", Empty()),
        // ...
    };
```

## Common Entities

### Universal Entities
- **channels** - Communication channels/workspaces
- **messages** - Individual messages and conversations
- **users** - Platform users and members
- **files** - Shared files and attachments
- **reactions** - Message reactions and responses

### Platform-Specific Entities
- **teams** (Microsoft Teams) - Team organizations
- **meetings** (Zoom, Teams) - Video/audio conferences
- **threads** (Slack, Discord) - Conversation threads
- **bots** (Most platforms) - Automated assistants
- **webhooks** (Most platforms) - Event notifications

## Authentication Patterns

### OAuth 2.0 Platforms
- Slack, Microsoft Teams, Google Chat, Discord, Zoom, WhatsApp Business
- Requires client registration and user consent
- Supports refresh tokens for long-term access

### Token-Based Platforms
- Telegram, Twist, Flock
- Uses API tokens or bot tokens
- Simpler authentication flow

### API Key Platforms
- Chanty, Rocket.Chat
- Uses API keys for authentication
- Direct key-based access

## Best Practices

1. **Rate Limiting**: Respect platform rate limits (Slack: 1 req/sec, Discord: 50 req/sec)
2. **Pagination**: Support cursor-based or page-based pagination
3. **Error Handling**: Handle authentication failures, rate limits, and API errors gracefully
4. **Webhooks**: Configure webhooks for real-time updates when available
5. **Scopes**: Request appropriate OAuth scopes for required functionality

## Configuration Requirements

### Slack
- Bot Token: `xoxb-...` or User Token: `xoxp-...`
- Base URL: `https://slack.com/api`

### Microsoft Teams
- Client ID, Client Secret, Tenant ID
- Base URL: `https://graph.microsoft.com/v1.0`

### Discord
- Bot Token
- Base URL: `https://discord.com/api/v10`

### Telegram
- Bot Token (from @BotFather)
- Base URL: `https://api.telegram.org/bot{token}`

## Status

All Communication connectors are **✅ Completed** and ready for use.
