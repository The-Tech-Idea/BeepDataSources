# Beep DataSources

`IDataSource` plugin library for the [BeepDM](https://github.com/The-Tech-Idea/BeepDM) data management framework. Provides 130+ ready-to-use data connectors across databases, cloud services, SaaS APIs, message queues, and vector stores.

[View on GitHub](https://github.com/The-Tech-Idea/BeepDataSources)

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
  - [Class Hierarchy](#class-hierarchy)
  - [Plugin Registration](#plugin-registration)
  - [Runtime Discovery Flow](#runtime-discovery-flow)
- [Plugin Categories](#plugin-categories)
- [Adding a New Plugin](#adding-a-new-plugin)
- [Configuration](#configuration)
- [Documentation](#documentation)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later (`net8.0`–`net10.0`)

## Getting Started

### 1. Clone with Dependencies

```bash
git clone https://github.com/The-Tech-Idea/BeepDataSources
git clone https://github.com/The-Tech-Idea/BeepDM ../BeepDM
```

### 2. Restore & Build

```bash
cd BeepDataSources
dotnet restore DataSourcePluginSolution.sln
dotnet build DataSourcePluginSolution.sln
```

## Project Structure

### DataSourcesPluginsCore/ — Database Plugins (40+ projects)

| Engine | Project Directory | Class | Base |
|---|---|---|---|
| **SQL Server** | `SQlServerDataSourceCore/` | `SQLServerDataSource` | `RDBSource` |
| **PostgreSQL** | `PostgreDataSourceCore/` | `PostgreDataSource` | `RDBSource` |
| **MySQL** | `MySqlDataSourceCore/` | `MySQLDataSource` | `RDBSource` |
| **Oracle** | `OracleDataSourceCore/` | `OracleDataSource` | `RDBSource` |
| **SQLite** | `SqliteDatasourceCore/` | `SQLiteDataSource` | `InMemoryRDBSource` |
| **Firebird** | `FirebirdDataSourceCore/` | `FireBirdDataSource` | `RDBSource` |
| **CockroachDB** | `CockroachDBDataSourceCore/` | `CockRoachDataSource` | `RDBSource` |
| **MongoDB** | `MongoDBDataSourceCore/` | `MongoDBDataSource` | `IDataSource` |
| **Redis** | `RedisDataSourceCore/` | `RedisDataSource` | `IDataSource` |
| **RavenDB** | `RavenDBDataSourceCore/` | `RavenDBDataSource` | `IDataSource` |
| **CouchDB** | `CouchDBDataSourceCore/` | `CouchDBDataSource` | `IDataSource` |
| **Couchbase** | `CouchBaseDataSource/` | `CouchBaseDataSource` | `IDataSource` |
| **LiteDB** | `LiteDBDataSourceCore/` | `LiteDBDataSource` | `IDataSource` |
| **Firebase** | `FireBaseDataSourceCore/` | `FireBaseDataSource` | `IDataSource` |
| **InfluxDB** | `InfluxDB/` | `InfluxDBDataSource` | `IDataSource` |
| **Snowflake** | `SnowFlakeDataSource/` | `SnowFlakeDataSource` | `RDBSource` |
| **BigQuery** | `GoogleBigQuery/` | `GoogleBigQueryDataSource` | `RDBSource` |
| **Hadoop** | `HadoopDataSourceCore/` | `HadoopDataSource` | `RDBSource` |
| **Databricks** | `DataBricksDataSource/` | `DataBricksDataSource` | `RDBSource` |
| **CSV/Excel** | `TxtXlsCSVFileSourceCore/` | `TxtXlsCSVFileSource` | `IDataSource` |
| **Parquet** | `ParquetDataSource/` | `ParquetDataSource` | `IDataSource` |

Plus: Hana, Vertica, Trino, Presto, Kusto, TimeScaleDB, TerraData, Hologres, Rockset, Spanner, HDF5, ONNX, OPC UA, ML Model, Petastorm, RealM, and more.

### DataSourcesPlugins/ — RDBMS Foundation

| Project | Description |
|---|---|
| `RDBMSDataSource/` | Shared `RDBSource` and `InMemoryRDBSource` base classes. All SQL-based plugins inherit from here. Contains 14 partial class files covering connections, CRUD, queries, schema discovery, bulk operations, transactions, DML generation, type mapping, pagination, resilience, caching, and disposal. |

### Connectors/ — REST/SaaS API Connectors (75+ projects)

| Category | Vendors |
|---|---|
| **CRM** | HubSpot, Salesforce, Zoho, Pipedrive, SugarCRM, Insightly, Nutshell |
| **Marketing** | Mailchimp, ActiveCampaign, Klaviyo, ConvertKit, ConstantContact, CampaignMonitor, Drip, Sendinblue, Marketo, GoogleAds |
| **E-commerce** | Shopify, WooCommerce, Magento, Wix, Squarespace, OpenCart, Volusion |
| **Social Media** | Facebook, Twitter/X, TikTok, YouTube, LinkedIn, Reddit, Pinterest, Snapchat, Instagram, Hootsuite, Buffer |
| **Accounting** | QuickBooks Online, Xero, FreshBooks, SageIntacct, MYOB, Wave, ZohoBooks |
| **Communication** | Slack, Telegram, WhatsApp Business, Zoom, Twist |
| **Cloud Storage** | Google Drive, OneDrive, Dropbox, Box, Amazon S3, iCloud, pCloud, MediaFire, Egnyte, Citrix ShareFile |
| **Forms** | Typeform, Jotform |
| **Mail Services** | Gmail, Outlook, Yahoo |
| **SMS** | Twilio |
| **IoT** | AWS IoT, Azure IoT Hub, Particle |
| **Content Management** | Contentful, Kentico |
| **Business Intelligence** | Power BI, Metabase |
| **Task Management** | AnyDo |
| **Customer Support** | Front, Freshdesk |
| **Meeting Tools** | TLDV, Fathom |

### Messaging/ — Message Queue Plugins (8 projects)

| Plugin | Broker | Category |
|---|---|---|
| `KafkaDataSourceCore/` | Apache Kafka | `QUEUE` |
| `RabbitMQDataSourceCore/` | RabbitMQ | `MessageQueue` |
| `NATSDataSourceCore/` | NATS | `MessageQueue` |
| `MassTransitDataSource/` | MassTransit | `MessageQueue` |
| `RedisStreamsDataSourceCore/` | Redis Streams | `MessageQueue` |
| `GooglePubSubDataSourceCore/` | Google Pub/Sub | `MessageQueue` |
| `AzureServiceBusDataSourceCore/` | Azure Service Bus | `MessageQueue` |
| `AmazonSQSDataSourceCore/` | Amazon SQS | `MessageQueue` |

Each plugin includes `XxxDataSource.cs`, `XxxDataConnection.cs`, and `XxxConnectionProperties.cs`.

### VectorDatabase/ — Vector Store Plugins (5 projects)

| Plugin | Vector DB | Status |
|---|---|---|
| `QdrantDatasource/` | Qdrant | Active |
| `MilvusDatasource/` | Milvus | Active |
| `ChromaDBDatasource/` | ChromaDB | Active |
| `PineConeDatasource/` | Pinecone | In development |
| `ShapVectorDatasource/` | SharpVector | In development |

### InMemoryDB/

| Plugin | Description |
|---|---|
| `DuckDBDataSourceCore/` | DuckDB analytical in-memory database |

## Architecture

### Class Hierarchy

```
BeepDM NuGet Packages (TheTechIdea.Beep.*)
├── IDataSource              — Core contract all plugins implement
├── ILocalDB                — File-based databases (SQLite, LiteDB, FirebirdEmbedded)
├── IInMemoryDB             — In-memory data stores (Redis, DuckDB, SQLite)
├── IRDBSource              — RDBMS-specific interface
├── IDataConnection         — Connection management
├── EntityStructure         — Schema metadata model
├── AddinAttribute           — Plugin registration decorator
├── WebAPIDataSource         — REST/SaaS base class
└── WebAPIDataConnection     — HTTP client wrapper

This Repository
│
├── RDBSource (partial class, 14 files)
│   ├── SQLServerDataSource
│   ├── PostgreDataSource
│   ├── MySQLDataSource
│   ├── OracleDataSource
│   └── ... (all RDBMS and cloud DBs)
│   └── InMemoryRDBSource
│       ├── SQLiteDataSource
│       ├── DuckDBDataSource
│       └── FireBirdEmbeddedDataSource
│
├── IDataSource directly
│   ├── RedisDataSource       (NOSQL + IInMemoryDB)
│   ├── MongoDBDataSource     (NOSQL)
│   ├── RavenDBDataSource     (NOSQL + IInMemoryDB)
│   ├── CouchDBDataSource     (NOSQL)
│   ├── LiteDBDataSource      (NOSQL + ILocalDB)
│   ├── KafkaDataSource       (QUEUE)
│   ├── RabbitMQDataSource    (MessageQueue)
│   ├── NATSDataSource        (MessageQueue)
│   ├── QdrantDatasource      (VectorDB + IInMemoryDB)
│   ├── MilvusDataSource      (VectorDB)
│   ├── ChromaDBDataSource    (VectorDB)
│   └── ... (all non-RDBMS plugins)
│
└── WebAPIDataSource
    ├── HubSpotDataSource      (CRM)
    ├── SalesforceDataSource   (CRM)
    ├── ShopifyDataSource      (E-commerce)
    └── ... (75+ connectors)
```

### Plugin Registration

Every plugin class is decorated with `[AddinAttribute]` specifying its category and type:

```csharp
// RDBMS
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.SqlServer)]
public class SQLServerDataSource : RDBSource, IDataSource { }

// NoSQL
[AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.Redis)]
public class RedisDataSource : IDataSource, IInMemoryDB { }

// Connector
[AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.HubSpot)]
public class HubSpotDataSource : WebAPIDataSource { }

// Messaging
[AddinAttribute(Category = DatasourceCategory.QUEUE, DatasourceType = DataSourceType.Kafka)]
public class KafkaDataSource : IDataSource, IDisposable, IMessageDataSource<GenericMessage, StreamConfig> { }

// Vector
[AddinAttribute(Category = DatasourceCategory.VectorDB, DatasourceType = DataSourceType.Qdrant)]
public class QdrantDatasourceGeneric : IDataSource, IInMemoryDB { }
```

### Runtime Discovery Flow

```
BeepDM starts
  → AssemblyHandler scans assemblies for [AddinAttribute] classes
  → DatasourceType enum values map to plugin types
  → User creates connection via ConfigEditor (ConnectionProperties)
  → BeepDM instantiates matching IDataSource via standard constructor
  → Connection properties flow from ConfigEditor.DataConnections → Dataconnection.ConnectionProp
  → Openconnection() → driver/client resolution → connected
  → GetEntitesList() → populate Entities/EntitiesNames
  → CRUD operations via IDataSource methods
```

### IDataSource Contract

Every plugin must implement:

| Method | Purpose |
|---|---|
| `Openconnection()` / `Closeconnection()` | Connection lifecycle |
| `GetEntitesList()` | List entities (tables, collections, topics) |
| `GetEntity(name, filters)` | Fetch data |
| `GetEntityStructure(name)` | Discover schema/metadata |
| `InsertEntity(name, object)` | Write data |
| `UpdateEntity(name, object)` / `UpdateEntities(...)` | Modify data |
| `DeleteEntity(name, key)` | Remove data |
| `CreateEntityAs(structure)` / `CreateEntities(...)` | Create schema |
| `RunQuery(sql)` | Execute query |
| `ExecuteSql(sql)` | Execute command |
| `GetScalar(sql)` | Single-value query |
| `BeginTransaction()` / `Commit()` / `EndTransaction()` | Transaction support |
| `GetEntityforeignkeys()` / `GetChildTablesList()` | Relationship discovery |

## Adding a New Plugin

### RDBMS Plugin (easiest — inherit RDBSource)

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.MyNewDb)]
public class MyNewDbDataSource : RDBSource, IDataSource
{
    public MyNewDbDataSource(string datasourcename, IDMLogger logger,
        IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
        : base(datasourcename, logger, DMEEditor, databasetype, per) { }

    // Override only vendor-specific behavior
    public override string DisableFKConstraints(EntityStructure t) { ... }
    public override string EnableFKConstraints(EntityStructure t) { ... }
}
```

### NoSQL / Non-RDBMS Plugin

```csharp
[AddinAttribute(Category = DatasourceCategory.NOSQL, DatasourceType = DataSourceType.MyXxxDb)]
public class MyXxxDataSource : IDataSource
{
    public MyXxxDataSource(string datasourcename, IDMLogger logger,
        IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
    {
        DatasourceName = datasourcename;
        DMEEditor = pDMEEditor;
        Category = DatasourceCategory.NOSQL;

        // Resolve connection properties
        Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections
            .Where(c => c.ConnectionName == datasourcename).FirstOrDefault();

        Dataconnection.ConnectionProp.DatabaseType = DataSourceType.MyXxxDb;
    }

    // Implement all IDataSource methods...
}
```

### REST Connector Plugin

```csharp
[AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.MyService)]
public class MyServiceDataSource : WebAPIDataSource
{
    private static readonly List<string> KnownEntities = new() { "users", "orders" };

    public MyServiceDataSource(string datasourcename, IDMLogger logger,
        IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        : base(datasourcename, logger, pDMEEditor, databasetype, per)
    {
        if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
        EntitiesNames = KnownEntities.ToList();
    }

    public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
    {
        // Make API call, deserialize, return results
    }
}
```

### Requirements Checklist for New Plugins

1. Add `[AddinAttribute]` with correct `DatasourceCategory` and `DataSourceType`
2. Implement the standard 5-argument constructor
3. Resolve `ConnectionProp` from `DMEEditor.ConfigEditor.DataConnections` in the constructor
4. Add NuGet reference to `TheTechIdea.Beep.DataManagementEngine`
5. Add project to `DataSourcePluginSolution.sln`

## Configuration

Connections are managed through BeepDM's `ConfigEditor`:

```csharp
// Connection properties are stored in:
DMEEditor.ConfigEditor.DataConnections      // List of all ConnectionProperties

// Driver configs with connection string templates:
DMEEditor.ConfigEditor.DataDriversClasses   // List of ConnectionDriversConfig

// Entity structures are persisted/loaded via:
DMEEditor.ConfigEditor.SaveDataSourceEntitiesValues()
DMEEditor.ConfigEditor.LoadDataSourceEntitiesValues()
```

Each plugin resolves its connection at construction time by matching `ConnectionName == datasourcename`.

## Documentation

Comprehensive HTML documentation is available in the `Help/` directory:

| Page | Content |
|---|---|
| [Help/index.html](Help/index.html) | Documentation home |
| [Help/getting-started.html](Help/getting-started.html) | Getting started guide |
| [Help/repo-layout.html](Help/repo-layout.html) | Repository layout |
| [Help/platform-beepdm.html](Help/platform-beepdm.html) | BeepDM platform overview |
| [Help/platform-idatasource.html](Help/platform-idatasource.html) | IDataSource interface reference |
| [Help/platform-connection-properties.html](Help/platform-connection-properties.html) | Connection lifecycle |
| [Help/platform-configeditor.html](Help/platform-configeditor.html) | ConfigEditor reference |
| [Help/impl-rdbms.html](Help/impl-rdbms.html) | RDBMS plugin architecture |
| [Help/impl-nosql.html](Help/impl-nosql.html) | NoSQL plugin architecture |
| [Help/impl-connectors.html](Help/impl-connectors.html) | REST connector architecture |
| [Help/impl-messaging-vector.html](Help/impl-messaging-vector.html) | Messaging & vector DB architecture |
| [Help/providers/](Help/providers/) | Per-provider connector deep dives |

See [Help/README.md](Help/README.md) for documentation authoring workflow.
