# Communication Data Sources

This directory contains individual data source projects for various communication platforms, implemented as part of the Beep Data Connectors framework. Each platform is implemented as a separate .NET project with embedded driver logic.

## Available Platforms

### Enterprise & Team Communication
- **Slack**: Team communication and collaboration platform
- **Microsoft Teams**: Enterprise communication and collaboration platform
- **Google Chat**: Google's enterprise messaging platform
- **Zoom**: Video conferencing and communication platform

### Social & Community Communication
- **Discord**: Gaming and community communication platform
- **Telegram**: Cloud-based instant messaging platform
- **WhatsApp Business**: Business communication platform

### Specialized Communication
- **Twist**: Asynchronous communication platform
- **Chanty**: Team communication platform
- **Rocket.Chat**: Open-source team communication platform
- **Flock**: Team messaging and collaboration platform

## Project Structure

Each platform follows the same structure:
```
PlatformDataSource/
├── PlatformDataSource.csproj
└── PlatformDataSource.cs (to be implemented)
```

## Common Features

All communication data sources implement the `IDataSource` interface and provide:

- **Authentication**: Platform-specific authentication handling
- **Entity Discovery**: List available entities (channels, messages, users, etc.)
- **CRUD Operations**: Create, Read, Update, Delete operations where supported
- **Metadata**: Entity structure and field information
- **Error Handling**: Comprehensive error handling and logging

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Beep Data Connectors framework
- Platform-specific API credentials

### Installation
1. Clone the repository
2. Navigate to the desired platform directory
3. Build the project: `dotnet build`

### Configuration
Each platform requires specific configuration parameters:

#### Slack
```csharp
var parameters = new Dictionary<string, object>
{
    ["BotToken"] = "xoxb-your-bot-token",
    ["UserToken"] = "xoxp-your-user-token" // optional
};
```

#### Microsoft Teams
```csharp
var parameters = new Dictionary<string, object>
{
    ["ClientId"] = "your-client-id",
    ["ClientSecret"] = "your-client-secret",
    ["TenantId"] = "your-tenant-id"
};
```

#### Zoom
```csharp
var parameters = new Dictionary<string, object>
{
    ["ClientId"] = "your-client-id",
    ["ClientSecret"] = "your-client-secret",
    ["AccountId"] = "your-account-id"
};
```

#### Discord
```csharp
var parameters = new Dictionary<string, object>
{
    ["BotToken"] = "your-bot-token",
    ["ApplicationId"] = "your-application-id"
};
```

#### Telegram
```csharp
var parameters = new Dictionary<string, object>
{
    ["BotToken"] = "your-bot-token"
};
```

#### WhatsApp Business
```csharp
var parameters = new Dictionary<string, object>
{
    ["AccessToken"] = "your-access-token",
    ["PhoneNumberId"] = "your-phone-number-id"
};
```

## Usage Example

```csharp
using BeepDM.DataManagementModelsStandard;
using BeepDM.Connectors.Communication.Slack;

// Create data source instance
var dataSource = new SlackDataSource(logger);

// Configure connection parameters
var parameters = new Dictionary<string, object>
{
    ["BotToken"] = "xoxb-your-bot-token"
};

// Connect to Slack API
await dataSource.ConnectAsync(parameters);

// Get available entities
var entities = await dataSource.GetEntitiesAsync();

// Get messages from a channel
var messages = await dataSource.GetEntityAsync("messages", new Dictionary<string, object>
{
    ["channel"] = "C1234567890"
});

// Disconnect
await dataSource.DisconnectAsync();
```

## Common Entities

### Universal Entities
- **channels**: Communication channels/workspaces
- **messages**: Individual messages and conversations
- **users**: Platform users and members
- **files**: Shared files and attachments
- **reactions**: Message reactions and responses

### Platform-Specific Entities
- **teams** (Microsoft Teams): Team organizations
- **meetings** (Zoom, Teams): Video/audio conferences
- **threads** (Slack, Discord): Conversation threads
- **bots** (Most platforms): Automated assistants
- **webhooks** (Most platforms): Event notifications

## Authentication Patterns

### OAuth 2.0 Platforms
- Slack, Microsoft Teams, Google Chat, Discord, Zoom
- Requires client registration and user consent
- Supports refresh tokens for long-term access

### Token-Based Platforms
- Telegram, WhatsApp Business, Twist, Flock
- Uses API tokens or access tokens
- Simpler authentication flow

### API Key Platforms
- Chanty, Rocket.Chat
- Uses API keys for authentication
- Direct key-based access

## Error Handling

All data sources include comprehensive error handling for:
- Authentication failures
- Rate limiting
- Network connectivity issues
- API errors and exceptions
- Invalid parameters

## Rate Limiting

Each platform implements appropriate rate limiting:
- **Slack**: 1 request per second (free tier), higher for paid plans
- **Discord**: 50 requests per second for bots
- **Microsoft Teams**: Varies by endpoint, generally 10-100 requests per second
- **Zoom**: 1 request per second (free tier), higher for paid plans

## Contributing

1. Follow the established pattern for new platforms
2. Implement the `IDataSource` interface completely
3. Add comprehensive error handling
4. Update documentation
5. Test thoroughly

## License

This project is part of the Beep Data Connectors framework.

## Support

For issues and questions:
1. Check platform-specific API documentation
2. Review existing implementations
3. Create an issue in the main repository

---

**Last Updated**: August 27, 2025
**Version**: 1.0.0
