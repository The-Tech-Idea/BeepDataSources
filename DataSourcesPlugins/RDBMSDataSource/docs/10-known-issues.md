# Known issues

The defect register for `RDBSource`. This file is the single source of truth; the Help pages under
`Help/providers/` link here rather than keeping their own list.

Every entry was verified against the source. Line references are to
`DataSourcesPlugins/RDBMSDataSource/PartialClasses/RDBSource/` unless stated otherwise.

**Fixed and no longer worth reporting** — earlier documents still list these as open:
`UpdateEntities` calling `InsertEntity` (fixed, `CRUD.cs:305`); `Commit`/`EndTransaction` as empty
stubs (fixed 2026-08-03, commit `c3901629`); the `MemoryCache` key-enumeration gap (tracking added,
though the result cache is dead — see K17); the Debug-only "Get Tables List Query" trace logged as
`Errors.Failed` (fixed, `Schema.cs:396-403`).

---

## F1 — A null logger turns failures into successes — *addressed in `RDBSource`*

**Where** `../../../BeepDM/DataManagementEngineStandard/Editor/DM/DMEEditor.cs:204-209`

`AddLogMessage` returns before recording the flag when `Logger == null`. Roughly forty failure paths
in `RDBSource` reported *only* through that call and never set a flag themselves.

**Failing scenario** Run any headless service or test host with no logger configured. Close the
connection, then call `CreateEntityAs(entity)`. `GetDataCommand()` returned `null` with `Flag == Ok`
→ `ExecuteSql` had no `else` on `if (cmd != null)` and returned `Ok` → `CreateEntityAs` logged
**"Entity 'X' created successfully"** (`Schema.cs:635-647`) for a `CREATE TABLE` that never ran.
`RunScript` reported the same for un-executed DDL, and `InMemoryRDBSource.cs:356,397` treated the
same `Ok` as proof a table had been truncated, then reloaded data on top of rows that were still
there.

**Status — fixed in this class; the engine-level cause is unchanged.** `RDBSource` no longer depends
on `AddLogMessage`'s side effect: two helpers, `SetFailure` and `SetSuccess`
(`Utilities.cs`, "Error Handling Helpers"), assign the flag on both error objects themselves and
then log, and `HandleDatabaseError` is now null-guarded so the error handler cannot itself throw.
The sites converted are `GetDataCommand`, `GetDataAdapter` (including its four silent early
returns and the swallowing catch), `ExecuteSql` (the missing `else`), `GetScalar`,
`GetScalarAsync`, `RunQuery`, `GetDataReader`, the paged `GetEntity`, `GetTableSchema`,
`GetEntitesList`, `GetChildTablesList`, `GetTablesFKColumnList`, `GetEntityforeignkeys`,
`GetFloatPrecision`, `Openconnection`, `Closeconnection`, `BeginTransaction`, `Commit`,
`EndTransaction`, `CreateAutoNumber` and `RDBDataConnection.CloseConn`.

A later sweep finished the job: **all 73 remaining `DMEEditor.AddLogMessage` calls in the class are
null-guarded**, and the failure paths that still wrote `DMEEditor.ErrorObject` longhand —
`CreateEntityAs`'s validation and script-generation branches among them — go through `SetFailure`.
Those were two defects each: a `NullReferenceException` waiting for a headless caller, and an F1
instance. `CreateEntityAs` is the clearest case: it wrote `DMEEditor.ErrorObject` on three branches
and assigned `ExecuteSql`'s result to it on the fourth, so with no editor attached it threw before
the `CREATE TABLE` ever ran. It now works with no engine at all, which
`ServiceIndependenceTests.cs` demonstrates by creating a table through it.

`DMEEditor.AddLogMessage` itself was **not** changed — moving the flag assignment above its
`Logger == null` return would alter behaviour for every BeepDM consumer at once, which is a
decision for the engine, not for this plugin.

Covered by `tests/RDBDataSource.Tests/ErrorModelTests.cs`, which runs with neither a logger nor an
`IDMEEditor` — a strictly harsher condition than `Logger == null`. Verified to catch the original
defect: with the `GetDataCommand` and `ExecuteSql` fixes reverted, four of those tests fail.

Detail: [01-error-model.md](01-error-model.md).

---

## Tier 1 — wrong data, silently

### K1 — UPDATE can target a row chosen by the wrong column — *fixed*

**Where** `DMLGeneration.cs:304-307`, with `:41`, `:110`, `:166`

```csharp
if (usedParameterNames.Contains(paramName))
    paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
```

**Failing scenario** An entity with primary key `Id` and a non-key column `ProductId`. The SET clause
registers `p_ProductId` first; the WHERE clause then looks up `p_Id`, `Contains("p_Id")` matches
`p_ProductId`, and the statement becomes `... where Id = @p_ProductId`. The UPDATE lands on whichever
row has `Id` equal to that row's `ProductId` value. A sibling case: columns `Name` and `NameSuffix`
with the `StartsWith` lookups at `:41/:110/:166` — `HashSet` enumeration order is undefined, so
`p_Name` may resolve to `p_NameSuffix`.

**Status — fixed.** Generation now allocates through `AllocateParameterName`, which records the
`field → parameter name` association, and binding recovers it with `ResolveParameterName` — an exact
keyed lookup. Both substring searches are gone, and `ResetParameterAllocation` keeps the name set and
the map in step. Covered by `ParameterBindingTests.cs`; the deterministic reproduction (a key whose
normalised name collides with an already-allocated parameter) was verified to fail against the old
logic.

### K2 — Zero rows affected is returned as success — *fixed*

**Where** `CRUD.cs:74-78`, and the same shape at `:126`, `:506`, `:553`

`ErrorObject.Flag` is set `Ok` at entry; the zero-rows branch logs `Errors.Failed` but never changes
it.

**Failing scenario** `UpdateEntity` with a primary-key value that matches no row returns
`Errors.Ok`. Worse, `BulkOperations` counts successes by this flag (`:321,338,644,660,701,721`), so a
bulk update of 10,000 rows that matched none reports 10,000 successes.

**Status — fixed, with one deliberate carve-out.** `ReportAffectedRows` now decides this in one
place: zero rows affected is `Errors.Failed`, **except** for UPDATE on MySQL and MariaDB, which
report 0 for a row that matched but was already up to date unless the connection enables
`CLIENT_FOUND_ROWS` — treating that as failure would make every re-save of an unchanged entity fail.
DELETE has no such ambiguity on any engine and always reports failure at zero rows.

