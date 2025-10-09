# BeepDataSources

A comprehensive collection of data source connectors for the Beep framework, providing seamless integration with various APIs and services.

## 📁 Project Structure

```
BeepDataSources/
├── Connectors/
│   ├── Communication/
│   │   ├── Buffer/
│   │   └── Hootsuite/
│   ├── CRM/
│   ├── E-commerce/
│   ├── MailServices/
│   ├── Marketing/
│   └── SocialMedia/
│       ├── Twitter/
│       ├── Facebook/
│       ├── Instagram/
│       ├── LinkedIn/
│       ├── Pinterest/
│       ├── Reddit/
│       ├── Snapchat/
│       ├── TikTok/
│       └── YouTube/
├── DataManagementModelsStandard/
└── DataManagementEngineStandard/
```

## 🚀 Available Connectors

### Social Media Platforms
- **Twitter** - Twitter API v2 integration with strongly typed POCO models
- **Facebook** - Facebook Graph API integration
- **Instagram** - Instagram Basic Display API
- **LinkedIn** - LinkedIn API integration
- **Pinterest** - Pinterest API
- **Reddit** - Reddit API
- **Snapchat** - Snapchat API
- **TikTok** - TikTok API
- **YouTube** - YouTube Data API

### Communication Services
- **Buffer** - Social media scheduling platform
- **Hootsuite** - Social media management tool

### Forms & Surveys
- **Typeform** - Typeform API v2
- **Jotform** - Jotform API v1

### Content Management
- **WordPress** - WordPress REST API v2

### Task Management
- **Any.do** - Any.do API v2

### Meeting Tools
- **TLDV** - tl;dv API v1
- **Fathom** - Fathom Analytics API

### Business Intelligence
- **Tableau** - Tableau REST API v3.21

### SMS Services
- **ClickSend** - ClickSend REST API
- **Kudosity** - Kudosity SMS API

## 🏗️ Architecture

Each connector follows the `WebAPIDataSource` pattern:

- **Base Class**: Inherits from `WebAPIDataSource`
- **Models**: Strongly typed POCO classes with JSON serialization attributes
- **Entity Mapping**: REST API endpoints mapped to entity names
- **Authentication**: API key, Bearer token, or OAuth support
- **Command Attributes**: Dynamic loading support with metadata

## 🛠️ Development

### Prerequisites
- .NET 9.0
- Visual Studio 2022 or VS Code
- Beep framework dependencies

### Building
```bash
cd BeepDataSources/Connectors
dotnet build --no-restore
```

### Adding New Connectors
See `co-pilot.instructions.md` for detailed guidance on creating new data sources following established patterns.

## 📋 Connector Status

| Connector | Status | API Version | Authentication |
|-----------|--------|-------------|----------------|
| Twitter | ✅ Complete | v2 | Bearer Token |
| Facebook | ✅ Complete | Graph API | Access Token |
| Typeform | ✅ Complete | v2 | API Key |
| Jotform | ✅ Complete | v1 | API Key |
| WordPress | ✅ Complete | REST v2 | API Key |
| Any.do | ✅ Complete | v2 | API Key |
| TLDV | ✅ Complete | v1 | API Key |
| ClickSend | ✅ Complete | REST | API Key |
| Kudosity | ✅ Complete | API | API Key |
| Tableau | ✅ Complete | v3.21 | Personal Access Token |
| Loomly | ✅ Complete | API | API Key |
| Fathom | ✅ Complete | API | API Key |

## 🤝 Contributing

1. Follow the established patterns in existing connectors
2. Use strongly typed POCO models with `JsonPropertyName` attributes
3. Implement proper error handling and logging
4. Add comprehensive CommandAttribute metadata
5. Test compilation and basic functionality

## 📄 License

This project is part of the Beep framework ecosystem.