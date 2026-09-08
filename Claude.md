# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

`IDataSource` plugin implementations for the **BeepDM** data management framework — ~164 project folders across
`DataSourcesPluginsCore/` (53 engine cores), `Connectors/` (94 REST/SaaS), `Messaging/` (8), `VectorDatabase/` (5),
`InMemoryDB/` (1), and `DataSourcesPlugins/RDBMSDataSource/` (the shared `RDBSource` base). There is no application
here — every project is a class library packaged as a NuGet plugin that BeepDM discovers by reflection at runtime.

## Sibling-repo dependency (read first)

BeepDM is **not** vendored. It must be cloned as a sibling: `../BeepDM` relative to this repo root
(i.e. `The-Tech-Idea/BeepDM` next to `The-Tech-Idea/BeepDataSources`). Two dependency modes coexist and must not be
mixed:

| Area | How it gets BeepDM |
|---|---|
| `DataSourcePluginSolution.sln` projects (cores, RDBMS, messaging, vector) | `<ProjectReference>` to `..\..\..\BeepDM\DataManagementEngineStandard` and `...ModelsStandard`. The solution itself includes those two BeepDM projects. |
| `Connectors/*` | `<PackageReference Include="TheTechIdea.Beep.DataManagementEngine" />` + `...DataManagementModels` |

The csproj comments spell out why: the local BeepDM sources carry unpublished fixes, and a package copy alongside the
project copy loads the same types twice. **Never convert a `DataSourcePluginSolution.sln` project to PackageReference
for BeepDM**, and never add a BeepDM ProjectReference to a `Connectors/` project.

The interface and base-class source of truth lives in the sibling repo, not here:

- `../BeepDM/DataManagementModelsStandard/IDataSource.cs` — the contract
- `../BeepDM/DataManagementEngineStandard/WebAPI/WebAPIDataSource.cs` — connector base class
- `../BeepDM/DataManagementEngineStandard/Helpers/ConnectionHelpers/ConnectionHelper_*.cs` — driver configs (below)
- `../BeepDM/.cursor/<skill>/SKILL.md` — authoritative runtime behavior write-ups

## Build & test

```bash
# .NET 10 SDK installed; global.json pins 8.0 with rollForward=latestFeature.
# Cores/RDBMS/messaging/vector multi-target net8.0;net9.0;net10.0. Connectors target net9.0 only.

dotnet build DataSourcePluginSolution.sln          # 53 projects incl. the two BeepDM projects
dotnet build Connectors/Connectors.sln             # 82 of the 94 connector projects
dotnet build DataSourcesPluginsCore/DataSourcesPluginsCore.sln
dotnet build Messaging/Messaging.sln
dotnet build DataSourcesPlugins/RDBMSDataSource/RDBMSDataSource.sln

# ~12 connector projects are in no .sln — build those by path:
dotnet build Connectors/<Category>/<Service>/<Service>.csproj
```

Tests — one xUnit project, **not referenced by any solution**, so a bare `dotnet test` at the root finds nothing:

```bash
dotnet test tests/RDBDataSource.Tests/RDBDataSource.Tests.csproj      # 35 tests, net9.0
dotnet test tests/RDBDataSource.Tests/RDBDataSource.Tests.csproj --filter "FullyQualifiedName~InsertEntity_ShouldReturnIdentity"
dotnet test tests/RDBDataSource.Tests/RDBDataSource.Tests.csproj --filter "FullyQualifiedName~RDBSourceIntegrationTests"
```

Those tests exercise RDBSource's SQL patterns through in-memory SQLite rather than instantiating `RDBSource` itself
(`RDBDataSource.csproj` grants `InternalsVisibleTo("RDBDataSource.Tests")`). Everything else in the repo is verified by
compiling — most plugins need live credentials, so there is no broader automated suite. Builds are warning-noisy
(nullable + CS0067); judge a change by errors, not by warning count.

Documentation checks, only when touching `Help/`:

```bash
python Help/tools/verify-help.py    # runs check-nav-mapping.py + check-help-links.py; run from repo root
```

## Build side effects

Every plugin csproj carries two custom targets, so expect writes outside the repo:

- `PostBuild` copies the built DLL to `../outputDLL/DataSources/<Project>/<TFM>/` (i.e. `The-Tech-Idea/outputDLL/`)
- `CopyPackage` (after `dotnet pack`) copies the `.nupkg` to `repos/LocalNugetFiles/DataSources/`

`GeneratePackageOnBuild=True` is set nearly everywhere, so an ordinary build also produces a package. Bump the
`<Version>` in the csproj when a plugin's behavior changes — versions are per-project, not repo-wide.

## Architecture

### Three implementation patterns

