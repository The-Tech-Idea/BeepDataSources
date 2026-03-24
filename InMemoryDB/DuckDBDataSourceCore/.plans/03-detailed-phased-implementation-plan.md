# Detailed phased plan: DuckDB-backed file readers (full surface)

This document turns DuckDB’s **reader architecture** (local files, schema detection, parallel scan, globbing, compression, extensions, nested types) into a **sequenced implementation plan** for Beep **`IFileFormatReader`** implementations used by **`FileDataSource`**, plus notes for features that belong on **`DuckDBDataSource`** instead.

**Companion docs:** `00-duckdb-native-file-readers-catalog.md` (format matrix), `02-phased-roadmap.md` (short summary).

### DuckDB reader landscape (what it can do — mapped to this plan)

| Area | DuckDB capability | Where we implement it |
|------|-------------------|----------------------|
| **File formats** | Parquet, CSV (auto schema), JSON / NDJSON, Arrow, Avro, ORC, Excel, raw `read_text` / `read_blob` | Phases **1–4** |
| **Native DB files** | `.duckdb` / `.db` via `ATTACH` | **Phase 5** (optional; may defer to `DuckDBDataSource` only) |
| **Lakehouse** | Delta Lake, Iceberg via extensions | **Phase 6** |
| **Storage locations** | Local paths; HTTP(S); S3/GCS/Azure/R2 with extensions | **Phase 7** (optional); **Phases 0–6** assume local/UNC paths unless noted |
| **Optimizations** | Parallel scan, streaming, glob multi-file, compression (e.g. `.gz`), Parquet pushdown | **Cross-cutting**; engine runs DuckDB SQL (parallelism is inside DuckDB) |
| **External databases** | `postgres_scan`, `sqlite_scan`, `mysql_scan` | **Out of scope** for `IFileFormatReader` — use **`DuckDBDataSource`** / SQL |
| **Nested types** | Structs, lists, maps in files | **Phase 0** policy + per-reader stringify/JSON rules |

---

## 1. Scope model

| Scope | What DuckDB can do | Beep integration target |
|-------|-------------------|-------------------------|
| **A — Local file path** | `read_csv*`, `read_parquet`, `read_json*`, `read_ndjson`, Arrow/Avro/ORC, `read_text`, `read_blob`, Excel (extension), compressed `.gz`/zstd, glob patterns, `.duckdb` ATTACH | **`IFileFormatReader`** + optional `ConnectionProperties` flags |
| **B — Remote / cloud path** | `httpfs`, S3/GCS/Azure/R2 URLs | **Future** `FileDataSource` path convention **or** separate datasource; same DuckDB SQL once path resolves |
| **C — Lakehouse** | Delta Lake, Iceberg via **extensions** | **Phase** after core; `INSTALL`/`LOAD` + table functions |
| **D — External databases** | `postgres_scan`, `sqlite_scan`, `mysql_scan` | **`DuckDBDataSource`** commands / SQL, **not** `FileDataSource` readers |
| **E — In-memory objects** | Pandas/Polars in Python API | **N/A** for .NET `FileDataSource` |

Everything in **Scope A** is the primary target for “all readers DuckDB can handle” **as files on disk** (or UNC paths on Windows).

---

## 2. Capability checklist (from DuckDB — map to work items)

| Capability | Engineering tasks |
|------------|---------------------|
| **Automatic schema detection** | Use `read_csv_auto`, `read_json_auto`, `read_parquet` metadata; map DuckDB types → `EntityStructure` / field types |
| **Parallel reading** | Free with DuckDB; no extra code beyond running SQL |
| **Streaming / out-of-core** | Prefer **`DataReader`** streaming in `ReadRows`; avoid `DataTable.Load` for huge files |
| **Glob / multiple files** | Extend `Configure` / connection: `FilePath` or extra key accepts `C:\data\*.parquet`; SQL uses `parquet_scan` / `read_csv_auto` with pattern + `union_by_name` where needed |
| **Filter/projection pushdown** | Optional **Phase**: expose “preview” queries with `WHERE`/`LIMIT` via connection options (advanced) |
| **Compressed files** | Pass paths ending in `.gz` / zstd where DuckDB auto-detects; document **test matrix** |
| **Nested types** | Policy: stringify structs/lists for `string[]` **or** JSON-serialize nested columns — document per format |

---

## 3. Master inventory — formats → DuckDB API → reader class → phase

> **Verify** function names against your **embedded DuckDB** version (`SELECT version();`). Extensions require `INSTALL`/`LOAD` once per process (cache in a small helper).

### Tier P0 — Core analytics & interchange (implement first)

