# Marketing Connectors

## Overview

The Marketing connectors category provides integration with email marketing, marketing automation, and advertising platforms, enabling campaign management, subscriber operations, analytics, and automation workflows. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Keys or OAuth 2.0
- **Models**: Strongly-typed POCO classes for campaigns, subscribers, lists, analytics, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Mailchimp (`MailchimpDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{dc}.api.mailchimp.com/3.0`  
**Authentication**: API Key

#### CommandAttribute Methods
- List management
- Subscriber operations
- Campaign management
- Automation workflows
- Analytics and reporting

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://us1.api.mailchimp.com/3.0",
    AuthType = AuthTypeEnum.Basic,
    UserID = "apikey",
    Password = "your_api_key"
};
```

---

### ActiveCampaign (`ActiveCampaignDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{account}.api-us1.com/api/3`  
**Authentication**: API Key

#### CommandAttribute Methods
- Contact management
- Campaign operations
- Automation workflows
- List management
- Deal tracking

---

### Campaign Monitor (`CampaignMonitorDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.createsend.com/api/v3.3`  
**Authentication**: API Key

#### CommandAttribute Methods
- List management
- Subscriber operations
- Campaign management
- Template operations
- Analytics

---

### Constant Contact (`ConstantContactDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.cc.email/v3`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Contact management
- Campaign operations
- List management
- Email tracking

---

### ConvertKit (`ConvertKitDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.convertkit.com/v3`  
**Authentication**: API Key + API Secret

#### CommandAttribute Methods
- Subscriber management
- Form operations
- Sequence management
- Tag operations
- Webhook configuration

---

### Drip (`DripDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.getdrip.com/v2`  
**Authentication**: API Token

#### CommandAttribute Methods
- Subscriber management
- Campaign operations
- Workflow automation
- Event tracking

---

### Google Ads (`GoogleAdsDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://googleads.googleapis.com/v14`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Campaign management
- Ad group operations
- Keyword management
- Performance reporting
- Budget management

---

### Klaviyo (`KlaviyoDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://a.klaviyo.com/api`  
**Authentication**: API Key

#### CommandAttribute Methods
- List management
- Profile operations
- Event tracking
- Flow management
- Analytics

---

### MailerLite (`MailerLiteDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.mailerlite.com/api/v2`  
**Authentication**: API Key

#### CommandAttribute Methods
- Subscriber management
- Campaign operations
- Group management
- Automation workflows

---

### Marketo (`MarketoDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{instance}.mktorest.com/rest`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Lead management
- Campaign operations
- Program management
- Activity tracking
- Analytics

---

### Sendinblue (`SendinblueDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.sendinblue.com/v3`  
**Authentication**: API Key

#### CommandAttribute Methods
- Contact management
- Campaign operations
- List management
- Transactional emails
- Analytics

---

## Common Patterns

### CommandAttribute Structure

All marketing connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.MarketingPlatform,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetEntities(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Marketing connectors typically support:
- **Lists** - Email lists and segments
- **Subscribers/Contacts** - Subscriber management and segmentation
- **Campaigns** - Email campaign creation and management
- **Automations** - Automated workflows and sequences
- **Analytics** - Campaign performance and subscriber analytics
- **Templates** - Email templates and designs
- **Tags** - Subscriber tagging and segmentation

## Authentication Patterns

### API Key Platforms
- Mailchimp, ActiveCampaign, Campaign Monitor, ConvertKit, Drip, Klaviyo, MailerLite, Sendinblue
- Uses API keys for authentication
- Direct key-based access

### OAuth 2.0 Platforms
- Constant Contact, Google Ads, Marketo
- Requires client registration and user consent
- Supports refresh tokens for long-term access

## Best Practices

1. **Rate Limiting**: Respect platform rate limits (Mailchimp: varies, Google Ads: 15,000 req/day)
2. **List Hygiene**: Maintain clean subscriber lists and handle unsubscribes
3. **Segmentation**: Use tags and segments for targeted campaigns
4. **Automation**: Leverage automation workflows for efficiency
5. **Analytics**: Track campaign performance and subscriber engagement
6. **Compliance**: Follow email marketing regulations (CAN-SPAM, GDPR)

## Configuration Requirements

### Mailchimp
- API Key
- Data Center (us1, us2, etc.)

### ActiveCampaign
- API Key
- Account URL

### Google Ads
- Client ID, Client Secret
- Developer Token
- OAuth 2.0 tokens

## Status

All Marketing connectors are **✅ Completed** and ready for use. See `progress.md` for detailed implementation status.
