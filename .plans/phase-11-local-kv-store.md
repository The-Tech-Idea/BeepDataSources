# Phase 11 — Embedded / Local Key-Value Stores

**Status:** 3 of 4 complete · **Last updated:** 2026-07-03 · **Owner:** BeepDM + BeepDataSources

> Adds three widely-used embedded KV stores to Beep: **RocksDB**, **LevelDB**, **LMDB**.
> Each becomes a real `IDataSource` against its native API + a colocated
> Tier-1 `ISchemaMigrationProvider`. Fills the `KVStore` category alongside existing
> file/SQL/vector data sources. The fourth planned entry, **NoSQLite (VoidNone)**, was
> deferred due to SDK API inconsistencies — see `.plans/databases/nosqlite-deferred.md`.

## Why these three

Selection criteria: **widely used in production** + **mature .NET wrapper available** +
**not already in Beep**.

| DB | Why included | .NET package | Existing in Beep? |
|---|---|---|---|
| **RocksDB** | Most popular modern embedded KV; powers MyRocks, Kafka, TiKV | `RocksDbSharp` + `RocksDbNative` | ❌ |
| **LevelDB** | Most deployments (Chrome, Bitcoin Core); simplest KV | `LevelDB.Standard` | ❌ |
| **LMDB** | Fastest transactional embedded KV; OpenLDAP, Monero | `LightningDB` | ❌ |
| ~~NoSQLite~~ | *(deferred — see `nosqlite-deferred.md`)* | ~~`NoSQLite` (VoidNone)~~ | — |

> **Originally planned:** NitriteDB (mzheravin/nitrite-db). Withdrawn from NuGet.
> **NoSQLite** was the second attempt; deferred due to SDK API quality.
> **RocksDB / LevelDB / LMDB** are the three that actually shipped.

Per-DB plans live at `.plans/databases/<name>.md`:
- [`.plans/databases/rocksdb.md`](databases/rocksdb.md) — ✅ built
- [`.plans/databases/leveldb.md`](databases/leveldb.md) — ✅ built
- [`.plans/databases/lmdb.md`](databases/lmdb.md) — ✅ built
- [`.plans/databases/nosqlite-deferred.md`](databases/nosqlite-deferred.md) — ❌ deferred

## BeepDM contract additions (shipped 3.1.0)

`BeepDM/DataManagementModelsStandard/Enums/Enums.cs`:
- `DataSourceType.RocksDB` (= 56) — appended under "Embedded / Local Key-Value Stores (Phase 11)"
- `DataSourceType.LevelDB` (= 57)
- `DataSourceType.LMDB` (= 58)
- ~~`DataSourceType.NoSQLite`~~ (removed when project deferred)
- `DatasourceCategory.KVStore` (= 19) — appended at end of `DatasourceCategory`