Note the old behaviour was not merely "reports success": `AddLogMessage(..., Errors.Failed)` *does*
set the flag when a logger is attached, so the outcome depended on whether logging was configured.
It is now deterministic either way. Bulk success counters inherit this automatically.
Covered by `WriteContractTests.cs`.

### K3 — Identifiers are emitted as string literals — *fixed*

**Where** `Utilities.cs:272-289` with `RDBSource.cs:119`

`GetFieldName` quotes only when the name contains a space, using `ColumnDelimiter`: default `"''"`,
overridden to `"'"` by six drivers, to `"[]"` by SQLite alone.

**Failing scenario** A column named `Cust Id`. `DeleteEntity` generates
`DELETE FROM Orders WHERE 'Cust Id' = @p_Cust_Id` — a literal compared to a parameter. MySQL and
SQLite accept it and delete nothing; per K2 that returns `Ok`. If the bound value ever equals the
literal string, the predicate is true for every row. Separately, a column named `Order` or `User` is
never quoted at all, on any driver, producing a syntax error. And `ORDER BY 'Order Id'` in the paging
fallback (`Query.cs:767`) sorts by a constant, so pages overlap and drop rows.

**Status — fixed.** `GetFieldName` now delegates to a new `protected virtual QuoteIdentifier`,
which uses the dialect's real characters — `[]` for SQL Server/Azure SQL/SQL CE/VistaDB, backticks
for MySQL/MariaDB, ANSI double quotes elsewhere — and escapes the closing delimiter by doubling it.
`ColumnDelimiter` stays (drivers set it, and it is part of `IDataSource`) but is no longer the
quoting mechanism.

Deliberately conservative about **when** it quotes: only names that are not plain identifiers
(anything outside `[A-Za-z0-9_]`, or a leading digit) and names that
`DatabaseEntityReservedKeywordChecker` reports as reserved. Quoting unconditionally would be simpler,
but on PostgreSQL and Oracle a quoted identifier is case-SENSITIVE while an unquoted one folds — so
any hand-authored `EntityStructure` whose column case does not match the stored case works today
*because* the name goes out unquoted, and blanket quoting would break it. Names from schema
discovery already carry the stored case and are unaffected either way.
Covered by `IdentifierQuotingTests.cs`.

### K4 — SQL injection via `AppFilter.Operator` and `FieldName` — *fixed*

**Where** `../../../BeepDM/DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs`
— `NormalizeOperator`'s default arm, and `QuoteIdentifier` at `:551-553`

`FilterValue` **is** parameterised everywhere; these two are not. `NormalizeOperator` has no
whitelist and returns unknown operators verbatim; `QuoteIdentifier` returns the identifier
**unquoted** precisely when it contains a space, dot, bracket, quote or backtick.

**Failing scenario** An `AppFilter` whose `Operator` is `"= 1 OR 1=1 --"` is concatenated straight
into the WHERE clause.

**Status — fixed in BeepDM.** Three changes to
`DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs`:

1. `NormalizeOperator` returns `string.Empty` for anything outside the alias table instead of
   passing the token through. Callers drop a filter whose operator normalises to empty.
2. Both WHERE builders' `default:` arms — the ones that interpolate the operator — now accept only
   `= != > >= < <=`. Second lock on the same door, so a future alias cannot reach the concatenation.
3. `QuoteIdentifier` quotes and escapes instead of giving up, splitting dotted names so each part is
   quoted separately.

**Note the residual behaviour:** a rejected operator means the filter is *dropped*, so the query
returns the unfiltered set. That is safe against injection but not against over-fetching — a caller
relying on a filter for access control would see more rows, not fewer. Callers should validate
operators before they get here.

Covered by `FilterInjectionTests.cs`, which asserts on the generated SQL rather than row counts:
row counts cannot tell the two outcomes apart, because a successful `OR 1=1` injection and a dropped
filter both return every row. Verified against the reverted fix — seven tests fail, including the
end-to-end one, which confirms `DROP TABLE` really did execute.

### K5 — Bulk insert writes an all-NULL table and reports success — *fixed*

**Where** `BulkOperations.cs:763-769`

Reflects on `typeof(T)` rather than the instance type.

**Failing scenario** The ETL layer passes `IEnumerable<object>`. Every `GetProperty` on `typeof(object)`
returns null, so every parameter becomes `DBNull.Value`, and the rows land as all-NULL with
`Errors.Ok`.

**Status — fixed.** Reflection now uses the row's runtime type via `FindPropertyCaseInsensitive`,
matching the single-row path. The same edit also sets `DbType` and runs values through
`ConvertToDbTypeValue`, which the bulk path skipped entirely — so `byte[]`, `Guid`, `decimal` scale
and `DateTime` bounds are no longer left to provider inference.

### K6 — Three drivers cannot write at all

**Where** `HanaDataSource.cs:23,27`, and the equivalents in `SnowFlakeDataSource` and
`PrestoDataSource`, with `DMLGeneration.cs:233`

All three set `ParameterDelimiter = "?"`, so the generated SQL says `?p_Name` and the parameter is
*named* `?p_Name`. Neither positional nor named binding accepts that.

**Failing scenario** Any `InsertEntity` / `UpdateEntity` / `DeleteEntity` on Hana, Snowflake or
Presto.

**Fix direction** Use each provider's real named-parameter prefix.

### K7 — Every SQL Server connection permanently reconfigures the login — *fixed*

**Where** `RDBDataConnection.cs:180`

```csharp
cmd.CommandText = $"ALTER LOGIN {ConnectionProp.UserID} with DEFAULT_DATABASE = {ConnectionProp.Database}";
```

**Failing scenario** Open any SQL Server connection with a non-null `SchemaName`. The login's default
database is permanently changed server-side, affecting every future connection by that login from
any application. It does not change the current session's database — that needs `USE`. The branch is
gated on `SchemaName` but uses `Database`; the identifiers are interpolated with no quoting; the
command is never disposed; and a failure is logged as `Warning` and ignored, leaving a connection
that reports `Open` while pointing at the wrong context.

**Status — fixed.** The SQL Server arm is removed rather than corrected: the database is already
selected by `Initial Catalog` in the connection string, so there was nothing for it to do. The Oracle
arm (`ALTER SESSION SET CURRENT_SCHEMA`) is legitimate and stays, now with the schema name validated
as a plain identifier before interpolation and the command in a `using`.

---

## Tier 2 — broken by default on major engines

### K8 — Bulk batches open a transaction the row commands never join — *fixed*

**Where** `BulkOperations.cs:312-331`, `:370`, `:635`, `:693`

