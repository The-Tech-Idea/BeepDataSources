# Plans: DuckDB-native `IFileFormatReader` for `FileDataSource`

## Start here

1. **`03-detailed-phased-implementation-plan.md`** — **Full phased plan**: every DuckDB-relevant format (P0→P6), scopes (file vs remote vs lakehouse vs external DB), cross-cutting (glob, compression, nested types), testing and rollout.
2. **`00-duckdb-native-file-readers-catalog.md`** — Compact **format → class → `DataSourceType`** matrix.
3. **`02-phased-roadmap.md`** — Short roadmap (summary of early phases).

## BeepDM reference

- Skill: `BeepDM/.cursor/filedatasource/SKILL.md`
- Checklist: `BeepDM/.cursor/filedatasource/reference.md`

## Code in this repo to align with

- `DuckDbExtendedFunctions.cs` — existing `read_csv_auto`, `read_parquet`, `read_json` usage patterns.
