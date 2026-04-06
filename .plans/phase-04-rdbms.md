# Phase 04 — RDBMS plugin family

## Objective

Document the shared RDBMS plugin and SQL-family cores: `DataSourcesPlugins/RDBMSDataSource` and `DataSourcesPluginsCore/*DataSourceCore` entries that use universal/RDBMS helpers.

## BeepDM `.cursor` sources

- `BeepDM/.cursor/idatasource/SKILL.md`
- `BeepDM/.cursor/universal-helper-factory/SKILL.md`
- `BeepDM/.cursor/universal-rdbms-helper/SKILL.md`
- `BeepDM/.cursor/rdbms-helper-facade/SKILL.md` and related `rdbms-*` skills as needed

## Repo targets

- `DataSourcesPlugins/RDBMSDataSource/` — `RDBSource` partials + `InMemoryRDBSource` (`IInMemoryDB`)
- Cores that inherit **`RDBSource` directly** (typical `DatasourceCategory.RDBMS` or `CLOUD`): `SQlServerDataSourceCore`, `PostgreDataSourceCore`, `MySqlDataSourceCore`, `OracleDataSourceCore`, `SnowFlakeDataSource`, `SpannerDataSourceCore`, `PrestoDatasource`
- Cores on **`InMemoryRDBSource`**: `InMemoryDB/DuckDBDataSourceCore` (`DuckDBDataSource`), `SqliteDatasourceCore` (`SQLiteDataSource` + `ILocalDB`) — Help: `impl-rdbms.html` § InMemoryRDBSource + `providers/duckdb.html` / `sqlite.html`
- Other: `FirebirdDataSourceCore`, etc.

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-rdbms.html` | Architecture: plugin vs core, helper delegation; **Contrast: REST / SaaS** links `impl-connectors.html` + flagship anchor — **shipped** |
| `Help/providers/rdbms-sqlserver.html` | SQL Server — **shipped** |
| `Help/providers/rdbms-postgresql.html` | PostgreSQL — **shipped** |
| `Help/providers/rdbms-mysql.html` | MySQL — **shipped** |
| `Help/providers/rdbms-oracle.html` | Oracle — **shipped** |
| … | Additional engines (Firebird, …) incremental |

## TODO checklist

- [x] Explain `IDataSourceHelper` / `RDBMSHelper` boundary for consumers (`Help/impl-rdbms.html`)
- [x] Document dialect-specific caveats only on provider pages (initial four engines)
- [x] Align naming with actual `DataSourceType` enum usage in BeepDM (`SqlServer`, `Postgre`, `Mysql`, `Oracle`)

## Verification

- [x] Help pages and sidebar entries exist; links use repo-accurate project paths and enum spellings
- [ ] Optional: add copy-paste connection examples once driver metadata is centralized in help

## Dependency

Phases 02–03 recommended (connection + contract understood).