The local transaction is never published to `_activeTransaction`, so `InsertEntity` →
`GetDataCommand()` leaves `cmd.Transaction` unset.

**Failing scenario** `BulkInsertEntities` on Oracle with defaults. Oracle is absent from
`SupportsMultiRowInsert()`, so it takes the batched path; `UseBulkTransactions` defaults `true`; the
first `ExecuteNonQuery` fails with *"requires the command to have a transaction…"*. Only SQLite
survives, because it does not enforce the association. `:202` separately overwrites a caller's outer
transaction.

**Status — fixed.** All six sites go through `BeginPublishedBulkTransaction` /
`EndPublishedBulkTransaction`, which **publish** the transaction as `_activeTransaction` so
`GetDataCommand` attaches it to the commands `InsertEntity`/`UpdateEntity` build. The helper returns
null when the caller already owns a transaction, so a bulk operation inside an outer
`BeginTransaction`/`Commit` scope joins it instead of nesting or hijacking it. Rollback now goes
through `RollbackPublishedBulkTransaction`, which logs rather than letting a failed rollback replace
the exception that caused it. The bogus `ConnectionProp.Database != null` guard is gone.

**Testing caveat:** SQLite cannot reproduce the primary failure, because it does not enforce the
command/transaction association — that is precisely why this survived. `BulkOperationTests.cs` pins
the observable half (no nesting, no hijacking, no transaction left dangling); the provider rejection
itself needs a real Oracle or SQL Server.

### K9 — Temp-table DDL emits .NET type names as SQL types — *fixed*

**Where** `BulkOperations.cs:950-951`, `:966-967`, `:982-983`

```csharp
var columns = DataStruct.Fields.Select(f => $"{GetFieldName(f.FieldName)} {f.Fieldtype}");
```

**Failing scenario** `BulkUpdateEntities` on SQL Server, MySQL or PostgreSQL with defaults produces
`CREATE TABLE #t (Name System.String, Id System.Int32)` and fails on its first statement.

**Status — fixed.** All three temp-table builders resolve the column type through a new
`ResolveDdlColumnType`, which reuses `GetFallbackDbType` and `NormalizeDbTypeForProvider` — the same
pair `GenerateCreateEntityScript` already used.

