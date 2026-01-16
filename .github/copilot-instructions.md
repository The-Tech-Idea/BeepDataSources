# BeepDataSources — Copilot Instructions

**Purpose:** Guide AI agents to be immediately productive implementing diverse data source implementations for BeepDM (287 supported datasource types).

## 🎯 Repository Structure

**BeepDataSources** provides implementations of `IDataSource` and `IDataSourceHelper` for diverse data sources:

```
BeepDataSources/
├── Connectors/           ← WebAPI connectors (100+ REST APIs via WebAPIDataSource)
├── VectorDatabase/       ← Vector DBs (Qdrant, Sharp Vector)
├── InMemoryDB/           ← In-memory implementations
├── Messaging/            ← Message queue implementations
└── DataSourcesPluginsCore/  ← Core RDBMS, file-based, cache via IDataSource
```

**Supported Categories** (287 types via factory):
- RDBMS: SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Hana, etc.
- NoSQL: MongoDB, Redis, Cassandra, DynamoDB, Firestore, etc.
- Vector DBs: Qdrant, Pinecone, ChromaDB, Weaviate, Milvus, etc.
- Cloud APIs: Azure, AWS, GCP services
- WebAPIs: 100+ external service connectors
- File Formats: CSV, JSON, XML, Parquet, Avro, ORC, Excel, etc.
- Streaming: Kafka, RabbitMQ, Kinesis, PubSub, etc.
- Graph: Neo4j, TigerGraph, ArangoDB
- Search: ElasticSearch, Solr, Algolia
- Time Series: InfluxDB, TimeScaleDB, Prometheus
- Blockchain: Ethereum, Hyperledger, BitcoinCore

---

## 🏗️ Core Architecture: IDataSource + IDataSourceHelper

### IDataSource (BeepDM)
**Contract:** All datasources implement this interface for CRUD, metadata, and transaction operations.

**Location:** `DataManagementModelsStandard/IDataSource.cs`

```csharp
public interface IDataSource : IDisposable
{
    // Properties
    string ColumnDelimiter { get; set; }
    string ParameterDelimiter { get; set; }
    string GuidID { get; set; }
    DataSourceType DatasourceType { get; set; }
    DatasourceCategory Category { get; set; }
    IDataConnection Dataconnection { get; set; }
    string DatasourceName { get; set; }
    IErrorsInfo ErrorObject { get; set; }
    string Id { get; set; }
    IDMLogger Logger { get; set; }
    List<string> EntitiesNames { get; set; }
    List<EntityStructure> Entities { get; set; }
    IDMEEditor DMEEditor { get; set; }
    ConnectionState ConnectionStatus { get; set; }
    event EventHandler<PassedArgs> PassEvent;
    
    // Connection Management
    ConnectionState Openconnection();
    ConnectionState Closeconnection();
    
    // Entity Listing & Structure
    IEnumerable<string> GetEntitesList();
    EntityStructure GetEntityStructure(string EntityName, bool refresh);
    EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false);
    bool CheckEntityExist(string EntityName);
    int GetEntityIdx(string entityName);
    Type GetEntityType(string EntityName);
    
    // CRUD Operations
    IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter);
    PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize);
    Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter);
    IErrorsInfo InsertEntity(string EntityName, object InsertedData);
    IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow);
    IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress);
    IErrorsInfo DeleteEntity(string EntityName, object UploadDataRow);
    
    // Scalar Operations
    double GetScalar(string query);
    Task<double> GetScalarAsync(string query);
    
    // Query Execution
    IEnumerable<object> RunQuery(string qrystr);
    IErrorsInfo ExecuteSql(string sql);
    
    // Entity Creation & Metadata
    bool CreateEntityAs(EntityStructure entity);
    IErrorsInfo CreateEntities(List<EntityStructure> entities);
    IEnumerable<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities = null);
    IEnumerable<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters);
    IEnumerable<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName);
    
    // Script Execution
    IErrorsInfo RunScript(ETLScriptDet dDLScripts);
    
    // Transaction Management
    IErrorsInfo BeginTransaction(PassedArgs args);
    IErrorsInfo Commit(PassedArgs args);
    IErrorsInfo EndTransaction(PassedArgs args);
}
```

