# Known issues

The defect register for `RDBSource`. This file is the single source of truth; the Help pages under
`Help/providers/` link here rather than keeping their own list.

Every entry was verified against the source. Line references are to
`DataSourcesPlugins/RDBMSDataSource/PartialClasses/RDBSource/` unless stated otherwise. F1 and K1
through K49 are defects inside `RDBSource`/`InMemoryRDBSource`; Tier 5 (D1-D7) is a separate sweep
of the 14 concrete driver classes for defects specific to them, done after the base-class rework was
otherwise complete.

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
## Tier 5 -- driver-level defects (not in `RDBSource` itself)

Everything above is inside `RDBSource`/`InMemoryRDBSource`. This tier is different: these are
defects in the 14 concrete driver classes under `DataSourcesPluginsCore/` and `InMemoryDB/` --
found while checking, driver by driver, whether each one needs special handling relative to the
reworked base. `D` numbering, to keep it visibly separate from the base-class `K` register.

### D1 -- 10 of 14 drivers were not building against this `RDBSource` at all -- *fixed*

**Where** `CockroachDBDataSourceCore.csproj`, `FirebirdDataSourceCore.csproj`,
`FireboltDataSourceCore.csproj`, `HanaDataSourceCore.csproj`, `MySqlDataSourceCore.csproj`,
`OracleDataSourceCore.csproj`, `PostgreDataSourceCore.csproj`, `PrestoDatasourceCore.csproj`,
`SnowFlakeDataSourceCore.csproj`, `SpannerDataSourceCore.csproj`, `SqlCompactDatasourceCore.csproj`

**The defect.** CLAUDE.md's own architecture table says `DataSourcePluginSolution.sln` projects use
`ProjectReference` to the local sources because "the local BeepDM sources carry unpublished fixes,
and a package copy alongside the project copy loads the same types twice." The same reasoning
applies, unstated, to `RDBDataSource` itself -- and it was being violated by 10 of the 14 drivers
that inherit it. Nine referenced `TheTechIdea.Beep.RDBDataSource` version `2.0.22` as a
**published NuGet package** (`SqlCompact` referenced a *different* package, `RDBDataSource`
`1.0.42`); Firebolt referenced no `RDBDataSource` package at all, despite `FireBoltDataSource : RDBSource`.
2.0.22 was also, not coincidentally, the local project's own `<Version>` -- the number had never
been bumped across all eleven phases of this rework, so the pinned version looked current without
being current.

**The consequence.** None of F1 or K1 through K49 above ever reached CockroachDB, Firebird, Hana,
MySQL, Oracle, PostgreSQL, Presto, Snowflake, Spanner or SQL Server Compact -- at compile time or
at run time. Every rebuild-and-test cycle across all eleven phases that reported "0 errors" for
these projects was compiling against a stale package predating this work, not against the source
under review; a green build proved nothing about them. Only SQLite, SQL Server and DuckDB, which
already used `ProjectReference`, ever actually saw the fixes.

**Status -- fixed.** All 10 converted to `ProjectReference Include="..\..\DataSourcesPlugins\RDBMSDataSource\RDBDataSource.csproj"`,
matching SQLite/SQL Server/DuckDB exactly; Firebolt's missing reference was added the same way.
Fixing the reference surfaced two further problems, both fixed alongside it:

- **Package downgrade errors.** Firebird, Hana, MySQL, Oracle, Presto, Snowflake and Spanner each
  pinned `Microsoft.Extensions.Caching.Memory` at `10.0.9`; the local `RDBDataSource.csproj` needs
  `>= 10.0.10`. NuGet's downgrade detection (`NU1605`) is an error in this SDK, not a warning.
  Bumped all seven to `10.0.10`.
- **A namespace collision that blocked Oracle specifically (CS0234).** `OracleDataSource.cs`
  sits in `namespace TheTechIdea.Beep.DataBase`; `OracleDataSourceServiceExtensions.cs`, in the same
  project, declares `namespace TheTechIdea.Beep.DataBase.Oracle`. Once Oracle was pointed at a
  `RDBSource` that actually has `ConfigureCommand` as `protected virtual` (the earlier
  `CS0115: no suitable method found to override` failure, previously written off as an unrelated
  pre-existing issue, was this same root cause under a different symptom), `ConfigureCommand`'s
  unqualified `Oracle.ManagedDataAccess.Client.OracleCommand` resolved to the sibling namespace
  instead of the NuGet package's top-level one. Fixed with `global::Oracle.ManagedDataAccess...`
  rather than renaming either namespace.

All 13 buildable RDBMS driver projects (Firebolt still cannot restore -- see below) now compile
against the local, current `RDBSource`, most of them for the first time in this project's history.

**Still open:** Firebolt (`FireboltDataSourceCore.csproj`) references `Firebolt.Ado` `1.1.2`, which
this environment's configured NuGet sources cannot find (`NU1101`) -- unrelated to `RDBDataSource`,
and not something fixable without network access to wherever that package is actually published.
The missing `RDBDataSource` reference is added regardless, so the project will pick up the base
class correctly once that package becomes resolvable.

