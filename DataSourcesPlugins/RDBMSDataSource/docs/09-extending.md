# Writing an RDBMS driver

The good news: a driver is small. `OracleDataSource` — the most heavily customised one — is under
90 lines. The bad news: it is small because the extension surface is narrow, and most dialect
differences have to be added to the base class instead.

## The minimum

```csharp
[AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType = DataSourceType.MyEngine)]
public class MyEngineDataSource : RDBSource, IDataSource
{
    public MyEngineDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor,
                              DataSourceType databasetype, IErrorsInfo per)
        : base(datasourcename, logger, DMEEditor, databasetype, per)
    {
    }
}
```

That is genuinely enough to get a working driver, provided the engine speaks reasonably standard
SQL and BeepDM has:

1. A `DataSourceType.MyEngine` enum member (in BeepDM — must be published before this compiles).
2. A `Create*Config` entry in `ConnectionHelper_*.cs` whose `classHandler` matches your class name,
   or the driver never appears in the connection UI.
3. Catalog SQL registered for `Sqlcommandtype.getlistoftables`, `getFKforTable` and `getChildTable`
   for your `DataSourceType`.

See the repository `CLAUDE.md` for the three-way registration rules.

## What you can override

This is the complete list that any existing driver uses:

| Member | Why you would |
|---|---|
| `ColumnDelimiter` | Identifier quoting characters. See the warning below. |
| `ParameterDelimiter` | Bind-parameter prefix — `@`, `:`, `$`. |
| `DisableFKConstraints` / `EnableFKConstraints` | Vendor DDL for suspending constraints during ETL. Base returns `string.Empty`. |
| `ConfigureCommand(IDbCommand)` | Per-command provider setup. |
| `GetEntitesList` | Only if the config-driven catalog query cannot express your table list. |
| `CreateEntityAs` | Only if `CREATE TABLE` needs shaping the DDL generator cannot do. |
| `Openconnection` / `Closeconnection` | Rarely — only if the connection lifecycle genuinely differs. |

`ConfigureCommand` is the model to copy. Oracle's override, with its reasoning intact:

```csharp
/// ODP.NET binds positionally by default (BindByName = false). RDBSource
/// generates named placeholders (:p_<Field>) and adds parameters in field
/// order, so positional binding silently shifts values into the wrong
/// columns whenever a parameter is skipped. Name-based binding makes a
/// missing parameter fail loudly (ORA-01008) instead of corrupting data.
protected override void ConfigureCommand(IDbCommand command)
{
    if (command is OracleCommand oracleCommand)
        oracleCommand.BindByName = true;
}
```

## What you cannot override

These are `virtual` but no driver overrides them, and several are shaped so that overriding is
unsafe or impossible:

| Member | Problem |
|---|---|
| `GetEntityforeignkeys` | Declared `IEnumerable<RelationShipKeys>`, but `Schema.cs:237` **hard-casts the result to `List<RelationShipKeys>`**. Return anything else — a LINQ projection, an array, an iterator — and you get `InvalidCastException` at runtime, not a compile error. If you override it, return a `List`. |
| `GenerateCreatEntityScript` | Now `public virtual` (both overloads), and the `GenerateCreateEntityScript(EntityStructure)` that does the actual work is `protected virtual`. A driver can specialise DDL generation by overriding either. |
| `GetDbType`, `ConvertToDbTypeValue` | Now `protected virtual`, so type mapping can be specialised. `TypeToDbType` remains `private` and is dead. |
| `GetTableSchema`, `GetChildTablesList`, `GetTablesFKColumnList`, `GetSchemaName`, `GetCreateEntityScript` | `virtual`, never overridden by anyone; the config-driven SQL path is the intended route. |

<a id="columndelimiter"></a>
## `ColumnDelimiter` — read before setting it

`ColumnDelimiter` is used in exactly one place, `GetFieldName` (`Utilities.cs:272-289`):

```csharp
if (FieldName.IndexOf(" ") != -1)                       // only when the name contains a space
{
    if (ColumnDelimiter.Length == 2)
        retval = $"{ColumnDelimiter[0]}{FieldName}{ColumnDelimiter[1]}";   // "[]" → [Field]
    else
        retval = $"{ColumnDelimiter}{FieldName}{ColumnDelimiter}";         // "'"  → 'Field'
}
```

Two consequences that affect every driver:

**A single-character delimiter produces a string literal, not an identifier.** The base default is
`"''"` and six drivers override to `"'"`; both render `Cust Id` as `'Cust Id'`. In
`WHERE 'Cust Id' = @p` that is a constant compared to a parameter — zero rows matched, and this class
reports zero rows as success. In `ORDER BY 'Order Id'` it is a constant sort key, so paging returns
overlapping and missing rows. Only SQLite, with `"[]"`, currently produces a correct quoted
identifier.

**If you set it, use a two-character open/close pair**: `"[]"` for SQL Server, `` "``" `` for MySQL,
`"\"\""` for PostgreSQL, Oracle and standard SQL.

**Quoting only happens when the name contains a space.** Reserved-word columns (`Order`, `User`,
`Key`, `Group`, `Date`, `Value`) and case-sensitive PostgreSQL identifiers are never quoted. Until
that is fixed centrally, a driver cannot work around it.

## `ParameterDelimiter` — it must be a named-parameter prefix

`GetInsertString` emits `{ParameterDelimiter}p_{name}` and the binder names the parameter
`{ParameterDelimiter}p_{name}`. Setting `"?"` — as Hana, Snowflake and Presto currently do —
produces `?p_Name` as both the placeholder and the parameter name, which neither positional nor
named binding accepts. **Those three drivers cannot write today.** Use the provider's real named
prefix.

## Adding dialect behaviour the extension points do not cover

