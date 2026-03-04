# SQLiteDataSource Enhancement Plan

## Scope
- Target file: `SqliteDatasourceCore/SQLiteDataSource.cs`
- Target interfaces and behaviors:
  - `IDataSource` contract completeness and reliability
  - `ILocalDB` file lifecycle safety
  - `InMemoryRDBSource` base class alignment
  - In-memory vs file-backed mode consistency
  - Alignment with BeepDM service/environment patterns
- Skills applied:
  - `beepdm`, `idatasource`, `localdb`, `inmemorydb`, `connection`, `beepserviceregistration`, `environmentservice`

## Current Gaps (Observed)

### Connection and State
- **CopyDB logic bug**: Condition `!File.Exists(...)` is inverted—copy runs when file does *not* exist; should run when it does.
- **Connection status drift**: `Openconnection()` delegates to base but may not consistently sync `ConnectionStatus` with `Dataconnection.ConnectionStatus`.
- **Closeconnection safety**: Accesses `base.RDBMSConnection.DbConn` without null checks; `SQLiteConnection.ClearAllPools()` may affect other connections.
- **CreateDB redundancy**: `CreateDB(string)` creates file twice in some branches; logic is duplicated and confusing.

### LocalDB Operations
- **CreateDB(bool inMemory)**: Returns `false` unconditionally; in-memory creation is handled elsewhere but interface is incomplete.
- **DeleteDB**: Uses `GC.Collect()` and `GC.WaitForPendingFinalizers()` as a workaround for open handles; no explicit connection closure verification.
- **CopyDB**: No validation of destination path, overwrite policy, or source file existence before copy.
- **Path handling**: Mix of `Path.Combine(FilePath, FileName)` and direct `ConnectionString`; no centralized path normalization.

### Transaction Support
- **BeginTransaction, Commit, EndTransaction**: Empty try blocks; no actual SQLite transaction (`BEGIN`, `COMMIT`, `ROLLBACK`) implementation.
- Base `InMemoryRDBSource` may provide transaction logic; SQLite overrides do not delegate or extend it.

### In-Memory and Structure Lifecycle
- **LoadStructure, CreateStructure**: Called from `Openconnection()` but implementation lives in base; flow is complex and error paths unclear.
- **SaveStructure**: Called from `Closeconnection()` only when in-memory; no validation that structure was modified.
- **InMemoryStructures sync**: `GetEntitesList()` merges `InMemoryStructures` into `Entities`/`EntitiesNames` but sync direction and invalidation rules are implicit.
- **Static folder paths**: `BeepDataPath`, `InMemoryPath`, `Filepath`, `InMemoryStructuresfilepath` are static; not compatible with multi-instance or test isolation.

### Code Quality and Maintainability
- **Large commented-out blocks**: ~250 lines of commented `LoadStructure`, `SaveStructure`, `SyncEntitiesNameandEntities`, `SaveEntites`, `LoadEntities`, `CreateStructure`—dead code that obscures intent.
- **Duplicate logic**: `CreateDB(string)` has repeated file-exists checks and create steps.
- **Error handling inconsistency**: Some methods use `DMEEditor.ErrorObject`, others `ErrorObject`; `DMEEditor.AddLogMessage` parameters vary (first arg sometimes "Beep", "Success", "Fail", "Error").
- **FK constraints**: `DisableFKConstraints` / `EnableFKConstraints` use `ignore_check_constraints` (0/1) which may not match SQLite `foreign_keys` pragma semantics.

### IDataSource / ILocalDB Contract
- **DropEntity**: Uses `base.ExecuteSql` and `base.CheckEntityExist`; table name quoting (`'EntityName'`) may need schema/identifier escaping.
- **CreateEntityAs**: Duplicate checks for `EntitiesNames` and `Entities`; in-memory path bypasses some checks—behavior divergence.
- **GetEntitesList**: Override adds in-memory entities but does not clearly document when `InMemoryStructures` is authoritative vs `Entities`.

## Enhancement Goals
- Make `SQLiteDataSource` deterministic, safe, and fully aligned with `IDataSource` and `ILocalDB` expectations.
- Fix LocalDB operations (Create, Copy, Delete) with correct logic and robust path handling.
- Implement or properly delegate transaction support.
- Clarify in-memory vs file-backed lifecycle and remove dead code.
- Standardize error handling and logging.
- Prepare for partial class split and improved testability.

## Phased Plan

### Phase 1 - Connection and State Backbone
- Fix `CopyDB` condition: copy when source file *exists*; validate destination directory and overwrite policy.
- Add null checks in `Closeconnection()` before accessing `RDBMSConnection.DbConn`.
- Ensure `ConnectionStatus` is set consistently in `Openconnection()` and `Closeconnection()`.
- Document when `Dataconnection.ConnectionStatus` vs `ConnectionStatus` is authoritative.
- Consider scoping `SQLiteConnection.ClearAllPools()` or replacing with connection-specific cleanup.