| # | Format | DuckDB entry points | Proposed class | Suggested `DataSourceType` | Phase |
|---|--------|---------------------|----------------|----------------------------|-------|
| 1 | **CSV** | `read_csv_auto`, `read_csv` | `DuckDbCsvFileReader` | `CSV` | 1 |
| 2 | **TSV / delimited** | `read_csv_auto` + `delim` / `sep` | `DuckDbTsvFileReader` | `TSV` | 1 |
| 3 | **Parquet** | `read_parquet`, `parquet_scan` | `DuckDbParquetFileReader` | `Parquet` | 1 |
| 4 | **JSON** (array / documents) | `read_json`, `read_json_auto` | `DuckDbJsonFileReader` | `Json` | 1 |
| 5 | **JSON Lines / NDJSON** | `read_ndjson` / line-delimited | `DuckDbNdjsonFileReader` | `Json` or new enum | 1 |

### Tier P1 — Columnar / Hadoop ecosystem

| # | Format | DuckDB entry points | Proposed class | Suggested `DataSourceType` | Phase |
|---|--------|---------------------|----------------|----------------------------|-------|
| 6 | **Apache Arrow / Feather** | `read_arrow`, IPC | `DuckDbArrowFileReader` | `Feather` | 2 |
| 7 | **Avro** | `read_avro` | `DuckDbAvroFileReader` | `Avro` | 2 |
| 8 | **ORC** | `read_orc` | `DuckDbOrcFileReader` | `ORC` | 2 |

### Tier P2 — Spreadsheets & office

| # | Format | DuckDB entry points | Proposed class | Suggested `DataSourceType` | Phase |
|---|--------|---------------------|----------------|----------------------------|-------|
| 9 | **Excel** | Excel extension / `read_xlsx` (version-specific) | `DuckDbExcelFileReader` | `Xls` | 3 |

### Tier P3 — Raw & opaque

| # | Format | DuckDB entry points | Proposed class | Suggested `DataSourceType` | Phase |
|---|--------|---------------------|----------------|----------------------------|-------|
| 10 | **Plain text** | `read_text` | `DuckDbTextBlobFileReader` (mode=text) | `Text` | 4 |
| 11 | **Binary blob** | `read_blob` | `DuckDbTextBlobFileReader` (mode=blob) | `FlatFile` or `Text` | 4 |

### Tier P4 — DuckDB native database files

| # | Format | DuckDB entry points | Proposed class | Notes | Phase |
|---|--------|---------------------|----------------|-------|-------|
| 12 | **`.duckdb` / `.db`** | `ATTACH` + `SHOW TABLES` / query | `DuckDbAttachedDatabaseReader` **or** defer to `DuckDBDataSource` | Semantics differ from “single flat file entity”; may expose **one entity per table** — product decision | 6 |

### Tier P5 — Lakehouse (extensions)

| # | Format | DuckDB entry points | Proposed class | Phase |
|---|--------|---------------------|----------------|-------|
| 13 | **Delta Lake** | delta extension + `delta_scan` / versioned paths | `DuckDbDeltaFileReader` | 6 |
| 14 | **Apache Iceberg** | iceberg extension + `iceberg_scan` | `DuckDbIcebergFileReader` | 6 |

### Tier P6 — Remote paths (optional product)

| # | Source | DuckDB entry points | Notes | Phase |
|---|--------|---------------------|-------|-------|
| 15 | **HTTP(S) URL** | `read_parquet('https://...')` with `httpfs` | Treat URL string as “path” in connection; `INSTALL httpfs` | 7 |
| 16 | **S3 / GCS / Azure** | Same + cloud extension + secrets | Usually **not** pure `FileDataSource`; connection UI for creds | 7 |

### Out of scope for `IFileFormatReader` (implement on `DuckDBDataSource` / SQL)

| Source | DuckDB | Reason |
|--------|--------|--------|
| PostgreSQL | `postgres_scan` | Connection-based, not a file |
| SQLite file | `sqlite_scan` | Use **Sqlite** datasource or DuckDB SQL from engine |
| MySQL | `mysql_scan` | Connection-based |
| Python in-memory frames | API-only | Not applicable to .NET file pipeline |

---

## 4. Detailed phases

### Phase 0 — Foundation (1 sprint)

**Objectives**

- Pin **DuckDB.NET** / native version; document `SELECT version()`.
- Implement **`DuckDbFileReaderEngine`** (or equivalent):
  - Create/release in-memory `DuckDBConnection`
  - **Path literal** escaping for SQL (quotes, Windows paths, UNC)
  - `ExecuteQueryToDataReader(sql)` + schema → `EntityStructure` mapper
  - Optional: **`EnsureExtensionLoaded(name)`** idempotent helper for `INSTALL`/`LOAD`
