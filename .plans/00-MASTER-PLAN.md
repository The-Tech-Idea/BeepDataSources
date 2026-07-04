# Master plan — Beep DataSources HTML help

**Goal:** Static HTML help for the Beep DataSources repository, grounded in **BeepDM** `.cursor` skills, delivered in phases so each **IDataSource** family can be documented one-by-one without blocking others.

## Principles

- **Platform first:** Phases 01–02 establish the Help shell and how datasources plug into `IDMEEditor`, `ConfigEditor`, and `IDataSource`.
- **Contract before implementations:** Readers understand `ConnectionProperties`, connection lifecycle, and `IDataSource` before reading provider-specific pages.
- **One phase doc = one execution unit:** Each `phase-NN-*.md` lists target files, checklists, and verification.
- **Skills are source of truth:** Summaries in HTML point to `BeepDM/.cursor/<topic>/` for depth.

## BeepDM skill map (authoritative references)

| Topic | BeepDM path |
|--------|-------------|
| Editor & orchestration | `.cursor/beepdm/` |
| App initialization | `.cursor/beepservice/`, `.cursor/beepserviceregistration/` |
| Persisted config | `.cursor/configeditor/` |
| Connection lifecycle | `.cursor/connection/` |
| Connection definitions | `.cursor/connectionproperties/` |
| IDataSource contract | `.cursor/idatasource/` |
| Local file DB | `.cursor/localdb/` |
| In-memory engines | `.cursor/inmemorydb/` |
| Universal / RDBMS helpers | `.cursor/universal-*`, `.cursor/rdbms-*` (when documenting SQL providers) |

## Phase sequence

| # | Name | Output (Help) | Status |
|---|------|----------------|--------|
| 01 | HTML framework | `index.html`, `getting-started.html`, `navigation.js`, `sphinx-style.css`, `roadmap.html` | Done |
| 02 | BeepDM platform | `platform-*.html` (six topics; each links `phased-implementations.html` and/or `impl-connectors.html#flagship-provider-pages` where useful) | Summaries shipped |
| 03 | Local & in-memory | `impl-local-inmemory.html`, `providers/sqlite|litedb|duckdb.html` | Done |
| 04 | RDBMS | `impl-rdbms.html` + SQL cores (SQL Server, PostgreSQL, MySQL, Oracle shipped; more incremental) | Done (first wave) |
| 05 | NoSQL / document / graph | `impl-nosql.html` + MongoDB, Redis, RavenDB, CouchDB, InfluxDB (LiteDB linked) | Done (first wave) |
| 06 | Cloud & analytics | `impl-cloud-analytics.html` + BigQuery, Snowflake, Spanner, Kusto, Presto | Done (first wave) |
| 07 | Messaging & vector | `impl-messaging-vector.html` + messaging & vector provider pages | Done (first wave) |
| 08 | REST connectors | `impl-connectors.html` + category hubs (`connectors-*.html`); eight hubs with flagship rows link `#flagship-provider-pages` | Done (first wave + hub notes) |
| 09 | Flagship connectors | `Help/providers/conn-*.html` (13 vendors); master table `impl-connectors.html#flagship-provider-pages` (see phase-09) | Done (waves A–D) |

> **Note:** Phases 01–09 above are the **HTML help** track. Phase 10 below is a **code implementation** track — different deliverable, tracked here so it's discoverable.

| # | Name | Output (Code) | Status |
|---|------|----------------|--------|
| 10 | Schema migration providers | `ISchemaMigrationProvider` resolved via a 3-tier registry (exact type → category fallback → null) so `MigrationManager` migrates **all 157** data-source types. ~25 colocated provider files (NoSQL/vector/read-only-file/cloud-native) + category fallbacks cover the other ~132. Contracts in BeepDM; providers colocated here. See `phase-10-schema-migration-providers.md`. | Planned |
| 11 | Local KV stores | **Code (not help):** Four widely-used embedded KV stores (RocksDB, LevelDB, LMDB, NitriteDB) + new `KVStore` category. BeepDM enum additions shipped in 3.1.0. See `phase-11-local-kv-store.md`; per-DB plans in `databases/`. | Planned |

## Verification (global)

- [ ] Open `Help/index.html` locally; sidebar expands; theme toggle works.
- [ ] Every new HTML page appears in `navigation.js` with a unique `activeId`. **Automated basename check:** from repo root, `python Help/tools/check-nav-mapping.py` (reports mismatches between `Help/**/*.html` and `createNavigationMapping()` keys).
- [ ] Phase doc updated with **Completed** checkboxes when its Help pages are done.
- [ ] No broken relative links between Help pages. **Automated:** `python Help/tools/check-help-links.py` from repo root.
- [x] Flagship REST index discoverable: `impl-connectors.html#flagship-provider-pages`, `navigation.js` sidebar entry **Flagship conn-* index (table)**, and cross-links from platform + family overviews stay consistent (phase 09 + cross-discovery pass).

## Repository layout (for authors)

```
BeepDataSources/
├── Connectors/           # Phase 08 — REST/SaaS by category
├── DataSourcesPlugins/   # Phase 04 — shared RDBMS plugin
├── DataSourcesPluginsCore/  # Phases 03–06 — per-engine projects
├── InMemoryDB/           # Phase 03 — e.g. DuckDB
├── Messaging/            # Phase 07
├── VectorDatabase/       # Phase 07
├── Help/                 # Static HTML
└── .plans/               # This folder
```
