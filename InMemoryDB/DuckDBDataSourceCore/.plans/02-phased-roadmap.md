# Phased roadmap: one `IFileFormatReader` per DuckDB-supported format

This roadmap implements **new file readers** whose behavior is **defined by DuckDB’s native file readers** — see the master list in **`00-duckdb-native-file-readers-catalog.md`**.

**For the complete plan** (all formats, glob/compression, Delta/Iceberg, remote paths, external DB scan scope, testing): **`03-detailed-phased-implementation-plan.md`**.

---

## Phase 0 — Contracts & enum mapping

**Goals**

- Confirm `IFileFormatReader` members in your pinned `DataManagementEngine` package.
- For each row in **Tier 1** of the catalog, assign:
  - `SupportedType` → `DataSourceType` (`CSV`, `TSV`, `Parquet`, `Json`, …).
  - Default file extension (`GetDefaultExtension()`).
- If you want **both** a legacy C# CSV reader and a DuckDB CSV reader, assign **different** `SupportedType` / connection profiles (or accept that `Register` for the same `DataSourceType` **replaces** the previous reader for that key).

**Exit criteria**

- Written matrix: **DuckDB function** ↔ **reader class name** ↔ **`DataSourceType`** ↔ **extensions**.

---

## Phase 1 — Shared engine helper (recommended)

**Goals**

- Add e.g. `DuckDbFileReaderEngine` (internal): holds `DuckDBConnection`, escapes paths, runs `SELECT * FROM read_*('...')`, returns `IDataReader` / schema for `EntityStructure` mapping.
- Optionally refactor **`DuckDbExtendedFunctions`** import SQL into shared helpers so **IDE commands** and **file readers** never drift.

**Exit criteria**

- One code path for “run DuckDB read on file path” used by the first Tier-1 reader.

---

## Phase 2 — Tier 1 readers (ship in this order)

| Order | Reader | DuckDB API |
|-------|--------|------------|
| 1 | `DuckDbParquetFileReader` | `read_parquet` / `parquet_scan` |
| 2 | `DuckDbCsvFileReader` | `read_csv_auto` |
| 3 | `DuckDbTsvFileReader` | `read_csv_auto` with tab delimiter (or dedicated options) |
| 4 | `DuckDbJsonFileReader` | `read_json` / `read_json_auto` |
| 5 | `DuckDbNdjsonFileReader` | `read_ndjson` or equivalent line-delimited JSON |

**Per reader**

- `Configure(IConnectionProperties)` → delimiter, header, encoding if exposed; JSON auto_detect flags.
- `GetEntityStructure` / `ReadRows` via shared engine.
- Write path: **`COPY ... TO`** where applicable; else document read-only.

**Exit criteria**

- Manual test: `FileDataSource` + each `DataSourceType` opens a sample file and returns rows.

---

## Phase 3 — Tier 2 (Arrow / Avro / ORC)

**Goals**

- Implement `DuckDbArrowFileReader`, `DuckDbAvroFileReader`, `DuckDbOrcFileReader` **only after** verifying the function exists in your DuckDB build.
- Same engine helper; swap table function name + options.

**Exit criteria**

- Sample files per format pass the validation matrix in `BeepDM/.cursor/filedatasource/reference.md` §4 (read path).

---

## Phase 4 — Tier 3 (Excel)

**Goals**

- `DuckDbExcelFileReader` for `Xls` / `.xlsx`: `INSTALL`/`LOAD` if required, then read via DuckDB’s Excel API for your version.

**Exit criteria**

- One `.xlsx` round-trip or read-only smoke test.

---

## Phase 5 — Registration & host

**Goals**

- `DuckDbNativeFileReaders.RegisterAll()` → `FileReaderFactory.Register(...)` for each implemented reader.
- Document startup: call **after** `RegisterDefaults()` only if you intend to **replace** a default reader for the **same** `SupportedType` (same dictionary key); otherwise register additional readers with **distinct** `SupportedType` values so **all** coexist.

**Exit criteria**

- Sample host app registers once and opens Parquet + CSV via `FileDataSource`.

---

## Risks

| Topic | Mitigation |
|-------|------------|
| DuckDB version vs docs | Pin DuckDB.NET; integration-test `SELECT version()`. |
| Same `SupportedType`, two implementations | Use **different** `DataSourceType` values **or** explicit replace-on-purpose at startup. |
| Nested JSON → flat `string[]` | Define flattening or single VARCHAR column policy in JSON readers. |