### D2 -- Hana, Presto, Snowflake and SQLite's transactions were fake through `IDataSource` -- *fixed*

**Where** `HanaDataSource.cs`, `PrestoDataSource.cs`, `SnowFlakeDataSource.cs`, `SQLiteDataSource.Transactions.cs`

**The defect.** All four declared `BeginTransaction(PassedArgs)`, `Commit(PassedArgs)` and
`EndTransaction(PassedArgs)` as `public virtual` methods -- not `override` -- with the exact same
signatures as `RDBSource.BeginTransaction`/`Commit`/`EndTransaction`. Because each of these classes
also re-lists `IDataSource` in its own type declaration (`class HanaDataSource : RDBSource,
IDataSource`, etc.), C# does not treat the hiding methods as dead code sitting beside the real ones:
**it rebinds the `IDataSource` interface slot to the most-derived member matching the signature --
the hiding method, not the inherited `override`.** Verified empirically (a standalone repro, then
`Type.GetInterfaceMap` against the actual compiled DLLs, before and after the fix) --
`((IDataSource)new HanaDataSource(...)).BeginTransaction(...)` called `HanaDataSource`'s own method,
not `RDBSource`'s, which is exactly how the engine consumes every plugin.

Hana's, Presto's and Snowflake's versions were pure no-ops: an empty `try` block that always set
`Errors.Ok`, with a comment reading "*Transactions are handled by RDBSource base class*" -- true in
intent, false in effect. `BeginTransaction` never opened a real `IDbTransaction`; `Commit` had
nothing to commit; `EndTransaction` (rollback) had nothing to roll back. Every write made "inside"
a transaction on these three engines committed immediately and individually, and a rollback after a
partial failure silently did nothing while reporting success.

