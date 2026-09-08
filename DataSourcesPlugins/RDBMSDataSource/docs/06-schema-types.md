# Schema discovery and type mapping

Covers `RDBSource.Schema.cs` (911 lines), `RDBSource.TypeMapping.cs` (108),
`Helpers/DbTypeMapper.cs`, `Helpers/EntityStructureCache.cs`, and the schema half of
`InMemoryRDBSource.cs`.

## How a structure is loaded

```
GetEntityStructure(name, refresh)          Schema.cs:26
        └─ EntityCache.Get(name, refresh)  RDBSource.cs:39-49
                └─ LoadEntityStructure     Schema.cs:35
                        └─ GetEntityStructure(EntityStructure, refresh)   Schema.cs:111
                                └─ GetTableSchema(name)                   Schema.cs:767
                                        └─ "Select * from {schema}.{table} where 1=2"
                                           + CommandBehavior.KeyInfo
                                           + reader.GetSchemaTable()
```

Column metadata comes from the ADO.NET schema table, read defensively through `SafeField<T>` and
`SafeShort` (`Schema.cs:94-109`), which tolerate providers that omit a column or return a different
numeric width. That defensiveness is deliberate and worth preserving — the comment about
System.Data.SQLite returning `Int32` where `Int16` is expected is accurate.

`LoadEntityStructure` calls `GetEntitesList()` when `Entities` is empty (`:39-41`), so the very
first structure lookup in a process triggers a full table-list round trip.

### `refresh: false` does nothing

The entire population body of `GetEntityStructure(EntityStructure, bool)` sits inside
`if (refresh)` (`:127`), returning `fnd` unchanged otherwise (`:265`). Combined with the cache, a
structure that is present but field-less is returned as-is. That is why `SetObjects` carries this
workaround:

```csharp
// Query.cs:1005-1008
if (DataStruct == null || DataStruct.Fields == null || DataStruct.Fields.Count == 0)
{
    DataStruct = GetEntityStructure(Entityname, true);
}
```

## Primary keys

`PrimaryKeys` is populated solely from the schema table's `IsKey` column:

```csharp
// Schema.cs:226-230
if (x.IsKey)
    fnd.PrimaryKeys.Add(x);
fnd.Fields.Add(x);
```

It ends up **empty** in six situations, all of which downstream code has to cope with:

1. `GetTableSchema` threw, so the table has no rows — the `else` at `:259` sets only
   `fnd.IsCreated = false` and leaves `Fields`/`PrimaryKeys` untouched.
2. `refresh == false`, so the population block never ran.
3. The provider omits `IsKey`, or returns it null — `SafeField` yields `false` for every column.
4. `CommandBehavior.KeyInfo` needs extra catalog permissions on SQL Server and Oracle; without them
   the provider silently reports no keys rather than erroring.
5. `Viewtype == ViewType.Query` (`:145`) — most providers report no key info for an arbitrary
   projection.
6. The Oracle FLOAT branch threw (below), aborting the field loop before `IsKey` was read at `:193`,
   while the field is still added at `:230`.

The consequences show up as `ORDER BY 1` fallbacks in paging and as WHERE-clause-less UPDATE and
DELETE statements; see [04-crud-dml.md](04-crud-dml.md) and [05-query-paging.md](05-query-paging.md).

## Vendor SQL

Most catalog queries are correctly delegated to configuration rather than hardcoded:

```csharp
DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, ..., DatasourceType)
DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getFKforTable,   ..., DatasourceType)
DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getChildTable,   ..., DatasourceType)
```

Two places break that pattern:

- `GetFloatPrecision` (`:883`) is hardcoded to Oracle's `ALL_TAB_COLUMNS` **and** interpolates the
  table and column names directly into the SQL. It also leaks its command and, on the exception
  path, its reader — and it is the only method in the file that reports through
  `Console.WriteLine` rather than `DMEEditor`.
- `GetEntityStructure(DataTable)` (`:299`) indexes `INFORMATION_SCHEMA`-flavoured column names
  (`COLUMN_NAME`, `DATA_TYPE`, `AUTOINCREMENT`, `PRIMARY_KEY`, …) with no `SafeField` guard, so it
  would throw on any provider whose `GetSchema("Columns")` differs. Both of its callers are dead, so
  it never runs today.

### The Oracle FLOAT branch is unreachable

```csharp
// Schema.cs:169-175
if (DatasourceType == DataSourceType.Oracle
 && x.Fieldtype.Equals("FLOAT", StringComparison.OrdinalIgnoreCase))
{
    int precision = GetFloatPrecision(x.EntityName, x.FieldName);
    x.Fieldtype = MapOracleFloatToDotNetType(precision);
}
```

