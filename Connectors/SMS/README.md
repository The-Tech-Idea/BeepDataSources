# SMS Connectors

## Overview

The SMS connectors category provides integration with SMS service providers, enabling message sending, contact management, campaign operations, and message history tracking. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily API Key or Basic Authentication
- **Models**: Strongly-typed POCO classes for SMS messages, contacts, campaigns, and account information
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### ClickSend (`ClickSendDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://rest.clicksend.com/v3`  
**Authentication**: Basic Authentication (Username/Password) or API Key

#### Models
- `ClickSendSMS` - SMS message to send
- `ClickSendSMSMessage` - SMS message history item
- `ClickSendSMSResponse` - Response from sending SMS
- `ClickSendAccount` - Account information
- `ClickSendContact` - Contact information
- `ClickSendContactResponse` - Contact list response

#### CommandAttribute Methods

**Read Operations:**
- `GetSMSHistory()` - Get SMS message history
- `GetAccount()` - Get account information
- `GetContacts(string listId)` - Get contacts from a list

**Write Operations:**
- `SendSMS(ClickSendSMS sms)` - Send an SMS message
- `CreateContact(string listId, ClickSendContact contact)` - Create a new contact
- `UpdateContact(string listId, string contactId, ClickSendContact contact)` - Update an existing contact

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://rest.clicksend.com/v3",
    AuthType = AuthTypeEnum.Basic,
    UserID = "your_username",
    Password = "your_api_key"
};
```

#### Example Usage
```csharp
var clickSend = new ClickSendDataSource("ClickSend", logger, editor, DataSourceType.ClickSend, errors);
clickSend.Dataconnection.ConnectionProp = props;

// Get SMS history
var history = await clickSend.GetSMSHistory();

// Send SMS
var sms = new ClickSendSMS
{
    To = "+1234567890",
    Message = "Hello from BeepDM!",
    From = "BeepDM"
};
var response = await clickSend.SendSMS(sms);

// Get contacts
var contacts = await clickSend.GetContacts("list_id_here");
```

---

### Kudosity (`KudosityDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.kudosity.com`  
**Authentication**: API Key (Bearer Token)

#### Models
- `KudositySMS` - SMS message to send
- `KudositySMSResponse` - Response from sending SMS
- `KudosityMessageHistory` - Message history item
- `KudosityCampaign` - SMS campaign
- `KudosityContact` - Contact information
- `KudosityContactList` - Contact list
- `KudosityAccount` - Account information
- `KudosityWebhook` - Webhook configuration
- `KudosityTemplate` - Message template
- `KudosityPaginationResponse<T>` - Paginated response wrapper

#### CommandAttribute Methods

**Read Operations:**
- `GetMessageHistory()` - Get message history
- `GetCampaigns()` - Get all campaigns
- `GetContacts()` - Get all contacts
- `GetContactLists()` - Get contact lists
- `GetAccount()` - Get account information
- `GetWebhooks()` - Get webhook configurations
- `GetTemplates()` - Get message templates

**Write Operations:**
- `SendSMSAsync(KudositySMS sms)` - Send an SMS message
- `CreateContactAsync(KudosityContact contact)` - Create a new contact
- `CreateCampaignAsync(KudosityCampaign campaign)` - Create a new campaign

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.kudosity.com",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "your_api_key"
};
```

#### Example Usage
```csharp
var kudosity = new KudosityDataSource("Kudosity", logger, editor, DataSourceType.Kudosity, errors);
kudosity.Dataconnection.ConnectionProp = props;

// Get message history
var history = await kudosity.GetMessageHistory();

// Send SMS
var sms = new KudositySMS
{
    To = "+1234567890",
    Message = "Hello from BeepDM!",
    From = "BeepDM"
};
var response = await kudosity.SendSMSAsync(sms);

// Get campaigns
var campaigns = await kudosity.GetCampaigns();
```

---

## Common Patterns

### Entity Mapping

Both connectors use entity name mapping to API endpoints:

```csharp
private string GetEndpointForEntity(string entityName)
{
    return entityName.ToLower() switch
    {
        "sms_history" => "sms/history",
        "account" => "account",
        "contacts" => "lists/{list_id}/contacts",
        _ => null
    };
}
```

### Response Processing

Connectors process API responses based on entity type:

```csharp
private IEnumerable<object> ProcessApiResponse(string entityName, string jsonResponse)
{
    return entityName.ToLower() switch
    {
        "sms_history" => JsonSerializer.Deserialize<ResponseType>(jsonResponse)?.Data ?? new List<ModelType>(),
        "account" => new[] { JsonSerializer.Deserialize<AccountType>(jsonResponse) },
        _ => Array.Empty<object>()
    };
}
```

### CommandAttribute Structure

All methods use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    ObjectType = "ModelClassName",
    PointType = EnumPointType.Function,
    Name = "MethodName",
    Caption = "User-Friendly Description",
    ClassName = "DataSourceClassName",
    misc = "ReturnType: ReturnType"
)]
public async Task<ReturnType> MethodName(...)
{
    // Implementation
}
```

## Best Practices

1. **Error Handling**: Always handle API errors gracefully and log them
2. **Pagination**: Support pagination for list endpoints when available
3. **Contact Management**: Use contact lists for organizing recipients
4. **Campaigns**: Leverage campaign features for bulk messaging
5. **Templates**: Use message templates for consistent messaging
6. **Webhooks**: Configure webhooks for real-time delivery status updates

## Configuration Requirements

### ClickSend
- Username (API username)
- API Key (password)
- Base URL: `https://rest.clicksend.com/v3`

### Kudosity
- API Key (Bearer token)
- Base URL: `https://api.kudosity.com`

## Status

Both SMS connectors are **✅ Completed** and ready for use.

