# Caching and resilience

Covers `RDBSource.Cache.cs` (290 lines) and `RDBSource.Resilience.cs` (426 lines).

Both files present a complete, well-documented subsystem. Neither is connected to the paths it was
built for. Read this before assuming either does anything.

---

## `RDBSource.Cache.cs`

### Declared surface

| Member | Line | Wired up? |
|---|---|---|
| `_queryCache` (compiled query strings) | 26 | Only from dead code |
| `_preparedStatementCache` | 32 | **No callers** |
| `_resultCache` (`MemoryCache`) | 37 | **No callers** |
| `_entityResultKeys` (entity → cache keys) | 44 | **No callers** |
| `EnableResultCache` (public, default `false`) | 59 | Wired to nothing |
| `ResultCacheTTL` (public) | 65 | Wired to nothing |
| `GenerateQueryCacheKey` / `TryGetCachedQuery` / `CacheQuery` | 92, 122, 132 | Only from `BuildQuery` |
| `TryGetCachedResult<T>` / `CacheResult<T>` | 147, 163 | **No callers** |
| `InvalidateEntityCache` | 203 | 10 callers |
| `TryGetPreparedStatement` / `CachePreparedStatement` | 270, 280 | **No callers** |
| `ClearAllCaches` (public) | 245 | No internal callers; not called from `Dispose` |

### What actually runs

