# Marketing Data Sources

A comprehensive collection of marketing automation and email marketing data source connectors for the Beep Data Connectors framework. This module provides seamless integration with leading marketing platforms through individual .NET projects with embedded driver logic.

## 🚀 Overview

The Marketing Data Sources module enables you to connect, read, and manage data from various marketing automation and email marketing platforms. Each platform is implemented as a separate data source following consistent patterns established by the Beep framework.

## 📦 Available Data Sources

### ✅ Completed Data Sources

| Platform | Status | Description | Authentication |
|----------|--------|-------------|----------------|
| **Mailchimp** | ✅ Complete | Leading email marketing platform with advanced automation | API Key + Data Center |

### 🔄 In Development

| Platform | Status | Description | Authentication |
|----------|--------|-------------|----------------|
| **ActiveCampaign** | ⏳ Pending | CRM and email marketing automation | API Key |
| **Klaviyo** | ⏳ Pending | E-commerce email marketing | Private API Key |
| **GoogleAds** | ⏳ Pending | Google advertising platform | OAuth 2.0 |
| **Marketo** | ⏳ Pending | Marketing automation platform | OAuth 2.0 |
| **ConstantContact** | ⏳ Pending | Email marketing and SMS | API Key |
| **Sendinblue** | ⏳ Pending | Email marketing and SMS (now Brevo) | API Key |
| **CampaignMonitor** | ⏳ Pending | Email marketing platform | API Key |
| **ConvertKit** | ⏳ Pending | Creator economy email marketing | API Key |
| **Drip** | ⏳ Pending | E-commerce marketing automation | API Token |
| **MailerLite** | ⏳ Pending | Email marketing and automation | API Key |

## 🛠️ Technical Specifications

- **Framework**: .NET 9.0
- **Architecture**: Individual projects with embedded drivers
- **Interface**: Implements `IDataSource` from Beep framework
- **Dependencies**: Microsoft.Extensions.Http, System.Text.Json

## 📁 Project Structure

```
Marketing/
├── README.md (this file)
├── progress.md
├── plan.md
├── MailchimpDataSource/
│   ├── MailchimpDataSource.csproj
│   └── MailchimpDataSource.cs
├── ActiveCampaignDataSource/
│   └── ActiveCampaignDataSource.csproj
├── [Other Platform]DataSource/
│   └── [Platform]DataSource.csproj
└── ...
```

## 🚀 Quick Start

### Prerequisites

1. **Beep Framework**: Ensure you have the Beep Data Connectors framework installed
2. **Platform Account**: You need an active account with the marketing platform
3. **API Credentials**: Obtain the required API keys/tokens from your platform account

### Installation

1. Clone or download the Marketing data sources
2. Reference the desired data source project in your solution
3. Ensure all NuGet dependencies are restored

## 📖 Usage Examples

### Mailchimp Data Source

#### Basic Connection

```csharp
using BeepDM.Connectors.Marketing.Mailchimp;
using Microsoft.Extensions.Logging;

// Create logger (you can use any logging framework)
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MailchimpDataSource>();

// Initialize the data source
var mailchimpDS = new MailchimpDataSource(logger);

// Connection parameters
var parameters = new Dictionary<string, object>
{
    ["ApiKey"] = "your-mailchimp-api-key",
    ["DataCenter"] = "us1"  // Replace with your data center (e.g., us1, us2, eu1)
};

// Connect to Mailchimp
bool connected = await mailchimpDS.ConnectAsync(parameters);
if (connected)
{
    Console.WriteLine("Successfully connected to Mailchimp!");
}
```

#### Get Available Entities

```csharp
// Get list of available entities
List<string> entities = await mailchimpDS.GetEntitiesAsync();
foreach (var entity in entities)
{
    Console.WriteLine($"Available entity: {entity}");
}
// Output: lists, campaigns, templates, automations, reports, segments, members, etc.
```

#### Retrieve Data

```csharp
// Get all lists
DataTable lists = await mailchimpDS.GetEntityAsync("lists");

// Get campaigns with parameters
var campaignParams = new Dictionary<string, object>
{
    ["count"] = 10,
    ["status"] = "sent"
};
DataTable campaigns = await mailchimpDS.GetEntityAsync("campaigns", campaignParams);

// Get entity metadata
DataTable metadata = await mailchimpDS.GetEntityMetadataAsync("lists");
```

#### Update Data

