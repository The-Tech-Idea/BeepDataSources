# RocksDB Data Source — Plan

**Status:** Planned · **Target:** `DataSourcesPluginsCore/RocksDBDataSourceCore/` · **Phase:** 11

## What it is

[Facebook RocksDB](https://rocksdb.org/) is an embeddable persistent key-value store for
fast storage on SSD/RAM. Originally forked from Google's LevelDB with:
- Multi-thread compactions
- Column families (logical key namespaces sharing one DB)
- Backups, checkpoints, TTL
- Transactions (optimistic + pessimistic)
- Secondary indexes, merge operators, Bloom filters
- Compression (Snappy, Zstd, LZ4)

**Widely used:** MyRocks (MariaDB/MySQL), Kafka, Flink, TiKV / TiDB / CockroachDB, Druid,
Pulsar, NFS-Ganesha, Ceph BlueStore, Snowflake metadata layer.

## .NET package

- `RocksDbSharp` (NuGet) — P/Invoke wrapper around `librocksdb`. Mature, .NET Standard 2.0+.
- Alternative: `RocksDB` (Microsoft port for Windows). Use RocksDbSharp for portability.

## NuGet package this project produces

- `TheTechIdea.Beep.RocksDBDataSourceCore 1.0.0`

## Target folder layout

```
DataSourcesPluginsCore/RocksDBDataSourceCore/
├── RocksDBDataSourceCore.csproj          # net8.0/9.0/10.0, refs RocksDbSharp + BeepDM 3.1.0
├── GlobalUsings.cs
├── RocksDBDataSource.cs                  # IDataSource (open/close, CRUD, scan)
├── RocksDBConnectionProperties.cs        # 46-member IConnectionProperties + typed props
├── RocksDBDataConnection.cs              # 2-arg ctor + ReplaceValueFromConnectionString
├── RocksDBMigrationProvider.cs           # [SchemaMigrationProvider(RocksDB, KVStore)]
└── plans/rocksdb-datasource-enhancement-plan.md
```

## Data model mapping

RocksDB is a byte-key / byte-value store with no built-in document structure. We expose
two storage strategies:

1. **Column-family = entity** (default). Each entity lives in its own column family.
   Keys are stable hashes of the document's primary key; values are JSON-serialized POCOs.
2. **Single-column family with key prefixes**. One CF, prefix = entity name + primary key.
   Pick this when you want one physical DB across many entities.

`EntityStructure.Fields` is populated from a sample read on first `GetEntityStructure` call
(similar to LiteDB introspection). Schemaless per CF — AddColumn is a no-op.

## API surface to implement

| IDataSource member | Implementation |
|---|---|
| `Openconnection()` | `Db = RocksDb.Open(new DbOptions(), path)` |
| `Closeconnection()` | `Db?.Dispose()` |
| `GetEntitesList()` | `Db.ListColumnFamilies()` |
| `CheckEntityExist(name)` | `Db.TryGetColumnFamilyMetaData(name, out _)` |
| `CreateEntityAs(structure)` | `Db.CreateColumnFamily(options, name)` |
| `DeleteEntity(name, null)` | `Db.DropColumnFamily(name)` |
| `GetEntity(name, filter)` | Iterator scan over `Db.NewIterator(cfh)` with filter expressions |
| `GetEntity(name, filter, page, size)` | Same with `seek()` + count limit |
| `GetEntityAsync` | `Task.FromResult` over the sync iterator |
| `InsertEntity` / `UpdateEntity` | Serialize POCO/Dictionary to JSON, write with `Db.Put(cfh, key, json)` |
| `UpdateEntities` | Bulk write loop with `IProgress` |
| `ExecuteSql` | **Unsupported** — return honest failure (RocksDB has no SQL) |
| `RunQuery` | Parses very small subset: `GET <key>` / `SCAN <prefix>` |
| `GetScalar` | Returns count for `COUNT(<entity>)` |
| `GetEntityStructure` | Sample read + JSON-shape introspection |
| `BeginTransaction` / `Commit` / `EndTransaction` | `Db.BeginTransaction(...)` (WriteBatch + `Transaction.Commit()`) |
| `RunScript` | Not supported (no ETL scripts) |

## Schema migration capabilities (Tier-1 colocated provider)

| Capability | Support | Implementation |
|---|---|---|
| `SupportsCreateEntity` | true | `CreateColumnFamily` |
| `SupportsDropEntity` | true | `DropColumnFamily` |
| `SupportsAddColumn` | true (no-op) | RocksDB is schemaless |
| `SupportsTruncateEntity` | true | `Db.DropColumnFamily` + `Db.CreateColumnFamily` (or range-delete) |
| `SupportsRenameEntity` | true | `Db.DropColumnFamily(old)` + `Db.CreateColumnFamily(new)` + iterate-copy |
| `SupportsCreateIndex` | false | No first-class secondary index |
| `SupportsDropIndex` | false | — |
| `SupportsTransactionalDdl` | false | CF ops are metadata-only, not transactional |

The other 7 capabilities remain `Unsupported` honestly.

## Connection properties (typed, beyond the 46 base)

```
RocksDbPath           : string  (folder for the DB)
ColumnFamilies        : string  (CSV, hint for default CF list)
Compression           : string  (None / Snappy / Zstd / LZ4)
MaxOpenFiles          : int
WriteBufferSizeMb     : int
MaxWriteBufferNumber  : int
EnableStatistics      : bool
ParanoidChecks        : bool
```

## BeepDM enum additions (already shipped in 3.1.0)

```
DataSourceType.RocksDB      // appended under "Embedded / Local Key-Value Stores (Phase 11)"
DatasourceCategory.KVStore  // appended at end of DatasourceCategory
```

## Files to touch

- `BeepDM/DataManagementModelsStandard/Enums/Enums.cs` (✅ done — RocksDB + KVStore)
- `DataSourcesPluginsCore/RocksDBDataSourceCore/` (new folder)
- `MASTER-TODO-TRACKER.md` (add Phase 11 row)
- `.plans/phase-11-local-kv-store.md` (master doc)
- `.plans/databases/rocksdb.md` (this file)

## Implementation status

- [x] dotnet build DataSourcesPluginsCore/RocksDBDataSourceCore/RocksDBDataSourceCore.csproj -c Debug → 0 errors
- [x] Round-trip smoke test: open → create CF → put → get → delete → close
- [x] Package TheTechIdea.Beep.RocksDBDataSourceCore.1.0.1.nupkg produced and copied to feed
- [x] AssemblyScanningAssistant discovers RocksDBMigrationProvider automatically (proved by sweep)
- [x] Migration provider capabilities match the table above
- [x] ILocalDB file lifecycle implemented (CreateDB / DeleteDB / CopyDB / DropEntity / Extension)
- [ ] Help page stub Help/providers/rocksdb.html links back to this plan## Risks / open questions

- RocksDbSharp native binary distribution on Windows / macOS / Linux — RocksDbSharp ships
  runtimes for all three; validate in CI.
- Filter / query semantics on KV are not SQL-like; document the prefix-scan + JSON-filter
  approach in `RunQuery` clearly.
- C# nullable warning surface — RocksDbSharp API is not annotated; suppress per-call as needed.