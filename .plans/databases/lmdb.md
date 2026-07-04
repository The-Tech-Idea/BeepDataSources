# LMDB Data Source — Plan

**Status:** Planned · **Target:** `DataSourcesPluginsCore/LMDBDataSourceCore/` · **Phase:** 11

## What it is

[LMDB (Lightning Memory-Mapped Database)](https://www.symas.com/lmdb) is a transactional
key-value store with **memory-mapped I/O**, **zero-copy reads**, and **single-writer /
many-reader** semantics. The fastest embedded transactional KV on the planet. Pure C, ~50 KB
of source.

**Widely used:** OpenLDAP (default backend), Monero / Zcash (blockchain state),
Bitcoin Core `coins` cache (pre-LevelDB), various DNS servers, libfuse, MariaDB
(MDBX, the fork), Open vSwitch, etc.

## .NET package

- `LightningDB` (NuGet) — `lindexi` author's mature wrapper. .NET Standard 2.0+.
- Alternative: `lmdb-net` (older, less maintained).

## NuGet package this project produces

- `TheTechIdea.Beep.LMDBDataSourceCore 1.0.0`

## Target folder layout

```
DataSourcesPluginsCore/LMDBDataSourceCore/
├── LMDBDataSourceCore.csproj             # net8.0/9.0/10.0
├── GlobalUsings.cs
├── LMDBDataSource.cs                     # IDataSource
├── LMDBConnectionProperties.cs
├── LMDBDataConnection.cs
├── LMDBMigrationProvider.cs              # [SchemaMigrationProvider(LMDB, KVStore)]
└── plans/lmdb-datasource-enhancement-plan.md
```

## Data model mapping

LMDB exposes **named databases** within a single env file (similar to column families). We use:

```
DB = entity name (each entity is its own named DB inside the env)
key = primary key (string)
value = JSON-serialized POCO
```

`GetEntitesList()` calls `env.ListDatabases()` (LightningDB native).

## API surface

| IDataSource member | Implementation |
|---|---|
| `Openconnection()` | `Env = new LightningEnvironment(path); Env.Open(); Env.MaxDatabases = N;` |
| `Closeconnection()` | `Env?.Dispose()` |
| `GetEntitesList()` | `env.ListDatabases()` |
| `CheckEntityExist(name)` | `env.OpenDatabase(name)` in try/catch |
| `CreateEntityAs(structure)` | `env.CreateDatabase(name, Configuration)` |
| `DeleteEntity(name, null)` | `env.OpenDatabase(name).Drop()` |
| `GetEntity(name, filter)` | `using var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly); var db = tx.OpenDatabase(name); foreach (var kv in tx.CreateCursor(db))` |
| `GetEntity(name, filter, page, size)` | Iterate cursor with skip/count |
| `GetEntityAsync` | Async-friendly with `Task.Run` (LMDB is sync) |
| `InsertEntity` / `UpdateEntity` | Read-write TX → `tx.Put(db, key, value)` |
| `UpdateEntities` | Bulk put within one TX |
| `ExecuteSql` | **Unsupported** |
| `RunQuery` | `GET <key>` / `SCAN <db>` / `COUNT <db>` |
| `GetScalar` | `COUNT(<db>)` |
| `GetEntityStructure` | Sample 100 entries → field inference |
| `BeginTransaction` / `Commit` / `EndTransaction` | `env.BeginTransaction()` (writer) + `tx.Commit()` + `tx.Abort()` |
| `RunScript` | Not supported |

## Schema migration capabilities

| Capability | Support |
|---|---|
| `SupportsCreateEntity` | true (named DB) |
| `SupportsDropEntity` | true (Drop database) |
| `SupportsAddColumn` | true (no-op) |
| `SupportsTruncateEntity` | true (Drop + Create) |
| `SupportsRenameEntity` | true (Drop old + Create new + iterate copy) |
| `SupportsCreateIndex` | false (no built-in secondary index) |
| `SupportsDropIndex` | false |
| `SupportsTransactionalDdl` | **true** (LMDB supports MVCC-like semantics) |

Note: DDL happens within transactions when wrapping the underlying ops in TX. The provider
honestly reports this capability.

## Connection properties (typed, beyond the 46 base)

```
LMDBPath                 : string  (env folder)
MaxDatabases             : int
MapSizeBytes             : long    (max env size, default 1 GB)
SubDirectory             : bool    (use sub-DB per entity vs flat files)
Sync                     : bool    (fsync on commit)
ReadOnly                 : bool
MaxReaders               : int     (default 126)
```

## BeepDM enum additions (already shipped in 3.1.0)

```
DataSourceType.LMDB
DatasourceCategory.KVStore
```

## Implementation status

- [x] dotnet build DataSourcesPluginsCore/LMDBDataSourceCore/LMDBDataSourceCore.csproj -c Debug → 0 errors
- [x] Round-trip: open env → create DB → put → cursor-iterate → drop DB → close env
- [x] Transaction commit/rollback working
- [x] Package TheTechIdea.Beep.LMDBDataSourceCore.1.0.1.nupkg produced and copied to feed
- [x] Provider discovered by scanning sweep
- [x] SupportsTransactionalDdl = true reflected in capabilities
- [x] ILocalDB file lifecycle implemented (CreateDB / DeleteDB / CopyDB / DropEntity / Extension)## Risks / open questions

- **Single-writer** semantics — only one writer transaction at a time. The data source
  must serialize writes internally (use a lock).
- **Map size** must be set up-front; exceeding it raises `MDB_MAP_FULL`. Document and
  document well.
- Memory-mapped I/O means the env file size on disk ≈ map size. Don't default map size
  to 1 TB.
- `LightningEnvironment` is `IDisposable`; careful with TX/Cursor lifetimes.