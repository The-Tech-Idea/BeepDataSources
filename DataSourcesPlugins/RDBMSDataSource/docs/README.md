# RDBSource — developer reference

`RDBSource` is the base class every relational driver in this repository inherits. It is one
`public partial class RDBSource : IRDBSource` spread across 16 files (~6.7k lines), plus five
helpers and a connection wrapper.

A change here reaches every relational driver at once. Read [09-extending.md](09-extending.md)
before adding a driver, and [10-known-issues.md](10-known-issues.md) before assuming any behaviour
in this class is correct.

## Who inherits it

Twenty drivers extend `RDBSource` directly. Eight of these (marked *new*) did not exist until the
Tier 5 sweep (`docs/10-known-issues.md`, D8/D15) found their `classHandler` registered in BeepDM's
`ConnectionHelper_RDBMS.cs` with no matching class anywhere in the repo:

| Driver | Project |
|---|---|
| `SQLServerDataSource` | `DataSourcesPluginsCore/SQlServerDataSourceCore/` |
| `AzureSQLDataSource` *(new)* | `DataSourcesPluginsCore/SQlServerDataSourceCore/` |
| `PostgreDataSource` | `DataSourcesPluginsCore/PostgreDataSourceCore/` |
| `TimeScaleDBDataSource` *(new)* | `DataSourcesPluginsCore/PostgreDataSourceCore/` |
| `MySQLDataSource` | `DataSourcesPluginsCore/MySqlDataSourceCore/` |
| `AWSRDSDataSource` *(new)* | `DataSourcesPluginsCore/MySqlDataSourceCore/` |
| `OracleDataSource` | `DataSourcesPluginsCore/OracleDataSourceCore/` |
| `HanaDataSource` | `DataSourcesPluginsCore/HanaDataSource/` |
| `FireBirdDataSource` | `DataSourcesPluginsCore/FirebirdDataSourceCore/` |
| `FireBirdEmbeddedDataSource` | `DataSourcesPluginsCore/FirebirdDataSourceCore/` |
| `FireBoltDataSource` | `DataSourcesPluginsCore/FireboltDataSource/` |
| `CockRoachDataSource` | `DataSourcesPluginsCore/CockroachDBDataSourceCore/` |
| `PrestoDataSource` | `DataSourcesPluginsCore/PrestoDatasource/` |
| `SnowFlakeDataSource` | `DataSourcesPluginsCore/SnowFlakeDataSource/` |
| `SpannerDataSource` | `DataSourcesPluginsCore/SpannerDataSourceCore/` |
| `SQLCompactDataSource` *(new)* | `DataSourcesPluginsCore/SqlCompactDatasourceCore/` |
| `DB2DataSource` *(new)* | `DataSourcesPluginsCore/DB2DataSourceCore/` |
| `VerticaDataSource` *(new)* | `DataSourcesPluginsCore/VerticaDataSourceCore/` |
| `TerraDataDataSource` *(new)* | `DataSourcesPluginsCore/TerraDataDataSourceCore/` |
| `VistaDBDataSource` *(new, minimal — see D15)* | `DataSourcesPluginsCore/VistaDBDataSourceCore/` |

Two extend it through `InMemoryRDBSource : RDBSource, IInMemoryDB`:

| Driver | Project |
|---|---|
| `SQLiteDataSource` | `DataSourcesPluginsCore/SqliteDatasourceCore/` |
| `DuckDBDataSource` | `InMemoryDB/DuckDBDataSourceCore/` |

## File map

The class is split by concern. Only 15 of the 16 files contain code — `RDBSource.Pagination.cs` is
a comment block.

| File | Lines | What it holds | Doc |
|---|---|---|---|
| `RDBSource.cs` | 164 | Fields, properties, constructor. The shared mutable state lives here. | [02](02-lifecycle.md) |
| `RDBSource.Connection.cs` | 33 | `Openconnection` / `Closeconnection` — thin delegation. | [02](02-lifecycle.md) |
| `RDBSource.Dispose.cs` | 34 | `IDisposable`. | [02](02-lifecycle.md) |
| `RDBSource.Transaction.cs` | 155 | `BeginTransaction` / `Commit` / `EndTransaction`, `_activeTransaction`. | [03](03-transactions.md) |
| `RDBSource.CRUD.cs` | 599 | `InsertEntity` / `UpdateEntity` / `DeleteEntity` / `UpdateEntities` and async twins. | [04](04-crud-dml.md) |
| `RDBSource.DMLGeneration.cs` | 961 | INSERT/UPDATE/DELETE text, parameter binding, CREATE TABLE DDL. | [04](04-crud-dml.md) |
| `RDBSource.Query.cs` | 1040 | `GetEntity`, `RunQuery`, `ExecuteSql`, `GetScalar`, `SetObjects`. | [05](05-query-paging.md) |
| `RDBSource.Modernization.cs` | 497 | `IAsyncEnumerable` streaming, async paging, async scalar/non-query. | [05](05-query-paging.md) |
| `RDBSource.Pagination.cs` | 36 | **No code.** A comment describing a consolidation that never happened. | [05](05-query-paging.md) |
| `RDBSource.Schema.cs` | 911 | `GetEntityStructure`, `GetEntitesList`, FK/child tables, `GetEntityType`, `CreateEntityAs`. | [06](06-schema-types.md) |
| `RDBSource.TypeMapping.cs` | 108 | `.NET type name → DbType`, value conversion. All `private`. | [06](06-schema-types.md) |
| `RDBSource.BulkOperations.cs` | 1086 | `BulkInsertEntities` / `BulkUpdateEntities` and async twins, temp-table strategies. | [07](07-bulk.md) |
| `RDBSource.Cache.cs` | 290 | Query / result / prepared-statement caches. Mostly unwired. | [08](08-cache-resilience.md) |
| `RDBSource.Resilience.cs` | 426 | Polly retry + circuit breaker, health checks. Not wired to any query path. | [08](08-cache-resilience.md) |
| `RDBSource.Utilities.cs` | 343 | `GetDataCommand`, `GetDataAdapter`, `GetFieldName`, `GetTableName`, `HandleDatabaseError`. | [02](02-lifecycle.md), [04](04-crud-dml.md) |
| `RDBSource.Dapper.cs` | 38 | `GetData<T>` / `SaveData<T>`. | [05](05-query-paging.md) |