**Key Responsibilities:**
- **Connection State:** Manage open/close lifecycle with `ConnectionStatus` property
- **Entity Discovery:** Populate `Entities` list with metadata during `Openconnection()`
- **CRUD Operations:** Implement GetEntity (sync/async/paged), InsertEntity, UpdateEntity, DeleteEntity
- **Metadata Queries:** Return entity structures, foreign keys, and child relations
- **Transaction Support:** Support explicit transactions via BeginTransaction/Commit/EndTransaction
- **Error Handling:** Always populate `ErrorObject` with status and messages
- **Async Support:** Provide async variants for long-running operations (GetEntityAsync, GetScalarAsync)

### IDataSourceHelper (BeepDM)
**Contract:** Query generation and datasource-specific operations (managed by factory).

```csharp
public interface IDataSourceHelper
{
    DataSourceType SupportedType { get; set; }
    string Name { get; }
    DataSourceCapabilities Capabilities { get; }
    
    // Schema discovery
    (string Query, bool Success) GetSchemaQuery(string userName);
    (string Query, bool Success) GetTableExistsQuery(string tableName);
    (string Query, bool Success) GetColumnInfoQuery(string tableName);
    
    // DDL
    (string Sql, bool Success, string ErrorMessage) GenerateCreateTableSql(EntityStructure entity, string schemaName);
    (string Sql, bool Success, string ErrorMessage) GenerateDropTableSql(string tableName, string schemaName);
    (string Sql, bool Success, string ErrorMessage) GenerateTruncateTableSql(string tableName, string schemaName);
    
    // DML
    (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateInsertSql(string tableName, Dictionary<string, object> data);
    (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateUpdateSql(string tableName, Dictionary<string, object> data, Dictionary<string, object> conditions);
    (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateDeleteSql(string tableName, Dictionary<string, object> conditions);
    (string Sql, Dictionary<string, object> Parameters, bool Success, string ErrorMessage) GenerateSelectSql(string tableName, IEnumerable<string> columns, Dictionary<string, object> conditions, string orderBy, int? skip, int? take);
    
    // Transactions
    (string Sql, bool Success) GenerateBeginTransactionSql();
    (string Sql, bool Success) GenerateCommitSql();
    (string Sql, bool Success) GenerateRollbackSql();
    
    // Utilities
    string QuoteIdentifier(string identifier);
    bool SupportsCapability(CapabilityType capability);
}
```

All datasources are discovered via `[AddinAttribute]`:
```csharp
[AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Twitter)]
public class TwitterDataSource : WebAPIDataSource { }
```

---

## 🔧 Three Implementation Patterns

### Pattern A: WebAPI Connectors (Connectors/)
For REST/HTTP APIs via `WebAPIDataSource`:

```
├── Models.cs                 ← Sealed POCOs with [JsonPropertyName]
├── {ServiceName}DataSource.cs   ← Inherits WebAPIDataSource
└── {ServiceName}Helpers.cs      ← Optional utilities
```

**Reference:** `Connectors/SocialMedia/Twitter/` — full example with entities, CommandAttributes, paging.

### Pattern B: Direct IDataSource (VectorDatabase/, Messaging/, etc.)
For non-HTTP datasources — implement `IDataSource` directly:

```
├── {ServiceName}DataSource.cs   ← Implements IDataSource
├── Models/ (optional)           ← Custom data structures
└── Helpers/ (optional)          ← Utilities
```

**Reference:** `VectorDatabase/QdrantDatasource/` — HTTP client + custom serialization.

### Pattern C: IDataSourceHelper Factory-Provided
For datasource types where query generation is centralized:

```csharp
// From DataSourceHelperFactory
var helper = factory.GetHelper(DataSourceType.SqlServer);
var (sql, @params, success, error) = helper.GenerateSelectSql(...);
```

**67 RDBMS variants** (SQL Server, MySQL, PostgreSQL, Oracle, etc.) use `RdbmsHelper` with `SupportedType` property to switch dialects.

---

## 🔍 IDataSourceHelper Implementations (5 Core + 9 Planned)

### Core Helpers (Implemented)

1. **RdbmsHelper** (28+ variants)
   - SQL Server, MySQL, PostgreSQL, Oracle, SQLite, DB2, Hana, etc.
   - Properties: `SupportedType` determines dialect
   - Schema generation: T-SQL, PL/pgSQL, etc.
   - DDL: CREATE/DROP/ALTER TABLE with dialect-specific syntax
   - DML: INSERT/UPDATE/DELETE/SELECT with parameter binding

2. **MongoDBHelper**
   - Aggregation pipelines instead of SQL
   - BSON document serialization
   - Transactions on replica sets

