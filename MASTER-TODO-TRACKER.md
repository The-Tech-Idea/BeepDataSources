# MASTER-TODO — Beep DataSources documentation

Single tracker for phased HTML help and provider docs. Detailed checklists per phase live in **`.plans/`**.

## Phases

| Phase | Document | Help deliverables | Status |
|------|-----------|-------------------|--------|
| 01 | `.plans/phase-01-html-framework.md` | `Help/index.html`, `getting-started.html`, `roadmap.html`, `navigation.js`, `sphinx-style.css` | Done |
| 02 | `.plans/phase-02-beepdm-platform.md` | `Help/platform-*.html` (six pages); each links `phased-implementations.html` and/or `impl-connectors.html#flagship-provider-pages` where relevant | Summaries done |
| 03 | `.plans/phase-03-local-inmemory.md` | `impl-local-inmemory.html`, `providers/sqlite|litedb|duckdb.html` | Done |
| 04 | `.plans/phase-04-rdbms.md` | `impl-rdbms.html`, `providers/rdbms-sqlserver|postgresql|mysql|oracle.html` | Done (first wave; add more engines as needed) |
| 05 | `.plans/phase-05-nosql-doc-graph.md` | `impl-nosql.html`, `providers/mongodb|redis|ravendb|couchdb|influxdb.html` | Done (first wave; more NOSQL cores incremental) |
| 06 | `.plans/phase-06-cloud-analytics.md` | `impl-cloud-analytics.html`, `providers/cloud-bigquery|snowflake|spanner|kusto|presto.html` | Done (first wave; more CLOUD cores incremental) |
| 07 | `.plans/phase-07-messaging-vector.md` | `impl-messaging-vector.html` (incl. **Contrast: product REST** + flagship anchor), `providers/msg-*`, `providers/vector-*` (see phase doc) | Done (first wave) |
| 08 | `.plans/phase-08-connectors-rest.md` | `impl-connectors.html`, `connectors-*.html` (16 categories), `navigation.js`; eight hubs with flagship `conn-*` rows link `impl-connectors.html#flagship-provider-pages` after the category table | Done (first wave + hub notes) |
| 09 | `.plans/phase-09-connectors-flagship.md` | `providers/conn-*.html` (13 flagships) with **source-derived command + model tables**; `impl-connectors.html#flagship-provider-pages` index table (hub path + `DataSourceType`); skills section on overview links in-page table; `navigation.js` (sidebar **Flagship conn-* index**, hash-aware active state); each `conn-*` **Also read** links the flagship table; `EXECUTION_SUMMARY.md` + `repo-layout` pointers; cross-links from messaging overview, `platform-beepdm`, `platform-configeditor`, and other family pages; provider section order **Source → Commands → Models → Connection** where applicable | Done (waves A–D + polish) |

## Next actions

1. Optionally add more `Help/providers/conn-*.html` pages (e.g. Stripe when implemented, Trello) per phase-09 follow-ups.
2. Mark phase rows complete in this file when each phase’s verification checklist in `.plans` is satisfied.
3. When touching Help, run `python Help/tools/verify-help.py` from the repo root (or run `check-nav-mapping.py` and `check-help-links.py` separately).

## BeepDM skill paths (reference)

`BeepDM/.cursor/` — `beepdm`, `beepservice`, `configeditor`, `connection`, `connectionproperties`, `idatasource`, `localdb`, `inmemorydb`, `universal-*`, `rdbms-*`, etc.
