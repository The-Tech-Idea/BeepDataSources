# Query, paging and streaming

Covers `RDBSource.Query.cs` (1040), `RDBSource.Modernization.cs` (497), `RDBSource.Pagination.cs`
(36, no code), `RDBSource.Dapper.cs` (38), and the two paging helpers.

## The live read path

`GetEntity(string, List<AppFilter>)` (`Query.cs:579`) is the main entry point and is an **iterator**.
Its flow:

1. Decide whether `EntityName` is a table name or a full SELECT — `Query.cs:588` treats it as a
   table when it contains **neither** `"select"` nor `"from"`.
2. Build the base query, applying schema qualification via `GetTableName(qrystr.ToLower())`.
3. If the entity's `EntityStructure.CustomBuildQuery` is set, use that instead (`:612`).
4. `BuildSelectQueryDefinition(qrystr, Filter)` — a BeepDM extension that produces the SQL text plus
   a parameter definition.
5. Open the connection if needed, `GetDataCommand()`, `ApplyFilterQueryDefinition(...)`,
   `ExecuteReader(CommandBehavior.SequentialAccess)`.
6. Stream rows through `DataStreamer.Stream(reader)`, which yields one case-insensitive
   `Dictionary<string, object>` per row; the read path then materialises each into the generated
   entity type from `GetEntityType`, or yields the dictionary itself when the type cannot be
   resolved. It logs when it takes that fallback, and reports a column that fails to convert once
   per column rather than silently defaulting the property on every row.

This path is in noticeably better shape than the write path. It null-checks `GetDataCommand()`, and
its failure diagnostic is genuinely useful — it names the SQL, the datasource type and the parameter
delimiter, precisely so a dialect mismatch is attributable (`:636-646`).

### Filter values are parameterised; operators and field names are not

`AppFilter.FilterValue` is always bound as a parameter. **There is no injection path through filter
values.**

`AppFilter.Operator` and `AppFilter.FieldName` are a different story, and the code responsible lives
in BeepDM, not here —
`../../../BeepDM/DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs`:

```csharp
// NormalizeOperator — no whitelist; unknown operators pass through
var token = Regex.Replace(op.Trim(), @"\s+", " ");
if (OperatorAliases.TryGetValue(token, out var normalized))
    return normalized;
return token.ToLowerInvariant();

// QuoteIdentifier — gives up exactly when quoting is needed
if (clean.Contains(" ") || clean.Contains(".") || clean.Contains("[")
 || clean.Contains("\"") || clean.Contains("`"))
{
    return clean;      // returned UNQUOTED
}
```

Both are concatenated into the WHERE clause. Treat `AppFilter` as untrusted input at every call
site until this is fixed. Tracked as T4 in [10-known-issues.md](10-known-issues.md).

<a id="dead-code"></a>
## Dead code — roughly 40% of `Query.cs`

None of the following has any caller anywhere in the repository:

`BuildQuery` (231) · `BuildQueryInternal` (258) · `ParseQueryComponents` (383) ·
`FindNextClausePosition` (441) · `GetSchemaPrefix` (460) · `BuildWhereClause` (469) ·
`IsValidFilter` (512) · `FormatFilterCondition` (526) · `SanitizeParameterName` (544) ·
`AppendClauseIfExists` (561)

This has a knock-on effect: `BuildQuery` is the only caller of `GenerateQueryCacheKey`,
`TryGetCachedQuery` and `CacheQuery`, so the query cache in `Cache.cs` is permanently empty. See
[08-cache-resilience.md](08-cache-resilience.md).

`BuildQueryInternal` is worth not reviving as-is: it lowercases the whole query (destroying
identifier case), indexes `sp[1]` without checking (`SELECT 1` with no FROM throws), and detects
clauses with `Contains("where")` / `Contains("having")` with no word boundary — so
`SELECT anywhere FROM t` takes the WHERE branch.

<a id="paging"></a>
## Paging — four implementations, two of them dead

`RDBSource.Pagination.cs` contains **no code**. All 36 lines are a comment naming the four paging
routes and describing a consolidation that was never carried out. Its description is also slightly
wrong: it says `Query.cs` uses `PaginationHelper`, but `Query.cs:797` calls
`RDBMSHelper.GetPagingSyntax` directly.

| Route | Status | Dialect source |
|---|---|---|
| `Query.cs:730` `GetEntity(..., pageNumber, pageSize)` | **live** | `RDBMSHelper.GetPagingSyntax` — default `LIMIT/OFFSET` |
| `Modernization.cs:389` `GetPagedQuery` | **live** (async/streaming callers) | own `switch`, 5 engines, default `OFFSET/FETCH` |
| `Helpers/PagedQueryExecutor.cs` | **dead** — no callers | — |
| `Helpers/PaginationHelper.cs` | **dead** — only `PagedQueryExecutor` calls it | `RDBMSHelper.GetPagingSyntax` |

The two live routes **disagree**, and their defaults are opposites. Oracle gets `OFFSET/FETCH` from
one and a `ROWNUM` subquery from the other. Engines not named in the `Modernization` switch —
MariaDB, Snowflake, ClickHouse, CockroachDB, Vertica, BigQuery, Redshift, DuckDB, TimeScale,
Supabase, Firebolt, Hologres, Databricks, Presto, Trino, Hana, H2, Spanner, Athena — receive
SQL Server syntax on the async path. `AzureSQL` and `SqlCompact` fall to `LIMIT/OFFSET` on the
`Query.cs` path and produce a syntax error on every paged read.

### Deterministic ordering

Both routes append an ORDER BY when the query lacks one, because OFFSET/FETCH requires it and
LIMIT/OFFSET is otherwise non-deterministic:

```csharp
// Query.cs:763-772
if (ent?.PrimaryKeys != null && ent.PrimaryKeys.Count > 0)
    baseQuery += $" ORDER BY {GetFieldName(ent.PrimaryKeys[0].FieldName)}";