```csharp
// Create a new list
DataTable newList = new DataTable("lists");
newList.Columns.Add("name", typeof(string));
newList.Columns.Add("contact", typeof(object));
newList.Columns.Add("permission_reminder", typeof(string));

DataRow listRow = newList.NewRow();
listRow["name"] = "My New List";
listRow["contact"] = new { company = "My Company", address1 = "123 Main St", city = "Anytown", state = "CA", zip = "12345", country = "US" };
listRow["permission_reminder"] = "You signed up for updates from our company.";
newList.Rows.Add(listRow);

// Insert the new list
bool success = await mailchimpDS.UpdateEntityAsync("lists", newList);
```

#### Disconnect

```csharp
// Always disconnect when done
await mailchimpDS.DisconnectAsync();
```

### Connection String Format

Alternatively, you can use connection strings:

```csharp
// For Mailchimp
string connectionString = "ApiKey=your-api-key;DataCenter=us1";

// For other platforms (when implemented)
string connectionString = "ApiKey=your-api-key;ApiUrl=https://api.platform.com";
```

## 🔧 Configuration

### Mailchimp Configuration

To use Mailchimp data source, you need:

1. **API Key**: Generate from Mailchimp Account → Account → Extras → API Keys
2. **Data Center**: Found in your API key (e.g., `us1`, `us2`, `eu1`)

Example API Key format: `1234567890abcdef-us1`

### Authentication Methods by Platform

| Platform | Method | Parameters |
|----------|--------|------------|
| Mailchimp | API Key + Data Center | `ApiKey`, `DataCenter` |
| ActiveCampaign | API Key + URL | `ApiKey`, `ApiUrl` |
| Klaviyo | Private API Key | `ApiKey` |
| GoogleAds | OAuth 2.0 | `ClientId`, `ClientSecret`, `RefreshToken` |
| Marketo | OAuth 2.0 | `ClientId`, `ClientSecret`, `Endpoint` |

## 📊 Supported Entities

### Mailchimp Entities

- **lists**: Subscriber lists and their configuration
- **campaigns**: Email campaigns and performance data
- **templates**: Email templates
- **automations**: Marketing automation workflows
- **reports**: Campaign performance and analytics
- **segments**: List segments for targeted campaigns
- **members**: Individual subscribers
- **merge-fields**: Custom fields for lists
- **interest-categories**: Interest groups
- **interests**: Specific interests within categories

## 🐛 Error Handling

All data sources include comprehensive error handling:

```csharp
try
{
    bool connected = await mailchimpDS.ConnectAsync(parameters);
    if (!connected)
    {
        Console.WriteLine("Connection failed. Check your credentials.");
        return;
    }

    DataTable data = await mailchimpDS.GetEntityAsync("lists");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Log error details for debugging
}
finally
{
    await mailchimpDS.DisconnectAsync();
}
```

## 🔍 Testing Connection

```csharp
// Test connection without performing operations
bool isConnected = await mailchimpDS.TestConnectionAsync();

// Get connection information
Dictionary<string, object> info = await mailchimpDS.GetConnectionInfoAsync();
Console.WriteLine($"Data Source: {info["DataSourceName"]}");
Console.WriteLine($"Connected: {info["IsConnected"]}");
```

## 📈 Best Practices

1. **Connection Management**: Always disconnect when finished
2. **Error Handling**: Implement proper try-catch blocks
3. **Rate Limiting**: Be aware of API rate limits for each platform
4. **Data Types**: Check entity metadata for proper data types
5. **Authentication**: Keep API keys secure and rotate regularly

## 🛣️ Roadmap

### Phase 2: Core Implementation (In Progress)
- [x] MailchimpDataSource - Complete
- [ ] ActiveCampaignDataSource - In Development
- [ ] KlaviyoDataSource - In Development
- [ ] GoogleAdsDataSource - In Development
- [ ] MarketoDataSource - In Development
- [ ] Remaining platforms - Planned

### Phase 3: Advanced Features
- Platform-specific optimizations
- Bulk operations support
- Webhook integrations
- Advanced filtering and segmentation

## 📚 Additional Resources

- [Beep Framework Documentation](https://github.com/The-Tech-Idea/BeepDM)
- [Mailchimp API Documentation](https://mailchimp.com/developer/)
- [CRM Data Sources](../CRM/README.md) - Reference implementation pattern

## 🤝 Contributing

Contributions are welcome! Please see the main Beep Data Connectors repository for contribution guidelines.

## 📄 License

This project follows the same license as the Beep Data Connectors framework.

---

**Last Updated**: August 27, 2025
**Version**: 1.0.0</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\Marketing\plan.md