SQLite's version was worse in a different way: not a no-op, but a **second, disconnected**
transaction mechanism -- a private `_transactionStarted` flag and literal `"BEGIN TRANSACTION;"` /
`"COMMIT;"` / `"ROLLBACK;"` SQL text, entirely separate from `RDBSource._activeTransaction` (the
ADO.NET `IDbTransaction` that `GetDataCommand()` attaches to every command, per K8). Two
consequences: the interface-hijack above applied here too, and `RDBSource.ActiveTransaction` stayed
`null` throughout, so `BulkOperations`'s check for an already-open transaction (`if
(ActiveTransaction != null) return null;`, K8's guard against nesting) could not see that a
transaction was already open at the SQL level -- risking a second, real ADO.NET transaction
starting on top of the raw-SQL one.

**Status -- fixed.** All four hiding declarations removed (the `SQLiteDataSource.Transactions.cs`
file deleted outright; `_transactionStarted` was read and written nowhere else). `RDBSource`'s real,
tested `BeginTransaction`/`Commit`/`EndTransaction` now occupy the `IDataSource` slot for all four
drivers, confirmed the same way the bug was found: `Type.GetInterfaceMap` against the rebuilt DLLs
now resolves all three methods to `RDBSource`.

A concrete side effect for SQLite specifically: `SQLiteDataSource.InMemory.cs`'s
`RefreshData(IProgress, CancellationToken)` calls `BeginTransaction(null)` / `Commit(null)` /
`EndTransaction(null)` on `this` to wrap a clear-and-reload of every entity. Those calls resolve at
compile time to whatever this class declares, hiding or not -- so this method was *already* using
the disconnected raw-SQL mechanism from within SQLite's own code, not just for outside callers. It
now uses the real one.

### D3 -- SQLite's and DuckDB's `Dispose()` never ran the K22 rework at all -- *fixed*

**Where** `SQLiteDataSource.cs`, `DuckDBDataSource.cs`

**The defect.** Both classes declared a complete, independent Dispose pattern -- `public void
Dispose()` hiding `RDBSource.Dispose()`, and `protected virtual void Dispose(bool disposing)` hiding
`InMemoryRDBSource.Dispose(bool)` -- with no `override` anywhere and, critically, **no call to
`base.Dispose(disposing)`**. `using var ds = new SQLiteDataSource(...)` (or DuckDB) calls `Dispose()`
on the compile-time-declared type directly, which is the hiding method regardless of any interface
subtlety -- so every ordinary disposal ran only the local override-that-wasn't and never reached
`RDBSource.Dispose(bool)` at all.

Concretely, this meant the entire K22 rework never executed for these two drivers: a pending
`_activeTransaction` was never rolled back or disposed, the cached `command` field was never
released, `_entityCache` was never cleared, `Entities`/`EntitiesNames` were never cleared, and the
provider connection was only ever `Close()`d (by SQLite's own `Closeconnection()` call) rather than
`Dispose()`d -- precisely the K12 leak K22 was written to close. SQLite's own body did nothing but
call `Closeconnection()`; DuckDB's own body disposed its own `DuckConn`/`Transaction`/`command`
fields (separate from `RDBSource`'s, so still necessary) and called `SaveStructure()`, but likewise
never reached the base.

**Status -- fixed.** SQLite: both hiding methods and the `disposedValue` field deleted outright:
`RDBSource.Dispose()`'s own chain already does everything the deleted `Dispose(bool)` did (it also
calls `Closeconnection()`) and more. DuckDB: `Dispose()` deleted (its body was identical to
`RDBSource.Dispose()`'s), `Dispose(bool)` changed to `protected override` with a `base.Dispose(disposing)`
call added at the end -- additive only; every line DuckDB's version already ran still runs, and
`RDBSource`'s cleanup now runs alongside it for the first time.

### D4 -- SQLite's `IInMemoryDB` state properties had two separate backing fields -- *fixed*

**Where** `SQLiteDataSource.cs`

**The defect.** `IsCreated`, `IsLoaded`, `IsSaved`, `IsSynced`, `IsStructureLoaded`,
`IsStructureCreated`, `CreateScript` and `InMemoryStructures` were re-declared as plain
auto-properties with the same names as `InMemoryRDBSource`'s `IInMemoryDB` properties -- giving
`SQLiteDataSource` a **second, disconnected backing field** for each. `SQLiteDataSource` does not
itself re-list `IInMemoryDB`, so any caller holding this datasource as `IInMemoryDB` read and wrote
the *base* class's copy, while this class's own methods (`OpenDatabaseInMemory` setting `IsCreated
= true`, for instance) read and wrote the *shadowed* copy declared here. The two views of "is this
database created" could silently disagree depending on which typed reference asked.

**Status -- fixed.** The eight shadow declarations are gone; `SQLiteDataSource` now inherits
`InMemoryRDBSource`'s single copy of each, which already satisfies `IInMemoryDB`.

### D5 -- SQLite's and DuckDB's `IInMemoryDB` overrides were hidden, not overriding -- *fixed*

**Where** `SQLiteDataSource.InMemory.cs`, `DuckDBDataSource.cs`

**The defect.** `LoadData(IProgress, CancellationToken)`, `SyncData(IProgress, CancellationToken)`,
`OpenDatabaseInMemory(string)` and `GetConnectionString()` are declared `virtual` on
`InMemoryRDBSource`; both drivers re-declared them with matching signatures but no `override`.
Unlike D2/D3, `SQLiteDataSource`/`DuckDBDataSource` do not themselves re-list `IInMemoryDB`, so an
`IInMemoryDB`-typed caller was not hijacked the way `IDataSource` callers were for D2 -- it got the
generic base implementation regardless. The real risk is narrower but still concrete: any future
`RDBSource`/`InMemoryRDBSource` method that calls one of these internally (as the base class is free
to do, since they are declared `virtual` precisely so overrides are reachable) would silently use
the generic version instead of the driver-specific one -- the SQLite-specific
`:memory:`-connection-string handling and folder bookkeeping, or DuckDB's `DuckDBCommand`-returning
`GetDataCommand()` (a covariant override, `DuckDBCommand : DbCommand : IDbCommand`, legal under this
project's `LangVersion Latest`), would simply not run where the base class expected an override to.

**Status -- fixed.** `override` added to all four sites on both classes, plus DuckDB's
`GetDataCommand()`. Three further sites on each class -- `SyncData(string, ...)`,
`RefreshData(string, ...)`, `RefreshData(IProgress, ...)` -- hide **non-virtual** base members (also
part of `IInMemoryDB`, but `InMemoryRDBSource` does not mark them `virtual`, so `override` is not an
option without a base-class change outside today's driver-focused scope); each is marked `new`
instead, which changes nothing behaviourally but records the shadow as deliberate rather than an
accident the compiler is warning about.

### D6 -- SQL Server's `PagedQuery` was dead code duplicating the consolidated paging path -- *fixed*

**Where** `SQLServerDataSource.cs`

A private `PagedQuery(string, List<AppFilter>)` method built SQL Server OFFSET/FETCH paging by hand,
reading `"Pagesize"`/`"pagenumber"` filter entries -- and had no caller anywhere in the file. Paging
for every RDBMS driver, including SQL Server, already runs through the single dialect source K11
consolidated everything onto (`RDBMSHelper.GetPagingSyntax`); a second, unreachable implementation
sitting next to it risked exactly the "two paging implementations disagree" defect K11 fixed, the
moment anyone wired it up thinking it was the live path. Deleted.

### D7 -- versions bumped

Per CLAUDE.md ("Bump the `<Version>` in the csproj when a plugin's behavior changes -- versions are
per-project, not repo-wide"): `RDBDataSource.csproj` had not been bumped once across the eleven
phases of this rework (`2.0.22` throughout) despite being the version string ten drivers were
pinning against by number (D1) -- bumped to `2.1.0`. Every driver project touched by D1-D6, whether
by source edit or by the reference fix alone (a fixed reference changes what a published build of
that driver actually contains, even with no `.cs` change), had its own `<Version>` bumped by one
patch.

### D8 -- SQLCompactDataSource did not exist -- *implemented*

**Where** `SqlCompactDatasourceCore/SqlCompactDataSource.cs` (new)

BeepDM's `ConnectionHelper.CreateSqlCompactConfig()` has always declared `classHandler =
"SQLCompactDataSource"` -- the plugin discovery system has been looking, by reflection, for a class
with exactly this name since that config was written, and finding nothing. SQL Compact was
registerable in the connection UI but unusable: selecting it could never construct a working
datasource instance. Implemented as a minimal `RDBSource` subclass matching `FireBirdDataSource`'s
pattern (no ADO.NET package reference needed at compile time -- `RDBDataConnection` resolves the
actual provider types from `ConnectionDriversConfig` by name at runtime), with FK toggling reported
as an honest no-op (SQL CE has no way to disable an individual constraint without dropping and
recreating it).

**Still open, and not fixable here:** `CreateSqlCompactConfig()` names the provider package as
`System.Data.SqlServerCe` 4.0.0.0, which was never published to nuget.org (confirmed: the package ID
has no versions there) and, being SQL Server Compact's native ADO.NET provider, is
Windows/.NET-Framework-only regardless -- it was never ported to .NET Core or later. An environment
limitation of the discontinued engine itself, not something this class can work around.

### D9 -- DDL type mapping only covered 4 of 14 dialects -- *fixed for 6 more*

**Where** `RDBSource.DMLGeneration.cs`, `GetFallbackDbType` and `NormalizeDbTypeForProvider`

Both methods -- the ones `CreateEntityAs` and the bulk temp-table path use to turn a `.NET` type name
into a column type when the configured per-datasource type map is absent or leaks a type from a
different provider -- only branched for SQL Server, PostgreSQL, MySQL and Oracle. Every other engine
(CockroachDB, HANA, Firebird, Presto/Trino, Snowflake, Spanner, SQLite, DuckDB) fell to a single
`default` case using SQLite's own permissive, affinity-typed names (`TEXT`/`REAL`/`BLOB`). Several of
those engines do not recognise those names as types at all: Spanner shares essentially no type
vocabulary with SQLite (`INT64`/`STRING`/`FLOAT64`/`BOOL`/`BYTES`, not
`INTEGER`/`TEXT`/`REAL`/`BOOLEAN`/`BLOB`), and Firebird has neither a bare `TEXT` nor a bare
`BLOB`-as-binary. The fallback these six replaced would have produced a `CREATE TABLE` several of
them reject outright, not merely one that is suboptimal.

**Status -- fixed for CockroachDB, HANA, Firebird, Presto/Trino, Snowflake and Spanner**, each using
that engine's own documented type names (`GetFallbackDbType`'s new branches carry the reasoning
inline). `NormalizeDbTypeForProvider` -- which runs on **every** provider's `CreateEntityAs`, not
only the three engines that reach the bulk temp-table path, guarding against
`DMEEditor.typesHelper.GetDataType` returning a type belonging to a different provider -- was
extended the same way, reusing `GetFallbackDbType`'s own target vocabulary per engine so the two
never disagree about what "this provider's TEXT type" is called. It previously normalised only for
SQL Server and passed every other provider's input straight through unchanged.

One bug in the new code was caught by its own test before landing: the Postgre and CockroachDB
normalize branches were first written as one shared switch arm using `BYTEA` as the canonical binary
output for both, which meant a value `GetFallbackDbType` had just picked as Cockroach's own name
(`BYTES`) got "corrected" straight back to `BYTEA` on the very next line. Split into two branches;
`DdlTypeMappingTests.CockroachDB_UsesPostgresTypeNames_NotSqliteAffinities` is what caught it.

**Deliberately not extended:** Firebolt (no confident, verified DDL type vocabulary -- `Firebolt.Ado`
is not resolvable in this environment either, so nothing here can be checked against a real
connection) and SQL Compact (no reachable ADO.NET provider to verify against, per D1/D8). SQLite and
DuckDB stay on the shared default, which both engines' own permissive type-alias acceptance makes
correct as-is.

Covered by `DdlTypeMappingTests.cs` (8 tests), asserting on the generated `CREATE TABLE` text per
this suite's established practice, since SQLite -- the only engine actually reachable through the
harness -- would execute any of these strings without distinguishing a name a real engine rejects
from one it accepts.

### D10 -- `SQLiteMigrationProvider.Capabilities` hid the base instead of overriding it -- *fixed*

**Where** `SqliteDatasourceCore/SQLiteMigrationProvider.cs`

The same hiding-vs-overriding defect class as D2-D5, in a completely different subsystem.
`RdbmsSqlMigrationProvider.Capabilities` is declared `public virtual`; `SQLiteMigrationProvider`
re-declared it as `public new SchemaMigrationCapabilities Capabilities`, explicitly (the `new`
keyword suppresses the compiler's hiding warning, so this was a deliberate choice by whoever wrote
it, not an accidentally-silenced one). `IDMEEditor.GetMigrationProvider(IDataSource)` -- what
`MigrationManager` actually calls -- is declared to return `ISchemaMigrationProvider`, and every
guard in `MigrationManager.EntityOperations.cs` (`if (!provider.Capabilities.Supports(...))`) reads
`Capabilities` through that interface reference. Verified empirically both ways
(`Type.GetInterfaceMap` against the compiled DLL, before and after): before the fix,
`provider.Capabilities` on a `SQLiteMigrationProvider` resolved to
`RdbmsSqlMigrationProvider`'s generic, all-supported capabilities -- not this class's honest,
degraded ones (`SupportsAlterColumn = false`, `SupportsDropColumn = false`,
`SupportsDropForeignKey = false`, `SupportsTransactionalDdl = false`). Every guard `MigrationManager`
runs before attempting one of those four operations was silently bypassed, and it would have
attempted each directly against SQLite, which does not support any of them in the form
`RdbmsSqlMigrationProvider` emits.

**Status -- fixed**: `new` changed to `override`. Swept every other `MigrationProvider` in the repo
(25 in total, across RDBMS/NoSQL/File/Vector/Messaging) for the same `public new ... Capabilities`
pattern -- `SQLiteMigrationProvider` was the only one.

### D11 -- 4 of 14 RDBMS drivers had no migration provider -- *implemented for 4, deliberately skipped for 2*

**Where** `HanaDataSource/HanaMigrationProvider.cs`, `SnowFlakeDataSource/SnowFlakeMigrationProvider.cs`,
`PrestoDatasource/PrestoMigrationProvider.cs`, `SqlCompactDatasourceCore/SqlCompactMigrationProvider.cs` (all new)

Per CLAUDE.md's "Schema migration providers" section, a Tier-1 provider is expected colocated with
each driver; `MigrationManager` falls back to a generic Tier-2 default (`RdbmsSqlMigrationProvider`
itself, for `DatasourceCategory.RDBMS`) when one is missing, so this was not a hard failure the way
D1/D8 were -- but it meant six of fourteen drivers (Firebolt, HANA, Presto, Snowflake, SQL Compact,
DuckDB) had never had their engine's real DDL limitations declared, and were silently treated as
fully capable by the generic fallback.

**HANA**: full 12/12 DDL, a thin wrapper like Postgre/MySQL/Oracle/SqlServer/CockroachDB/Firebird --
HANA is a full-featured enterprise RDBMS with no capability gap to declare.

**Snowflake**: overrides `Capabilities` -- no user-managed indexes at all (Snowflake relies on
automatic micro-partition pruning instead of B-tree/hash indexes; `CREATE INDEX`/`DROP INDEX` are
not Snowflake syntax), and DDL auto-commits so it cannot be wrapped in a transaction
(`SupportsTransactionalDdl = false`).

**Presto/Trino**: overrides `Capabilities` conservatively, matching `PrestoDataSource`'s own stance
in its FK-toggle override ("Presto is primarily a query engine, not a transactional database"): no
enforced foreign keys, no user-managed indexes (connector-specific at best), no transactional DDL,
and `AlterColumn` (type changes) left unsupported since that is the one column operation without
broad, connector-independent support across Presto/Trino's many backing catalogs.

**SQL Compact**: overrides `Capabilities` -- no `RenameEntity`/`RenameColumn` at all (SQL CE has no
`sp_rename` equivalent and no `ALTER TABLE ... RENAME`), no `AlterColumn` (cannot change a column's
data type once created), and `SupportsTransactionalDdl = false` (SQL CE's transaction model is a
single-connection subset of SQL Server's and DDL-in-transaction support was not confident enough to
declare true).

**Deliberately skipped**: Firebolt (same reasoning as D9 -- no verified DDL vocabulary and no
reachable connection to check one against) and DuckDB (its own datasource class already bypasses
`RDBSource`'s standard schema path almost entirely with ~20 of its own overrides; writing a
migration provider for it needs auditing that integration first, which is a larger piece of work
than a capability declaration).

### D12 -- the temp-table bulk-update path was broken for every engine except SQL Server -- *fixed*

**Where** `RDBSource.BulkOperations.cs`, `BulkUpdateWithTempTable`/`BulkUpdateWithTempTableAsync`

Found while extending `SupportsTempTables()` to CockroachDB (below): the temp table name was
generated as `$"#TempUpdate_{entityName}_{Guid.NewGuid():N}"` **unconditionally, for every engine**.
The leading `#` is SQL Server's own local-temp-table sigil and means nothing anywhere else --
PostgreSQL and CockroachDB reject it as a syntax error, and MySQL treats `#` as a to-end-of-line
comment marker, so `CREATE TEMPORARY TABLE #TempUpdate_...` became `CREATE TEMPORARY TABLE` with the
rest of the line silently commented out. `SupportsTempTables()` has listed MySQL and PostgreSQL
alongside SQL Server from the start, so **this broke the temp-table path on two of the three engines
it was originally written for**, from day one -- invisible until this work actually drove the path
against a real, non-SqlServer execution for the first time (SQLite was never one of the three, so
nothing before this exercised it).

