# CRUD and DML generation

Covers `RDBSource.CRUD.cs` (599 lines) and `RDBSource.DMLGeneration.cs` (961 lines).

`CRUD.cs` orchestrates; it builds no SQL text of its own. `DMLGeneration.cs` produces the statement
text, binds the parameters, and generates DDL.

## The write path

Every single-row write follows the same six steps:

1. `SetObjects(EntityName)` — populates the shared `DataStruct`, `command` and `enttype` fields
   (`Query.cs:995-1026`).
2. `ErrorObject.Flag = Errors.Ok`.
3. Bump `recNumber` / `recEntity` (the ordering relative to step 1 differs between methods — pure
   copy-paste drift).
4. `usedParameterNames = new HashSet<string>()`.
5. Build the statement — `GetInsertString` / `GetUpdateString` / `GetDeleteString`.
6. `GetDataCommand()`, set `CommandText`, bind parameters, `ExecuteNonQuery()`.

| Method | Line |
|---|---|
| `UpdateEntity` | `CRUD.cs:50` |
| `DeleteEntity` | `CRUD.cs:102` |
| `InsertEntity` | `CRUD.cs:154` |
| `UpdateEntities` (collection) | `CRUD.cs:237` |
| `CreateEntities` | `CRUD.cs:372` |
| `InsertEntityAsync` / `UpdateEntityAsync` / `DeleteEntityAsync` | `CRUD.cs:405,481,529` |

The async trio are near-verbatim copies of the sync ones and have already drifted — the async insert
lost the `else { ...Errors.Failed }` branch that the sync one has when the identity scalar comes back
null.

## Values are always parameterised

**No row value is ever concatenated into SQL.** INSERT, UPDATE and DELETE all bind through
`CreateCommandParameters` / `CreateUpdateCommandParameters` / `CreateDeleteCommandParameters`
(`DMLGeneration.cs:17,69,146`). There is no SQL-injection path through the data being written.

What *is* concatenated is identifiers — table names, column names, and in DDL the type strings from
configuration. See [09-extending.md](09-extending.md) for the reserved-word and quoting consequences.

## Parameter naming, and the defect it hides

Parameter names are `p_` + the field name with whitespace collapsed to underscores, de-duplicated by
appending `_1`, `_2`, … when the name is already in `usedParameterNames`
(`DMLGeneration.cs:226-230`, `:283-287`, `:324-328`, `:367-371`).

The binder then has to find the name it generated. It does so **by substring match**:

```csharp
// DMLGeneration.cs:41, identical at :110 and :166
string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
```

and, for the UPDATE primary-key clause, by an even looser test:

```csharp
// DMLGeneration.cs:304-307
if (usedParameterNames.Contains(paramName))
{
    paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
}
```

`usedParameterNames` is a `HashSet<string>`, whose enumeration order is undefined. With columns
`Name` and `NameSuffix`, the lookup for `Name` may return `NameSuffix`. With primary key `Id` and a
SET-clause parameter `ProductId`, the WHERE clause becomes `... where Id = @p_ProductId`.

**This is the most serious defect in the class**: an UPDATE can be steered onto a row chosen by the
wrong column's value, with no error. The `_1` de-duplication scheme manufactures exactly the
`Col` / `Col_1` prefix pairs that trigger it. Tracked as T1 in
[10-known-issues.md](10-known-issues.md).

## Statement shapes

### INSERT — `GetInsertString` (`DMLGeneration.cs:195`)

```csharp
string Insertstr = "INSERT INTO " + EntityName + " (";
Insertstr = GetTableName(Insertstr.ToLower());     // adds the schema prefix — and lowercases
...
Insertstr += $"{FieldName},";
Valuestr  += $"{ParameterDelimiter}p_" + paramName + ",";
```

Auto-increment fields are skipped (`:207`). Note the `.ToLower()`: the whole fragment is lowercased
before schema qualification, which destroys identifier case on case-sensitive servers.

### UPDATE — `GetUpdateString` (`DMLGeneration.cs:243`)

