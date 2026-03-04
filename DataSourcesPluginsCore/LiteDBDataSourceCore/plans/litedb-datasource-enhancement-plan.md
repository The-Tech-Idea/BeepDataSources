# LiteDBDataSource Enhancement Plan

## Scope
- Target file: `LiteDBDataSourceCore/LiteDBDataSource.cs`
- Target interfaces and behaviors:
  - `IDataSource` contract completeness and reliability
  - `ILocalDB` file lifecycle safety
  - Mapping/schema consistency with `EntityStructure` and `EntityField`
  - Alignment with BeepDM service/environment patterns
- Skills applied:
  - `beepdm`, `idatasource`, `localdb`, `inmemorydb`, `mapping`, `beepserviceregistration`, `environmentservice`

## Current Gaps (Observed)
- Connection lifecycle is inconsistent:
  - Many methods open ad-hoc `new LiteDatabase(_connectionString)` instances instead of using a consistent connection strategy.
  - `Openconnection()` sets `ConnectionStatus` but does not retain the opened `db` instance for reuse.
- Error handling is mixed:
  - Some methods return `IErrorsInfo`, others throw (`RunQuery`), violating expected datasource behavior.
  - `ErrorObject` is not always the single source of operation status.
- Metadata refresh is partial:
  - `Entities`/`EntitiesNames` synchronization exists but is repeated and can drift after create/drop/update operations.
- Schema inference and conversion path has fragility:
  - `ConvertToBsonDocument` assumes `DataStruct` availability.
  - Type conversion paths are duplicated and partially inconsistent.
- Query/filter translation is limited:
  - `BuildLiteDBExpression` only handles a narrow set of operators.
- LocalDB file operations are unsafe:
  - `DeleteDB` and `CopyDB` can run with open handles and without robust path/permission guards.
- Naming and cleanup debt:
  - Legacy method `HandleConnectionStringforMongoDB` is unrelated to LiteDB naming.
  - Duplicated code blocks for initialization and list syncing.

## Enhancement Goals
- Make `LiteDBDataSource` deterministic, safe, and fully aligned with `IDataSource` and `ILocalDB` expectations.
- Standardize error and logging behavior: no routine throws, always populate `ErrorObject`/`IErrorsInfo`.
- Centralize schema and mapping conversion with stronger type handling.
- Improve operational reliability for file-backed local DB workflows.
- Prepare clean integration with Beep service registration and environment bootstrap.

## Phased Plan

### Phase 1 - Connection and State Backbone
- Introduce a single internal connection pattern:
  - `EnsureOpen()` helper to initialize connection and enforce `ConnectionState.Open`.
  - `WithDatabase(Func<LiteDatabase, ...>)` wrappers for safe operation execution.
- Decide one strategy and enforce it:
  - Either maintain a long-lived `db` instance, or consistently use short-lived instances with centralized guard logic.
- Normalize `Openconnection()` and `Closeconnection()`:
  - Accurate `ConnectionStatus` transitions.
  - Idempotent behavior for repeated open/close calls.
- Add connection-string and file-path validation before open.

### Phase 2 - IDataSource Contract Reliability
- Standardize all CRUD/query/DDL operations to:
  - Return `IErrorsInfo` or expected result objects without routine exceptions.
  - Set `ErrorObject.Flag` and `ErrorObject.Message` consistently.
- Refactor duplicated null/open checks in:
  - `GetEntity`, `InsertEntity`, `UpdateEntity`, `DeleteEntity`, `ExecuteSql`, `RunScript`, `CreateEntityAs`, `DropEntity`.
- Update `RunQuery` to follow non-throwing datasource pattern.
- Ensure async wrappers are thin and safe (`Task.Run` usage reviewed per operation type).

### Phase 3 - Metadata and Schema Consistency
- Create a single metadata sync helper:
  - Rebuild and reconcile `EntitiesNames` and `Entities` after schema-changing operations.