**Status -- fixed.** A new `BuildTempTableName(entityName)` helper applies the `#` prefix only for
`DataSourceType.SqlServer`/`AzureSQL`/`SqlCompact` (the engines where it means something) and emits a
plain, portable name everywhere else. Covered by `TempTableNamingTests.cs`: PostgreSQL executes the
whole bulk update end to end through SQLite (its `UPDATE ... FROM` join-update syntax happens to also
be valid SQLite); MySQL and SQL Server are asserted on the generated SQL text instead, since MySQL's
`UPDATE ... INNER JOIN ... SET` merge syntax and SQL Server's bare `#` identifier are both things a
real server understands and SQLite, as a stand-in, does not.

### D13 -- `SupportsTempTables()`/temp-table builders did not cover CockroachDB -- *fixed*

**Where** `RDBSource.BulkOperations.cs`

CockroachDB is PostgreSQL wire- and DDL-compatible for the exact statements the temp-table path
emits (`CREATE TEMP TABLE`, `UPDATE ... SET ... FROM ... AS source`), but `SupportsTempTables()` only
listed SqlServer/Mysql/Postgre, so Cockroach fell back to the slower, row-at-a-time batched path.
Extended `SupportsTempTables()` and the two dispatch switches (`CreateTempTableForUpdate`,
`BuildMergeUpdateQuery`) to reuse `BuildPostgreSqlTempTableCreate`/`BuildPostgreSqlUpdateFromQuery`
directly for `DataSourceType.Cockroach` rather than writing a separate, identical builder pair.
`ResolveDdlColumnType` already dispatches on `DatasourceType` at call time, so the column types the
reused builder emits are Cockroach's own (D9), not Postgres's `BYTEA`-for-`BYTES` naming. Covered by
`CockroachTempTableTests.cs` (2 tests) -- the first of which is what surfaced D12.