```csharp
string Updatestr = @"Update " + EntityName + " set " + Environment.NewLine;
//      Updatestr = GetTableName(Updatestr.ToLower());     ← commented out
```

Schema qualification is **commented out** here, so INSERT targets `schema.Table` while UPDATE
targets `Table`. `GetDeleteString` never had it at all. On any datasource with a non-default
`SchemaName` these address different objects.

`UpdateFieldSequnce` is built first from the non-key fields (`:252-260`), then the primary keys are
appended (`:296`) so the binder walks SET columns and WHERE columns in one pass. Auto-increment
columns are **not** excluded from the SET clause, so a non-PK identity column is emitted as
`SET identcol = @p_identcol` and then never bound.

### DELETE — `GetDeleteString` (`DMLGeneration.cs:344`)

```csharp
string deleteStr = $"DELETE FROM {EntityName} WHERE ";
...
deleteStr += $"{GetFieldName(item.FieldName)} = {ParameterDelimiter}p_{paramName}";
```

### What happens when there are no primary keys

Neither UPDATE nor DELETE guards against an empty `PrimaryKeys` collection. The WHERE loop simply
produces nothing, leaving `DELETE FROM T WHERE ` — a syntax error, so it does **not** delete every
row, but the caller gets an opaque provider message rather than a clear diagnostic. On the UPDATE
side it is worse: `:295` runs `Updatestr.Remove(Updatestr.Length - 1)` first, chopping a character
off `" set \r\n"` and producing a mangled statement.

A *null* primary-key value binds `DBNull`, so `WHERE id = NULL` matches nothing — zero rows, which
this class reports as success (see below). In DELETE it throws instead: `:175` lacks the
`?? DBNull.Value` coalesce that its two sibling binders have.

## Identity fetch-back

After a successful INSERT, `CRUD.cs:186-219` optionally reads the generated key back:

```csharp
string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
var pkField = DataStruct.PrimaryKeys.Count() > 0 ? DataStruct.PrimaryKeys.First() : null;
if (fetchIdentityQuery.ToUpper().Contains("SELECT") && pkField != null && pkField.IsAutoIncrement)
{
    cmd.CommandText = fetchIdentityQuery;
    object result = cmd.ExecuteScalar();
    ...
}
```

The `pkField.IsAutoIncrement` guard is deliberate and the comment above it explains why: every
provider's `last_insert_rowid()` equivalent returns *something* after a successful insert regardless
of the declared key, and on SQLite that implicit ROWID was silently overwriting client-generated
TEXT/GUID primary keys on the in-memory object. Keep the guard.

One rough edge remains: the identity query is executed on the **same command**, whose
`Parameters` collection still holds every INSERT parameter. SqlClient and SQLite tolerate the unused
parameters; MySqlConnector and some ODBC bridges do not. There is no `cmd.Parameters.Clear()`.

## Zero rows affected is reported as success

```csharp
// CRUD.cs:74-78, and the same shape at :126, :506, :553
int rowsUpdated = cmd.ExecuteNonQuery();
if (rowsUpdated == 0)
{
    string msg = $"No records updated in {EntityName}";
    DMEEditor.AddLogMessage("Beep", msg, DateTime.Now, 0, null, Errors.Failed);
}
```

`ErrorObject.Flag` was set `Ok` at entry and is never changed, so the method returns success. This
matters well beyond the single-row case: `BulkOperations` counts successful rows by exactly this
flag (`:321,338,644,660,701,721`), so a bulk update that changed nothing reports N successes. It also
combines with the identifier-quoting defect (T3) to make a silently-no-op DELETE look like it worked.

`InsertEntity` is the exception — its `else` branch does set `Errors.Failed` for "No records
inserted" (`:221-225`).

## `UpdateEntities` — the collection overload

Three problems worth knowing before using it:

