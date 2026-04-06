# Phase 06 — Cloud & analytics cores

## Objective

Document cloud warehouses, managed SQL, and analytics connectors under `DataSourcesPluginsCore/` (e.g. BigQuery, Snowflake, Databricks, Spanner).

## BeepDM `.cursor` sources

- `BeepDM/.cursor/connectionproperties/SKILL.md` (OAuth, keys, warehouse IDs)
- `BeepDM/.cursor/idatasource/SKILL.md`

## Repo targets (examples)

- `GoogleBigQuery/`, `SnowFlakeDataSource/`, `DataBricksDataSource/`, `SpannerDataSourceCore/`, `AzureCloudDataSourceCore/`, `AmazonCloudDatasourceCore/`, etc.

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-cloud-analytics.html` | Auth patterns, billing/latency cautions, staging, vendor auth links; **Contrast: Connectors REST** vs CLOUD warehouses + flagship anchor — **shipped** |
| `Help/providers/cloud-bigquery.html` | **shipped** |
| `Help/providers/cloud-snowflake.html` | **shipped** |
| `Help/providers/cloud-spanner.html` | **shipped** |
| `Help/providers/cloud-kusto.html` | **shipped** (notes `DataSourceType.Kudosity` in code) |
| `Help/providers/cloud-presto.html` | **shipped** |
| … | Additional `cloud-*.html` incremental (Rockset, Firebolt, …) |

## TODO checklist

- [x] Separate **authentication** from **warehouse** configuration in examples (see overview)
- [x] Never paste live secrets; use placeholders only

## Verification

- [x] Links to vendor docs for auth rotation where relevant (overview table)
- [ ] Optional: Databricks page when `DataBricksDataSource` sources are present in repo

## Dependency

Phase 02 complete.
