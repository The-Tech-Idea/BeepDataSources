# Claude Instructions for BeepDataSources

You are an expert software engineer assisting with the BeepDataSources repository. This codebase provides implementations of `IDataSource` and `IDataSourceHelper` for 287+ diverse data source types supporting BeepDM.

## Repository Scope

**BeepDataSources** contains:
- **Connectors/** — 100+ WebAPI implementations (Twitter, Zoom, Slack, etc.) inheriting `WebAPIDataSource`
- **VectorDatabase/** — Vector DB implementations (Qdrant, Sharp Vector, etc.)
- **InMemoryDB/** — In-memory cache and storage systems
- **Messaging/** — Message queue implementations (Kafka, RabbitMQ, etc.)
- **DataSourcesPlugins/RDBMSDataSource/** — Shared **`RDBSource`** (Dapper, CRUD, schema, transactions). Engine projects inherit **`RDBSource`** directly (e.g. SQL Server, PostgreSQL, MySQL, Oracle, Snowflake, Spanner, Presto) or **`InMemoryRDBSource`** → **`RDBSource`** for **DuckDB** and **SQLite** (`IInMemoryDB` / `ILocalDB`). Help: **`Help/impl-rdbms.html`**, **`Help/providers/duckdb.html`**
- **DataSourcesPluginsCore/** — Per-engine cores (many reference the RDBMS plugin above), file-based, and cache implementations

## Static HTML documentation (`Help/`)

Browser help lives under **`Help/`** (open `Help/index.html`; no build step). **Flagship REST/SaaS** command and model write-ups: thirteen `Help/providers/conn-*.html` files, indexed from **`Help/impl-connectors.html`** at anchor **`#flagship-provider-pages`**. Category hubs: `Help/connectors-*.html`. Phased authoring checklists: **`.plans/`** (`README.md`, `00-MASTER-PLAN.md`). After adding or renaming Help pages, run **`python Help/tools/check-nav-mapping.py`** from the repo root to confirm every `*.html` basename under `Help/` has a matching `createNavigationMapping()` entry (and no orphan keys). For relative `href` and local `src=` targets, run **`python Help/tools/check-help-links.py`** (same root); it flags missing files (external URLs, `data:`/`blob:`, and same-page `#` anchors are skipped). **`python Help/tools/verify-help.py`** runs the nav-mapping check and the link check in one step.

## Critical Architecture Concepts

### 1. IDataSource Interface (Primary Contract)
Every implementation must inherit/implement `IDataSource` from BeepDM:
- **Location:** `DataManagementModelsStandard/IDataSource.cs` (in BeepDM repo)
- **Responsibilities:** Connection lifecycle, CRUD operations, metadata discovery, transactions
- **Key Methods:** `Openconnection()`, `GetEntity()`, `GetEntityAsync()`, `InsertEntity()`, `UpdateEntity()`, `DeleteEntity()`, `BeginTransaction()`, `Commit()`

### 2. Three Implementation Patterns

#### Pattern A: WebAPI Connectors (Connectors/)
- **Base Class:** Inherit `WebAPIDataSource` (from BeepDM)
- **Structure:**
  - `Models.cs` — Sealed POCOs with `[JsonPropertyName]` attributes
  - `{ServiceName}DataSource.cs` — Inherits WebAPIDataSource, defines entity endpoints
  - `[AddinAttribute]` — Enables assembly discovery
  - `[CommandAttribute]` — Makes methods discoverable in UI
- **Example:** `Connectors/SocialMedia/Twitter/`

#### Pattern B: Direct IDataSource Implementation
- **Base Class:** Implement `IDataSource` directly
- **Used For:** Vector DBs, in-memory stores, message queues, file systems
- **Responsibility:** Implement all ~40 IDataSource methods
- **Examples:** `VectorDatabase/QdrantDatasource/`, `VectorDatabase/SharpVectorDatasource/`

#### Pattern C: Factory-Provided Helpers
- **IDataSourceHelper:** Query generation for datasource-specific operations
- **Factory:** `DataSourceHelperFactory` manages 287 datasource types
- **Core Helpers:** RdbmsHelper (RDBMS dialects), MongoDBHelper, RedisHelper, CassandraHelper, RestApiHelper
- **Planned:** 9 additional specialized helpers (FileFormat, Streaming, Graph, Search, TimeSeries, Vector, BigData, Blockchain, Email)

## Quick Start: Creating a WebAPI Connector

1. **Create project structure:**
   ```
   Connectors/{Category}/{ServiceName}/
   ├── {ServiceName}.csproj
   ├── {ServiceName}DataSource.cs
   ├── Models.cs
   └── {ServiceName}Helpers.cs (optional)
   ```

2. **Define Models.cs:**
   - Base class: `{ServiceName}EntityBase` with `Attach<T>()` method
   - Sealed POCOs with `[JsonPropertyName]` matching API response fields

3. **Implement DataSource:**
   - Inherit `WebAPIDataSource`
   - Add `[AddinAttribute(Category = ..., DatasourceType = ...)]`
   - Define static dicts: `EntityEndpoints`, `RequiredFilters`
   - Override: `GetEntity()`, `GetEntityAsync()`, optionally `GetEntity(pageNumber, pageSize)`
   - Add strongly typed methods with `[CommandAttribute]` for UI discovery

4. **Register in BeepDM:**
   - Place DLL in `ProjectClasses/` or `ConnectionDrivers/`
   - `AssemblyHandler` auto-discovers via `[AddinAttribute]`

## Key Developer Rules

1. **Always implement IDataSource:** All datasources must respect the interface contract
2. **Use [AddinAttribute] for discovery:** Without it, AssemblyHandler won't load the datasource
3. **ObjectType must match POCO class name:** In `[CommandAttribute(ObjectType = "ClassName")]`, must be exact (case-sensitive)
4. **Keep Models.cs at project root:** Don't nest in subfolder
5. **Use sealed classes for POCOs:** Better JIT optimization and convention
6. **Apply [JsonPropertyName] attributes:** Properties without these deserialize as null
7. **Implement async variants:** Provide async methods (`GetEntityAsync`, `GetScalarAsync`)
8. **Handle errors gracefully:** Always populate `ErrorObject.Flag` and `ErrorObject.Message`
9. **Support transactions:** Implement `BeginTransaction()`, `Commit()`, `EndTransaction()`

## WebAPIDataSource Helper Methods (Connectors Only)

Available when inheriting `WebAPIDataSource`:
- `FiltersToQuery(List<AppFilter>)` — Convert filters to query dict
- `RequireFilters(entityName, queryDict, required[])` — Validate required filters
- `ResolveEndpoint(template, queryDict)` — Replace `{id}` placeholders
- `GetAsync(endpoint, queryDict)` — Execute HTTP GET
- `ExtractArray(httpResponse, jsonPath)` — Extract JSON array from response
- `GetNextToken(httpResponse)` — Get pagination token for cursor-based paging

## Reference Implementations

Use these as templates:
- **WebAPI Pattern:** `Connectors/SocialMedia/Twitter/` — full example with multiple entities, paging, CommandAttributes
- **Direct IDataSource:** `VectorDatabase/QdrantDatasource/` — HTTP client with custom serialization
- **In-Memory:** `VectorDatabase/SharpVectorDatasource/` — collections-based storage

## Critical File Locations

- `DataManagementModelsStandard/IDataSource.cs` (BeepDM) — Interface contract
- `DataManagementEngineStandard/WebAPI/WebAPIDataSource.cs` (BeepDM) — Base class for connectors
- `DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/` (BeepDM) — IDataSourceHelper implementations
- `.github/copilot-instructions.md` — Full copilot instructions
- `idatasourceimplementationplan.md` (BeepDM) — IDataSourceHelper implementation roadmap
- `REMAINING_REFACTORING_TASKS.md` — Connector consolidation patterns

## Common Mistakes to Avoid

1. **ObjectType mismatch:** POCO class is `TwitterUser` but `[CommandAttribute(ObjectType = "User")]` → UI won't discover
2. **Missing [JsonPropertyName]:** Properties deserialize as null, no error thrown
3. **Forgetting [AddinAttribute]:** Datasource won't be discovered by AssemblyHandler
4. **Models in subfolder:** Must be at project root as `Models.cs`
5. **Not implementing all IDataSource methods:** Direct implementations require full interface coverage
6. **Connection string mismanagement:** Use `IDataConnection` and `ConnectionProp` from base class

## Build & Test Commands

```bash
# Build all datasources
dotnet build DataSourcePluginSolution.sln

# Build BeepDM (if modifying base classes)
dotnet build BeepDM.sln

# Run tests
dotnet test
```

## When to Ask for Help

- Unclear how `IDataSource` methods should behave for a specific datasource type
- Need to understand why `[CommandAttribute]` methods aren't appearing in UI
- Debugging async/await patterns in GetEntityAsync implementations
- Understanding pagination strategies (cursor-based, offset-based, token-based)
- Implementing authentication (Bearer tokens, OAuth2, custom headers)

## Notes

- This codebase emphasizes **type safety** and **metadata-driven discovery**
- **Sealed POCOs + [JsonPropertyName]** enable both performance and UI integration
- **Factory pattern** in BeepDM handles 287 datasource types — new helpers follow established patterns
- **No breaking changes allowed** — preserve IDataSource interface compatibility
- **Async-first design** — prefer async methods for long-running operations
