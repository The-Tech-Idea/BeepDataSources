# DuckDB-native file readers for `FileDataSource`

## What you asked for

Implement **new `IFileFormatReader` classes** where **all parsing / schema inference** is delegated to **DuckDB’s built-in file readers** (`read_csv_auto`, `read_parquet`, `read_json`, …).  
`FileDataSource` stays unchanged: it resolves a reader by `DataSourceType` via `FileReaderFactory` (see `BeepDM/.cursor/filedatasource/SKILL.md`).

This document is the **master list** of **file formats DuckDB can read** → **concrete reader class** → **DuckDB table function** → **suggested `DataSourceType`**.

> **Version note:** Exact function names/options depend on the **DuckDB engine version** shipped with **DuckDB.NET** (your project uses `DuckDB.NET.*` 1.5.x). Validate each row against [DuckDB docs](https://duckdb.org/docs/) for that version before coding.

---

## Tier 1 — Core tabular formats (implement first)

These are the usual “data files” and match patterns already used in `DuckDbExtendedFunctions.cs`.

| DuckDB read API | Typical extensions | Proposed reader class | Suggested `DataSourceType` |
|-----------------|-------------------|------------------------|----------------------------|
| `read_csv_auto(...)` / `read_csv(...)` | `.csv`, `.txt` | `DuckDbCsvFileReader` | `CSV` |
| `read_csv_auto(..., delim='\\t')` or options | `.tsv`, `.tab` | `DuckDbTsvFileReader` *(or same class + `Configure`)* | `TSV` |
| `read_parquet(...)` / `parquet_scan(...)` | `.parquet`, `.pq` | `DuckDbParquetFileReader` | `Parquet` |
| `read_json(...)` / `read_json_auto(...)` | `.json` (array / objects) | `DuckDbJsonFileReader` | `Json` |
| `read_ndjson(...)` *(or JSON per-line)* | `.ndjson`, `.jsonl` | `DuckDbNdjsonFileReader` | `Json` *or* `Text` *(if you need both JSON and NDJSON at once, add enum value — see below)* |

**Glob / multi-file:** DuckDB supports patterns in several APIs (e.g. `read_csv_auto('path/*.csv', union_by_name=...)`). Expose optional `ConnectionProperties` / extra keys for **file glob** where `FileDataSource` already carries path config.

---

## Tier 2 — Columnar / analytics formats (DuckDB built-ins)

DuckDB documents additional readers for columnar and structured binary formats. Implement after Tier 1.

| DuckDB read API | Typical extensions | Proposed reader class | Suggested `DataSourceType` |
|-----------------|-------------------|------------------------|----------------------------|
| `read_parquet` already covers | `.parquet` | *(same as Tier 1)* | `Parquet` |
| Arrow IPC / Feather | `.arrow`, `.feather` | `DuckDbArrowFileReader` | `Feather` *(closest existing enum)* |
| `read_avro(...)` *(if available in your DuckDB build)* | `.avro` | `DuckDbAvroFileReader` | `Avro` |
| `read_orc(...)` *(if available)* | `.orc` | `DuckDbOrcFileReader` | `ORC` |

> If a function is missing in your embedded DuckDB build, either **upgrade DuckDB.NET** or mark the reader as **unsupported** with a clear error.

---

## Tier 3 — Excel / spreadsheets (often extension or built-in)

| DuckDB read API | Typical extensions | Proposed reader class | Suggested `DataSourceType` |
|-----------------|-------------------|------------------------|----------------------------|
| Excel reader *(INSTALL/LOAD extension or built-in `read_xlsx` / similar — check docs)* | `.xlsx`, `.xls` | `DuckDbExcelFileReader` | `Xls` |

**Caveat:** May require `INSTALL excel` / `LOAD excel` (or equivalent) once per process; the reader’s `Configure` / first open should run that idempotently.

---

## Out of scope (different products)

- **Cloud / HTTP**: `httpfs` + S3/GCS paths — not a local `FileDataSource` path (handle as separate datasource or future).
- **Spatial / GDAL**: `st_read` — GIS pipelines, not generic tabular `FileDataSource`.
- **SQLite attach**: `sqlite_scan` — use SQLite datasource, not file reader.

---

## `DataSourceType` and `FileReaderFactory` (how many readers?)

You can have **many** `IFileFormatReader` implementations in the solution and **many** of them registered at once.

**What the current static factory does** (`DataManagementEngineStandard/FileManager/FileReaderFactory.cs`): it keeps a `Dictionary<DataSourceType, IFileFormatReader>`. Each `Register(reader)` does `_registry[reader.SupportedType] = reader` — so for a **given** `SupportedType` key, the **last** `Register` wins. `GetReader(type)` returns that single entry for `type`.

**Implications**

- **Different formats:** register `CsvFileReader` (`CSV`), `TsvFileReader` (`TSV`), `DuckDbParquetFileReader` (`Parquet`), etc. — **all** stay registered; no conflict.
- **Two engines for the same format** (e.g. legacy C# CSV **and** DuckDB CSV): give them **different** `SupportedType` values (e.g. keep `CSV` for one, add a dedicated enum value for the other **or** use another existing value your UI maps to “DuckDB CSV”), **or** intentionally `Register` twice for `CSV` so the second replaces the first at startup.

**Plugin discovery:** `FileReaderRegistry` can discover multiple attributed readers and register them; see `FileReaderRegistry` / `[FileReader]` in BeepDM.

The enum already includes **`CSV`, `TSV`, `Json`, `Parquet`, `Feather`, `Avro`, `ORC`, `Xls`** — reuse these for DuckDB-backed readers where they match product semantics.

---

## Implementation rule (all tiers)

Each reader:

1. Opens an **embedded** `DuckDBConnection` (typically in-memory) for the lifetime of the read session.
2. Builds SQL: `SELECT * FROM <duckdb_read_function>('<escaped_path>' [, options])`.
3. Implements `GetEntityStructure` / `ReadHeaders` / `ReadRows` by consuming DuckDB’s result schema and streaming rows into `string[]` per `IFileFormatReader`.
4. Implements writes (`CreateFile`, `AppendRow`, `RewriteFile`) via DuckDB **`COPY ... TO`** / export functions **where the format supports it**; otherwise `NotSupportedException` with a clear message (common for Parquet append).

---

## Cross-references

- Beep checklist: `../../../../BeepDM/.cursor/filedatasource/reference.md`
- SQL patterns aligned with: `DuckDbExtendedFunctions.cs` (`ImportCSV`, `ImportParquet`, `read_json`, …)
- Short phased build order: `02-phased-roadmap.md`
- **Full DuckDB reader surface + detailed phases** (CSV through Iceberg, compression, glob, remote, out-of-scope DB scans): **`03-detailed-phased-implementation-plan.md`**
