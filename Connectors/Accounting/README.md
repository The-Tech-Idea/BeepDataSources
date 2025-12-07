# Accounting Connectors

## Overview

The Accounting connectors category provides integration with accounting and financial management platforms, enabling invoice management, expense tracking, financial reporting, and bookkeeping operations. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0, API Keys, or Consumer Key/Secret
- **Models**: Strongly-typed POCO classes for invoices, expenses, contacts, accounts, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### QuickBooks Online (`QuickBooksOnlineDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://sandbox-quickbooks.api.intuit.com/v3/company/{companyId}`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice management
- Expense tracking
- Contact operations
- Account management
- Financial reporting

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://sandbox-quickbooks.api.intuit.com/v3",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer"
};
```

---

### Xero (`XeroDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.xero.com/api.xro/2.0`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice operations
- Contact management
- Bank transaction tracking
- Expense management
- Financial reporting

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.xero.com/api.xro/2.0",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://identity.xero.com/connect/token"
};
```

---

### FreshBooks (`FreshBooksDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.freshbooks.com`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice management
- Expense tracking
- Client operations
- Project management
- Time tracking

---

### Sage Intacct (`SageIntacctDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.intacct.com/ia/xml/xmlgw.phtml`  
**Authentication**: API Key + User Credentials

#### CommandAttribute Methods
- Financial data operations
- Invoice management
- Expense tracking
- Account operations
- Reporting

---

### MYOB (`MYOBDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.myob.com/accountright`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice operations
- Contact management
- Account management
- Financial reporting

---

### Zoho Books (`ZohoBooksDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://books.zoho.com/api/v3`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice management
- Expense tracking
- Contact operations
- Bank reconciliation
- Financial reporting

---

### Wave (`WaveDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.waveapps.com`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Invoice operations
- Expense tracking
- Contact management
- Account operations

---

## Common Patterns

### CommandAttribute Structure

All accounting connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.AccountingPlatform,
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

Accounting connectors typically support:
- **Invoices** - Invoice creation, management, and tracking
- **Expenses** - Expense recording and categorization
- **Contacts** - Customer and vendor management
- **Accounts** - Chart of accounts and account management
- **Transactions** - Bank and financial transactions
- **Reports** - Financial reports and analytics
- **Payments** - Payment processing and tracking

## Authentication Patterns

### OAuth 2.0 Platforms
- QuickBooks Online, Xero, FreshBooks, MYOB, Zoho Books, Wave
- Requires client registration and user consent
- Supports refresh tokens for long-term access

### API Key Platforms
- Sage Intacct (API Key + User Credentials)
- Uses API keys with user authentication

## Best Practices

1. **Rate Limiting**: Respect platform rate limits (QuickBooks: 500 req/min, Xero: 60 req/min)
2. **Data Sync**: Implement incremental sync for large datasets
3. **Error Handling**: Handle authentication failures, rate limits, and API errors gracefully
4. **Financial Data**: Ensure proper handling of currency and decimal precision
5. **Audit Trail**: Maintain audit logs for financial data changes
6. **Compliance**: Follow accounting standards and regulations

## Configuration Requirements

### QuickBooks Online
- Client ID, Client Secret
- Company ID
- OAuth 2.0 tokens

### Xero
- Client ID, Client Secret
- Tenant ID
- OAuth 2.0 tokens

### FreshBooks
- Client ID, Client Secret
- Account ID
- OAuth 2.0 tokens

## Status

All Accounting connectors are **✅ Completed** and ready for use. See `progress.md` for detailed implementation status.