- Harden `GetEntityStructure`:
  - Avoid expensive full scans unless refresh is requested.
  - Handle empty collections safely (no first-document assumptions).
- Improve schema inference:
  - Preserve `_id` key semantics.
  - Better handling for arrays/documents and nullable value patterns.
- Ensure `EntityName`, `DatasourceEntityName`, and source mapping fields stay coherent.

### Phase 4 - Conversion and Mapping Pipeline
- Unify BSON conversion utilities:
  - Consolidate `ConvertToBsonValue` overload logic.
  - Guard for null `DataStruct` and fallback to reflection-based mapping.
- Build a robust property/field resolver:
  - Case-insensitive matching
  - Optional mapping metadata hook points for future `MappingManager` integration
- Reduce conversion duplication and improve diagnostics:
  - Include field name/type details in failure logs.
- Keep conversion deterministic for `DataRow`, POCO, and `BsonDocument`.

### Phase 5 - Filter/Query Capability Expansion
- Extend `BuildLiteDBExpression` operator support:
  - `!=`, `>=`, `<=`, `contains`, `startswith`, `endswith`, `in`, null checks.
- Add safe value formatting by inferred type (string/number/date/bool) instead of forcing quoted strings.
- Add input validation and fallback behavior for malformed filters.

### Phase 6 - ILocalDB Hardening
- `CreateDB` variants:
  - Validate/normalize paths, ensure directory exists.
  - Respect in-memory flag semantics clearly.
- `DeleteDB`:
  - Guarantee closure of handles before deletion.
  - Return precise failure reasons (file locked, missing file, unauthorized).
- `CopyDB`:
  - Ensure source is closed or checkpointed.
  - Validate destination path, overwrite policy, and atomic copy behavior where possible.

### Phase 7 - Service and Environment Integration
- Add explicit integration checklist with Beep service/environment bootstrap:
  - Works with configured `DataFilePath` from environment service.
  - Connection object registration and retrieval through config editor remains consistent.
- Validate compatibility with modern service registration flows (`AppRepoName`, directory path configuration).
- Add clear startup diagnostics for missing environment/config prerequisites.

### Phase 8 - Performance and Maintainability Cleanup
- Remove dead/legacy naming and unrelated methods:
  - Rename/replace `HandleConnectionStringforMongoDB` to LiteDB-appropriate naming or remove if unused.
- Break monolithic file into partials by responsibility:
  - Connection, CRUD, Query, Schema/Metadata, LocalDB, Conversion helpers.
- Add targeted caches where safe (entity type/property maps), with invalidation rules.

### Phase 9 - Validation and Regression Suite
- Add focused tests (or executable validation scripts) for:
  - Open/close/create/delete/copy lifecycle
  - CRUD operations on POCO + DataRow + BsonDocument
  - Schema inference with sparse/mixed documents
  - Filter translation for each supported operator
  - Metadata synchronization after entity create/drop
- Add negative tests:
  - invalid path, invalid filter, missing `_id`, locked file, null payload.

## Deliverables
- Refactored `LiteDBDataSource` with consistent connection/error/metadata patterns.
- Expanded filter and conversion reliability.
- Hardened local file operations.
- Documentation notes for service/environment integration expectations.
- Validation checklist and regression test coverage for core workflows.

## Execution Order (Recommended)
1. Phase 1 + Phase 2 (stability baseline)
2. Phase 6 (file safety)
3. Phase 3 + Phase 4 (schema/mapping correctness)
4. Phase 5 (query capability)
5. Phase 7 + Phase 8 + Phase 9 (integration, cleanup, regression)

## Success Criteria
- No routine operation throws for expected runtime errors; all use `IErrorsInfo`.
- `ConnectionStatus` accurately reflects real state in all flows.
- `Entities` and `EntitiesNames` remain consistent after all schema operations.
- Local DB create/copy/delete operations are deterministic and safe.
- CRUD/query behavior is stable across POCO, `DataRow`, and `BsonDocument` payloads.