Supporting types:

| File | Role |
|---|---|
| `InMemoryRDBSource.cs` | `RDBSource` + `IInMemoryDB`; ETL-backed load/sync/refresh for file-and-memory engines. |
| `RDBDataConnection.cs` | `IDataConnection` implementation: driver instantiation, connection-string assembly, open/close. |
| `Helpers/DbTypeMapper.cs` | Static `.NET type name → DbType` dictionary. |
| `Helpers/DataStreamer.cs` | `IDataReader` → `Dictionary` streaming. The typed-object overload was removed: it had no callers, pinned every Roslyn-generated type it saw in an unbounded static cache, and dropped nullable properties silently. |
| `Helpers/CommandOwningDataReader.cs` | An `IDataReader` that disposes the `IDbCommand` it was executed from. Lets `GetDataReader` hand out a reader without leaking the command behind it. |
| `Helpers/EntityStructureCache.cs` | Per-instance `ConcurrentDictionary` of entity structures. |
| `Helpers/PagedQueryExecutor.cs` | Count query + paged query execution. **No callers.** |
| `Helpers/PaginationHelper.cs` | Wrapper over `RDBMSHelper.GetPagingSyntax`. **No callers.** |

## Where the contract comes from

`IRDBSource` and `IDataSource` are defined in the sibling BeepDM repository, not here:

- `../../../BeepDM/DataManagementModelsStandard/DataBase/IRDBSource.cs`
- `../../../BeepDM/DataManagementModelsStandard/DataBase/IDataSource.cs`

`IRDBSource` adds to `IDataSource`: `GetTableSchema`, `GetDataReader`, `GetTablesFKColumnList`,
`CreateAutoNumber`, `DisableFKConstraints`, `EnableFKConstraints`, `GetData<T>`, `SaveData<T>`,
`GetSchemaName`, `BeginTransaction`, `EndTransaction`, `Commit`, and `GetListofEntitiesSql`.

BeepDM also ships a dialect helper library that this class only partially uses —
`../../../BeepDM/DataManagementEngineStandard/Helpers/RDBMSHelpers/`. Prefer it over adding a new
`switch (DatasourceType)`; see [09-extending.md](09-extending.md).

## What a driver actually overrides

Very little. Measured across every driver project, the complete overridden surface is:

| Member | Drivers overriding it |
|---|---|
| `EnableFKConstraints` | 11 |
| `DisableFKConstraints` | 11 |
| `ColumnDelimiter` | 7 |
| `ParameterDelimiter` | 6 |
| `GetEntitesList` | 1 |
| `CreateEntityAs` | 1 |
| `Openconnection` / `Closeconnection` | 1 |
| `ConfigureCommand` | 1 (Oracle, for `BindByName`) |

`GetTableSchema`, `GetEntityforeignkeys`, `GetChildTablesList`, `GetTablesFKColumnList`,
`GetSchemaName` and `GetCreateEntityScript` are `virtual` but **never** overridden by any driver.
Every other dialect difference is handled inside the base class. That started as 22
`switch (DatasourceType)` sites; consolidating paging, quoting, dead-code removal and the move of
multi-row INSERT support to `RDBMSHelper.SupportsFeature` has taken it down to a handful, and
`09-extending.md` sets out which of the remainder belong here versus in BeepDM's shared dialect
table.

The one driver that needed more than this, `DuckDBDataSource`, gave up on the base and overrides
about twenty members including the whole CRUD surface, `GetEntityStructure`, `Openconnection` and
the transaction trio.

## Reading order

1. **[01-error-model.md](01-error-model.md)** — read this first. How success and failure are
   reported here is unusual, and it explains why several other behaviours look the way they do.
2. [02-lifecycle.md](02-lifecycle.md) — construction, connection, disposal, and the shared state.
3. [03-transactions.md](03-transactions.md) — small, self-contained, and recently fixed.
4. [04-crud-dml.md](04-crud-dml.md) — the write path.
5. [05-query-paging.md](05-query-paging.md) — the read path, and its four competing paging routes.
6. [06-schema-types.md](06-schema-types.md) — metadata discovery and type mapping.
7. [07-bulk.md](07-bulk.md) — bulk insert and update.
8. [08-cache-resilience.md](08-cache-resilience.md) — two subsystems that are largely not wired up.
9. [09-extending.md](09-extending.md) — writing a driver.
10. [10-known-issues.md](10-known-issues.md) — the defect register, with failing scenarios.

## Testing

`tests/RDBDataSource.Tests/` at the repository root:

```bash
dotnet test tests/RDBDataSource.Tests/RDBDataSource.Tests.csproj
```

Be aware of what that suite does and does not cover: 25 of its 35 tests hand-write SQL against
in-memory SQLite and never instantiate `RDBSource`. Only the `DbTypeMapper` tests exercise
production code. See [10-known-issues.md](10-known-issues.md#coverage).