Two related items fixed alongside it: the temp-table **column-set mismatch** (the CREATE used every
field while the load filtered out auto-increment ones, so an identity key was never populated and the
MERGE's `ON` clause matched nothing — `BuildMultiRowInsertCommand` now takes `includeAutoIncrement`,
set true for temp-table loads), and **`MaxParametersPerBatch` was never applied to the update paths**
(1000 rows x 20 columns = 20,000 parameters, well past SQL Server's 2,100).

**Still open:** the temp-table *name* still uses SQL Server's `#` prefix on MySQL and PostgreSQL,
where it is not a legal identifier, and can exceed their 63/64-character limits.

### K10 — Schema and table concatenated with no separator — *fixed*

**Where** `BulkOperations.cs:746,885,907,927` and `Modernization.cs:351,371`

**Failing scenario** Any datasource with `SchemaName = "dbo"`. Bulk insert emits
`INSERT INTO dboCustomers`. Because two of the six sites are `GetQueryString` and `GetCountQuery`,
the async, streaming and paged **read** paths break too, not only bulk.

**Status — fixed.** All six sites go through `QualifyWithSchema`, which inserts the separator and
mirrors `GetTableName`'s "schema differs from the user id" rule so built and rewritten statements
qualify identically. **Still open:** the temp-table name uses SQL Server's `#` prefix on MySQL and
PostgreSQL (`:528`, `:964`, `:980`), where it is not a legal identifier, and exceeds their 63/64-character
limits.

### K11 — Two paging implementations that disagree — *fixed*

**Where** `Query.cs:797` versus `Modernization.cs:412-421`

The first delegates to `RDBMSHelper.GetPagingSyntax` (default `LIMIT/OFFSET`); the second handles
five engines and defaults to `OFFSET/FETCH`.

**Failing scenario** Oracle gets `OFFSET/FETCH` from one path and a `ROWNUM` subquery from the other.
`AzureSQL` and `SqlCompact` fall to `LIMIT/OFFSET` and produce a syntax error on every paged read.
About nineteen engines — MariaDB, Snowflake, CockroachDB, Vertica, BigQuery, Redshift, DuckDB,
Databricks, Presto, Trino, Hana, Spanner and others — receive SQL Server syntax on the async path.
Oracle's `OFFSET/FETCH` is 12c+ only.

**Status — fixed.** `Modernization.GetPagedQuery` now delegates to `RDBMSHelper.GetPagingSyntax`,
the same source the synchronous path uses, and its private switch and `BuildOraclePagingQuery` are
gone (which also removes the stray `rnum` column the ROWNUM wrapper leaked into `SELECT *`).
`AzureSQL`, `SqlCompact` and `MariaDB` were added to the helper's table in BeepDM.

**Two consequences worth knowing.** Oracle now settles on `OFFSET/FETCH` everywhere, so **11g loses
paging** — it was already broken on the synchronous path, so this makes it consistently broken
rather than half-broken; add an 11g arm to `GetPagingSyntax` if that matters.
And **Presto/Trino are still wrong**: they take the `LIMIT n OFFSET m` default, but Trino requires
`OFFSET` before `LIMIT`. Left alone rather than guessed at — see the note under K6.
Covered by `PagingDialectTests.cs`.

### K12 — A provider connection leaks on every reconnect — *fixed*

**Where** `RDBDataConnection.cs:100-116`

The early return fires only when the existing `DbConn` is `Open`; a `Closed` or `Broken` one falls
through to `:116` and is overwritten **without disposal**. `CloseConn()` calls `Close()`, never
`Dispose()`, and never nulls the field. `DbConn` is not disposed anywhere in the solution.

**Failing scenario** Repeated open→close→open cycles strand one provider connection object each
time — native handles for Oracle, ODBC and SQLite.

**Status — fixed.** `OpenConn` disposes the previous `DbConn` before replacing it. `CloseConn` still
only closes (it does not dispose or null the field), which is fine now that reopen disposes — but
`RDBSource.Dispose` still never disposes the connection at all; that is K22, Phase 8.

### K13 — The stored password is silently destroyed — *fixed*

**Where** `RDBDataConnection.cs:127-128`

```csharp
DbConn.ConnectionString = ReplaceValueFromConnectionString();
ConnectionProp.ConnectionString = DbConn.ConnectionString;
```

**Failing scenario** Providers strip the password from the `ConnectionString` getter unless
`Persist Security Info=true`. Open a connection, then let `ConfigEditor` persist it: the saved
connection has no password and every later connect fails.

**Status — fixed.** The read-back is simply gone: `DbConn.ConnectionString` is assigned from
`ReplaceValueFromConnectionString()` and never copied back into `ConnectionProp`. There was no
reason to round-trip it — the value is rebuilt from `ConnectionProp` on every open.

### K14 — Schema qualification applied to INSERT only — *fixed*

**Where** `DMLGeneration.cs:201` versus `:249` (commented out) and `:346`

**Failing scenario** Any datasource with a non-default `SchemaName`: INSERT targets `schema.Table`
while UPDATE and DELETE target `Table`.

**Status — fixed.** All three statements qualify through `QualifyWithSchema` at build time.
Covered by `SchemaQualificationTests.cs`.

### K15 — `GetTableName` rewrites SQL by string surgery — *fixed at the call sites*

**Where** `Utilities.cs:291-340`, live from `DMLGeneration.cs:201` and `Query.cs:270,592,597`

Four separate faults:

- `if (querystring.IndexOf("select") > 0)` never fires for a query that *starts* with `select`, and
  it is the one branch using a case-sensitive comparison while the others use
  `InvariantCultureIgnoreCase`.
- The DELETE branch searches token 1 and substitutes token 2 (`:336`), so `delete from Customers`
  becomes `delete  dbo.Customers  Customers`.
- `Query.cs:597` passes a bare *entity name* into this SQL rewriter. **A table named `updates`,
  `deleted_records` or `insert_log` enters a branch that indexes `tokens[1]` on a single-token
  string → `IndexOutOfRangeException`.**
- Callers pass `.ToLower()` of the whole statement, destroying identifier case on case-sensitive
  servers, and `String.Replace` rewrites every occurrence of the table name, including a column that
  shares it.

**Correction to an earlier reading of this entry:** the crash scenario is not
`IndexOutOfRangeException`. `GetTableName` is only ever handed a full statement, never a bare entity
name, so the real failure is silent corruption: `select * from updates` matches the *update* branch
(the table name contains the word), rewrites token 1 — the `*` — and yields
`select  main.*  from updates`.

**Status — fixed at the call sites.** INSERT, UPDATE, DELETE and the `GetEntity` SELECT now qualify
the entity name directly with `QualifyWithSchema` at build time, and the `.ToLower()` calls are gone.
A caller-supplied SELECT is left exactly as written. `GetTableName` itself still exists and is still
`public virtual` — it has no callers left in the live paths and should be removed in Phase 7 (K19).
Covered by `SchemaQualificationTests.cs`, verified to fail against the old path.

---

## Tier 3 — subsystems that exist but do nothing

### K16 — The resilience layer is inert — *classifier and ordering fixed; still not wired to query paths*

**Where** `Resilience.cs` (426 lines); `RDBDataConnection.cs:205-211`

Nothing outside the file uses it — no CRUD, query, schema or bulk path is wrapped. And even
`CheckConnectionHealth`'s use never fires, because `OpenConn()` catches every exception internally
and *returns* a `ConnectionState`, while no result-based Polly predicate is configured.

Configuration defects, should it ever be wired up: `IsTransientException` returns `true`
unconditionally for anything whose type name contains `sqlexception`/`dbexception` (so PK violations
and syntax errors are "transient"); the error-code table reads `Exception.HResult` rather than
`SqlException.Number` and can never match; the pipeline order is inverted (breaker outer, retry
inner) so `MinimumThroughput = 5` is effectively unreachable; and the default health-check query
`SELECT 1` is invalid on Firebird, Hana and Teradata, all of which ship here.


**Status — the dangerous parts are fixed; it is still not wired to the query paths.**

- `IsTransientException` no longer returns `true` for every provider exception. It used to end in an
  unconditional `return true` for anything whose type name contained `sqlexception`/`dbexception`,
  so PK violations, syntax errors, "invalid object name" and permission-denied were all retried
  three times with 1/2/4-second delays. Unrecognised failures are now treated as permanent. The dead
  error-code table is gone — it compared `Exception.HResult` against SQL Server *error numbers*,
  which can never match.
- The inner-exception walk is now an iterative loop with a depth cap, replacing unbounded recursion
  with no cycle guard.
- Pipeline order corrected to retry-outer / breaker-inner, so each attempt is visible to the breaker.
  Previously one `Execute` performed up to four attempts but registered as a single outcome, and with
  `MinimumThroughput = 5` the breaker essentially never opened.
- Health-check queries added for Firebird and Hana; both fell through to a bare `SELECT 1`, a syntax
  error on each, so a healthy connection was reported unhealthy.

**Still open, deliberately:** nothing outside this file executes through `ResilientPipeline`, and
`OpenConn` still swallows exceptions and returns a `ConnectionState`, so Polly sees no failure to
retry. Wiring it up needs a result-based predicate — and should not be extended to write paths
without care, since a client-side timeout on an INSERT often means the statement committed anyway.
Covered by `CacheAndResilienceTests.cs`.

### K17 — Most of `Cache.cs` is dead, and it logs that it did work — *fixed*

**Where** `Cache.cs`

`TryGetCachedResult`, `CacheResult`, `TryGetPreparedStatement` and `CachePreparedStatement` have no
callers. `GenerateQueryCacheKey`/`TryGetCachedQuery`/`CacheQuery` are called only from `BuildQuery`,
which itself has none — so `_queryCache` is permanently empty too.

**Failing scenario** `InvalidateEntityCache` runs on every INSERT, UPDATE and DELETE and logs
*"Cache invalidated for entity: X"* (`:239`) having invalidated nothing.


**Status — fixed.** The query-string, result and prepared-statement caches are gone; once the dead
`BuildQuery` cluster was removed (K19) the query cache had no callers left either.
`InvalidateEntityCache` now drops the entry in `EntityStructureCache`, and `ClearAllCaches` empties
it. The misleading log line is gone. `EnableResultCache` and `ResultCacheTTL` remain as `[Obsolete]`
no-ops so existing callers still compile.

Result caching was **not** reinstated: `GetEntity` is an iterator streaming rows off an open reader,
so caching its return value would cache an un-enumerated sequence, and forcing it to a list to make
caching possible would discard the streaming the read path is built on.

### K18 — The cache that matters has no invalidation — *partly fixed*

**Where** `Helpers/EntityStructureCache.cs`

No TTL, no size limit, no `Remove`, no `Clear`, and `InvalidateEntityCache` does not know it exists.

**Failing scenario** `ALTER TABLE t ADD COLUMN c` through `ExecuteSql`, then `GetEntityStructure("t")`
— the pre-DDL field list is returned for the lifetime of the datasource. Separately, `GetOrAdd` does
not hold a lock across the loader, so N threads asking for the same uncached entity issue N
concurrent `ExecuteReader` calls on the single shared connection; and a failed load is cached
permanently.


**Status — the cache itself is fixed; the staleness is not fully gone.** `EntityStructureCache` now
has `Remove` and `Clear`, serialises loads per key (so racing threads no longer open concurrent
readers on the shared connection), caps itself at 2048 entries — callers pass whole SQL statements
as keys, so it grew without limit — and no longer caches a null result.

**But cache eviction alone does not force a database re-read, and this was initially claimed as a fix
in error.** `LoadEntityStructure` resolves the entity out of the `Entities` list and returns that same
instance, and `GetEntityStructure(fnd, refresh: false)` returns it unchanged. So for an entity already
present in `Entities` — the common case — evicting the cache re-runs the lookup and queries
nothing. Invalidation only forces a real read for entities *not* in `Entities`, where the loader sets
`refresh` itself.

Reading a changed schema therefore still requires `refresh: true`. Fixing that properly means either
refreshing the `Entities` entry on invalidation or giving `GetEntityStructure(fnd, false)` a dirty
flag to consult — a behavioural change to how `Entities` is maintained, and left open.
`CacheAndResilienceTests.StructureCacheEviction_DoesNotByItselfForceAReRead` pins the limitation so
it is not mistaken for working.

### K19 — Roughly 40% of `Query.cs` is unreachable — *fixed*

**Where** `Query.cs` — `BuildQuery`, `BuildQueryInternal`, `ParseQueryComponents`,
`FindNextClausePosition`, `GetSchemaPrefix`, `BuildWhereClause`, `IsValidFilter`,
`FormatFilterCondition`, `SanitizeParameterName`, `AppendClauseIfExists`.
Also `Helpers/PagedQueryExecutor.cs` (118 lines) and `Helpers/PaginationHelper.cs`, both entirely
dead, and `RDBSource.Pagination.cs`, which contains no code at all — 36 lines of comment describing a
consolidation that never happened, inaccurately.


**Status — fixed.** 359 lines removed from `Query.cs`: the whole cluster hung off `BuildQuery`,
which had no callers anywhere, and it was also the last caller of `GetTableName` and of the
query-string cache. `PagedQueryExecutor.cs` and `PaginationHelper.cs` are deleted. `Pagination.cs`
now carries an accurate signpost saying paging has a single dialect source.

`GetTableName` is left in place but has no live callers — it is `public virtual`, so removing it
would break any driver that overrides or calls it. It should be marked `[Obsolete]` and removed on
the next deliberate breaking release.

### K20 — `GetEntityAsync` does not offload — *fixed*

**Where** `Query.cs`

`Task.Run(() => GetEntity(...))` on an iterator returned the un-enumerated sequence immediately, so
all database work ran on the consuming thread. The XML comment claiming otherwise was false.

**Status — fixed.** The sequence is now enumerated *inside* the task, which is what makes the
offload real and what the signature already promised: a caller awaiting a
`Task<IEnumerable<object>>` reasonably expects the awaited result to be data, not a reader still
attached to the shared connection that will do its I/O later — or never, if the caller abandons
enumeration.

The cost is buffering: the whole result set is materialised. That is a deliberate trade, and the
remark now says so and points streaming callers at `GetEntityStreamAsync<T>`, a genuine
`IAsyncEnumerable` over `ExecuteReaderAsync`. Covered by `AsyncContractTests.cs`, including a test
that closes the connection after the await and still reads every row.

---

## Tier 4 — correctness, resources, lifecycle

| ID | Issue | Where |
|---|---|---|
| K21 | **Partly fixed.** `GetDataCommand()` returns `null` and was unchecked at 11 sites; `using (null)` is legal so it surfaced as "Object reference not set". Now guarded in `GetDataReader`, `GetScalar`, `GetScalarAsync`, `RunQuery`, the paged `GetEntity` and `GetFloatPrecision`, and `GetDataCommand` records the reason on `ErrorObject`. **Still unguarded in the `CRUD.cs` and `BulkOperations.cs` call sites**, where `BulkOperations.cs:249` also misreports it as *"does not support async operations"*. | `CRUD.cs`, `BulkOperations.cs` |
| K22 | **Fixed.** `Dispose.cs` was rewritten. `Entities`/`EntitiesNames` are now `Clear()`ed rather than nulled, so post-dispose access yields an empty list instead of an NRE. A pending `_activeTransaction` is rolled back *before* the connection closes, since closing underneath it leaves the connection unusable on pooled providers. The retained `command` and `_entityCache` are released, and `DbConn` is now `Dispose()`d and not merely `Close()`d -- `Close()` returns a pooled connection but leaves the native handles Oracle, ODBC and SQLite hold. Every step runs through a `SafelyDispose(Action, string)` wrapper, so one failing step neither escapes `Dispose()` nor skips the rest. `IAsyncDisposable` is implemented, using `DbConnection.DisposeAsync` where the provider supports it. | `Dispose.cs` |
| K23 | **Partly fixed -- deliberately.** The empty catch is gone; a failure during disposal now reaches `Logger`. The `SaveStructure()` call itself was **kept**: SQLite and DuckDB rely on it to persist their structure at teardown, and removing it silently loses data rather than fixing a defect. Doing database round-trips and `CREATE TABLE` inside `Dispose` is still wrong and should move to an explicit call the host drives; that is a contract change for both in-memory drivers and is left open. | `InMemoryRDBSource.cs` |
| K24 | **Fixed.** Every catch that discarded its exception now records it. The per-property conversion catches -- the ones that silently produced a default value for every row of a mis-mapped column -- report **once per column** through a method-local `HashSet`, in both `Query.cs` read paths and both `Modernization.cs` read paths, naming the column, the target type and the fact that the property is defaulted for the whole read. The structure-lookup and entity-type-resolution catches in `Query.cs` kept their fallbacks but now say they took them. `DataStreamer.cs:104` went away with the dead code it sat in (see K43). Two are intentionally silent and say so: the static SQL table-name scan (no logger is reachable from a static method, and `null` *is* its contract) and the quoted-identifier fallback in `GetDeleteAllSql`. | `Query.cs`, `Modernization.cs`, `Schema.cs`, `InMemoryRDBSource.cs`, `Helpers/DataStreamer.cs` |
| K25 | **Fixed.** `(List<RelationShipKeys>)GetEntityforeignkeys(...)` became `GetEntityforeignkeys(...)?.ToList() ?? new List<RelationShipKeys>()`, so an override returning a LINQ projection, an array or `null` works instead of throwing `InvalidCastException` at runtime. | `Schema.cs` |
| K26 | **Fixed.** Two lines: the `.ToUpper()` on catalog names is gone, and the existence check now uses `StringComparer.OrdinalIgnoreCase`. The surrounding loop is unchanged. **Still open nearby:** `FindIndex` can return `-1` and `Entities[-1]` would throw (it cannot today — `item` came from `Entities` moments earlier — but the code relies on that invariant), and a null `TABLE_NAME` now reaches `EntitiesNames` instead of throwing on `.ToUpper()`. | `Schema.cs` |
| K27 | **Mostly fixed.** `GetEntityType` now uses `"Beep." + DatasourceName`, matching every other connector and the rule in `CLAUDE.md`, which also removes the `"TheTechIdea.Classes"` fallback that would have collided across datasources when `DatasourceName` is empty. Note this changes generated type FullNames for all 14 RDBMS drivers from `{DatasourceName}.{Entity}` to `Beep.{DatasourceName}.{Entity}`; per commit `ea222c4b` nothing references those literals. **Still open:** it writes the shared `enttype` field on every read, racing `SetObjects`. | `Schema.cs` |
| K28 | Per-operation state lives in shared fields. `RDBSource.cs:32`'s "thread-safe per-operation" comment is wrong — reassigning a shared field at operation start is what makes it unsafe. `recNumber` is `protected static`, shared process-wide, keyed by an instance field. `CRUD.cs:363` disposes the shared `command` without clearing `ObjectsCreated`. | `RDBSource.cs:32-34,134-147` |
| K29 | `GetDataAdapter` swallows the command-builder failure (log commented out — the build's `CS0168`) then sets `Errors.Ok`, returning an adapter with null Insert/Update/DeleteCommand. Date filters get `DbType.DateTime` with a culture-formatted `ToShortDateString()` value that drops the time; `between` forces `DbType.DateTime` regardless of field type; `item.Operator.ToLower()` NREs on a null operator; the `DbCommandBuilder` is never disposed. | `Utilities.cs:190-253` |
| K30 | **Partly fixed.** All parsing in `TypeMapping.cs` is now culture-invariant (`CultureInfo.InvariantCulture`, `NumberStyles.Any`, `DateTimeStyles.None`), so a decimal or a date no longer changes meaning with the thread's locale -- the failure that only shows up on a machine configured differently from the developer's. `GetDbType` and `ConvertToDbTypeValue` are now `protected virtual`, so a driver can finally specialise type mapping; previously every live map was `private`. **Still open:** the `String` default for SQL type names, and the four separate maps. | `TypeMapping.cs`, `Helpers/DbTypeMapper.cs` |
| K31 | **Mostly fixed.** Empty `PrimaryKeys` now throws a named `InvalidOperationException` from `GetUpdateString`/`GetDeleteString` instead of emitting `DELETE FROM T WHERE ` or a statement mangled by `Remove(Length - 1)`; the same guard covers the all-columns-are-keys case. The DELETE binder now coalesces to `DBNull.Value` like its two siblings, so a null key value no longer NREs. **Still open:** auto-increment columns are excluded from INSERT but not from the UPDATE SET clause. | `DMLGeneration.cs` |
| K32 | **Fixed for parameter names.** `AllocateParameterName` now takes its budget from `RDBMSHelper.GetMaxIdentifierLength(DatasourceType)` less the two characters of the `p_` prefix every call site adds, instead of assuming 30. Two consequences. First, SQL Server (128), MySQL (64), PostgreSQL (63) and the rest stop losing the tail of any name over 28 characters, and each truncation was another chance for two distinct fields to collide onto one parameter. Second — caught by the revert check rather than by reading — the old clamp did not account for the `p_` prefix at all, so on **Oracle** a long field produced a 32-character parameter identifier against a 30-character limit: over the limit on the one engine whose limit the constant was copied from. The uniquifying suffix now trims the stem instead of pushing the name back over. Covered by `DialectDelegationTests.cs`. **Still open:** the separate truncation applied to *identifiers* after delimiting, which can cut a closing quote, and which is dead for the emitted SQL anyway since the call sites re-call `GetFieldName` and ignore the truncated local. | `DMLGeneration.cs` |
| K33 | Count-query failure is swallowed as a `Warning`, leaving `totalRecords = 0` so `HasNextPage` is `false` while `Data` has rows — a caller looping on `HasNextPage` stops after page one. The count is narrowed with an unchecked `(int)` cast. | `Query.cs:815,817` |
| K34 | **Mostly fixed.** The four `Task.Run(...).Wait()` blocks — three in `GetDDLScriptfromDatabase`, one in `RunScript` — are gone; each wrapped a *synchronous* method, so the caller blocked anyway, one pool thread was consumed per call, a limited scheduler could deadlock on it, and `.Wait()` rewrapped the real exception in an `AggregateException` (`RunScript`'s catch logged "One or more errors occurred." instead of the failure). They are direct calls now. `OpenConnectionResilientAsync` no longer does `Task.Run(() => Openconnection(), ct)`, which parked a pool thread on a blocking open the token could not interrupt; it awaits the new `OpenconnectionAsync`, which reaches `DbConnection.OpenAsync(ct)` when the connection object exists and is merely closed — the case `CloseConn` leaves behind, and the one the pipeline actually retries. And **`ConfigureAwait(false)` is now on all 50 `await` expressions** in the class, which CLAUDE.md requires and which matters because this library is called from WinForms and WPF hosts. **Still open, and legitimately:** the two `Task.Run` fallbacks for providers whose command is not a `DbCommand` and so has no async execute at all (`Resilience.cs`, `Query.cs` `GetScalarAsync`) — there is no awaitable operation to reach for there. | various |
| K35 | Cancellation is checked only at batch boundaries and never passed to per-row work; the `finally` cleanup at `BulkOperations.cs:612` receives the already-cancelled token, so **the temp table leaks on every cancellation**. The sync bulk methods have no cancellation at all. | `BulkOperations.cs` |
| K36 | The Dapper path bypasses `ActiveTransaction`, returns `null` rather than an empty list, and returns a **null `Task`** from `SaveData<T>` so `await` throws. Its guard checks a cached status field rather than `DbConn.State`. | `Dapper.cs` |
| K37 | **Mostly fixed.** `Closeconnection()` called `CloseConn()` three times on the same object (now once), neither connection method set `ErrorObject` (both now do) and neither had a try/catch (both now do, so an `InvalidCastException` from the `RDBMSConnection` cast no longer escapes `Dispose`). The dead assignments to `ConnectionStatus` — whose setter is `set { }` — were removed. **Still open:** neither method rolls back an open transaction, and the no-op setter remains. | `Connection.cs`, `RDBSource.cs:74` |
| K38 | **Fixed — all five sites.** Three were in `InMemoryRDBSource`; the base class had two more, `CreateEntityAs` (`Schema.cs`) and `UpdateEntities` (`CRUD.cs`), both found in the Phase 11 sweep. Each now reads the result into a local instead of assigning it to `DMEEditor.ErrorObject`. `ExecuteSql` returns *this datasource's* `ErrorObject`, so the assignment permanently aliased the engine-wide error object to one datasource's field -- after which every unrelated component writing `DMEEditor.ErrorObject` was writing into this datasource, and every failure this datasource recorded surfaced as an engine-wide failure. | `InMemoryRDBSource.cs` |
| K39 | **Fixed, all five.** `LoadStructure` now honours `copydata` -- it was accepted and never referenced, so `LoadStructureWithData`, whose only job is to pass `copydata: true`, loaded the structure, loaded no data, reported success and raised `DataChanged`. `FillFromDataSource` now builds its copy list from `source`: `GetCopyDataEntityScript` resolves each row's origin from that entity's own `DataSourceID`, so passing `InMemoryStructures` generated scripts copying every entity onto itself while `source` was merely validated, opened and named in the success message; the structures are cloned before their `DataSourceID` is stamped, so the source datasource's own cached structures are not rewritten. `CreateStructure` records what `CreateEntityAs` actually returned instead of setting `IsCreated = true` unconditionally, and `IsStructureCreated` is now true only if every entity was created. `LoadEntities` populates **this** instance (and still updates the registry's object when it is a different one) -- it previously assigned only to whatever `GetDataSource(name)` handed back, so when that was not this same object, `LoadStructure`'s `Entities.Any()` check immediately afterwards saw an empty list and reported the structure as not loaded. `SaveStructure` sets `IsSaved`. `ExportToDataSource` was checked and is correct as written. | `InMemoryRDBSource.cs` |
| K40 | **Fixed.** The raise in `UpdateEntities` is uncommented, so the `PassedArgs` built on every row is no longer discarded and `PassEvent` actually fires. (`DMEEditor.RaiseEvent` next to it is deliberately left commented -- that broadcasts engine-wide and its effect on other subscribers is out of scope here.) The shadowing re-declaration in `InMemoryRDBSource` is deleted, so subscribers are no longer split between a base event that fires and a derived event that never did. `StructureChanged`, also declared and never raised, is now raised when the structure is created, loaded or saved. Every `CS0067`, `CS0108`, `CS0168` and `CS0219` warning in this project is gone. | `RDBSource.cs`, `CRUD.cs`, `InMemoryRDBSource.cs` |
| K41 | Foreign-key constraint names use `Random.Shared.Next(10, 1000)` — 990 possible suffixes, non-idempotent migrations — and FK scripts are generated twice (`:599` already calls it via `:569`, then `:602` calls it again). | `DMLGeneration.cs:685,599,602` |
| K42 | **Fallback fixed; the TRUNCATE itself is not.** The fallback was a three-arm switch that knew SQL Server and MySQL only — so MariaDB got ANSI double quotes instead of backticks, AzureSQL and SQL Server Compact got double quotes instead of brackets — and it interpolated `tableName` straight into the delimiters with no escaping. It now calls the base `QuoteIdentifier`, which covers every dialect the drivers use and escapes the closing delimiter. **Still open:** the primary path still resolves to **TRUNCATE**, which is DDL on Oracle and MySQL and implicitly commits any open transaction — changing that is a behavioural decision about what "delete all" should mean here, not a defect fix. | `InMemoryRDBSource.cs` |
| K43 | **Fixed by deletion.** `_propertyCache` belonged to `DataStreamer.Stream(IDataReader, Type)`, which had no callers anywhere -- both live read paths use the dictionary overload and do their own per-column conversion. Removing the overload took the unbounded static cache, the empty catch in `SetPropertyValue`, and a third defect nobody had noticed: it converted with `Convert.ChangeType(value, prop.PropertyType)` **without unwrapping `Nullable<>`**, and `ChangeType` cannot target `Nullable<T>` -- so every nullable property took the throw and silently kept its default, which is exactly what the empty catch was hiding. A comment in the file records what to do differently if typed streaming is ever reinstated. | `Helpers/DataStreamer.cs` |
| K45 | **Fixed.** `GetDataReader` leaked one `IDbCommand` per call. It is an `IRDBSource` contract method that returns a reader and nothing else, while the command behind it comes from `GetDataCommand()`, which creates a fresh `IDbConnection.CreateCommand()` every time -- so the caller had no handle on it and nothing disposed it for the life of the connection. Disposing it before returning is not an option either: on SQLite and others that finalises the statement handle the open reader is reading through. Ownership now travels with the reader, through `Helpers/CommandOwningDataReader.cs`. Covered by `ReaderOwnershipTests.cs`, which asserts the command is disposed when the reader is *and* still live while the reader is open. | `Query.cs`, `Helpers/CommandOwningDataReader.cs` |
| K46 | **Fixed.** `SupportsMultiRowInsert()` was a four-arm switch naming SQL Server, MySQL, PostgreSQL and SQLite, so MariaDB, AzureSQL, CockroachDB, DuckDB, DB2, Snowflake, Spanner and Presto/Trino all reported `false` and fell back to one INSERT statement per row — correct, but far slower than the grammar they accept. It now delegates to `RDBMSHelper.SupportsFeature(DatasourceType, DatabaseFeature.MultiRowInsert)`; the `MultiRowInsert` feature was added to BeepDM's shared dialect table, listing only engines whose grammar is certain, because a false negative costs speed while a false positive is a syntax error on every bulk insert. Oracle, Firebird, Hana, SQL Server Compact and VistaDB stay `false` deliberately — they need a different statement shape (`INSERT ALL`, `INSERT INTO ... SELECT ... UNION ALL`), not a smaller batch. `SupportsTempTables()` was **not** delegated: it asks what *this class* can emit, and it has exactly three temp-table builders, so answering from an engine-capability table would route Oracle into the `NotSupportedException` those builders throw. | `BulkOperations.cs`, `../BeepDM/.../DatabaseFeatureHelper.cs` |
| K47 | **Fixed.** Both `GenerateCreatEntityScript` overloads were `public` but not `virtual`, and `GenerateCreateEntityScript(EntityStructure)` — which builds the statement — was `private`, so no driver could specialise DDL generation. They are now `public virtual` and `protected virtual` respectively. This is the gap that drove DuckDB to abandon the base class and override about twenty members. | `DMLGeneration.cs` |
| K48 | **Fixed.** Two failure paths dereferenced `DMEEditor` with no null guard, so with no editor attached they threw `NullReferenceException` out of the very code meant to report the failure — replacing the real error with an unrelated one. `ExecuteSql`'s catch wrote `DMEEditor.ErrorObject.Flag` and `.Message` directly and now goes through `SetFailure`, which guards both error objects and logs; `GetEntity`'s stream-preparation catch now calls `DMEEditor?.AddLogMessage`. Both were found by writing the Phase 10 tests, not by reading. | `Query.cs` |
| K49 | **Fixed.** `IDMEEditor` was used as a service locator throughout, but the overwhelming majority of those uses were not service lookups at all: 222 of roughly 300 references were `ErrorObject` and `AddLogMessage`, which is error reporting. All 73 remaining unguarded `AddLogMessage` calls are guarded, the longhand `DMEEditor.ErrorObject` failure writes go through `SetFailure`/`SetSuccess`, and the genuine service dependencies that remain — `ConfigEditor` (the query catalogue), `Utilfunction` (driver resolution and DataTable conversion), `assemblyHandler` (loading provider types by name), `ETL` (progress bookkeeping) and `typesHelper` (type-name mapping) — are guarded at the point of use and **name themselves** when absent, instead of surfacing as "Object reference not set" from somewhere unrelated. `UpdateEntities` in particular dereferenced `DMEEditor.ETL` four times for progress counters *before* touching the database, so it threw on a service it does not need to do its work; the ETL counters are now optional. No driver needed editing, which is what the plan flagged as this phase's risk. | `Schema.cs`, `CRUD.cs`, `DMLGeneration.cs`, `Utilities.cs`, `Query.cs` |
| K44 | The Oracle FLOAT mapping branch is unreachable (`Fieldtype` holds `"System.Double"`, never `"FLOAT"`) and contains a latent NRE — `x.EntityName` is never assigned, so `GetFloatPrecision` would call `null.ToUpper()`. `GetFloatPrecision` itself leaks its command and reader and reports through `Console.WriteLine`. | `Schema.cs:169-175,883-909` |

---

<a id="coverage"></a>
## Test coverage

`tests/RDBDataSource.Tests/` reports **176 passing tests**, and — unlike the suite this work
started from — they exercise the class under test.

The original `RDBSourceIntegrationTests.cs` had 25 tests that hand-wrote SQL against in-memory
SQLite and never instantiated `RDBSource`; their own comments said "Simulates
RDBSource.InsertEntity pattern". They asserted that SQLite behaves like SQLite, so every defect in
this document was invisible to them. That file has been replaced.

| Suite | Tests | Covers |
|---|---:|---|
| `RDBSourceIntegrationTests.cs` | 17 | CRUD, filtered and paged reads, `GetEntityStructure`, `GetTableSchema`, transactions, connection health — all through the real class |
| `HelperTests.cs` | 20 | `DbTypeMapper` (the only part of the original suite that tested production code) |
| `IdentifierQuotingTests.cs` | 16 | K3, per dialect, including the case-folding behaviour deliberately *not* changed |
| `FilterInjectionTests.cs` | 14 | K4, asserting on generated SQL rather than row counts |
| `PagingDialectTests.cs` | 15 | K11, the consolidated dialect table |
| `WriteContractTests.cs` | 10 | K2 zero-rows including the MySQL carve-out, K31 keyless guards |
| `ErrorModelTests.cs` | 9 | F1, with neither a logger nor an `IDMEEditor` |
| `BulkOperationTests.cs` | 7 | K5, K8, K9, batch sizing |
| `ParameterBindingTests.cs` | 6 | K1, at the generated-SQL level |
| `SchemaQualificationTests.cs` | 5 | K10, K14, K15 |
| `CacheAndResilienceTests.cs` | 14 | K16, K17, K18 -- including the eviction limitation K18 pins deliberately |
| `ReaderOwnershipTests.cs` | 3 | K45, the command `GetDataReader` used to leak |
| `DialectDelegationTests.cs` | 25 | K46 multi-row INSERT per engine, K32 parameter-name budget per engine |
| `AsyncContractTests.cs` | 7 | K20, `RunScript` without a blocking wait, the async connection open |
| `ServiceIndependenceTests.cs` | 8 | K49 — `CreateEntityAs`, `UpdateEntities` and the schema readers with no engine attached |

Each defect fix was verified by reverting it and confirming the relevant tests go red — see the
individual entries.

**What this suite still cannot reach.** `SqliteHarness` supplies no `IDMEEditor`, and subclasses
`RDBSource` to override the `virtual` `GetEntityType` so `DMTypeBuilder` is not needed.

Two of the three gaps recorded here are closed. **`CreateEntityAs` and DDL generation now run with
no engine attached** — the `typesHelper` lookup was already optional (it falls back to
`GetFallbackDbType`); what actually blocked the path was the unguarded `DMEEditor.ErrorObject`
writes on its failure branches, fixed under K49.
`ServiceIndependenceTests.CreateEntityAs_CreatesTheTable_WithNoEngineAttached` creates a real table
through it. And **`GetEntitesList` no longer looks like it worked**: with no `ConfigEditor` and no
driver-supplied `GetListofEntitiesSql` it names the missing service and flags the failure, rather
than swallowing a `NullReferenceException` and returning a stale list indistinguishable from a fresh
one. Assertions against it are safe now, as long as they assert the failure.

What remains out of reach: generated entity types and the read path's materialisation into them.

**And what SQLite cannot reproduce**, regardless of harness: it does not enforce the
command/transaction association (K8), it tolerates `'literal' = @p` predicates other engines reject
(K3), and it is unaffected by the `?` parameter prefix (K6). For those, assert on the generated SQL
— or run against a real engine.

`InternalsVisibleTo("RDBDataSource.Tests")` is already granted in `RDBDataSource.csproj`.
