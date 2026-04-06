# Phase 03 — Local & in-memory datasources

## Objective

Document file-backed and in-process engines: `ILocalDB`, `IInMemoryDB`, and representative projects under `DataSourcesPluginsCore/` and `InMemoryDB/`.

## BeepDM `.cursor` sources

- `BeepDM/.cursor/localdb/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/inmemorydb/SKILL.md` (+ `reference.md`)
- `BeepDM/.cursor/idatasource/SKILL.md` (contract refresh)

## Repo targets (examples)

- `DataSourcesPluginsCore/LiteDBDataSourceCore/`
- `DataSourcesPluginsCore/SqliteDatasourceCore/`
- `InMemoryDB/DuckDBDataSourceCore/`
- Other embedded cores as needed (SqlCompact, etc.)

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-local-inmemory.html` | Overview, when to use local vs server, pattern table; note distinguishes SaaS REST (<code>Connectors/</code>) with links to <code>impl-connectors.html#flagship-provider-pages</code> |
| `Help/providers/sqlite.html` | Stub → expand (connection, file path, limitations) |
| `Help/providers/litedb.html` | Stub |
| `Help/providers/duckdb.html` | Inheritance, file readers, **full `CommandAttribute` catalog** (`#duckdb-extended-commands`) from `DuckDbExtendedFunctions.cs` — cross-links `impl-rdbms.html` |

## TODO checklist

- [x] Overview page with decision matrix (local file vs in-memory vs server)
- [x] Per-provider pages: NuGet package name, `DataSourceType`, typical `ConnectionProperties`
- [x] Link phase 02 platform pages for configuration flow

## Verification

- [x] Each documented project name matches actual folder under `DataSourcesPluginsCore` or `InMemoryDB`

## Dependency

Phase 02 complete (platform context assumed understood).
