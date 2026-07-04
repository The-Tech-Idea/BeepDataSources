# Phase 10 — Schema Migration Providers (colocated per data source, scaled)

**Status:** In progress · **Last updated:** 2026-07-01 (rev 3 — implementation reality) · **Owner:** BeepDM + BeepDataSources

> Implementation plan (not a help/docs phase). Makes `MigrationManager` capable of
> migrating **all** data-source types, not just RDBMS.

## Rollout status (as built this session)

**BeepDM engine side — COMPLETE** (contracts in `DataManagementModelsStandard`; impls in `DataManagementEngineStandard`):
- Contracts: `ISchemaMigrationProvider`, `SchemaMigrationCapabilities`, `SchemaMigrationOp`, `[SchemaMigrationProvider]`, `IMigrationProviderRegistry`, `SchemaMigrationResults`.
- `IDMEEditor.GetMigrationProvider(IDataSource)` seam; `MigrationProviderRegistry` (3-tier resolve + attribute scan).
- Bases: `RdbmsSqlMigrationProvider`, `FileMutationMigrationProvider`, `ExternalReadOnlyMigrationProvider`, `NullMigrationProvider`.
- `MigrationManager.EntityOperations` rewired to dispatch through providers; `IsFileDataSource` special-casing + dead file helpers removed.
- Capability propagation: `BuildProviderCapabilityProfile` reads provider `Capabilities`.
- **Published BeepDM 3.1.0** (Models + Engine) to the local NuGet feed.

**Colocated native providers — built:**

| Folder | Provider | Native API | Build |
|---|---|---|---|
| `MongoDBDataSourceCore` | `MongoDBMigrationProvider` | `CreateCollection`, `drop`, `deleteMany`, admin `renameCollection`, `$unset`/`$rename`, `Indexes.Create/DropOne` | ✅ |
| `RavenDBDataSourceCore` | `RavenDBMigrationProvider` | `DeleteByQueryOperation`, `PutIndexesOperation`/`DeleteIndexOperation` (schemaless columns) | ✅ |
| `VectorDatabase/.../QdrantDatasource` | `QdrantMigrationProvider` | `QdrantDataSource` was **stub** — now implemented for real (REST `GET /collections`, `PUT /collections/{name}`, `DELETE /collections/{name}`, payload indexes). Provider drives the same REST via internal `MigrationHttp`/`MigrationBaseUrl` accessors. | ✅ |
| `CouchDBDataSourceCore` | `CouchDBMigrationProvider` | `CouchDBDataSource.CreateEntityAs` was stub — now real (`GetOrCreateDatabaseAsync<BeepCouchDocument>`). Provider delegates create + drives `MigrationClient.DeleteDatabaseAsync` for drop. Schemaless JSON columns. | ✅ |
| `ShapVectorDatasource` | `ShapVectorMigrationProvider` | `SharpVectorDatasource` was already real (private `CreateCollection`/`DeleteCollection` over REST). Provider delegates create + `DELETE /collections/{name}` for drop via internal `MigrationHttp`/`MigrationBaseUrl` accessors. Schemaless vector metadata. | ✅ |
| `ChromaDBDatasource` | `ChromaDBMigrationProvider` | `ChromaDBDataSource` was stub — now real (`POST /api/v1/collections`, `DELETE /api/v1/collections/{name}`, `GET` heartbeat/collections). Provider delegates create + DELETE via internal accessors. | ✅ |
| `MilvusDatasource` | `MilvusMigrationProvider` | `MilvusDataSource` was stub — now real (`POST /v1/vector/collections/create`, `/drop`, `/describe`; `GET /health`). Provider delegates create (reads `dimension` from entity field, default 1536) + drop via internal accessors. | ✅ |
| `CouchBaseDataSourceCore` | `CouchBaseMigrationProvider` | **Stale→real refresh** — `CouchBaseDataSource` was pinned to BeepDM 2.0.35 and didn't compile. Full IDataSource rewritten against BeepDM 3.1.0 (REST against Couchbase Server: real `Openconnection`/`CreateEntityAs`/`CheckEntityExist`/`GetEntitesList`/`Closeconnection` via `HttpClient`; non-migration methods honest IErrorsInfo failures to compile). Provider delegates create + `DELETE /pools/default/buckets/{name}` for drop. | ✅ |
| `InfluxDBDataSourceCore` | `InfluxDBMigrationProvider` | **Stale→real refresh** — full IDataSource rewrite against BeepDM 3.1.0 (InfluxDB.Client SDK: real bucket create/find/delete via `BucketsApi`; non-migration methods honest failures). Provider delegates create + drop. | ✅ |
| `VectorDatabase/.../PineConeDatasource` | `PineConeMigrationProvider` | **Stale→real refresh** — PineCone was already on 3.1.0 but had casing errors (`fieldname`→`FieldName`); fixed. Real REST (CreateIndex, UpsertVector, DeleteVectors). Provider delegates create + `DELETE /indexes/{name}` for drop via internal accessors. | ✅ |
| `LiteDBDataSourceCore` | `LiteDBMigrationProvider` | **Stale→real refresh** — full IDataSource rewrite against BeepDM 3.1.0 (LiteDB SDK: real collection create/drop; deleted 6 stale partial files; fixed `DefaulDataConnection` typo; fixed `GlobalUsings` stale namespace). Provider delegates create + drop. | ✅ |
| `CouchBaseLiteDataSourceCore` | `CouchBaseLiteMigrationProvider` | **Stale→real refresh** — full IDataSource rewrite against BeepDM 3.1.0 (Couchbase.Lite SDK: real collection create/drop). Required enum addition (`DataSourceType.CouchBaseLite`) + BeepDM 3.1.0 republish. Provider delegates create + drop. | ✅ |