**Not extended further:** Oracle, SQLite, HANA, Firebird, Presto, Snowflake and Spanner each need
genuinely dialect-specific temp-table/`MERGE` syntax this class has no builder for yet; only
CockroachDB is close enough to an existing dialect (Postgres) to reuse it verbatim.

### D14 -- versions bumped (second pass)

Every driver project touched by D8-D13 -- Hana, Snowflake, Presto, SQL Compact (each gained a new
migration provider file; SQL Compact also gained its missing datasource class) and the base
`RDBDataSource.csproj` itself (D9/D12/D13 all land there) -- had its `<Version>` bumped by one patch,
per the same CLAUDE.md rule D7 already applied.

### D15 -- 7 more RDBMS classHandlers named a class that did not exist -- *implemented for 7, one flagged*

**Where** (new) `SQlServerDataSourceCore/AzureSQLDataSource.cs`,
`PostgreDataSourceCore/TimeScaleDBDataSource.cs`, `MySqlDataSourceCore/AWSRDSDataSource.cs`,
`DB2DataSourceCore/`, `VerticaDataSourceCore/`, `TerraDataDataSourceCore/`, `VistaDBDataSourceCore/`
(four new projects)

The same gap as D8 (`SQLCompactDataSource`), found by checking every `classHandler` string in
`ConnectionHelper_RDBMS.cs` against an actual class in the repo. Seven more named classes that were
never written:

| `classHandler` | `DataSourceType` | Provider package | Dialect |
|---|---|---|---|
| `AzureSQLDataSource` | `AzureSQL` | `System.Data.SqlClient` | Same engine as SQL Server |
| `TimeScaleDBDataSource` | `TimeScale` | `Npgsql` | PostgreSQL extension -- same engine |
| `AWSRDSDataSource` | `AWSRDS` | `MySql.Data` | This config is RDS-for-MySQL specifically |
| `DB2DataSource` | `DB2` | `IBM.Data.DB2` | Db2 LUW |
| `VerticaDataSource` | `Vertica` | `Vertica.Data` | Vertica MPP |
| `TerraDataDataSource` | `TerraData` | `Teradata.Client.Provider` | Teradata MPP |
| `VistaDBDataSource` | `VistaDB` | `VistaDB` | -- |

**Status -- implemented for the first six.** `AzureSQLDataSource`/`TimeScaleDBDataSource`/
`AWSRDSDataSource` mirror `SQLServerDataSource`/`PostgreDataSource`/`MySQLDataSource` exactly, since
their `PackageName` in BeepDM's own config confirms each is the *same engine* wire-and-dialect-wise,
just hosted differently -- there was nothing to guess. `DB2DataSource` uses Db2's real
`SET INTEGRITY FOR ... OFF` / `... IMMEDIATE CHECKED` toggle, verified documented syntax, not the
SQL Server/MySQL `ALTER TABLE ... [NO]CHECK CONSTRAINT` family. `VerticaDataSource` and
`TerraDataDataSource` report FK toggling as an honest no-op rather than guessing an
enforcement-toggle syntax neither engine has: both are MPP engines where foreign keys are
conventionally unenforced (optimizer hints / `WITH NO CHECK OPTION`), matching this project's
established pattern for a genuinely unsupported operation (Spanner, Presto, Snowflake all do the
same for their own unsupported operations). None of the six needed a `PackageReference` to their own
ADO.NET client at compile time -- like `FireBirdDataSource`, `RDBDataConnection` resolves the actual
provider type from `ConnectionDriversConfig` by name at runtime, so this was zero-risk to add.