3. **RedisHelper**
   - Hash/String/List/Set operations
   - Lua scripts for transactions
   - TTL and key expiration

4. **CassandraHelper**
   - CQL generation with consistency levels
   - Composite partition keys
   - Batches and lightweight transactions

5. **RestApiHelper**
   - HTTP verb mapping (GET/POST/PUT/DELETE)
   - JSON/XML body construction
   - Header and query parameter management

### Planned Helpers (Phase 3)
- FileFormatHelper (CSV, JSON, XML, Parquet, Avro, ORC, Excel, etc.)
- StreamingHelper (Kafka, RabbitMQ, Kinesis, etc.)
- GraphDatabaseHelper (Neo4j, TigerGraph, ArangoDB)
- SearchEngineHelper (ElasticSearch, Solr, Algolia)
- TimeSeriesHelper (InfluxDB, TimeScaleDB, Prometheus)
- VectorDatabaseHelper (ChromaDB, Pinecone, Weaviate, Milvus)
- BigDataHelper (Hadoop, Kudu, Druid, Pinot)
- BlockchainHelper (Ethereum, Hyperledger, BitcoinCore)
- EmailProtocolHelper (IMAP, POP3, SMTP, OAuth2)

---

## 🏗️ Creating a New Connector (WebAPI Pattern)

### Step 1: Project Structure
```
Connectors/{Category}/{ServiceName}/
├── {ServiceName}.csproj
├── {ServiceName}DataSource.cs   ← Inherits WebAPIDataSource
├── Models.cs                    ← Root-level (no subfolder)
└── {ServiceName}Helpers.cs (opt.)
```

### Step 2: Define Models.cs
```csharp
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.{ServiceName}.Models
{
    // Base class for all entities
    public abstract class {ServiceName}EntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : {ServiceName}EntityBase { DataSource = ds; return (T)this; }
    }

    // Sealed POCOs with [JsonPropertyName] matching API response
    public sealed class {ServiceName}Item : {ServiceName}EntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        // ... all properties
    }
}
```

### Step 3: Implement DataSource
```csharp
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.{ServiceName}.Models;

[AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.{ServiceName})]
public class {ServiceName}DataSource : WebAPIDataSource
{
    // Entity endpoints
    private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["items"] = "/items",
        ["item_details"] = "/items/{id}"
    };

    // Required filters
    private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["items"] = new[] { "query" },
        ["item_details"] = new[] { "id" }
    };

    public {ServiceName}DataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
        : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
    {
        // Ensure WebAPI connection properties
        if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            if (Dataconnection != null)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

        // Register entities
        EntitiesNames = EntityEndpoints.Keys.ToList();
        Entities = EntitiesNames.Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n }).ToList();
    }

    // Override sync/async methods
    public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
    {
        var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
        return data ?? Array.Empty<object>();
    }

    public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
    {
        if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
            throw new InvalidOperationException($"Unknown entity '{EntityName}'.");

        var q = FiltersToQuery(Filter);
        RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

        var resolvedEndpoint = ResolveEndpoint(endpoint, q);
        using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);

        return resp?.IsSuccessStatusCode == true ? ExtractArray(resp, "data") : Array.Empty<object>();
    }

    // Add strongly typed methods with [CommandAttribute]
    [CommandAttribute(
        Name = "GetItems",
        Caption = "Get Items",
        Category = DatasourceCategory.Connector,
        DatasourceType = DataSourceType.{ServiceName},
        PointType = EnumPointType.Function,
        ObjectType = "{ServiceName}Item",      // ← MUST match POCO class
        ClassType = "{ServiceName}DataSource",
        Showin = ShowinType.Both,
        Order = 1,
        iconimage = "{servicename}.png",
        misc = "ReturnType: IEnumerable<{ServiceName}Item>"
    )]
    public async Task<IEnumerable<{ServiceName}Item>> GetItems(string query, int maxResults = 10)
    {
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "query", FilterValue = query, Operator = "=" },
            new AppFilter { FieldName = "max_results", FilterValue = maxResults.ToString(), Operator = "=" }
        };

        var result = await GetEntityAsync("items", filters);
        return result.Select(item => JsonSerializer.Deserialize<{ServiceName}Item>(JsonSerializer.Serialize(item)))
                     .Where(item => item != null).Cast<{ServiceName}Item>();
    }
}
```

### Step 4: Register in BeepDM
- Place DLL in `ProjectClasses` or `ConnectionDrivers`
- `AssemblyHandler` auto-discovers via `[AddinAttribute]`
- Optionally add to `ConnectionConfig.json`