- **It throws before its own null check.** `CRUD.cs:295` does
  `DMEEditor.ETL.ScriptCount += srcList.Count;` **outside** the `try` that starts at `:298`, while
  the `if (srcList != null)` guard is at `:300`. If the input matched none of the four
  `FullName.Contains(...)` tests, the NRE escapes the method entirely.
- **Its type dispatch does not work for typed lists.** `:272-275` tests
  `UploadData.GetType().FullName.Contains("List")` and then casts to `IList<object>`.
  `List<Customer>` does not implement `IList<object>`, so this is an unavoidable
  `InvalidCastException`. The `Contains("IEnumerable")` branch at `:278` can never match a concrete
  type's `FullName` and is dead.
- **It always reports success.** `:353` logs `"Finished Uploading Data to {EntityName}"` with
  `Errors.Ok` even if every row threw in the per-row catch at `:340`, which discards its exception
  variable. The catch at `:360` sets `ErrorObject.Ex` but not `Flag`, logs nothing, and disposes the
  shared `command` field without clearing `ObjectsCreated`.

The progress machinery in this method is inert: `msg` (`:315`) is never assigned,
`percentComplete` is computed and discarded, and the fully-populated `PassedArgs` at `:323-330` is
never raised because `PassEvent?.Invoke` at `:337` is commented out.

## DDL generation

`GenerateCreateEntityScript` (`DMLGeneration.cs:382`) builds `CREATE TABLE`, with type names
resolved through `GetFallbackDbType` and `NormalizeDbTypeForProvider` (`:851`, `:813`).
`CreatePrimaryKeyString` (`:630`) and `CreateAlterRalationString` (`:666`) add the key and foreign-key
clauses.

Points to be aware of:

- **DDL quotes identifiers differently from DML.** `:418-423` special-cases MySQL only — replacing
  spaces with underscores and backticking — while `GetFieldName` (used by DML) keeps the space. So a
  column with a space is *created* as `My_Col` and *inserted into* as `My Col`. Every other provider
  emits the bare name.
- **The primary-key clause is never quoted**, even for MySQL (`:648` appends `dbf.FieldName` raw),
  so it disagrees with the backticked column list above it.
- **Foreign-key constraint names are random** — `Random.Shared.Next(10, 1000)` (`:685`). 990 possible
  suffixes, non-deterministic and non-idempotent; re-running a migration adds duplicate constraints
  under different names.
- **FK scripts are generated twice.** `GetDDLScriptfromDatabase:599` calls
  `GenerateCreatEntityScript`, which already calls `CreateForKeyRelationScripts` at `:569`; then
  `:602` calls `CreateForKeyRelationScripts` again and appends the result.
- **`CreateAutoNumber` never signals failure.** Its catch (`:797-800`) logs but leaves the flag `Ok`,
  so the caller's `if (ErrorObject.Flag == Errors.Ok)` guard at `:456-468` passes and an **empty**
  identity clause is appended — the table is created without the identity property, silently.
- **Both `GenerateCreatEntityScript` overloads are now `public virtual`**, and
  `GenerateCreateEntityScript(EntityStructure)` — which builds the statement — is
  `protected virtual`, so a driver can specialise DDL generation. They used to be non-virtual, which
  is why the one driver that needed different DDL, DuckDB, abandoned the base class instead.
- **The four `Task.Run(...).Wait()` calls are gone.** They wrapped synchronous methods purely to
  block on them: the caller's thread blocked anyway, one pool thread was consumed per call, a limited
  scheduler could deadlock on it, and `.Wait()` rewrapped whatever the method threw in an
  `AggregateException`, so the catch logged "One or more errors occurred." instead of the failure.
  They are direct calls now.

## Type conversion on the way in

Parameters get their `DbType` from `GetDbType(field.Fieldtype)` and their value from
`ConvertToDbTypeValue(value, field.Fieldtype)`, both in `RDBSource.TypeMapping.cs` and both
`private`. See [06-schema-types.md](06-schema-types.md#type-mapping) — the short version is that an
unrecognised `Fieldtype` silently becomes `DbType.String` with the value passed through unconverted,
and all parsing is culture-sensitive.