**Category-fallback coverage (no folder file needed):** RDBMS→`RdbmsSql` (covers all 10 SQL engines incl. SQL-cloud), FILE→`FileMutation`, Connector/STREAM/QUEUE/WEBAPI→`ExternalReadOnly`, everything else→`Null`.

### ⚠️ Bottleneck is now the datasources, not the migration layer
Investigation this session found most non-RDBMS datasources are **not actually implemented** —
so they cannot faithfully receive a migration provider yet. Verified:

| DataSource | State | Can receive a provider? |
|---|---|---|
| MongoDB, RavenDB | Real (driver-backed) | ✅ done |
| RDBMS (10) | Real | covered by `RdbmsSql` fallback (SQL) — no colocated file needed |
| **Qdrant, CouchDB, ChromaDB, Milvus, CouchBase** | **Now real** + providers ✅ | done |
| **LiteDB, CouchBase, CouchBaseLite, InfluxDB, PineCone** | **All refreshed this session** (full IDataSource rewrites against BeepDM 3.1.0 + enum addition for CouchBaseLite + re-publish) | ✅ done |
| Redis, Chroma, Milvus, ShapVector, Cockroach, Snowflake, Spanner, BigQuery, Presto, Firebolt, … | Unverified — need a per-source `CreateEntityAs` reality check | TBD |

**Implication:** rolling more native providers requires the target datasource to actually call its
backend. The mechanical provider pattern is proven (2 engines); the next blocker is datasource
implementation, not migration architecture.

## 1. Problem & true scope

`MigrationManager` (in `BeepDM/.../Editor/Migration/`) only fully migrates RDBMS. Add/Alter/
Rename/Drop column, Rename/Drop/Truncate entity, Index, FK all go through
`IDMEEditor.GetDataSourceHelper(type)` → `IDataSourceHelper.Generate*Sql(...)` →
`MigrateDataSource.ExecuteSql(sql)`. That path is **SQL-string-centric**, so it only works
for `DatasourceCategory.RDBMS`.

### Real inventory (authoritative — via `[AddinAttribute]` scan, not the incomplete registry)

**157 `IDataSource` implementations** across 5 locations:

| Folder | Count | Schema migratable? |
|---|---|---|
| `Connectors/` | 99 | **No** — external SaaS/REST; schema owned by vendor |
| `DataSourcesPluginsCore/` | 44 | Mixed (RDBMS / NoSQL / File / Cloud) |
| `Messaging/` | 8 | **No** — queues/streams |
| `VectorDatabase/` | 6 | Partial (create collection + vector index) |
| `InMemoryDB/` | 1 (DuckDB) | Yes (SQL DDL) |

By `DatasourceCategory`: **Connector 95**, CLOUD 14, RDBMS 10, MessageQueue 7, NOSQL 7,
FILE 6, VectorDB 6, WEBAPI 3, QUEUE/STREAM 2 (+7 missing attribute).