else
    baseQuery += " ORDER BY 1";
```

Two problems. `ORDER BY 1` sorts by the first *projected* column, which is not necessarily unique,
so rows repeat and vanish across pages. And `GetFieldName` emits a **string literal** rather than a
quoted identifier on every driver but SQLite (see [09-extending.md](09-extending.md)), so
`ORDER BY 'Order Id'` sorts by a constant — the page boundaries become arbitrary.

### The count query

`Query.cs:775-820` extracts a table name by regex, or wraps the query as
`SELECT COUNT(*) FROM ( … ) __q` after stripping the trailing ORDER BY. Both steps are textual and
fragile: `StripTrailingOrderBy` aborts if a `)` appears after the last `ORDER BY` (so
`ORDER BY UPPER(name)` survives into the subquery and breaks it), and `ExtractWhereClause` finds the
first `" where "` by substring, including one inside a derived table or a string literal.

A count failure is swallowed as a `Warning` (`:817`), leaving `totalRecords = 0`, so `TotalPages`
is `0` and `HasNextPage` is `false` even though `Data` has rows. Any caller looping on
`HasNextPage` stops after the first page.

## `Modernization.cs` — the async surface

| Method | Line |
|---|---|
| `GetEntityStreamAsync<T>` | 34 |
| `ExecuteQueryStreamAsync` | 117 |
| `GetEntityPagedStreamAsync<T>` | 154 |
| `GetEntityPagedAsync<T>` | 192 |
| `ExecuteScalarAsync<T>` | 290 |
| `ExecuteNonQueryAsync` | 320 |

Disposal here is correct throughout — `await reader.DisposeAsync()` / `await cmd.DisposeAsync()` in
`finally` — and this is the only file in the class with no sync-over-async.

Its query building, however, is a parallel re-implementation that does not match `Query.cs`:

```csharp
// Modernization.cs:351 and :371 — note the missing '.'
string baseQuery  = $"SELECT * FROM {Dataconnection.ConnectionProp.SchemaName}{entityName}";
string countQuery = $"SELECT COUNT(*) FROM {Dataconnection.ConnectionProp.SchemaName}{entityName}";
```

With `SchemaName = "dbo"` that is `SELECT * FROM dboCustomers`. `GetTableName` gets this right;
this path does not. Every method in the file is affected.

Its WHERE builder has further gaps:

```csharp
// Modernization.cs:357
$"{GetFieldName(f.FieldName)} {GetOperator(f.Operator)} {ParameterDelimiter}p_{f.FieldName}"
```

- `GetOperator` maps `ISNULL`/`ISNOTNULL`/`IN`, but the caller unconditionally appends a parameter
  token, so those produce `[Status] IS NULL @p_Status` and `[Id] IN @p_Id`.
- Unknown operators silently become `"="` (`:457`), so a filter meaning `NOTLIKE` returns matches.
- Two filters on the same field both produce `@p_Field` — a duplicate-parameter exception.
- Parameter names are not sanitised, so a field name with a space produces an illegal identifier.
- Values are bound as raw strings, with no `DbType` and no conversion.

`PagedResult<T>` is redeclared here (`:485`) in the same namespace as the framework's non-generic
`PagedResult`, using `TotalCount` where the other uses `TotalRecords`.

## `GetEntityAsync` — the offload is now real

It used to be:

```csharp
return Task.Run(() => GetEntity(EntityName, Filter));
```

`GetEntity` is a `yield`-based iterator, so `Task.Run` invoked it, immediately received the
un-enumerated `IEnumerable<object>`, and completed. `Openconnection()`, `ExecuteReader` and all row
materialisation then ran on the **consuming** thread when it enumerated — the XML comment claiming
the caller stayed responsive was false, and the awaited result was a reader still attached to the
shared connection.

The sequence is now enumerated inside the task, so the work happens on the pool thread and the task
completes with the rows in hand. That buffers the result set; callers that need to stream should use
`GetEntityStreamAsync<T>` (`Modernization.cs`), which is a genuine `IAsyncEnumerable` over
`ExecuteReaderAsync`.

## The Dapper path

`RDBSource.Dapper.cs` is 38 lines and has three problems:

```csharp
public virtual List<T> GetData<T>(string sql)
{
    if (Dataconnection.ConnectionStatus == ConnectionState.Open)
        return RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();
    else
        return null;
}
```

- **It bypasses the transaction.** Neither method passes `ActiveTransaction` to Dapper's
  `transaction:` argument, so a Dapper call inside a `BeginTransaction` scope fails on SQL Server,
  PostgreSQL, Oracle and MySQL with the pending-local-transaction error described in
  [03-transactions.md](03-transactions.md).
- **It returns `null`, not empty.** And `SaveData<T>` returns a **null `Task`**, so
  `await source.SaveData(sql, p)` throws `NullReferenceException` at the await.
- **The guard checks the wrong thing.** `Dataconnection.ConnectionStatus` is a cached field on
  `RDBDataConnection`, not `DbConn.State`; a dropped connection still reads `Open`.

Neither method sets `ErrorObject` or catches anything.

## Resource notes

- `GetDataReader` now null-checks the command and hands the caller a
  `Helpers/CommandOwningDataReader`, so disposing the reader disposes the command with it. It used
  to return the raw provider reader, leaving nothing that could dispose the command — one leaked
  `IDbCommand` per call, held for the life of the connection. Disposing it before returning is not
  an option: on SQLite and other providers that finalises the statement handle the open reader is
  reading through, so ownership has to travel with the reader.
- `SetObjects` disposes the previous `command` before replacing it. It used to overwrite the shared
  field — one leaked command per entity switch.
- Reader ownership within the read paths is still inconsistent: one path wraps the reader in
  `using var` **and** hands it to `DataStreamer`, which also disposes it; the others rely on
  `DataStreamer` alone. Double disposal is harmless on every provider here, but the intent should
  be stated in one place.
- Because `GetEntity` is an iterator, a consumer that abandons enumeration without disposing the
  enumerator leaves the reader and command open on the shared connection.
- The empty catches in this file are gone. The two per-property conversion catches — which silently
  produced a default value for every column that failed to convert, on every row — now log once per
  column, naming the column, the target type and the fact that the property is defaulted for the
  whole read. The structure-lookup and entity-type-resolution catches kept their fallbacks and now
  record that they took them. The one that stays silent is the static SQL table-name scan: it is a
  `static` method with no logger in reach, and returning `null` is its contract.