---

## 🔍 WebAPIDataSource Helper Methods (Available in GetEntityAsync)

```csharp
// Convert AppFilter list to query dictionary
Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)

// Validate required filters present, throw if missing
void RequireFilters(string entityName, Dictionary<string, string> queryDict, string[] requiredNames)

// Replace {id} placeholders in endpoint
string ResolveEndpoint(string template, Dictionary<string, string> queryDict)

// Execute HTTP GET
Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> queryDict)

// Extract array from JSON response at path (default: "data")
IEnumerable<object> ExtractArray(HttpResponseMessage response, string jsonPath = "data")

// Get pagination token for cursor-based paging
string GetNextToken(HttpResponseMessage response)  // Looks for meta.next_token or pagination_token
```

---

## 🔐 Authentication Patterns

**Bearer Token:**
```csharp
if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties props)
    props.Headers.Add("Authorization", $"Bearer {apiKey}");
```

**Custom Headers:**
```csharp
props.Headers.Add("X-Custom-Header", "value");
```

**Query Parameter:**
```csharp
q.Add("api_key", apiKey);
```

---

## ✅ Build & Validation

- **Build:** `dotnet build DataSourcePluginSolution.sln`
- **Smoke Test:** Verify `[AddinAttribute]` present and `ObjectType` matches POCO class names
- **Integration:** Load in BeepDM, confirm `[CommandAttribute]` methods discovered

---

## 📚 Reference Implementations

- **Connectors (WebAPI)**: Twitter (`Connectors/SocialMedia/Twitter/`) — full example with paging, CommandAttributes
- **Vector DB**: Qdrant (`VectorDatabase/TheTechIdea.Beep.QdrantDatasource/`) — direct IDataSource
- **In-Memory**: SharpVector (`VectorDatabase/TheTechIdea.Beep.ShapVectorDatasource/`) — collections-based
- **Refactoring**: `REMAINING_REFACTORING_TASKS.md` — consolidation patterns
- **Implementation Plan**: `idatasourceimplementationplan.md` (BeepDM) — IDataSourceHelper factory and helpers

---

## 🚨 Common Mistakes to Avoid

1. **ObjectType mismatch (Connectors):** `[CommandAttribute(ObjectType = "User")]` but class is `TwitterUser` → UI won't discover
2. **Missing JsonPropertyName:** Properties deserialize as null silently
3. **Forgetting [AddinAttribute]:** AssemblyHandler won't discover datasource
4. **Model not sealed (Connectors):** Minor perf loss; convention
5. **Models in subfolder (Connectors):** Move to root `Models.cs`
6. **Not implementing all IDataSource methods:** Direct implementations must be complete

---

## 🔗 Cross-Component Communication

- **DMEEditor** (orchestrator): Access via constructor; use `DMEEditor.ConfigEditor` for connections
- **Logger**: Use `Logger.WriteLog(message)` for diagnostics
- **ErrorObject**: Set `.Flag = Errors.Ok/Failed` and `.Message`
- **PassEvent**: Raise `PassEvent?.Invoke(this, args)` for notifications
- **IDataSourceHelper**: Access via `DMEEditor.GetDataSourceHelper(DataSourceType)` or factory

---

## 📝 Notes for AI Agents

- **Type safety** (POCOs, sealed, `[JsonPropertyName]`) enables UI discovery
- **Metadata-driven** (`[CommandAttribute]`, `[AddinAttribute]`) for dynamic discovery
- **Three implementation styles**: WebAPI Connectors, Direct IDataSource, Factory-provided IDataSourceHelper
- **Factory pattern** (DataSourceHelperFactory) covers 287 datasource types with 5 core helpers + 9 planned
- **Twitter connector** is reference implementation for WebAPI pattern
- **Qdrant** / **SharpVector** are examples for non-WebAPI direct implementations
- Prefer **composition** for utilities; extend base classes carefully
- See `idatasourceimplementationplan.md` for comprehensive IDataSourceHelper implementation roadmap

**Use this pattern for REST/HTTP APIs:**

### Pattern 1: Entity Endpoints Mapping
Define endpoints as a static dictionary, keyed by logical entity name:

```csharp
private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
{
    ["tweets.search"] = "tweets/search/recent",
    ["users.by_username"] = "users/by",
    ["lists.by_user"] = "users/{id}/owned_lists"
};

// Required filters per entity (validates user input)
private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
{
    ["tweets.search"] = new[] { "query" },
    ["users.by_username"] = new[] { "usernames" }
};
```

