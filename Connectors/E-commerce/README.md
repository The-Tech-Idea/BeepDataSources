# E-commerce Data Sources

A comprehensive collection of e-commerce platform data source connectors for the Beep Data Connectors framework. This module provides seamless integration with leading e-commerce platforms through individual .NET projects with embedded driver logic.

## 🚀 Overview

The E-commerce Data Sources module enables you to connect, read, and manage data from various e-commerce platforms. Each platform is implemented as a separate data source following consistent patterns established by the Beep framework.

## 📦 Available Data Sources

### ✅ Completed Data Sources

| Platform | Status | Description | Authentication |
|----------|--------|-------------|----------------|
| **Shopify** | ✅ Complete | Leading cloud-based e-commerce platform | API Key + Store URL |

### 🔄 In Development

| Platform | Status | Description | Authentication |
|----------|--------|-------------|----------------|
| **WooCommerce** | ⏳ Pending | WordPress e-commerce plugin | Consumer Key/Secret |
| **Magento** | ⏳ Pending | Adobe Commerce (enterprise e-commerce) | Admin Token / OAuth |
| **BigCommerce** | ⏳ Pending | Enterprise e-commerce platform | Client ID/Secret |
| **Squarespace** | ⏳ Pending | Website builder with e-commerce | API Key |
| **Wix** | ⏳ Pending | Website builder with e-commerce | API Key / OAuth |
| **Etsy** | ⏳ Pending | Handmade marketplace | OAuth 2.0 |
| **OpenCart** | ⏳ Pending | PHP-based e-commerce platform | API Key |
| **Ecwid** | ⏳ Pending | E-commerce widgets | API Token |
| **Volusion** | ⏳ Pending | E-commerce for growing businesses | API Key |

## 🛠️ Technical Specifications

- **Framework**: .NET 9.0
- **Architecture**: Individual projects with embedded drivers
- **Interface**: Implements `IDataSource` from Beep framework
- **Dependencies**: Microsoft.Extensions.Http, System.Text.Json

## 📁 Project Structure

```
E-commerce/
├── README.md (this file)
├── progress.md
├── plan.md
├── ShopifyDataSource/
│   ├── ShopifyDataSource.csproj
│   └── ShopifyDataSource.cs
├── WooCommerceDataSource/
│   └── WooCommerceDataSource.csproj
├── [Other Platform]DataSource/
│   └── [Platform]DataSource.csproj
└── ...
```

## 🚀 Quick Start

### Prerequisites

1. **Beep Framework**: Ensure you have the Beep Data Connectors framework installed
2. **Platform Account**: You need an active e-commerce store account
3. **API Credentials**: Obtain the required API keys/tokens from your platform account

### Installation

1. Clone or download the E-commerce data sources
2. Reference the desired data source project in your solution
3. Ensure all NuGet dependencies are restored

## 📖 Usage Examples

### Shopify Data Source

#### Basic Connection

```csharp
using BeepDM.Connectors.Ecommerce.Shopify;
using Microsoft.Extensions.Logging;

// Create logger (you can use any logging framework)
var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ShopifyDataSource>();

// Initialize the data source
var shopifyDS = new ShopifyDataSource(logger);

// Connection parameters
var parameters = new Dictionary<string, object>
{
    ["ApiKey"] = "your-shopify-api-key",
    ["StoreUrl"] = "your-store.myshopify.com"  // Your Shopify store domain
};

// Connect to Shopify
bool connected = await shopifyDS.ConnectAsync(parameters);
if (connected)
{
    Console.WriteLine("Successfully connected to Shopify!");
}
```

#### Get Available Entities

```csharp
// Get list of available entities
List<string> entities = await shopifyDS.GetEntitiesAsync();
foreach (var entity in entities)
{
    Console.WriteLine($"Available entity: {entity}");
}
// Output: products, orders, customers, collections, inventory, analytics, etc.
```

#### Retrieve Products

```csharp
// Get all products
DataTable products = await shopifyDS.GetEntityAsync("products");

// Get products with parameters
var productParams = new Dictionary<string, object>
{
    ["limit"] = 50,
    ["status"] = "active",
    ["collection_id"] = "123456789"
};
DataTable filteredProducts = await shopifyDS.GetEntityAsync("products", productParams);

// Get specific product by ID
var singleProductParams = new Dictionary<string, object>
{
    ["id"] = "987654321"
};
DataTable product = await shopifyDS.GetEntityAsync("products", singleProductParams);
```

#### Retrieve Orders

```csharp
// Get all orders
DataTable orders = await shopifyDS.GetEntityAsync("orders");

// Get orders with filters
var orderParams = new Dictionary<string, object>
{
    ["status"] = "any",
    ["limit"] = 100,
    ["created_at_min"] = "2024-01-01T00:00:00Z"
};
DataTable recentOrders = await shopifyDS.GetEntityAsync("orders", orderParams);
```

#### Update Product Data

```csharp
// Create a new product
DataTable newProduct = new DataTable("products");
newProduct.Columns.Add("title", typeof(string));
newProduct.Columns.Add("body_html", typeof(string));
newProduct.Columns.Add("vendor", typeof(string));
newProduct.Columns.Add("product_type", typeof(string));
newProduct.Columns.Add("status", typeof(string));

DataRow productRow = newProduct.NewRow();
productRow["title"] = "New Product";
productRow["body_html"] = "<p>This is a new product description.</p>";
productRow["vendor"] = "My Store";
productRow["product_type"] = "Physical";
productRow["status"] = "active";
newProduct.Rows.Add(productRow);

// Insert the new product
bool success = await shopifyDS.UpdateEntityAsync("products", newProduct);
```

#### Disconnect