**~108 of 157 are external / read-only** (Connectors + Messaging + WebAPI) where schema
migration is a genuine no-op — you cannot `CREATE TABLE` on Salesforce or Kafka. Treating
these correctly is part of the fix, not a gap.

> NOTE: `DataSourcesPluginsCore/datasource-registry.json` only lists 23 packaged cores and
> is **not** the source of truth. The `[AddinAttribute]` scan (157) is.

## 2. Principle

- **Do NOT** change `IDataSource` (30+ implementors; breaking + ISP violation).
- Introduce `ISchemaMigrationProvider`; resolve it via a **3-tier registry** so the ~108
  read-only datasources are covered by **category fallback (zero per-folder files)**, and
  colocated provider files are written **only** for the ~25 datasources that need real
  native migration logic (NoSQL/vector/cloud-SQL engines).

## 3. Architecture — 3-tier resolution

```
MigrationManager.EntityOperations  ──dispatch by (DataSourceType, DatasourceCategory)──►
                IDMEEditor.GetMigrationProvider(type, category)
                          │
                IMigrationProviderRegistry.Resolve(type, category)
            ┌────────────┴─────────────────────────────────────┐
   TIER 1:  │ exact DataSourceType registration (overrides)    │  ← colocated native files
            ├──────────────────────────────────────────────────┤
   TIER 2:  │ DatasourceCategory fallback                      │  ← shared bases (no folder files)
            ├──────────────────────────────────────────────────┤
   TIER 3:  │ NullMigrationProvider (Unsupported)              │
            └──────────────────────────────────────────────────┘
```

`Resolve(type, category)` order: exact-type → category → `NullMigrationProvider`. So:

| Category | Tier-2 fallback provider | Covered count |
|---|---|---|
| Connector | `ExternalReadOnlyMigrationProvider` | 95 |
| MessageQueue / Queue / Stream | `ExternalReadOnlyMigrationProvider` | 9 |
| WEBAPI | `ExternalReadOnlyMigrationProvider` | 3 |
| RDBMS | `RdbmsSqlMigrationProvider` (wraps existing helper) | 10 |
| FILE | `FileMutationMigrationProvider` | 6 (2 mutable; 4 read-only get Tier-1 override) |
| VectorDB | `VectorMigrationProvider` (generic create-collection + vector index) | 6 |
| NOSQL | *(no shared fallback — each engine is Tier-1 native)* | 7 |
| CLOUD | `RdbmsSqlMigrationProvider` for SQL-cloud types (Tier-1 exact), else `ExternalReadOnlyMigrationProvider` | 14 |

This means **~108 datasources need no colocated file at all**; the fallback is their provider.
Only the datasources that must override get a colocated file (§5).

## 4. Contracts (shared `DataManagementModelsStandard` assembly)

All datasource csprojs already get this assembly transitively (they reference
`TheTechIdea.Beep.DataManagementEngine`), so **no datasource project needs a new reference**.

`BeepDM/DataManagementModelsStandard/Editor/SchemaMigration/ISchemaMigrationProvider.cs`

```csharp
namespace TheTechIdea.Beep.Editor.SchemaMigration
{
    public class SchemaMigrationCapabilities
    {
        public bool SupportsCreateEntity { get; set; } = true;
        public bool SupportsAddColumn, SupportsAlterColumn, SupportsDropColumn,
                    SupportsRenameColumn, SupportsRenameEntity, SupportsDropEntity,
                    SupportsTruncateEntity, SupportsCreateIndex, SupportsDropIndex,
                    SupportsAddForeignKey, SupportsDropForeignKey;  // default false
        public bool SupportsTransactionalDdl, IsReadOnly;
        public bool Supports(SchemaMigrationOp op) => /* switch */;
    }
    public enum SchemaMigrationOp { CreateEntity, AddColumn, AlterColumn, DropColumn,
        RenameColumn, RenameEntity, DropEntity, TruncateEntity, CreateIndex, DropIndex,
        AddForeignKey, DropForeignKey }

    public interface ISchemaMigrationProvider
    {
        DataSourceType DataSourceType { get; }
        DatasourceCategory Category { get; }
        SchemaMigrationCapabilities Capabilities { get; }
        IErrorsInfo CreateEntity(EntityStructure entity);
        IErrorsInfo AddColumn(string entityName, EntityField column);
        IErrorsInfo AlterColumn(string entityName, string columnName, EntityField newColumn);
        IErrorsInfo DropColumn(string entityName, string columnName);
        IErrorsInfo RenameColumn(string entityName, string oldName, string newName);
        IErrorsInfo RenameEntity(string oldName, string newName);
        IErrorsInfo DropEntity(string entityName);
        IErrorsInfo TruncateEntity(string entityName);
        IErrorsInfo CreateIndex(string entityName, string indexName, string[] columns, Dictionary<string,object> options = null);
        IErrorsInfo DropIndex(string entityName, string indexName);
        IErrorsInfo AddForeignKey(string entityName, string[] columns, string refEntity, string[] refColumns, string onDelete, string onUpdate, string constraintName);
        IErrorsInfo DropForeignKey(string entityName, string constraintName);
    }
}
```