**Why:** Decouples entity names from API endpoints; supports parameterized paths like `{id}`.

### Pattern 2: POCO Models with Fluent Attachment
Use `sealed` classes with `[JsonPropertyName]` and inherit from `{ServiceName}EntityBase`:

```csharp
public abstract class TwitterEntityBase
{
    [JsonIgnore] public IDataSource DataSource { get; private set; }
    public T Attach<T>(IDataSource ds) where T : TwitterEntityBase { DataSource = ds; return (T)this; }
}

public sealed class TwitterTweet : TwitterEntityBase
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; }
    [JsonPropertyName("public_metrics")] public TwitterTweetPublicMetrics PublicMetrics { get; set; }
}
```

**Why:** Sealed classes enable JIT optimizations; attachment pattern allows models to reference parent datasource without circular refs.

### Pattern 3: Override Sync/Async GetEntity Methods
All DataSource implementations must override the generic `GetEntity` / `GetEntityAsync`:

```csharp
// Sync wrapper - calls async and blocks
public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
{
    var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
    return data ?? Array.Empty<object>();
}

// Async implementation - resolves endpoint, validates filters, deserializes
public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
{
    if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
        throw new InvalidOperationException($"Unknown entity '{EntityName}'.");

    var q = FiltersToQuery(Filter);  // Convert AppFilter list to query dict
    RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

    var resolvedEndpoint = ResolveEndpoint(endpoint, q);  // Replace {id} placeholders
    using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);

    return resp?.IsSuccessStatusCode == true ? ExtractArray(resp, "data") : Array.Empty<object>();
}
```

**Why:** Delegates HTTP/deserialization to `WebAPIDataSource` helpers; enforces consistent error handling.

### Pattern 4: CommandAttributes for Strongly Typed Methods
Add discoverable functions via `[CommandAttribute]` for UI/API exposure:

```csharp
[CommandAttribute(
    Name = "GetUserByUsername",
    Caption = "Get User by Username",
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.Twitter,
    PointType = EnumPointType.Function,
    ObjectType = "TwitterUser",           // ← MUST match POCO class name
    ClassType = "TwitterDataSource",
    Showin = ShowinType.Both,              // UI + API
    Order = 1,
    iconimage = "user.png",
    misc = "ReturnType: IEnumerable<TwitterUser>"
)]
public async Task<IEnumerable<TwitterUser>> GetUserByUsername(string username)
{
    var filters = new List<AppFilter> { new AppFilter { FieldName = "usernames", FilterValue = username, Operator = "=" } };
    var result = await GetEntityAsync("users.by_username", filters);
    return result.Select(item => JsonSerializer.Deserialize<TwitterUser>(JsonSerializer.Serialize(item)))
                 .Where(u => u != null).Cast<TwitterUser>();
}
```

**Critical:** `ObjectType` must exactly match the POCO class name (case-sensitive). The BeepDM UI uses this to discover return types.

### Pattern 5: WebAPIConnectionProperties Initialization
In constructor, ensure WebAPI-specific connection properties exist:

```csharp
if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
{
    if (Dataconnection != null)
        Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
}
```

## 📁 Project Structure

```
Connectors/{Category}/{ServiceName}/
├── {ServiceName}.csproj              ← Project file (target: net9.0)
├── {ServiceName}DataSource.cs        ← Main datasource implementation (inherits WebAPIDataSource)
├── Models.cs                         ← All POCO models in one file
└── {ServiceName}Helpers.cs (opt.)    ← Reusable filters/helpers if needed
```

**Convention:** Keep Models.cs at project root (not subfolder).

---

## 🏗️ Creating a New Connector (WebAPI Pattern)