**A. RDBMS — inherit `RDBSource`** (`DataSourcesPlugins/RDBMSDataSource/`). One `public partial class RDBSource :
IRDBSource` split across 16 files in `PartialClasses/RDBSource/` (~6.7k lines): `.Connection`, `.CRUD`, `.Query`,
`.Schema`, `.DMLGeneration`, `.BulkOperations`, `.Transaction`, `.TypeMapping`, `.Pagination`, `.Cache`, `.Resilience`
(Polly), `.Dapper`, `.Modernization`, `.Utilities`, `.Dispose`. Support classes (`DbTypeMapper`, `PagedQueryExecutor`,
`EntityStructureCache`, `DataStreamer`, `PaginationHelper`) live in `Helpers/`. `InMemoryRDBSource : RDBSource,
IInMemoryDB` adds the file-backed/in-memory variants (SQLite, DuckDB, embedded Firebird). Engine plugins override only
vendor-specific SQL. **A fix here reaches every relational driver** — check the blast radius before editing.

**B. Direct `IDataSource`** — NoSQL, messaging, vector, file formats. ~40 members, no base class. Populate `Entities`
during `Openconnection()`, always set `ErrorObject.Flag`/`.Message`, and don't throw for expected failures.

**C. `WebAPIDataSource` connectors** (`Connectors/<Category>/<Service>/`). Static `EntityEndpoints` and
`RequiredFilters` dictionaries, a `Models.cs` of sealed POCOs at the project root, and `GetEntityAsync` built from the
base helpers (`FiltersToQuery`, `RequireFilters`, `ResolveEndpoint`, `GetAsync`, `ExtractArray`, `GetNextToken`).
Reference implementation: `Connectors/SocialMedia/Twitter/`.

### Discovery: three separate registrations

A plugin is invisible unless all three line up:

1. **`[AddinAttribute(Category = DatasourceCategory.X, DatasourceType = DataSourceType.Y)]`** on the datasource class —
   how `AssemblyHandler` finds the type by reflection.
2. **A `Create*Config` entry in BeepDM's `ConnectionHelper_*.cs`**, matched by `classHandler` == the class name. Without
   it the driver never appears in the connection UI. `IDATASOURCE_IMPLEMENTATIONS.md` is the generated audit of which
   folders have one — as of that snapshot, 91 of 164 folders do **not**.
3. **`DataSourceType` / `DatasourceCategory` enum values**, which live in BeepDM. A genuinely new backend needs the enum
   member added and BeepDM republished *before* the plugin can compile (that is what the `KVStore` category and the
   RocksDB/LevelDB/LMDB/NitriteDB members were in phase 11).

`DataSourcesPluginsCore/datasource-registry.json` is a hand-maintained catalog (packageId, `fullTypeName`,
`dataSourceType`, `category`, TFMs, `isImplemented`) — update it when adding or renaming a core plugin.

### Schema migration providers (Tier-1, colocated)

19 plugins ship a `<Name>MigrationProvider.cs` next to the datasource, decorated
`[SchemaMigrationProvider(DataSourceType.X, DatasourceCategory.Y)]` and implementing `ISchemaMigrationProvider` (defined
in BeepDM). Each takes the owning `IDataSource` in its constructor and declares a `SchemaMigrationCapabilities` object
stating what the backend can actually do — unsupported operations are declared `false` with a comment explaining why,
never silently faked. BeepDM's `MigrationManager` dispatches to these, falling back per category (RDBMS→SQL,
FILE→FileMutation, Connector/Queue/Stream/WebApi→ReadOnly). `MongoDBMigrationProvider.cs` is the model to copy.

## Conventions that bite

- **`[CommandAttribute(ObjectType = "...")]` must exactly match the POCO class name** (case-sensitive). A mismatch means
  the BeepDM UI silently fails to discover the method — no error anywhere.
- **Every POCO property needs `[JsonPropertyName]`.** Missing ones deserialize to null with no exception.
- `Models.cs` stays at the connector project root, not in a `Models/` subfolder. POCOs are `sealed` and derive from a
  `<Service>EntityBase` exposing `Attach<T>(IDataSource)`.
- **Generated entity types must be namespaced `"Beep." + DatasourceName`** when calling `DMTypeBuilder`. A constant
  namespace (the old `"TheTechIdea.Classes"`) makes two connections with a same-named entity share one generated type —
  wrong columns, no error, no log line. See commit `ea222c4b`.
- Several `*Core` csprojs still carry `<Compile Include="..\..\DataSourcesPlugins\<Name>\*.cs" />` globs pointing at
  folders that no longer exist (only `RDBMSDataSource` survives under `DataSourcesPlugins/`). They match nothing; the
  real sources sit beside the csproj. Leave them or delete them — do not recreate the folder.
- `.ConfigureAwait(false)` throughout; this library code is called from WinForms/WPF hosts.

## Planning docs

`.plans/` holds the phased plans and `MASTER-TODO-TRACKER.md` is the cross-phase status table (phases 01–09 are HTML
help under `Help/`; 10–11 are code: migration providers and embedded KV stores). Update the tracker row when finishing
a phase. Per-plugin plans also live in `DataSourcesPlugins/RDBMSDataSource/plan/`.

Other agent instruction files exist and largely overlap this one: `.github/copilot-instructions.md` (longest, with full
connector code templates), `.github/claude.md`, `.github/.cursorrules`.