### Phase 2 - LocalDB Operations Hardening
- **CreateDB()**: Consolidate logic; ensure directory exists; single create path; clear success/failure semantics.
- **CreateDB(string)**: Remove duplicate create branches; normalize `ConnectionProp` after create; validate path format.
- **CreateDB(bool inMemory)**: Implement or document that in-memory is handled via `OpenDatabaseInMemory` + connection config; return meaningful result.
- **DeleteDB()**: Explicitly close connection, verify closed state, then delete; return precise failure reasons (locked, missing, unauthorized).
- **CopyDB()**: Validate source exists and is closed; ensure destination directory exists; support overwrite option; return `IErrorsInfo` or bool with clear semantics.

### Phase 3 - Transaction Support
- Implement `BeginTransaction`, `Commit`, `EndTransaction` with SQLite `BEGIN`, `COMMIT`, `ROLLBACK` or delegate to base if base provides it.
- Ensure transaction state is tracked and nested transactions are handled (or explicitly unsupported with clear error).
- Align with `IDataSource` transaction contract.

### Phase 4 - In-Memory Lifecycle Clarity
- Document when `InMemoryStructures` is populated and when it is the source of truth.
- Ensure `GetEntitesList()` merge logic is idempotent and does not duplicate entities.
- Replace static folder paths with instance-scoped or config-driven paths for multi-instance safety.
- Remove or uncomment and fix the large commented block; extract reusable logic if needed.

### Phase 5 - Error Handling and Logging Standardization
- Use `ErrorObject` consistently as the operation result; avoid mixing `DMEEditor.ErrorObject` and `ErrorObject` without clear rule.
- Standardize `AddLogMessage` first parameter (e.g., "Beep" for app context, "Success"/"Fail" for outcome).
- Ensure all LocalDB and CRUD operations set `ErrorObject.Flag` and `ErrorObject.Message` on failure.
- No routine throws for expected runtime errors; use `IErrorsInfo` return pattern.

### Phase 6 - FK Constraints and SQLite Pragmas
- Verify `PRAGMA foreign_keys` vs `PRAGMA ignore_check_constraints` semantics for SQLite version in use.
- Align `EnableFKConstraints` / `DisableFKConstraints` with SQLite documentation and base class expectations.
- Add `enablefk()` call consistency (currently called from `CreateDB()` but not `CreateDB(string)` in some paths).

### Phase 7 - Structural Refactoring (Partial Classes)
- Split `SQLiteDataSource.cs` into partials by responsibility:
  - `SQLiteDataSource.Connection.cs` – Open, Close, connection state
  - `SQLiteDataSource.LocalDB.cs` – CreateDB, DeleteDB, CopyDB
  - `SQLiteDataSource.Transactions.cs` – Begin, Commit, End
  - `SQLiteDataSource.Entities.cs` – DropEntity, CreateEntityAs, GetEntitesList overrides
  - `SQLiteDataSource.InMemory.cs` – LoadData, SyncData, RefreshData, structure lifecycle
  - `SQLiteDataSource.Helpers.cs` – enablefk, Createfolder, GetSqlLiteTableKeysAsync
- Preserve behavior; no functional changes in this phase.

### Phase 8 - Validation and Regression
- Add focused tests or validation scripts for:
  - CreateDB / DeleteDB / CopyDB lifecycle
  - Open/close with file-backed and in-memory modes
  - Entity create/drop and `GetEntitesList` with in-memory structures
  - Transaction begin/commit/rollback (when implemented)
- Negative tests: invalid path, locked file, missing connection config.

## Deliverables
- Refactored `SQLiteDataSource` with correct LocalDB logic and consistent error handling.
- Fixed CopyDB, CreateDB, DeleteDB behavior.
- Transaction support implemented or clearly delegated.
- Dead code removed; structure lifecycle documented.
- Partial class split for maintainability.
- Validation checklist for core workflows.

## Execution Order (Recommended)
1. Phase 1 (connection/state fixes)
2. Phase 2 (LocalDB hardening)
3. Phase 5 (error handling)
4. Phase 3 (transactions)
5. Phase 4 (in-memory clarity)
6. Phase 6 (FK pragmas)
7. Phase 7 (partial split)
8. Phase 8 (validation)

## Success Criteria
- CopyDB copies when source exists; CreateDB has no duplicate logic; DeleteDB closes before delete.
- ConnectionStatus accurately reflects state in all flows.
- Transaction methods either implement SQLite transactions or document delegation to base.
- No routine throws for expected errors; ErrorObject is consistently set.
- Commented dead code removed or restored with tests.
- Partial classes preserve behavior; each file has clear responsibility.
