# Phase 05 — NoSQL, document, and graph cores

## Objective

Document non-relational `DataSourcesPluginsCore` projects: document stores, key-value, time-series where applicable.

## BeepDM `.cursor` sources

- `BeepDM/.cursor/idatasource/SKILL.md`
- `BeepDM/.cursor/universal-general-helper/SKILL.md` (if used)

## Repo targets (examples)

- `MongoDBDataSourceCore/`, `RedisDataSourceCore/`, `RavenDBDataSourceCore/`, `CouchDBDataSourceCore/`, `InfluxDBDataSourceCore/`, etc.

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-nosql.html` | Category overview, capability expectations vs RDBMS; **Contrast: REST / SaaS** links connectors overview + flagship anchor — **shipped** |
| `Help/providers/mongodb.html` | **shipped** |
| `Help/providers/redis.html` | **shipped** |
| `Help/providers/ravendb.html` | **shipped** |
| `Help/providers/couchdb.html` | **shipped** |
| `Help/providers/influxdb.html` | **shipped** |
| … | Add pages for other NOSQL cores incrementally |

## TODO checklist

- [x] Group providers by operational model (document, KV, column, TSDB) — see `impl-nosql.html` table
- [x] Call out metadata/CRUD limitations vs full SQL sources — overview + per-provider “Capabilities”

## Verification

- [x] Each provider page points readers to the matching `*DataSource.cs` for full `IDataSource` coverage
- [ ] Optional: operation matrix (CRUD, schema list) per provider when stabilized

## Dependency

Phase 02 complete.