BeepDM 3.1.0 republished to the local NuGet feed at `C:\Users\f_ald\source\repos\LocalNugetFiles\`.

## Folder layout (per database)

```
DataSourcesPluginsCore/<Name>DataSourceCore/
├── <Name>DataSourceCore.csproj         # net8.0/9.0/10.0
├── GlobalUsings.cs
├── <Name>DataSource.cs                 # IDataSource + ILocalDB (full CRUD + file lifecycle)
└── <Name>MigrationProvider.cs          # [SchemaMigrationProvider(<Type>, KVStore)]
```

(No custom `*ConnectionProperties.cs` / `*DataConnection.cs` needed for KV stores —
they use the base `ConnectionProperties` from `BeepDM 3.1.0` via `FilePath` + `FileName`.)

## ILocalDB implementation (added round 2)

Each shipped `*DataSource` implements `ILocalDB` for file lifecycle parity with SQLite/LiteDB:

| Method | Behavior |
|---|---|
| `bool CanCreateLocal { get; set; }` | `true` |
| `bool InMemory { get; set; }` | `false` (RocksDB/LevelDB/LMDB have no native in-memory; uses `Path.GetTempPath()` when true) |
| `string Extension { get; set; }` | `.rocksdb` / `.leveldb` / `.lmdb` |
| `bool CreateDB()` | Resolves path from `ConnectionProperties.FilePath/FileName`, opens with create-if-missing |
| `bool CreateDB(bool inMemory)` | Toggles `InMemory`; uses `Path.GetTempPath()` if true |
| `bool CreateDB(string filepathandname)` | Closes existing handle, sets path, opens |
| `bool DeleteDB()` | Closes handle, recursive `Directory.Delete` on the DB directory |
| `bool CopyDB(string destDbName, string desPath)` | Closes handle, recursive file copy |
| `IErrorsInfo DropEntity(string entityName)` | Delegates to `IDataSource.DeleteEntity` |

RocksDB / LevelDB / LMDB each use a directory (not a single file), so `CopyDB` and `DeleteDB`
do recursive directory operations. The `Extension` property is used for `CreateDB` defaulting.

## Capabilities matrix (colocated providers)

| Capability | RocksDB | LevelDB | LMDB | NoSQLite |
|---|---|---|---|---|
| CreateEntity | ✅ (CF) | ✅ (prefix marker) | ✅ (named DB) | (deferred) |
| DropEntity | ✅ | ✅ (prefix wipe) | ✅ (DropDatabase) | — |
| AddColumn | ✅ no-op | ✅ no-op | ✅ no-op | — |
| TruncateEntity | ✅ (iterate-remove) | ✅ (prefix wipe) | ✅ (TruncateDatabase) | — |
| RenameEntity | ✅ (CF rename + copy) | ✅ (prefix rename + copy) | ✅ (Drop + recreate + copy) | — |
| CreateIndex | ❌ | ❌ | ❌ | — |
| DropIndex | ❌ | ❌ | ❌ | — |
| TransactionalDdl | ❌ | ❌ (no TX at all) | ✅ (WriteBatch + Commit) | — |

Alter column / Drop column / Rename column / Add FK / Drop FK remain `Unsupported` for all
three shipped providers — KV stores have no column-level DDL or FK.

## Build artefacts

| DB | csproj | Built | Nupkg |
|---|---|---|---|
| RocksDB | `DataSourcesPluginsCore/RocksDBDataSourceCore/RocksDBDataSourceCore.csproj` (1.0.0) | ✅ 0 errors | `TheTechIdea.Beep.RocksDBDataSourceCore.1.0.0.nupkg` |
| LevelDB | `DataSourcesPluginsCore/LevelDBDataSourceCore/LevelDBDataSourceCore.csproj` (1.0.0) | ✅ 0 errors | `TheTechIdea.Beep.LevelDBDataSourceCore.1.0.0.nupkg` |
| LMDB   | `DataSourcesPluginsCore/LMDBDataSourceCore/LMDBDataSourceCore.csproj` (1.0.0)       | ✅ 0 errors | `TheTechIdea.Beep.LMDBDataSourceCore.1.0.0.nupkg` |

All three nupkgs copied to `LocalNugetFiles/DataSources/`.

## Master tracker

See `MASTER-TODO-TRACKER.md` — phase-11 row tracks the database scaffolds.

## Risks / mitigations

- **Native runtime distribution** (RocksDB, LevelDB, LMDB): validate `LevelDB.Standard`,
  `RocksDbSharp`, `LightningDB` ship runtimes for win-x64, osx-x64, linux-x64.
- **Single-writer model** (LMDB): serialize writes via internal lock; documented in `LMDBDataSource`.
- **NoSQLite SDK quality**: deferred rather than shipped broken — see `nosqlite-deferred.md`.
- **BeepDM 3.1.0 enum consumers**: every other consumer of `DataSourceType` /
  `DatasourceCategory` compiles without change because the new entries are appended.

## Success criteria

- [x] RocksDB project compiles clean against BeepDM 3.1.0.
- [x] LevelDB project compiles clean against BeepDM 3.1.0.
- [x] LMDB project compiles clean against BeepDM 3.1.0.
- [ ] NoSQLite project — deferred; revival criteria documented.
- [x] Each shipped provider's `Capabilities` matches the matrix above (no false `Supports...` claims).
- [ ] 156-project build sweep — confirm no regressions.