**`VistaDBDataSource` is implemented but structurally minimal**, deliberately: unlike SQL Compact
(D8), where `RDBSource`'s FK-toggle default was clearly wrong for that engine, whether VistaDB
(marketed as broadly SQL-Server-compatible, but a much simpler embedded engine, with no vendor
activity in years) accepts SQL Server's specific `ALTER TABLE ... [NO]CHECK CONSTRAINT ALL`
administrative syntax could not be verified, so no FK-toggle override was written rather than
guessed in either direction. The `VistaDB` NuGet package itself could not be found published under
that name.

Four new projects (`DB2DataSourceCore`, `VerticaDataSourceCore`, `TerraDataDataSourceCore`,
`VistaDBDataSourceCore`) were created and added to `DataSourcePluginSolution.sln`; the other three
classes were colocated in the existing project that already shares their exact dialect/package
(`SQlServerDataSourceCore`, `PostgreDataSourceCore`, `MySqlDataSourceCore` respectively), since
creating a whole separate project for a class needing nothing beyond what that project already
references would have been pure duplication.

### D16 -- CockroachDB's `classHandler` never matched its actual class name -- *fixed*

**Where** `../BeepDM/DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper_RDBMS.cs`

Found during the D15 sweep. `CreateCockroachConfig()` declared `classHandler = "CockroachDBDataSource"`;
the actual class (correctly discovered via `[AddinAttribute]` reflection, which is why every
CockroachDB test and fix earlier in this project worked) is `CockRoachDataSource` -- capital `R`, no
`DB`. Per CLAUDE.md's own description of the three-way registration ("A `Create*Config` entry ...
matched by `classHandler` == the class name. Without it the driver never appears in the connection
UI"), this meant CockroachDB was constructible and fully functional once instantiated directly, but
never appeared in the connection UI's driver list -- the one thing `classHandler` actually gates.
Corrected the string to match the real class name.

### D17 -- versions bumped (third pass)

`AzureSQLDataSource`/`TimeScaleDBDataSource`/`AWSRDSDataSource` each bumped the `<Version>` of the
existing project they were added to (`SqlServerDataSourceCore`, `PostgreDataSourceCore`,
`MySqlDataSourceCore`) by one patch. The four new projects (D15) start at `1.0.0`, matching every
other driver project's first-published convention.

## Tier 6 -- `IInMemoryDB` implementers repo-wide

Broader than Tier 5: `IInMemoryDB` (the interface `InMemoryRDBSource`, `SQLiteDataSource` and
`DuckDBDataSource` all implement) has 12 implementers repo-wide, most with no relationship to
`RDBSource` at all -- LevelDB, LiteDB, LMDB, RavenDB, RealM, Redis, RocksDB implement `IDataSource`/
`IInMemoryDB` directly, as do the three vector databases (PineCone, Qdrant, SharpVector). Checked
every one of them.

### D18 -- 5 of 12 `IInMemoryDB` implementers did not compile at all -- *fixed*

**Where** `RavenDBDataSourceCore/RavenDBDataSource.cs`, `RealMDataSource/RealMDataSource.cs`,
`RedisDataSourceCore/RedisDataSource.cs`, `VectorDatabase/TheTechIdea.Beep.PineConeDatasource/PineConeDatasource.cs`,
`VectorDatabase/TheTechIdea.Beep.QdrantDatasource/QdrantDatasourceGeneric.cs`

**The defect.** `IInMemoryDB` has grown since these five were last touched: `OpenInMemory(string)`,
`GetInMemoryConnectionString()`, `ResetInMemory()`, `LoadStructureWithData(...)`,
`FillFromDataSource(...)`, `ExportToDataSource(...)`, the `IsStructureLoaded` property, and the
`StructureChanged`/`DataChanged`/`StateChanged` events were all added to the interface at some point
-- visible in `InMemoryRDBSource.cs`'s own "IInMemoryDB v2" region, and in the fact that LevelDB,
LiteDB, LMDB and RocksDB (each carrying a "Phase 12" comment marking when they were migrated) already
implement the full v2 surface. These five were never migrated: each was missing eleven interface
members, which is not a partial or degraded implementation -- it is `CS0535` on every one of them,
and **none of these five projects compiled**, in isolation or as part of either `.sln` they are
listed in (`DataSourcePluginSolution.sln`, `DataSourcesPluginsCore.sln`). A twelfth error
(`LoadStructure(IProgress<PassedArgs>?, CancellationToken)`) was a related but distinct gap: each
class already had a *three*-parameter `LoadStructure(progress, token, copydata = false)`, but C#
does not let an optional third parameter satisfy an interface member declared with only two --
arity has to match exactly.