- `SchemaMigrationProviderAttribute(DataSourceType, DatasourceCategory)` — marks Tier-1
  override classes (mirrors existing `[AddinAttribute]`); scanned at startup.
- `IMigrationProviderRegistry` — `Register(type, factory)`, `RegisterCategoryFallback(category,
  factory)`, `Resolve(type, category)` (3-tier). Exact-type registrations come from the
  attribute scan; category fallbacks are wired in the engine.
- `IDMEEditor.GetMigrationProvider(DataSourceType type, DatasourceCategory category)` — new
  seam next to `GetDataSourceHelper` (IDMEEditor.cs:133).

## 5. Colocation rule — only for real overrides (~25 files)

**Rule:** a colocated `<Basename>MigrationProvider.cs` is written **only** where a data
source overrides its category default. It lives next to `*DataSource.cs`, same namespace,
decorated `[SchemaMigrationProvider(type, category)]`, constructed with the owning
`IDataSource`. Everything else is covered by §3's category fallback — **no folder file**.

### 5a. NOSQL — native (no shared fallback applies; each is colocated)

| Folder | DataSourceType | Provider file (colocated) | Class |
|---|---|---|---|
| `MongoDBDataSourceCore` | MongoDB | `MongoDBMigrationProvider.cs` | `MongoDBMigrationProvider` |
| `RavenDBDataSourceCore` | RavenDB | `RavenDBMigrationProvider.cs` | `RavenDBMigrationProvider` |
| `CouchBaseDataSourceCore` | CouchBase | `CouchBaseMigrationProvider.cs` | `CouchBaseMigrationProvider` |
| `CouchBaseLiteDataSourceCore` | CouchBaseLite | `CouchBaseLiteMigrationProvider.cs` | `CouchBaseLiteMigrationProvider` |
| `CouchDBDataSourceCore` | CouchDB | `CouchDBMigrationProvider.cs` | `CouchDBMigrationProvider` |
| `RedisDataSourceCore` | Redis | `RedisMigrationProvider.cs` | `RedisMigrationProvider` (limited) |
| `LiteDBDataSourceCore` | LiteDB | `LiteDBMigrationProvider.cs` | `LiteDBMigrationProvider` |
| `InfluxDBDataSourceCore` | InfluxDB | `InfluxDBMigrationProvider.cs` | `InfluxDBMigrationProvider` (time-series) |
| `FireBaseDataSourceCore` | Firebase | `FirebaseMigrationProvider.cs` | `FirebaseMigrationProvider` |
| `AzureCloudDataSourceCore` | AzureCloudCosmos | `CosmosMigrationProvider.cs` | `CosmosMigrationProvider` |
| `AmazonCloudDatasourceCore` | AWS DynamoDB | `DynamoDbMigrationProvider.cs` | `DynamoDbMigrationProvider` |

### 5b. Vector — colocated overrides where index APIs differ (else VectorMigrationProvider fallback)

| Folder | DataSourceType | Provider file | Class |
|---|---|---|---|
| `TheTechIdea.Beep.MilvusDatasource` | Milvus | `MilvusMigrationProvider.cs` | `MilvusMigrationProvider` |
| `TheTechIdea.Beep.QdrantDatasource` | Qdrant | `QdrantMigrationProvider.cs` | `QdrantMigrationProvider` |
| `TheTechIdea.Beep.PineConeDatasource` | PineCone | `PineConeMigrationProvider.cs` | `PineConeMigrationProvider` |
| `TheTechIdea.Beep.ChromaDBDatasource` | ChromaDB | `ChromaDBMigrationProvider.cs` | `ChromaDBMigrationProvider` |
| `TheTechIdea.Beep.ShapVectorDatasource` | ShapVector | `ShapVectorMigrationProvider.cs` | `ShapVectorMigrationProvider` |