1. **Create project** with correct references:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\..\DataManagementModelsStandard\DataManagementModels.csproj" />
     <ProjectReference Include="..\..\..\DataManagementEngineStandard\DataManagementEngine.csproj" />
   </ItemGroup>
   ```

2. **Define Models.cs** with:
   - Base class: `{ServiceName}EntityBase` with `Attach<T>` method
   - Sealed POCO classes for each API entity
   - `[JsonPropertyName(...)]` attributes matching API response field names

3. **Implement {ServiceName}DataSource.cs**:
   - Inherit: `WebAPIDataSource`
   - Decorate: `[AddinAttribute(Category = ..., DatasourceType = ...)]`
   - Define: `EntityEndpoints` and `RequiredFilters` static dicts
   - Override: `GetEntity` / `GetEntityAsync` / `GetEntity(pageNumber, pageSize)` if paginated
   - Add strongly typed methods with `[CommandAttribute]`

4. **Register** in BeepDM:
   - Add to `ConnectionConfig.json` or use ConfigEditor API
   - Place DLL in `ProjectClasses` or `ConnectionDrivers`
   - `AssemblyHandler` auto-discovers via `[AddinAttribute]`

**Available in derived classes** (call from `GetEntityAsync`):

- `FiltersToQuery(List<AppFilter>)` → `Dictionary<string, string>` query params
- `RequireFilters(entityName, queryDict, required[])` → Throws if missing required filter
- `ResolveEndpoint(template, queryDict)` → Replaces `{id}` placeholders
- `GetAsync(endpoint, queryDict)` → `Task<HttpResponseMessage>`
- `ExtractArray(httpResponse, jsonPath)` → `IEnumerable<object>` deserialized array
- `GetNextToken(httpResponse)` → Pagination token (from `meta.next_token` or `pagination_token`)

## 🔐 Authentication Patterns

**API Key (Bearer Token):**
```csharp
if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties props)
{
    props.Headers.Add("Authorization", $"Bearer {apiKey}");
}
```

**Custom Headers:**
```csharp
props.Headers.Add("X-Custom-Header", "value");
```

**Query Parameter (less secure):**
```csharp
q.Add("api_key", apiKey);
```

- **Build:** `dotnet build DataSourcePluginSolution.sln`
- **Smoke Test:** Verify `[AddinAttribute]` is present and `ObjectType` matches POCO class names (Connectors only)
- **Integration:** Load in BeepDM CLI or Enterprize.winform app, confirm UI discovers methods via `[CommandAttribute]` (Connectors only)

## 📚 Reference Implementations

- **Connectors (WebAPI)**: Twitter (`Connectors/SocialMedia/Twitter/`) — full example with paging, context annotations, multiple entities
- **Vector DB**: Qdrant (`VectorDatabase/TheTechIdea.Beep.QdrantDatasource/`) — direct IDataSource implementation
- **In-Memory**: SharpVector (`VectorDatabase/TheTechIdea.Beep.ShapVectorDatasource/`) — in-memory data structures
- **Refactoring Checklist**: `REMAINING_REFACTORING_TASKS.md` for Connectors consolidation patterns
- **Implementation Plan**: `idatasourceimplementationplan.md` (BeepDM) — IDataSourceHelper factory and helpers

## 🚨 Common Mistakes to Avoid

1. **ObjectType mismatch (Connectors):** `[CommandAttribute(ObjectType = "User")]` but class is `TwitterUser` → UI won't discover method
2. **Missing JsonPropertyName:** Deserializer fails silently; properties remain null
3. **Forgetting [AddinAttribute]:** AssemblyHandler won't discover the datasource
4. **Model not sealed (Connectors):** Minor performance loss; not critical but convention
5. **Placing Models in subfolder (Connectors):** Move to root `Models.cs` before compilation
6. **Not implementing all IDataSource methods:** Direct implementations must provide full interface coverage

## 🔗 Cross-Component Communication

- **DMEEditor** (orchestrator): Access via constructor; use `DMEEditor.ConfigEditor` for connections
- **Logger**: Use `Logger.WriteLog(message)` for diagnostics
- **ErrorObject**: Set `.Flag = Errors.Ok/Failed` and `.Message`
- **PassEvent**: Raise `PassEvent?.Invoke(this, args)` for notifications
- **IDataSourceHelper**: Access via `DMEEditor.GetDataSourceHelper(DataSourceType)` or factory

## 📝 Notes for AI Agents

- **Type safety** (POCOs, sealed, `[JsonPropertyName]`) enables UI discovery
- **Metadata-driven** (`[CommandAttribute]`, `[AddinAttribute]`) for dynamic discovery
- **Three implementation styles**: WebAPI Connectors, Direct IDataSource, Factory-provided IDataSourceHelper
- **Factory pattern** (DataSourceHelperFactory) covers 287 datasource types with 5 core helpers + 9 planned
- **Twitter connector** is reference implementation for WebAPI pattern
- **Qdrant** / **SharpVector** are examples for non-WebAPI direct implementations
- Prefer **composition** for utilities; extend base classes carefully
- See `idatasourceimplementationplan.md` for comprehensive IDataSourceHelper implementation roadmap