**Status -- fixed, all five.** Added the missing eleven members to each class, built almost entirely
by orchestrating primitives each class already had correctly working (`GetEntity`, `InsertEntity`,
`CreateEntityAs`, `CheckEntityExist`, and each class's own pre-v2 `OpenDatabaseInMemory`/
`GetConnectionString`, which `OpenInMemory`/`GetInMemoryConnectionString` now delegate to) rather
than inventing new engine-specific logic for RavenDB's document-session API, Realm's mobile object
store, Redis's key-value model, or the two vector stores' APIs. Added the missing 2-parameter
`LoadStructure` overload to all five, delegating to the existing 3-parameter one with
`copydata: false`.

**Not independently verified**: correctness of the underlying engine-specific methods these new
members call (`OpenDatabaseInMemory`, `GetEntity`, `InsertEntity`, etc.) was not re-audited here --
only that the class now compiles and that the new methods correctly delegate to what was already
there. A pre-existing, likely-broken line noticed in passing while reading RavenDB's file:
`OpenDatabaseInMemory` casts `EmbeddedServer.Instance.GetDocumentStoreAsync("Embedded")` -- a
`Task<IDocumentStore>` -- directly to `IDocumentStore`, which is not a valid conversion and would be
expected to throw at the call site if this path is ever actually exercised. Left as found; fixing it
needs RavenDB.Embedded API verification this pass did not do.

### D19 -- DuckDB's own CRUD reproduces the F1/K2 pattern the RDBSource rework fixed -- *fixed*

**Where** `InMemoryDB/DuckDBDataSourceCore/DuckDBDataSource.cs`, `UpdateEntity`/`InsertEntity`/`DeleteEntity`

DuckDB does not inherit `RDBSource.CRUD.cs` -- it overrides `UpdateEntity`, `InsertEntity` and
`DeleteEntity` itself, reimplementing roughly the same command-building-and-executing logic the base
class had before Phase 1/2 of this rework. That reimplementation carries the exact same defect F1
and K2 were written to fix, in all three methods: `ErrorObject.Flag = Errors.Ok` is set once at
method entry and never revisited when the server reports zero rows affected. `UpdateEntity` and
`DeleteEntity` had an `else` branch that only called `DMEEditor.AddLogMessage(..., Errors.Failed)` --
which, per F1, does nothing to the flag when no logger is attached -- so a key that matched no row
reported success. `InsertEntity` was worse: the zero-rows case had **no `else` branch at all**, not
even a log line, so a failed insert was completely silent.

None of this is exercised by `tests/RDBDataSource.Tests` (that suite drives `RDBSource` through
`SqliteHarness`, not `DuckDBDataSource`, which needs the actual DuckDB engine), so it was found by
reading, not by a failing test.

**Status -- fixed.** All three zero/negative-row branches now set `ErrorObject.Flag = Errors.Failed`
and `.Message` explicitly, matching the base class's K2 fix. Also fixed in the same pass: `command`
(the `DuckDBCommand` from `GetDataCommand()`) was disposed on every exception path but never on the
success path in `UpdateEntity` or `InsertEntity` -- one leaked native command handle per successful
write.

**Not fixed, and flagged rather than guessed at:** `DeleteEntity` opens
`RDBMSConnection?.DbConn.BeginTransaction()` but never assigns it to `command.Transaction` before
executing, so the delete is not actually protected by the transaction it opens (`sqlTran.Commit()`
runs regardless of whether the delete's own transaction membership was ever established) -- the same
shape as K8 in the base class, but fixing it here means verifying how strictly DuckDB.NET's provider
enforces the command/transaction association, which this pass did not do. `UpdateEntity`'s
transaction handling is commented out entirely (`//   var sqlTran = ...`), an inconsistency with
`DeleteEntity` left as found. `CreateCommandParameters`/`CreateDeleteCommandParameters` bind
parameters by field name directly (no shared-pool substring matching), so neither carries K1's
wrong-row-update defect -- that part of DuckDB's own CRUD is sound.

## Test coverage

`tests/RDBDataSource.Tests/` reports **189 passing tests**, and — unlike the suite this work
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
| `DdlTypeMappingTests.cs` | 8 | D9 — dialect-correct CREATE TABLE types for 6 engines |
| `CockroachTempTableTests.cs` | 2 | D13 — CockroachDB reusing Postgres's temp-table bulk-update path |
| `TempTableNamingTests.cs` | 3 | D12 — the SQL-Server-only `#` prefix that broke Postgres/MySQL |

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