Only `GenerateQueryCacheKey`, `TryGetCachedQuery` and `CacheQuery` have any caller, and their sole
caller is `BuildQuery` (`Query.cs:236-246`) — which itself has no callers
(see [05-query-paging.md](05-query-paging.md#dead-code)). So `_queryCache` is permanently empty too.

The consequence is that `InvalidateEntityCache`, called from six places in `CRUD.cs` and four in
`BulkOperations.cs`, does this on every INSERT, UPDATE and DELETE:

1. Iterate an empty `_queryCache`.
2. Skip the result-cache block, because `EnableResultCache` is `false` and nothing populated it.
3. Log **"Cache invalidated for entity: {name}"** with `Errors.Ok` (`:239`).

The log line is actively misleading — it reports work that did not happen.

### If it is ever revived

The design has three problems worth fixing before wiring it up:

**The key includes filter values.**

```csharp
// Cache.cs:109
key += $"|{filter.FieldName}:{filter.Operator}:{filter.FilterValue}";
```

What is being cached is a *query string*, which is value-independent because values are bound as
parameters. Including the value produces one permanent entry per distinct value — so filtering by id
adds an entry per id, unbounded, in a `ConcurrentDictionary` with no eviction. The key should cover
the filter *shape* only. It also omits `ParameterDelimiter`, `DatasourceType` and `SchemaName`, so a
schema switch on the same instance would serve the wrong SQL.

**Invalidation is prefix-based.**

```csharp
// Cache.cs:214
if (key.StartsWith(entityKey, StringComparison.OrdinalIgnoreCase))
```

Invalidating `ORDER` also invalidates `ORDERS`, `ORDER_ITEMS` and `ORDERDETAILS`. It never
invalidates a dependent entity — a view over the table, or a join cached under another name.

**The eviction callback mutates a `HashSet` without the lock the other paths use.**

```csharp
// Cache.cs:174-183 — no lock
cacheEntryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
{
    if (state is string entity && !string.IsNullOrEmpty(entity))
        if (_entityResultKeys.TryGetValue(entity, out var keys))
            keys.Remove(key as string ?? string.Empty);
}, entityName);
```

versus `lock (keys) { keys.Add(cacheKey); }` at `:193` and `lock (resultKeys) { … }` at `:228`.
`MemoryCache` eviction callbacks run on a thread-pool thread, so `Remove` races `Add` and `ToList()`.

Two smaller issues: `ResultCache`'s `??=` getter (`:74-78`) is an unsynchronised lazy init, so two
threads can each build a `MemoryCache` and one is orphaned undisposed; and `ClearAllCaches` disposes
`_resultCache` and nulls it while another thread may still hold the reference.

### The cache that actually matters is elsewhere

`EntityStructureCache` (`Helpers/EntityStructureCache.cs`) is live, and it has **no invalidation API
at all** — no TTL, no size limit, no `Remove`, no `Clear`. `InvalidateEntityCache` does not know it
exists. After a `CREATE TABLE` or `ALTER TABLE`, entity structures are stale for the lifetime of the
datasource. See [06-schema-types.md](06-schema-types.md#entitystructurecache).

---

## `RDBSource.Resilience.cs`

### Declared surface

| Member | Line | Default |
|---|---|---|
| `MaxRetryAttempts` | 28 | 3 |
| `RetryBaseDelay` | 34 | 1 s |
| `EnableResilience` | 40 | `true` |
| `CircuitBreakerThreshold` | 46 | 5 |
| `CircuitBreakerDuration` | 52 | 30 s |
| `HealthCheckTimeout` | 58 | 5 s |
| `OpenConnectionResilient` / `…Async` | 242, 270 | — |
| `CheckConnectionHealth` / `…Async` | 302, 345 | — |
| `ResetResiliencePolicies` | 414 | — |

Polly v8 (`ResiliencePipelineBuilder`, `RetryStrategyOptions`, `CircuitBreakerStrategyOptions`).

### It is inert, for two independent reasons

**Nothing outside this file uses it.** No CRUD, query, schema or bulk path is wrapped in
`ResilientPipeline`. The only consumers of `OpenConnectionResilient` / `OpenConnectionResilientAsync`
are `CheckConnectionHealth` / `CheckConnectionHealthAsync` in the same file. The ordinary
`Openconnection()` that everything actually calls has no resilience at all.

**Even there, Polly never fires.** `OpenConnectionResilient` wraps `Openconnection()`:

```csharp
// Resilience.cs:249
return ResilientPipeline.Execute(() => { return Openconnection(); });
```

but `RDBDataConnection.OpenConn()` catches every exception internally and *returns* a
`ConnectionState` (`RDBDataConnection.cs:205-211`). There is no result-based predicate configured, so
Polly only ever observes a successful return. Retry never retries and the circuit breaker never
counts a failure. The `"Failed to open connection after {MaxRetryAttempts} retries"` messages at
`:254` and `:284` are unreachable for genuine connection failures.

### Configuration problems, if it is wired up

**Transient classification is effectively "everything".**

```csharp
// Resilience.cs:207-225
if (exceptionType.Contains("timeout") || exceptionType.Contains("sqlexception")
                                      || exceptionType.Contains("dbexception"))
{
    if (ex is DbException dbEx)
    {
        var errorCode = dbEx.ErrorCode;
        if (errorCode == -2 || errorCode == 4060 || errorCode == 40197 || ...)
            return true;
    }
    return true;      // ← unconditional
}
```

The `return true` at the end makes the error-code table above it decorative, so primary-key
violations, syntax errors and permission-denied are all classified transient and retried three times
with 1/2/4-second delays. Separately, `dbEx.ErrorCode` is `Exception.HResult`, not
`SqlException.Number`, so those Azure SQL error *numbers* can never match. And the type-name matching
is arbitrary across providers: `MySqlException` contains `sqlexception` and matches;
`OracleException` and `NpgsqlException` match nothing. The message-substring checks at `:187-205`
(`"timeout"`, `"deadlock"`, `"network"`) are locale-dependent and fail entirely against a localised
server. `:228-229` recurses into `InnerException` with no depth cap.

**The pipeline order is inverted.**

```csharp
// Resilience.cs:165-168
return new ResiliencePipelineBuilder()
    .AddPipeline(CircuitBreakerPipeline)
    .AddPipeline(RetryPipeline)
    .Build();
```

Polly executes in registration order, so the breaker is *outer* and retry is *inner*: one `Execute`
performs up to four attempts but registers as a single outcome. With `MinimumThroughput = 5` over a
30-second window, the breaker will essentially never open. Convention is retry outer, breaker inner.

**`CircuitBreakerThreshold` is not what its doc says.** The XML comment calls it "consecutive
failures"; in Polly v8 it maps to `MinimumThroughput`, the minimum number of actions in the sampling
window before the failure *ratio* is evaluated at all.

**Setters stop working after first use.** `MaxRetryAttempts`, `RetryBaseDelay`,
`CircuitBreakerThreshold` and `CircuitBreakerDuration` are captured into the cached pipelines on
first access and never re-read. Changing them requires `ResetResiliencePolicies()`, which nothing
documents — and which nulls the fields with no synchronisation while another thread may be
mid-`Execute`.

**`ResilientPipeline` rebuilds on every read** (`:158-170`), allocating a builder and wrapping two
pipelines per call. The inner pipelines are field-cached, so breaker *state* survives; the wrapper is
not. Both lazy initialisers (`:76`, `:117`) are unsynchronised, so two threads can build two breaker
instances and split the failure state between them.

**Retries and non-idempotent writes.** Nothing writes through the pipeline today, so there is no
duplicate-write bug at present. But `IsTransientException` treats a bare `"timeout"` as transient,
and a client-side command timeout on an INSERT very often means the statement is still running or has
already committed. `MaxRetryAttempts`, `EnableResilience` and both `OpenConnectionResilient*` methods
are `public virtual`, so drivers can and will reach for them. Do not wrap a write in this pipeline
without first fixing the classifier.

### Health checks

`GetHealthCheckQuery` (`:396-407`) maps SQL Server / MySQL / PostgreSQL / SQLite → `SELECT 1`,
Oracle → `SELECT 1 FROM DUAL`, DB2 → `SELECT 1 FROM SYSIBM.SYSDUMMY1`, and defaults to `SELECT 1`.
That default is wrong for engines requiring a FROM — **Firebird** (`RDB$DATABASE`), **Hana**
(`DUMMY`) and **Teradata**, all of which ship as drivers in this repository.

`CheckConnectionHealth` reaches `GetDataCommand()`, which returns `null` on a closed connection, so
the NRE is caught at `:331` and reported as a connectivity failure. The outcome is right; the log
line is misleading. Neither health-check method sets `ErrorObject.Flag`.

---

## Decision to make

Both subsystems cost roughly 700 lines and currently deliver nothing. For each, pick one:

- **Wire it up** — and fix the defects above first, because the resilience classifier in particular
  is dangerous the moment a write path uses it.
- **Delete it** — and keep `InvalidateEntityCache` only if `EntityStructureCache` gains real
  invalidation, which is the caching gap that actually costs correctness today.

Whichever way, remove the misleading "Cache invalidated" log line.
