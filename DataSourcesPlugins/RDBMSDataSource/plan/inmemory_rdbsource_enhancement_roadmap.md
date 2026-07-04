# InMemoryRDBSource Enhancement Roadmap

This roadmap targets `InMemoryRDBSource` reliability, safety, and helper-architecture alignment without breaking existing datasource behavior.

## Scope
- File: `DataSourcesPlugins/RDBMSDataSource/InMemoryRDBSource.cs`
- Related base implementation:
  - `DataSourcesPlugins/RDBMSDataSource/PartialClasses/RDBSource/*.cs`
- Related helper architecture:
  - `BeepDM/DataManagementEngineStandard/Helpers/UniversalDataSourceHelpers/*`
  - `BeepDM/DataManagementEngineStandard/Helpers/RDBMSHelpers/*`

## Phase 1 - Correctness and Lifecycle Consistency

### Goals
- Remove incomplete behaviors and state inconsistencies.
- Ensure event and status flags accurately reflect operation outcomes.

### Tasks
1. Implement `OpenDatabaseInMemory(string databasename)` with explicit semantics:
   - validate input,
   - open datasource/connection,
   - initialize structure cache if needed,
   - return clear `ErrorObject` status.
2. Implement `SyncData(...)` overloads:
   - full sync from source into in-memory entities,
   - entity-scoped sync for a single table.
3. Implement entity-scoped `RefreshData(string entityname, ...)`:
   - truncate/delete one entity,
   - reload one entity via ETL script detail.
4. Fix state flags:
   - avoid setting `IsLoaded` in structure-only paths,
   - set `IsStructureLoaded`, `IsStructureCreated`, `IsSynced`, `IsSaved` only when true.
5. Standardize event invocation:
   - prefer `RaiseOn...` wrappers internally,
   - ensure `OnRefreshDataEntity` is raised in entity refresh path.
6. Remove dead code:
   - unused `entityNamesFromDb`,
   - unused `ETLDataCopier` allocation in `RefreshData`.

### Acceptance Criteria
- All methods return deterministic success/failure flags.
- Entity-scoped refresh/sync performs actual data operations.
- State flags and events match real execution outcomes.

## Phase 2 - SQL Safety and Operational Robustness

### Goals
- Remove fragile SQL string composition and improve recoverability.

### Tasks
1. Replace raw `delete from {item.EntityName}` SQL generation with helper-backed identifier-safe SQL:
   - use helper quote/DDL/DML generation path where possible.
2. Add transaction-aware full refresh sequence:
   - begin transaction,
   - clear entities,
   - ETL reload,
   - commit/rollback.
3. Improve cancellation and progress propagation:
   - forward caller `CancellationToken` to all ETL operations,
   - report granular progress for each entity operation.
4. Add structured log messages for each phase:
   - begin/complete/error per entity and overall run.

### Acceptance Criteria
- No direct unquoted table-name SQL in refresh paths.
- Partial failures during refresh do not leave unknown state.
- Cancellation can stop long refresh/sync operations safely.

## Phase 3 - Helper-Architecture Alignment

### Goals
- Align `InMemoryRDBSource` and inherited `RDBSource` behavior with `IDataSourceHelper` abstractions.

### Tasks
1. Introduce helper resolution in in-memory flow:
   - resolve via `DataSourceHelperFactory` / `GeneralDataSourceHelper`.
2. Replace direct SQL utility usage with helper methods where practical:
   - truncate/delete generation,
   - type and identifier handling,
   - capability checks.
3. Keep provider extension points explicit:
   - allow overrides for SQLite, DuckDB, LiteDB-specific in-memory variants.
4. Document migration boundaries:
   - what remains in `RDBSource`,
   - what moves to helper-backed paths.

### Acceptance Criteria
- In-memory operations use helper APIs for SQL generation and capability decisions.
- Provider-specific behavior remains overridable without copy-pasting core logic.

## Phase 4 - Verification and Rollout

### Goals
- Prevent regressions and provide adoption guidance.

### Tasks
1. Add regression tests for:
   - `LoadStructure`, `CreateStructure`, `SaveStructure`,
   - full and entity-scoped `RefreshData`,
   - `SyncData` overloads,
   - ETL-based data reload path.
2. Add concurrency-focused tests for collection consistency:
   - `Entities`, `EntitiesNames`, `InMemoryStructures`.
3. Add migration notes for downstream plugins.
4. Add performance baseline checks for large-entity refresh.

### Acceptance Criteria
- Tests pass on current supported providers.
- Existing plugins can adopt new behavior with documented steps.

## Suggested Implementation Order
1. Phase 1 state and method completion.
2. Phase 2 safe SQL + transaction controls.
3. Phase 3 helper adoption refactor.
4. Phase 4 tests and rollout notes.