### 5c. FILE — read-only model files override the FileMutation fallback

| Folder | DataSourceType | Provider file | Base |
|---|---|---|---|
| `OnnxDataSource` | ONNX | `OnnxMigrationProvider.cs` | `NullMigrationProvider` (read-only) |
| `hdf5DataSource` | Hdf5 | `Hdf5MigrationProvider.cs` | `NullMigrationProvider` |
| `PetastormDataSource` | Petastorm | `PetastormMigrationProvider.cs` | `NullMigrationProvider` |
| `ParquetDataSource` | Parquet | `ParquetMigrationProvider.cs` | `NullMigrationProvider` |

(CSV/Xls/Txt → covered by `FileMutationMigrationProvider` category fallback; no colocated file needed.)

### 5d. RDBMS — covered by `RdbmsSqlMigrationProvider` fallback; colocated override only for engine quirks

| Folder | DataSourceType | Provider file | Why override |
|---|---|---|---|
| `SqliteDatasourceCore` | SqlLite | `SqliteMigrationProvider.cs` | table-rebuild for ALTER/RENAME |
| `OracleDataSourceCore` | Oracle | `OracleMigrationProvider.cs` | identifier length / offline-window hints |

(SqlServer, Postgre, MySql, Hana, Firebird/FirebirdEmbedded, FireBolt, CockRoach, DuckDB →
fallback covers them; no colocated file unless an engine needs a quirk later.)

### 5e. CLOUD-SQL — exact-type registration, NO colocated file

SnowFlake, Spanner, GoogleBigQuery, Presto, Supabase speak SQL → registered (in the engine's
fallback wiring) to reuse `RdbmsSqlMigrationProvider`. Kusto → colocated
`KustoMigrationProvider.cs` (native `.create`/`.alter`); Rockset → colocated
`RocksetMigrationProvider.cs` (native SQL-API). AmazonS3 / Hadoop / GoogleSheets → read-only
fallback.

**Total colocated files: ~25** (11 NoSQL + 5 vector + 4 read-only-file + 2 RDBMS-quirk +
Kusto + Rockset). The other **~132 datasources are covered by category fallback with zero
folder files.**

### Example colocated override (MongoDB)

`BeepDataSources/DataSourcesPluginsCore/MongoDBDataSourceCore/MongoDBMigrationProvider.cs`

```csharp
[SchemaMigrationProvider(DataSourceType.MongoDB, DatasourceCategory.NOSQL)]
public class MongoDBMigrationProvider : ISchemaMigrationProvider
{
    private readonly MongoDBDataSource _owner;
    public DataSourceType DataSourceType => DataSourceType.MongoDB;
    public DatasourceCategory Category => DatasourceCategory.NOSQL;
    public SchemaMigrationCapabilities Capabilities { get; } = new()
    {
        SupportsCreateEntity = true, SupportsAddColumn = true, SupportsDropColumn = true,
        SupportsRenameColumn = true, SupportsCreateIndex = true, SupportsDropIndex = true,
        SupportsDropEntity = true, SupportsRenameEntity = true
        // AlterColumn / FK ops: false (no enforced schema / FKs)
    };
    public MongoDBMigrationProvider(IDataSource owner) { _owner = (MongoDBDataSource)owner; }
    public IErrorsInfo CreateEntity(EntityStructure e) { _owner.Database.CreateCollection(e.EntityName); return Ok(); }
    // …native driver calls; unsupported ops return ErrorsInfo(Unsupported)
}
```

## 6. Shared base/fallback providers (in BeepDM engine)

`BeepDM/DataManagementEngineStandard/Editor/SchemaMigration/`

| File | Class | Role |
|---|---|---|
| `RdbmsSqlMigrationProvider.cs` | `RdbmsSqlMigrationProvider` | **Behavior-preserving** — runs existing `IDataSourceHelper.Generate*Sql` + `ExecuteSql`. RDBMS + SQL-cloud engines reuse it. |
| `FileMutationMigrationProvider.cs` | `FileMutationMigrationProvider` | Existing `FileHelper` file-mutation path. |
| `ExternalReadOnlyMigrationProvider.cs` | `ExternalReadOnlyMigrationProvider` | All ops → `Unsupported`; `IsReadOnly=true`. Fallback for Connector/MessageQueue/Stream/Queue/WebApi + external cloud. |
| `VectorMigrationProvider.cs` | `VectorMigrationProvider` | Generic create-collection + create/drop vector index via the owner's API; per-engine overrides where index params differ. |
| `NullMigrationProvider.cs` | `NullMigrationProvider` | Final fallback (Tier 3) — everything `Unsupported`. |