`x.Fieldtype` was set at `:167` from `SafeField<Type>(r, "DataType")?.ToString()`, which yields
`"System.Double"` or `"System.Decimal"` — never `"FLOAT"`. So this never fires, and neither does the
`NullReferenceException` latent inside it: `x.EntityName` is never assigned (it should be `entname`),
so `GetFloatPrecision` would call `null.ToUpper()`. Both this branch and
`MapOracleFloatToDotNetType` are effectively dead. The Oracle NUMBER mapping just below it
(`:181-187`) compares against `"System.Decimal"` and does work.

## The override trap

```csharp
// Schema.cs:237
fnd.Relations = (List<RelationShipKeys>)GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName);
```

`GetEntityforeignkeys` is declared `IEnumerable<RelationShipKeys>` and is `virtual`. Any driver that
overrides it to return a LINQ projection, an array, or an iterator gets an `InvalidCastException`
here at runtime — not a compile error. **This is the most dangerous override point in the class.**
If you override it, return a `List<RelationShipKeys>`.

## `GetEntitesList` and the uppercasing problem

```csharp
// Schema.cs:410
EntitiesNames.Add(row.Field<string>("TABLE_NAME").ToUpper());
...
// Schema.cs:417
Entities.Where(p => !EntitiesNames.Contains(p.EntityName))
```

The list is uppercased on the way in, then compared with an ordinal, case-sensitive `Contains`. Any
cached entity whose name is not already uppercase is therefore judged missing and gets
`IsCreated = false; EntityType = InMemory; Drawn = false;` — **an existing physical table is
silently demoted to in-memory**. The `.ToUpper()` also corrupts case-sensitive identifiers on
PostgreSQL, Snowflake and any quoted-identifier setup.

The method also hardcodes the result column name `TABLE_NAME`, and calls `GetDataAdapter` without
checking for the `null` it can return on five separate paths.

## `GetEntityType` and generated types

```csharp
// Schema.cs:346-358
public virtual Type GetEntityType(string EntityName)
{
    EntityStructure x = GetEntityStructure(EntityName);
    var beepEntityType = DMTypeBuilder.CreateNewObject(DMEEditor, DatasourceName, DatasourceName,
                                                       EntityName, x.Fields)?.GetType();
    enttype = beepEntityType;
    return beepEntityType;
}
```

Taking the type from the object `CreateNewObject` returns — rather than reading the mutable static
`DMTypeBuilder.MyType` — is deliberate and correct; the comment at `:349-354` explains the race it
avoids. (`CRUD.cs:256` still reads that static, so the race survives one file over.)

Two things to know:

- **The namespace does not follow the repository convention.** Every other connector, and the rule
  recorded in the repo's `CLAUDE.md`, uses `"Beep." + DatasourceName`. This passes bare
  `DatasourceName`, so RDBMS entity types land in a different namespace root from all other
  drivers. If `DatasourceName` were ever empty, `DMTypeBuilder` falls back to
  `"TheTechIdea.Classes"` — the shared-namespace collision that commit `ea222c4b` fixed elsewhere.
  Nothing validates `datasourcename` in the constructor.
- **It writes shared state on every read.** `enttype = beepEntityType` clobbers the value
  `SetObjects` established for a concurrent CRUD operation, and `GetEntityType` is called from both
  `GetEntity` paths (`Query.cs:657`, `:827`).

## Type mapping

Three methods, all in `RDBSource.TypeMapping.cs`. `GetDbType` and `ConvertToDbTypeValue` are now
`protected virtual`, so a driver can specialise type mapping for its dialect; all three used to be
`private`, which is why no driver could:

| Method | Role |
|---|---|
| `GetDbType(string fieldType)` | Delegates to `DbTypeMapper.ToDbType`. |
| `ConvertToDbTypeValue(object, string)` | Pattern-matches the `Fieldtype` string and converts. |
| `TypeToDbType(Type)` | **Dead** — its only mention is inside a comment at `Utilities.cs:223`. |

`DbTypeMapper` (`Helpers/DbTypeMapper.cs`) is a static read-only dictionary keyed by exact .NET type
names, and is thread-safe. Its fallback is the problem:

```csharp
if (string.IsNullOrWhiteSpace(typeName)) return DbType.String;
return Map.TryGetValue(typeName, out var dbType) ? dbType : DbType.String;
```

