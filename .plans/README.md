# Beep DataSources — phased documentation plans

This folder drives **HTML help** under `Help/` and incremental **IDataSource** implementation guides.

## How to use

1. Read **`00-MASTER-PLAN.md`** for phase order and status.
2. When working a phase, open the matching **`phase-NN-*.md`** — each file has target HTML paths, checklists, and BeepDM `.cursor` skill references.
3. Authoritative runtime behavior remains in **BeepDM** skills under `BeepDM/.cursor/` (paths referenced in each phase).
4. **REST/SaaS deep dives (phase 09):** thirteen `Help/providers/conn-*.html` pages; one consolidated table with hubs and `DataSourceType` on `Help/impl-connectors.html` — in-browser anchor `#flagship-provider-pages`.
5. **Navigation parity:** after adding Help pages, run `python Help/tools/verify-help.py` from the repo root (nav mapping + `href`/`src=` checks), or the individual scripts under `Help/tools/`.

## Phase index

| Phase | Document | Focus |
|------|----------|--------|
| 01 | [phase-01-html-framework.md](./phase-01-html-framework.md) | Help shell, navigation, conventions |
| 02 | [phase-02-beepdm-platform.md](./phase-02-beepdm-platform.md) | beepdm, beepservice, configeditor, connection, connectionproperties, idatasource |
| 03 | [phase-03-local-inmemory.md](./phase-03-local-inmemory.md) | `ILocalDB`, `IInMemoryDB`, LiteDB, Sqlite, DuckDB, etc. |
| 04 | [phase-04-rdbms.md](./phase-04-rdbms.md) | `DataSourcesPlugins/RDBMSDataSource`, SQL family cores |
| 05 | [phase-05-nosql-doc-graph.md](./phase-05-nosql-doc-graph.md) | Mongo, Redis, Raven, Couch, etc. |
| 06 | [phase-06-cloud-analytics.md](./phase-06-cloud-analytics.md) | BigQuery, Snowflake, Databricks, cloud cores |
| 07 | [phase-07-messaging-vector.md](./phase-07-messaging-vector.md) | Messaging folder, VectorDatabase |
| 08 | [phase-08-connectors-rest.md](./phase-08-connectors-rest.md) | `Connectors/*` categories, Web API pattern |
| 09 | [phase-09-connectors-flagship.md](./phase-09-connectors-flagship.md) | `Help/providers/conn-*` flagship connector pages |

## Master tracker

See **`00-MASTER-PLAN.md`** for cross-phase dependencies and completion criteria.