## 7. MigrationManager rewiring

The ~11 `var helper = _editor.GetDataSourceHelper(...)` + `Generate*Sql` + `ExecuteSql`
blocks in `MigrationManager.EntityOperations.cs` collapse to one dispatch:

```csharp
private ISchemaMigrationProvider ResolveProvider()
    => _editor.GetMigrationProvider(MigrateDataSource.DatasourceType, MigrateDataSource.Category);

public IErrorsInfo AddColumn(EntityStructure entity, EntityField column)
{
    var p = ResolveProvider();
    if (!p.Capabilities.Supports(SchemaMigrationOp.AddColumn))
        return Unsupported("AddColumn", entity.EntityName);   // DdlOperationOutcome.Unsupported
    var result = p.AddColumn(entity.EntityName, column);
    TrackMigration("AddColumn", entity.EntityName, column.FieldName, sql:null, result);
    EmitDdlEvidence("AddColumn", entity.EntityName, column.FieldName, null, null,
        result.Flag == Errors.Ok ? DdlOperationOutcome.Executed : DdlOperationOutcome.Failed, DdlHelperSource.Direct);
    return result;
}
```

- `IsFileDataSource(...)` special-casing is **removed** (file behavior now lives in
  `FileMutationMigrationProvider`, dispatched uniformly).
- All `EmitDdlEvidence`/`TrackMigration` instrumentation is **kept**.
- `BuildProviderCapabilityProfile` (MigrationManager.Capabilities.cs) is augmented to read
  `ISchemaMigrationProvider.Capabilities`, so plan/dry-run/preflight report honest support
  flags per type (e.g. Mongo → `SupportsForeignKeys=false`, `SupportsIndexes=true`).

## 8. Per-category capability matrix (what "full migration" means per family)

| Capability | RDBMS | FILE-CSV/Xls | FILE-model | NOSQL(doc) | Redis/Influx | CLOUD-SQL | Vector | Kusto | Connector/Queue |
|---|---|---|---|---|---|---|---|---|---|
| CreateEntity | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌(read-only) |
| AddColumn | ✅ | ✅ | ❌ | ✅ | 🟡 | ✅ | ✅ | ✅ | ❌ |
| AlterColumn | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | 🟡 | ❌ |
| DropColumn | ✅ | ✅ | ❌ | ✅ | 🟡 | ✅ | 🟡 | ✅ | ❌ |
| RenameColumn | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Rename/Drop/Truncate Entity | ✅ | 🟡 | ❌ | ✅ | 🟡 | ✅ | 🟡 | ✅ | ❌ |
| CreateIndex | ✅ | ❌ | ❌ | ✅ | 🟡 | ✅ | ✅ | ✅ | ❌ |
| AddForeignKey | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |

✅ native · 🟡 emulated/limited · ❌ `Unsupported` (readiness report flags it). The plan
layer already tolerates `Unsupported` outcomes.

## 9. File-by-file deliverables

**BeepDM (contract + engine + shared bases): ~11 files**
- `DataManagementModelsStandard/Editor/SchemaMigration/`: `ISchemaMigrationProvider.cs`
  (+ `SchemaMigrationCapabilities`, `SchemaMigrationOp`),
  `SchemaMigrationProviderAttribute.cs`, `IMigrationProviderRegistry.cs`
- `DataManagementModelsStandard/Editor/IDMEEditor.cs` — add `GetMigrationProvider(...)`
- `DataManagementEngineStandard/Editor/SchemaMigration/`: `MigrationProviderRegistry.cs`
  (attribute scan + 3-tier resolve), `RdbmsSqlMigrationProvider.cs`,
  `FileMutationMigrationProvider.cs`, `ExternalReadOnlyMigrationProvider.cs`,
  `VectorMigrationProvider.cs`, `NullMigrationProvider.cs`
- `DataManagementEngineStandard/Editor/DM/DMEEditor.MigrationProviders.cs` — wires registry
  + category fallbacks into `IDMEEditor`
- `Editor/Migration/MigrationManager.EntityOperations.cs` — rewired (§7)
- `Editor/Migration/MigrationManager.Capabilities.cs` — augment profile from provider caps

