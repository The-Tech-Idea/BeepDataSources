# RDBMSDataSource Plugin

The `RDBMSDataSource` plugin provides a full featured implementation of the `IDataSource` / `IRDBSource` interfaces for relational database systems (SQL Server, Oracle, MySQL, PostgreSQL, SQLite, etc.). It encapsulates connection management, metadata discovery, CRUD, dynamic query generation with filtering & pagination, DDL generation, and schema inspection.

## Key Features

- Open/close connection abstraction via `RDBDataConnection`
- Dynamic entity metadata discovery (fields, PKs, relations, precision/scale)
- Table or ad‑hoc query support (`ViewType.Table` / `ViewType.Query`)
- Safe parameterized CRUD (Insert/Update/Delete) with automated parameter name collision handling
- Bulk entity update helper (`UpdateEntities`)
- Filter aware dynamic SQL builder (`BuildQuery`) including BETWEEN support
- Pagination support with provider specific syntax through `RDBMSHelper.GetPagingSyntax`
- Scalar query execution (`GetScalar`, `GetScalarAsync`)
- DDL generation for entities & foreign key relations
- Identity / sequence retrieval post insert (database aware)
- Foreign key & child relation discovery
- Dynamic runtime type creation for row materialization (`DMTypeBuilder` + `ObservableBindingList<T>`) 
- Dapper integration helpers (`GetData<T>`, `SaveData<T>`) for lightweight micro‑ORM use cases
- Provider variability controlled via drivers configuration (`ConnectionDriversConfig`)

## Core Classes & Concepts

| Component | Responsibility |
|-----------|----------------|
| `RDBSource` | Concrete relational `IDataSource` implementation |
| `RDBDataConnection` | Low level connection holder & state |
| `EntityStructure` / `EntityField` | In‑memory schema model |
| `RelationShipKeys` / `ChildRelation` | Relationship metadata |
| `ETLScriptDet` | DDL script descriptor |
| `RDBMSHelper` | Vendor specific helpers (paging, identity retrieval) |
| `DMTypeBuilder` | Runtime POCO type generation |

## CRUD Internals

### Insert
1. `GetInsertString` builds column & parameter lists (skips auto increment).
2. Ensures uniqueness of parameter names (`usedParameterNames`).
3. Executes command then optionally fetches identity (`GenerateFetchLastIdentityQuery`).

### Update
1. `GetUpdateString` builds SET part with non‑PK fields first, then WHERE PK composite.
2. Ordered sequence captured in `UpdateFieldSequnce` and bound via `CreateUpdateCommandParameters`.

### Delete
1. `GetDeleteString` builds WHERE predicate for PK fields.

### Query & Filtering
- `BuildQuery` augments base SELECT (table or custom) with filters (parameterized) and preserves GROUP BY / HAVING / ORDER BY.
- Supports BETWEEN (creates two parameters) & simple operators (=, >, <, LIKE, etc.).

### Pagination
Uses vendor specific paging snippet appended to the final query through `RDBMSHelper.GetPagingSyntax` and returns a `PagedResult` with metadata.

## Metadata Discovery Flow
`GetEntityStructure(string,bool)` → determines if table or query → fetches schema (`GetTableSchema`) → maps DataTable schema rows → populates fields, PKs, relations (FK introspection via `GetTablesFKColumnList`).

Oracle FLOAT precision is remapped to .NET types through `MapOracleFloatToDotNetType` + `GetFloatPrecision`.

## DDL Generation
`GenerateCreateEntityScript` + `CreatePrimaryKeyString` + relation scripts (`CreateForKeyRelationScripts`) produce ordered `ETLScriptDet` list for entity creation & FK constraints.

## Error & Logging
All operations log via `IDMEEditor.AddLogMessage` and propagate status through `IErrorsInfo` (`ErrorObject`).

## Threading & Async
Only lightweight async wrappers exist (e.g., `GetScalarAsync`). Heavy operations are synchronous presently.

## Extension Points
Override any virtual method (e.g., `GetScalar`, `ExecuteSql`, `GetDataAdapter`, `GetInsertString`, `CreateAutoNumber`) in a derived provider specialization.

## Typical Usage
```csharp
var rdb = new RDBSource("MyDb", logger, editor, DataSourceType.SqlServer, errors);
rdb.Openconnection();
var customers = rdb.GetEntity("Customers", new List<AppFilter>{ new AppFilter{ FieldName="Country", Operator="=", FilterValue="'USA'"}});
var entityStruct = rdb.GetEntityStructure("Customers", refresh:true);
var newCustomer = new { CustomerID = 0, CompanyName = "Acme", Country = "USA" }; // PK set if identity fetched
rdb.InsertEntity("Customers", newCustomer);
```

## When to Choose RDBMSDataSource
Use when you need rich metadata driven operations across multiple relational backends with minimal provider specific branching in your application code.

---
For advanced scenarios (custom paging, sharding, batching) consider subclassing `RDBSource` and overriding targeted build/execution pieces.
