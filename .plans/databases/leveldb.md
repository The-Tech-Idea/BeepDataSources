# LevelDB Data Source — Plan

**Status:** Planned · **Target:** `DataSourcesPluginsCore/LevelDBDataSourceCore/` · **Phase:** 11

## What it is

[Google LevelDB](https://github.com/google/leveldb) is a fast lightweight embeddable
key-value store. RocksDB's older sibling. Simpler API (no column families, no
transactions, no statistics), pure LSM tree on disk.

**Widely used:** Chrome IndexedDB, Bitcoin Core (chainstate), Riak, Apache Flink state,
Ethereum (legacy), various blockchain indexers.

## .NET package

- `LevelDB.Standard` (NuGet) — .NET Standard 2.0 wrapper around Google's native lib.
- Alternative: `LevelDB.NET` (older) — pick `LevelDB.Standard` for .NET 8/9/10 compatibility.

## NuGet package this project produces

- `TheTechIdea.Beep.LevelDBDataSourceCore 1.0.0`

## Target folder layout

```
DataSourcesPluginsCore/LevelDBDataSourceCore/
├── LevelDBDataSourceCore.csproj          # net8.0/9.0/10.0
├── GlobalUsings.cs
├── LevelDBDataSource.cs                  # IDataSource (open/close, CRUD, scan)
├── LevelDBConnectionProperties.cs
├── LevelDBDataConnection.cs
├── LevelDBMigrationProvider.cs           # [SchemaMigrationProvider(LevelDB, KVStore)]
└── plans/leveldb-datasource-enhancement-plan.md
```

## Data model mapping

LevelDB has no concept of entities — only `byte[]` keys and `byte[]` values. We use a
**key prefix scheme**:

```
key = entityName + "\x00" + primaryKey
value = JSON-serialized POCO
```

The collection of distinct prefixes is the entity list. `GetEntitesList()` returns prefixes
sampled from the iterator. `CreateEntityAs` creates the prefix marker
(`entityName + "\x00\x00__schema__"`) so subsequent `CheckEntityExist` is O(1).

## API surface

| IDataSource member | Implementation |
|---|---|
| `Openconnection()` | `Db = LevelDB.DB.Open(path, options)` |
| `Closeconnection()` | `Db?.Dispose()` |
| `GetEntitesList()` | Scan iterator for `entityName\x00\x00__schema__` markers |
| `CheckEntityExist(name)` | `Db.Contains(prefix + marker)` |
| `CreateEntityAs(structure)` | `Db.Put(prefix + marker, "{}")` |
| `DeleteEntity(name, null)` | Iterate-and-delete on prefix, then remove marker |
| `GetEntity(name, filter)` | Iterate prefix, deserialize JSON, apply filter predicates |
| `GetEntity(name, filter, page, size)` | Iterate + skip/limit |
| `GetEntityAsync` | Sync wrapped in Task |
| `InsertEntity` / `UpdateEntity` | Serialize POCO/Dictionary to JSON, write with `Db.Put(key, json)` |
| `UpdateEntities` | Bulk Put with progress |
| `ExecuteSql` | **Unsupported** |
| `RunQuery` | Tiny parser: `GET <key>` / `SCAN <prefix>` / `COUNT <prefix>` |
| `GetScalar` | `COUNT(<prefix>)` returns number of keys with that prefix |
| `GetEntityStructure` | Sample first 100 entries under prefix → derive fields |
| `BeginTransaction` / `Commit` / `EndTransaction` | **Unsupported** — LevelDB has no transactions; honest stub |
| `RunScript` | Not supported |

## Schema migration capabilities

| Capability | Support |
|---|---|
| `SupportsCreateEntity` | true (prefix marker) |
| `SupportsDropEntity` | true (prefix wipe) |
| `SupportsAddColumn` | true (no-op; schemaless) |
| `SupportsTruncateEntity` | true (prefix wipe) |
| `SupportsRenameEntity` | true (iterate copy under new prefix, then delete old) |
| `SupportsCreateIndex` | false |
| `SupportsDropIndex` | false |
| `SupportsTransactionalDdl` | false |

## Connection properties (typed, beyond the 46 base)

```
LevelDbPath         : string  (folder for the DB)
CreateIfMissing     : bool
Compression         : string  (None / Snappy)
BlockSize           : int     (bytes)
WriteBufferSize     : int
MaxOpenFiles        : int
ParanoidChecks      : bool
```

## BeepDM enum additions (already shipped in 3.1.0)

```
DataSourceType.LevelDB
DatasourceCategory.KVStore
```

## Implementation status

- [x] dotnet build DataSourcesPluginsCore/LevelDBDataSourceCore/LevelDBDataSourceCore.csproj -c Debug → 0 errors
- [x] Round-trip: open → create prefix → put → get → delete-prefix → close
- [x] Package TheTechIdea.Beep.LevelDBDataSourceCore.1.0.1.nupkg produced and copied to feed
- [x] Provider discovered by scanning sweep
- [x] ILocalDB file lifecycle implemented (CreateDB / DeleteDB / CopyDB / DropEntity / Extension)## Risks / open questions

- **No transactions** — `BeginTransaction` is honest failure. Document this loudly in the
  Help page so users don't expect ACID.
- Native lib version drift — LevelDB.Standard bundles a specific version. New releases
  require rebuild of the wrapper.
- Filter expression language is custom (not SQL). Document examples in Help.