**BeepDataSources (colocated overrides): ~25 files** — exactly the §5a–§5e tables. Each
datasource csproj needs **no new `<PackageReference>`** (already references the engine).

**~132 other datasources: 0 files** — covered by category fallback.

## 10. Phased rollout

| Phase | Scope | Outcome | Est |
|---|---|---|---|
| **10.1 Foundation** | Contracts, attribute, 3-tier registry, `IDMEEditor.GetMigrationProvider`, `NullMigrationProvider`. Wire category fallbacks (RDBMS→Rdbms, Connector/Queue/Stream/WebApi→ExternalReadOnly). Rewire `MigrationManager.EntityOperations` to dispatch. | Seam exists; RDBMS still works; everything else cleanly `Unsupported`. | 1.5d |
| **10.2 RDBMS behavior-preserving** | `RdbmsSqlMigrationProvider` (move existing helper+ExecuteSql behind it) + SQLite/Oracle colocated quirks. | All SQL engines identical; RDBMS tests green. | 2d |
| **10.3 FILE + read-only** | `FileMutationMigrationProvider` (CSV/Xls via fallback); 4 read-only-model colocated `NullMigrationProvider` overrides. Remove `IsFileDataSource`. | File routed uniformly; model files read-only. | 1d |
| **10.4 NOSQL native** | 11 colocated native providers (Mongo, Raven, Couch*, LiteDB, Redis, Influx, Firebase, Cosmos, DynamoDB). | First non-RDBMS engines migrate natively. | 4d |
| **10.5 Vector + CLOUD** | `VectorMigrationProvider` fallback + 5 vector overrides; CLOUD-SQL → Rdbms fallback; Kusto/Rockset colocated native. | Analytics/vector engines covered. | 2.5d |
| **10.6 Capability propagation + tests** | Augment `BuildProviderCapabilityProfile`; full test pass across families. | Plan/dry-run honest for all 157 types. | 1d |

Total ≈ 12 days. Each phase ships green and independently.

## 11. Testing strategy

- **Per-provider unit tests** (one project per family in `tests/`): each op either executes
  against an embedded/local instance (SQLite, LiteDB, Mongo-local, Raven embedded, Qdrant
  container) or returns `Unsupported` per §8.
- **Behavior-preservation tests**: existing `MigrationManager` RDBMS tests must pass
  unchanged after 10.2.
- **Resolution contract test**: for every one of the 157 `[AddinAttribute]` types,
  `Resolve(type, category)` returns a non-null provider whose `Capabilities` match §8.
- **Read-only contract test**: any `IsReadOnly` provider's mutating ops all return
  `Unsupported` (no silent no-op, no throw).
- **Integration**: `EnsureEntity` round-trip on Mongo + LiteDB + SQLite + Qdrant
  (create → add column → create index → drop) — currently impossible for Mongo, becomes the
  headline proof.

## 12. Acceptance criteria

1. `IDataSource` and all 157 implementations are **unchanged** (diff = 0 lines on those files).
2. RDBMS migration behavior is byte-for-byte identical (existing tests green).
3. Mongo + LiteDB + a vector DB can `CreateEntity → AddColumn → CreateIndex → DropColumn` natively.
4. For all ~108 read-only datasources, `Resolve` returns `ExternalReadOnlyMigrationProvider`
   and every mutating op returns `DdlOperationOutcome.Unsupported` with a clear readiness issue.
5. `Resolve` is never null for any of the 157 types (Tier-3 `NullMigrationProvider` guarantees it).
6. ~25 colocated override files exist at the §5a–§5e paths; the rest use category fallback.

## 13. Risks & notes

- **Registry population timing**: scan assemblies lazily on first `Resolve`, cache by type;
  scan must run after addin load (hook the existing addin loader).
- **`datasource-registry.json` is incomplete** (23 vs 157) — do NOT drive discovery off it;
  use the `[AddinAttribute]`/`[SchemaMigrationProvider]` scan.
- **RdbmsSqlMigrationProvider must not regress** SQLite table-rebuild / Oracle windows —
  keep quirks in colocated subclasses.
- **External/read-only is correct, not a gap**: Salesforce/Kafka/Slack schema is owned by
  the vendor; migration there is a no-op by design.
- **Future datasources**: adding migration = one colocated file + attribute (Open/Closed);
  adding a read-only datasource = zero files (fallback covers it).