Two routes, in order of preference.

**1. Use the BeepDM helper library.** `../../../BeepDM/DataManagementEngineStandard/Helpers/RDBMSHelpers/`
already covers most of what a new engine needs:

| Function | Use for |
|---|---|
| `RDBMSHelper.GetPagingSyntax` | LIMIT/OFFSET vs OFFSET/FETCH vs ROWNUM per engine |
| `RDBMSHelper.GenerateFetchLastIdentityQuery` | Identity read-back |
| `RDBMSHelper.GetTableExistsQuery`, `.GetColumnInfoQuery` | Catalog probes |
| `RDBMSHelper.GetTruncateTableQuery`, `.GetRecordCountQuery` | Common statements |
| `RDBMSHelper.GetMaxIdentifierLength` | Identifier truncation (the base hardcodes 30) |
| `RDBMSHelper.SafeQuote`, `.SupportsFeature` | Quoting and capability checks |
| `EntityHelpers/DatabaseEntityReservedKeywordChecker` | Deciding when quoting is required |
| `DMLHelpers/DatabaseDMLSpecificHelpers.GetAutoIncrementSyntax` | Identity DDL |

`RDBSource` currently calls only five of these. Prefer adding a case there over adding one here.

**2. Add a `switch (DatasourceType)` case in the base.** Last resort, and there are far fewer than
there were: the paging switches were consolidated onto one dialect source, the dead query builder
took several with it, quoting moved to one `QuoteIdentifier`, and multi-row INSERT support moved to
`RDBMSHelper.SupportsFeature`. What remains is where no shared equivalent exists — the temp-table
and MERGE builders for the three engines that have them, `GetFallbackDbType` /
`NormalizeDbTypeForProvider`, and `GetHealthCheckQuery`.

Before adding one, decide which question you are answering. If it is *"what does this engine
support"* or *"what is this engine's syntax for X"*, it belongs in BeepDM's dialect table, where
every consumer sees the same answer — add the `DatabaseFeature` member or helper there and call it
from here. If it is *"what can this class emit"*, like `SupportsTempTables`, it belongs here, and
should say so in a comment so nobody later "tidies" it into the shared table and routes engines into
a `NotSupportedException`.

## What the class needs from `IDMEEditor`

`RDBSource` takes an `IDMEEditor` in its constructor and used it as a service locator throughout —
about 300 references. Most of that was never service lookup: 222 of them were `ErrorObject` and
`AddLogMessage`, which is error reporting, and it now goes through `SetFailure`, `SetSuccess` and
`HandleDatabaseError`, all of which guard both error objects and log without depending on either
being present.

What is genuinely required is small, and each dependency now names itself when it is missing rather
than surfacing as "Object reference not set" from somewhere unrelated:

| Service | Needed for | Without it |
|---|---|---|
| `ConfigEditor` | The query catalogue behind `GetEntitesList`, `GetChildTablesList`, `GetTablesFKColumnList` | Those three fail with a message naming `ConfigEditor`. A driver that sets `GetListofEntitiesSql` does not need it for the table list. |
| `Utilfunction` | Resolving the driver config for `GetDataAdapter`; converting a `DataTable` in `UpdateEntities` | Both fail with a named message. |
| `assemblyHandler` | Loading the provider's adapter and command-builder types by name | `GetDataAdapter` returns `null` and says why. |
| `typesHelper` | Per-datasource type-name mapping in DDL generation | Already optional — falls back to `GetFallbackDbType`. |
| `ETL` | Progress bookkeeping in `UpdateEntities` | Optional; the update runs, the script counters are skipped. |
| `GetDataSource` | Called twice in the FK script generators, result discarded | Optional; guarded. |

Everything else — CRUD, query, paging, transactions, bulk, `GetEntityStructure`, `GetTableSchema`,
`CreateEntityAs` and DDL generation — runs with **no** `IDMEEditor` at all. That is what
`tests/RDBDataSource.Tests` does: the whole suite drives a real `RDBSource` with a null editor and a
null logger.

If you are writing a driver, this matters in one practical way: do not assume `DMEEditor` is
non-null in an override. Report through the base class's `SetFailure`/`HandleDatabaseError` rather
than calling `DMEEditor.AddLogMessage` directly, and your driver stays testable the same way.

## In-memory and file-based engines

Inherit `InMemoryRDBSource` instead, as `SQLiteDataSource` and `DuckDBDataSource` do. It adds
`IInMemoryDB` with ETL-driven `LoadData` / `SyncData` / `RefreshData` / `CreateStructure`. Read
[06-schema-types.md](06-schema-types.md#inmemoryrdbsource-schema-behaviour) first — several of those
methods do not do what their names suggest, and its `Dispose` performs database work.

`DuckDBDataSource` is the cautionary example: it needed more than the base offered and ended up
overriding about twenty members, including the entire CRUD surface and the transaction trio. If you
find yourself heading that way, fix the base instead — the whole point of the class is that a fix
there reaches every relational driver.

## Before you ship

- Build the driver **and** `DataSourcePluginSolution.sln`; a base-class change reaches 14 drivers.
- Test against the real engine, not SQLite. SQLite masks three whole classes of defect here: it does
  not enforce the command/transaction association, it is the only driver with a correct
  `ColumnDelimiter`, and it tolerates the `'literal' = @p` predicate that other engines reject.
- Test with `Logger == null` at least once. Roughly forty failure paths in this class report only
  through `AddLogMessage`, which does nothing without a logger — see
  [01-error-model.md](01-error-model.md).
- Set `ErrorObject` explicitly on both success and failure in anything you override; do not rely on
  `AddLogMessage` to do it.
- Check [10-known-issues.md](10-known-issues.md) before concluding a bug is yours.