- Define **nested type → string** policy (JSON stringify vs flatten vs reject).

**Exit criteria**

- One integration test: `SELECT * FROM read_csv_auto('…') LIMIT 5` returns rows.
- Documented type-mapping table (DuckDB → Beep field types).

---

### Phase 1 — P0 readers (CSV, TSV, Parquet, JSON, NDJSON)

**Per reader**

- `Configure`: delimiter, header, `sample_size`, `ignore_errors` (CSV); `auto_detect` (JSON); glob path passthrough.
- `ReadRows`: stream `IDataReader`; fill `string[]` per row.
- Writes: `COPY (…) TO '…' (FORMAT …)` where supported; else `NotSupportedException` with message.

**Order**

1. Parquet (simplest schema, pushdown-friendly)
2. CSV (`read_csv_auto`)
3. TSV (reuse engine with options)
4. JSON + NDJSON

**Exit criteria**

- Golden-file tests per format (small files in repo).
- `ParseMode` / diagnostics: define behavior when DuckDB throws vs partial read.

---

### Phase 2 — P1 readers (Arrow/Feather, Avro, ORC)

**Objectives**

- Probe `SELECT 1 FROM read_avro('…')` / `read_orc` — **feature-detect** if build lacks function.
- Reuse engine; swap table function + options.

**Exit criteria**

- Each format has at least one sample file test **or** clear “not available in this DuckDB build” at runtime.

---

### Phase 3 — Excel (P2)

**Objectives**

- `INSTALL`/`LOAD` excel (or version-correct API).
- Map sheets → entities **or** single sheet from `Configure` (sheet name / index).

**Exit criteria**

- Read one `.xlsx` with header row; optional write via `COPY` if supported.

---

### Phase 4 — Raw text & blob (P3)

**Objectives**

- `read_text`: single column entity (e.g. `content`).
- `read_blob`: hex/base64 policy for `string[]` — document for consumers.

**Exit criteria**

- Large file smoke test (streaming).

---

### Phase 5 — Native `.duckdb` attach (P4) — *optional / product-dependent*

**Objectives**

- Either: **attach** file and map **each table** to Beep entities (larger change to `FileDataSource` assumptions), or **defer** to opening via **`DuckDBDataSource`** only.

**Exit criteria**

- Written ADR: “file reader” vs “open as database datasource”.

---

### Phase 6 — Delta & Iceberg (P5)

**Objectives**

- Extension install helper; path to **table root** / catalog options.
- Often **directory paths** rather than single files — align with `ConnectionProperties`.

**Exit criteria**

- Sample table read from minimal local Delta/Iceberg test fixture **or** skip if CI cannot run.

---

### Phase 7 — Remote & cloud (P6) — *optional*

**Objectives**

- `httpfs` + URL as path string.
- Cloud: credential hooks **outside** `FileDataSource` or via extended connection model.

**Exit criteria**

- ADR: security model for secrets; may stay **out of v1**.

---

### Cross-cutting — Phases 1–7

| Topic | When | Tasks |
|-------|------|-------|
| **Glob & union_by_name** | With P0 | Connection flags; SQL generation for `*` patterns |
| **Compression** | With P0 | Test `.csv.gz`, `.parquet` compressed |
| **Pushdown / LIMIT** | Later | Optional connection: max rows, column projection |
| **Registration** | After each tier | `RegisterAll()` / `[FileReader]` discovery |
| **Docs** | Continuous | Update `BeepDM/.cursor/filedatasource/reference.md` §7 |

---

## 5. Testing strategy

| Layer | Content |
|-------|---------|
| **Unit** | SQL string building, path escape, no DB |
| **Integration** | DuckDB in-process, small fixtures per format |
| **Regression** | Version matrix: DuckDB.NET minor bumps |
| **Performance** | Optional: large Parquet/CSV — ensure streaming, not full load |

---

## 6. Rollout suggestion

1. Ship **Phase 0 + Phase 1 (P0)** as **MVP** for `FileDataSource`.
2. Add **Phases 2–4** (P1–P3 formats) by customer demand.
3. Treat **Phases 5–7** (attach, lakehouse, remote) as **optional** modules (separate NuGet or feature flags).

---

## 7. References

- Beep: `BeepDM/.cursor/filedatasource/SKILL.md`, `reference.md`
- Code: `DuckDbExtendedFunctions.cs`, `DuckDBDataSource.cs`
- DuckDB: official docs for `read_*`, extensions, and version notes