Any `EntityField.Fieldtype` that holds a **SQL** type name (`varbinary`, `uniqueidentifier`,
`NUMBER`, `nvarchar`) — which is what many schema readers produce — falls through to
`DbType.String`, silently. Meanwhile `ConvertToDbTypeValue`'s default arm is `_ => value`, returning
the value unconverted. So a `byte[]` is handed to a parameter declared `DbType.String`, and a
`decimal` loses its precision and scale (neither is ever set anywhere).

Missing from the map: `DateTimeOffset`, `DateOnly`, `TimeOnly`, `BigInteger`, nullable type names,
and enums.

`ConvertToDbTypeValue` has two further sharp edges:

- **All parsing is culture-sensitive.** Every arm calls `value.ToString()` and `TryParse(string)`
  with no `CultureInfo.InvariantCulture`. On a machine set to `de-DE` or `fr-FR`, `1.5` and `1,5`
  swap meaning and `03/04/2026` changes month.
- **A failed parse returns the original value.** `"abc"` with `Fieldtype == "System.Int32"` matches
  no arm, so `_ => value` hands the string to an `Int32` parameter. The failure surfaces as an
  opaque provider error at `ExecuteNonQuery` rather than a clean conversion error here.

One unrelated mismatch worth noting: `Schema.cs:90`'s `IsNumericType` tests for `"System.Float"`,
which is not a .NET type name (it is `System.Single`), so no `float` column ever gets its precision
and scale captured.

## `EntityStructureCache`

Twenty-seven lines wrapping a `ConcurrentDictionary<string, EntityStructure>` with an
`OrdinalIgnoreCase` comparer and a loader delegate. It is created lazily per `RDBSource` instance
(`RDBSource.cs:39-49`).

`RDBSource.cs:37` describes it as "thread-safe". The dictionary is; the usage is not:

- **`GetOrAdd` does not hold a lock across the value factory.** N threads asking for the same
  uncached entity all run `LoadEntityStructure` concurrently, issuing N `GetTableSchema` calls —
  each an `ExecuteReader` — on the **single shared `IDbConnection`**. Most providers respond with
  "There is already an open DataReader associated with this Connection".
- **The lazy initialiser is a check-then-act race** (`RDBSource.cs:41-47`): two threads can each
  construct a cache, and the loser's — with everything already loaded into it — is discarded.
- `LoadEntityStructure` also mutates the shared `List<EntityStructure> Entities` and replaces
  `EntitiesNames`, neither of which is thread-safe.

And two behavioural gaps:

- **There is no invalidation and no eviction.** No TTL, no size limit, no `Remove`, no `Clear`.
  After a `CREATE TABLE` or `ALTER TABLE`, `GetEntityStructure(name)` returns the pre-DDL structure
  for the lifetime of the datasource unless a caller explicitly passes `refresh: true`.
  `InvalidateEntityCache` in `Cache.cs` does not know this class exists.
- **Failures are cached permanently.** `GetOrAdd` stores whatever the loader returned, including the
  field-less "not created" structure produced when `GetTableSchema` throws. Every later caller gets
  that.

## `InMemoryRDBSource` schema behaviour

`InMemoryRDBSource` layers ETL-driven load/sync/refresh on top. Several of its methods do not do
what their names say, which matters when reading calling code:

| Method | Actual behaviour |
|---|---|
| `LoadStructureWithData` | Never loads data. It passes `copydata: true` to `LoadStructure`, which accepts the parameter and never references it. It then raises `DataChanged`. |
| `FillFromDataSource(source, …)` | Validates `source`, then never uses it — the copy is driven by each `EntityStructure.DataSourceID` in `InMemoryStructures`. It logs success naming `source`. |
| `CreateStructure` | Discards `CreateEntityAs`'s result and sets `Entities[i].IsCreated = true` unconditionally, overwriting the `false` the override just set. `IsStructureCreated = true` is set outside the guard. `progress` and `token` are unused. |
| `LoadEntities` | Populates the object returned by `DMEEditor.GetDataSource(name)`, not `this` — and `LoadStructure` clears `this.Entities` then checks it for content, so this only works when the registry returns literally the same instance. |
| `SaveStructure` | Never sets `IsSaved`. |

It also re-declares `PassEvent` without `new`, hiding the base event (CS0108), never raises
`StructureChanged` or calls `RaiseOnCreateStructure`, and performs 15 unchecked `(PassedArgs)`
downcasts of an `IPassedArgs` — two of them outside any try/catch.

Its `Dispose` override calls `SaveStructure()`, which can issue `GetEntitesList()` and
`CreateEntityAs()` — database round trips and `CREATE TABLE` — plus a config-file write, all inside
an empty catch.
