# NoSQLite (VoidNone) — DEFERRED

**Status:** Planned → Deferred (SDK quality issue). See `phase-11-local-kv-store.md` for context.

## Why deferred

The `NoSQLite` NuGet package (VoidNone, version 0.1.1) has API inconsistencies that make
clean wiring impractical:

1. **`Document<T>` required members** — `RowId`, `CreationTime`, `LastWriteTime`,
   `Note` are all marked `[RequiredMember]` (C# 11). Object initializer syntax fails to
   compile because these must be set in the constructor or with `[SetsRequiredMembers]`.
2. **`Query<T>.GetValue<T>(...)`** is declared non-generic in the XML view (`object` arg),
   so `GetValue<Dictionary<string, object>>()` does not bind.
3. **`Connection.Close()`** is shadowed by `Database.Close(path)` static — the instance
   close method is not directly callable from this binding surface.
4. **Microsoft.Data.Sqlite dependency** — the package transitively pulls in
   `Microsoft.Data.Sqlite 10.0.1`, which is heavy and doesn't match the "embedded pure-.NET"
   intent. A `net10.0`-specific transitive dep is required at runtime.

## Decision

Three of four Phase 11 KV-store providers (RocksDB, LevelDB, LMDB) shipped successfully.
NoSQLite was dropped to avoid shipping an unreliable scaffold. Re-evaluation can happen
when the SDK reaches 1.0 or when an alternative pure-.NET embedded NoSQL document store
becomes available.

## Alternative considered

- **Realm** (already in `mobile/RealMDataSource`) — covers similar territory.
- **LiteDB** (already in `DataSourcesPluginsCore/LiteDBDataSourceCore`) — already
  supports indexes + ACID + LINQ, similar role.

## If you need to revive NoSQLite later

1. Pin `NoSQLite` to a 1.0+ release where the API stabilizes.
2. Create `Document<Dictionary<string,object>>` with `[SetsRequiredMembers]` ctor.
3. Avoid `Query<T>.GetValue<T>(...)`; iterate via `col.GetById` / collection scan APIs.
4. Use `Connection` interface methods, not instance `Close()`.