```csharp
// Always disconnect when done
await shopifyDS.DisconnectAsync();
```

### Connection String Format

Alternatively, you can use connection strings:

```csharp
// For Shopify
string connectionString = "ApiKey=your-api-key;StoreUrl=your-store.myshopify.com";

// For WooCommerce
string connectionString = "ConsumerKey=your-key;ConsumerSecret=your-secret;StoreUrl=https://yourstore.com";

// For Magento
string connectionString = "AdminToken=your-token;StoreUrl=https://yourstore.com";

// For BigCommerce
string connectionString = "ClientId=your-id;ClientSecret=your-secret;StoreHash=your-hash";
```

## 🔧 Configuration

### Shopify Configuration

To use Shopify data source, you need:

1. **API Key**: Generate from Shopify Admin → Apps → Develop apps → Create app
2. **Store URL**: Your Shopify store domain (e.g., `mystorename.myshopify.com`)

Example API Key format: `shppa_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### Authentication Methods by Platform

| Platform | Method | Parameters |
|----------|--------|------------|
| Shopify | API Key + Store URL | `ApiKey`, `StoreUrl` |
| WooCommerce | Consumer Key/Secret | `ConsumerKey`, `ConsumerSecret`, `StoreUrl` |
| Magento | Admin Token / OAuth | `AdminToken`, `StoreUrl` |
| BigCommerce | Client ID/Secret + Store Hash | `ClientId`, `ClientSecret`, `StoreHash` |
| Squarespace | API Key | `ApiKey`, `StoreUrl` |
| Wix | API Key / OAuth | `ApiKey`, `StoreUrl` |
| Etsy | OAuth 2.0 | `ClientId`, `ClientSecret`, `RedirectUri` |
| OpenCart | API Key | `ApiKey`, `StoreUrl` |
| Ecwid | API Token | `ApiToken`, `StoreId` |
| Volusion | API Key | `ApiKey`, `StoreUrl` |

## 📊 Supported Entities

### Shopify Entities

- **products**: Product catalog with variants, images, inventory
- **orders**: Customer orders, transactions, fulfillment
- **customers**: Customer profiles, addresses, purchase history
- **collections**: Product categories and smart collections
- **inventory**: Stock levels, locations, reservations
- **analytics**: Sales reports, performance metrics
- **content**: Blog posts, pages, redirects
- **discounts**: Discount codes and automatic discounts
- **themes**: Store themes and assets
- **webhooks**: Event notifications

### Common Entities Across Platforms

- **Products**: Product catalog, variants, pricing, inventory
- **Orders**: Customer orders, payments, shipping, fulfillment
- **Customers**: Customer profiles, addresses, purchase history
- **Categories**: Product categories and collections
- **Inventory**: Stock levels, locations, reservations
- **Analytics**: Sales reports, performance metrics

## 🐛 Error Handling

All data sources include comprehensive error handling:

```csharp
try
{
    bool connected = await shopifyDS.ConnectAsync(parameters);
    if (!connected)
    {
        Console.WriteLine("Connection failed. Check your credentials.");
        return;
    }

    DataTable data = await shopifyDS.GetEntityAsync("products");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Log error details for debugging
}
finally
{
    await shopifyDS.DisconnectAsync();
}
```

## 🔍 Testing Connection

```csharp
// Test connection without performing operations
bool isConnected = await shopifyDS.TestConnectionAsync();

// Get connection information
Dictionary<string, object> info = await shopifyDS.GetConnectionInfoAsync();
Console.WriteLine($"Data Source: {info["DataSourceName"]}");
Console.WriteLine($"Connected: {info["IsConnected"]}");
Console.WriteLine($"Store URL: {info["StoreUrl"]}");
```

## 📈 Best Practices

1. **Connection Management**: Always disconnect when finished
2. **Rate Limiting**: Be aware of API rate limits for each platform
3. **Data Types**: Check entity metadata for proper data types
4. **Authentication**: Keep API keys secure and rotate regularly
5. **Error Handling**: Implement proper try-catch blocks
6. **Pagination**: Handle large datasets with proper pagination
7. **Webhooks**: Use webhooks for real-time data synchronization

## 🔄 Webhooks and Real-time Data

Some platforms support webhooks for real-time data synchronization:

```csharp
// Register webhook for order updates (platform-specific implementation)
var webhookParams = new Dictionary<string, object>
{
    ["topic"] = "orders/create",
    ["address"] = "https://your-app.com/webhooks/orders",
    ["format"] = "json"
};
DataTable webhook = await shopifyDS.UpdateEntityAsync("webhooks", webhookParams);
```

## 🛣️ Roadmap

### Phase 2: Core Implementation (In Progress)
- [x] ShopifyDataSource - Complete
- [ ] WooCommerceDataSource - In Development
- [ ] MagentoDataSource - In Development
- [ ] BigCommerceDataSource - In Development
- [ ] Remaining platforms - Planned

### Phase 3: Advanced Features
- Platform-specific optimizations
- Bulk operations support
- Advanced filtering and segmentation
- Webhook integrations
- Real-time synchronization

## 📚 Additional Resources

- [Beep Framework Documentation](https://github.com/The-Tech-Idea/BeepDM)
- [Shopify API Documentation](https://shopify.dev/docs/api)
- [CRM Data Sources](../CRM/README.md) - Reference implementation pattern
- [Marketing Data Sources](../Marketing/README.md) - Reference implementation pattern

## 🤝 Contributing

Contributions are welcome! Please see the main Beep Data Connectors repository for contribution guidelines.

## 📄 License

This project follows the same license as the Beep Data Connectors framework.

---

**Last Updated**: August 27, 2025
**Version**: 1.0.0
