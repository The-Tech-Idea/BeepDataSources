# RDBDataSource_v2 Refactor Plan

Goal: Introduce a cleaner, testable, extensible version of `RDBSource` named `RDBDataSource_v2` that implements `IRDBSource` and `IDataSource`, using helper classes to isolate responsibilities and reduce the monolithic implementation.

## Key Improvements
1. Streaming-first data access (no DataTable/materialization unless explicitly required).
2. Separation of concerns via helpers.
3. Reduced reflection + duplication when building queries and binding parameters.
4. Centralized entity structure caching + lazy refresh.
5. Unified filter + parameter binding.
6. Deterministic paging with provider-specific syntax.
7. Async-ready (introduce async variants / optional IAsyncEnumerable in future).
8. Defensive error handling with minimal coupling to UI/logging side.

## Helper Modules
| Helper | Purpose | Public Surface |
|--------|---------|----------------|
| `SqlQueryBuilder` | Build SELECT + WHERE + GROUP/HAVING/ORDER + filter injection | `BuildFilteredQuery(baseSql, filters, schemaName)` |
| `FilterParameterBinder` | Translate `List<AppFilter>` to IDbCommand parameters | `Bind(cmd, filters)` |
| `PaginationHelper` | Generate paging suffix depending on `DataSourceType` | `BuildPagedSql(sql, dbType, page, size)` |
| `PagedQueryExecutor` | Execute count + paged result returning `List<Dictionary<string,object>>` | `ExecutePaged(...)` |
| `DataStreamer` | Stream rows from a command | `IEnumerable<Dictionary<string,object>> Stream(cmd)` |
| `EntityStructureCache` | Cache + refresh entity metadata | `Get(name, refresh)` |
| `DbTypeMapper` | Map CLR/type names to DbType | `ToDbType(string)` |
| `TransactionManager` | (Future) Manage Begin/Commit/Rollback decoupled from source | `Begin() Commit() Rollback()` |

## Class Layout
```
RDBMSDataSource/
  Helpers/
    SqlQueryBuilder.cs
    FilterParameterBinder.cs
    PaginationHelper.cs
    DataStreamer.cs
    PagedQueryExecutor.cs
    EntityStructureCache.cs
    DbTypeMapper.cs
    TransactionManager.cs (placeholder)
  RDBDataSource_v2.cs
  Plan_RDBDataSource_v2.md
```

## RDBDataSource_v2 Responsibilities
- Maintain required properties (delegating to `Dataconnection`).
- Provide implementations for:
  - `GetEntity(entity, filters)` (stream)
  - `GetEntity(entity, filters, page, size)` (paged list)
  - `GetEntityAsync` wrapper
  - Metadata retrieval using `EntityStructureCache`
  - CRUD delegates (initially call existing `RDBSource` logic optional or implement minimal safe stubs calling helpers)
- Keep existing `IErrorsInfo` + logging integration.

## Filter Normalization Rules
- Ignore filters with missing `FieldName` / `Operator` / `FilterValue`.
- Support BETWEEN as two parameters (`p_field` + `p_field1`).
- Parameter names sanitized (spaces ? `_`, truncated for Oracle/Postgre if > 30 chars).

## Paging Strategy
- Reuse existing `RDBMSHelper.GetPagingSyntax` if available.
- Ensure ORDER BY exists; fall back to first primary key else constant `ORDER BY 1`.
- Count query:
  - Table: `SELECT COUNT(*) FROM table [WHERE ...]`
  - Complex: wrap filtered (without trailing ORDER BY) as subquery.

## Error Handling
- Catch and log but never throw to caller unless catastrophic.
- Return empty enumerables on failure; set `ErrorObject.Flag` + `Message`.

## Incremental Implementation Steps
1. Create plan (this file).
2. Implement `SqlQueryBuilder`.
3. Implement `FilterParameterBinder`.
4. Implement `PaginationHelper`.
5. Implement `DataStreamer`.
6. Implement `EntityStructureCache`.
7. Implement `DbTypeMapper`.
8. Implement `PagedQueryExecutor` (uses 2,3,4,5,6).
9. Implement `RDBDataSource_v2` using helpers.
10. (Optional) Add TransactionManager placeholder.
11. Build & adjust compile errors.
12. (Optional) Add async variants / IAsyncEnumerable wrappers.

## Minimal Interfaces Used
- `AppFilter`, `EntityStructure`, `EntityField`, `PagedResult`, `IErrorsInfo`, `IDMEEditor`, `IDataConnection`, `DataSourceType` (reuse existing definitions).

## Compatibility
- Does not replace original `RDBSource` yet; both coexist.
- Consumers can opt-in by instantiating `RDBDataSource_v2`.

## Future Extensions
- Expression-based filter building.
- Strongly typed projection (generic variant of GetEntity<T>). 
- Caching of compiled accessors for object materialization.
- Async streaming with `IAsyncEnumerable<Dictionary<string,object>>`.

---
Author: Automated Refactor Assistant.
Status: Draft Plan Approved for Implementation.
