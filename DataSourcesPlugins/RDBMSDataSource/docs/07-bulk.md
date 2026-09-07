# Bulk operations

Covers `RDBSource.BulkOperations.cs` (1086 lines).

There is **no bulk delete**, despite what the file's class summary says.

## Public surface

| Member | Line | Default |
|---|---|---|
| `DefaultBatchSize` | 29 | 1000 |
| `MaxParametersPerBatch` | 35 | 2000 |
| `EnableBulkOptimizations` | 40 | `true` |
| `UseBulkTransactions` | 45 | `true` |
| `BulkInsertEntities<T>` | 60 | — |
| `BulkInsertEntitiesAsync<T>` | 124 | — |
| `BulkUpdateEntities<T>` | 417 | — |
| `BulkUpdateEntitiesAsync<T>` | 467 | — |

## Strategy selection

Two capability switches decide which path runs:

```csharp
// engine grammar -- delegated to the shared dialect table
private bool SupportsMultiRowInsert()
    => RDBMSHelper.SupportsFeature(DatasourceType, DatabaseFeature.MultiRowInsert);

// what THIS class can emit -- deliberately not delegated
private bool SupportsTempTables()              // SqlServer, Mysql, Postgre
```

The two look alike and are not the same kind of question.

`SupportsMultiRowInsert` asks whether the engine accepts `INSERT INTO t (a, b) VALUES (1, 2),
(3, 4)`. That is a property of the engine's grammar, so it lives in
`RDBMSHelper.SupportsFeature` where every consumer sees the same answer. It used to be a four-arm
switch naming SQL Server, MySQL, PostgreSQL and SQLite, so MariaDB, AzureSQL, CockroachDB, DuckDB,
DB2, Snowflake, Spanner and Presto/Trino all reported `false` and fell back to a statement per row --
correct, but far slower than the grammar they support. Oracle, Firebird, Hana, SQL Server Compact
and VistaDB stay `false`, and not for speed: they reject the grammar outright and need a different
statement shape entirely (`INSERT ALL`, `INSERT INTO ... SELECT ... UNION ALL`).

`SupportsTempTables` asks whether **this class** has a temp-table builder for the engine, and it has
exactly three -- `CreateTempTableForUpdate` and `BuildMergeUpdateQuery` throw `NotSupportedException`
for anything else. Answering that from an engine-capability table would route Oracle straight into
that throw. The list and those two switches have to be widened together.

- **Insert**: `SupportsMultiRowInsert()` → `BulkInsertMultiRow` (one `INSERT … VALUES (…),(…),…`
  per batch). Otherwise → `BulkInsertBatched` (a loop calling `InsertEntity` per row inside a
  transaction).
- **Update**: `EnableBulkOptimizations && SupportsTempTables()` → `BulkUpdateWithTempTable`
  (create temp table, bulk-insert into it, then MERGE / UPDATE-JOIN / UPDATE-FROM).
  Otherwise → `BulkUpdateBatched` (a loop calling `UpdateEntity`).

Everything not named in those two switches — Oracle included — takes the batched path.

## What "atomicity" means here

`UseBulkTransactions` is documented as ensuring atomicity. It does not, in two distinct ways.

**The transaction is opened inside the batch loop.** A failure in batch 5 rolls back only batch 5;
batches 1–4 are already committed and are never compensated. There is also no fallback to
row-by-row when a multi-row batch fails — the strategy is chosen up front and never revisited.

**The row-level commands never join the transaction.** This is the more serious problem:

```csharp
// BulkOperations.cs:312-331, and the same shape at :370, :635, :693
if (UseBulkTransactions && Dataconnection.ConnectionProp.Database != null)
{
    using (var transaction = RDBMSConnection.DbConn.BeginTransaction())
    {
        try
        {
            foreach (var entity in batch)
            {
                var result = InsertEntity(entityName, entity);
```

The local `transaction` is never published to `_activeTransaction`, so `InsertEntity` →
`GetDataCommand()` sees `ActiveTransaction == null` and leaves `cmd.Transaction` unset. That is
exactly the failure `Transaction.cs:29-32` documents:

> ExecuteNonQuery requires the command to have a transaction when the connection assigned to the
> command is in a pending local transaction.

Since `UseBulkTransactions` defaults to `true` and Oracle is absent from `SupportsMultiRowInsert()`
-- correctly, since Oracle has no multi-row `VALUES` grammar -- Oracle took the row-at-a-time path
and **Oracle bulk insert failed out of the box**. Only SQLite survived, because it does not enforce
the command/transaction association. `:202` compounded it by overwriting `cmd.Transaction` with the
local transaction, silently taking work out of a caller's outer transaction scope. Both are fixed;
see K8.

The guard itself is also wrong: `Dataconnection.ConnectionProp.Database != null` tests whether a
database *name* is configured, which has nothing to do with transaction support, and dereferences
`Dataconnection` and `ConnectionProp` without null checks.

## The multi-row INSERT builder

`BuildMultiRowInsertCommand` (`:740`) is a second, independent implementation of INSERT generation
that disagrees with `GetInsertString` on almost every axis:

| | `GetInsertString` (CRUD) | `BuildMultiRowInsertCommand` (bulk) |
|---|---|---|
| Parameter names | `p_{FieldName}` | `p{index}` |
| Schema | `GetTableName` → `schema.Table` | `$"{SchemaName}{table}"` — **no separator** |
| `DbType` | set from `GetDbType` | **never set** |
| Value conversion | `ConvertToDbTypeValue` | none |
| Reflection target | `InsertedData.GetType()` | **`typeof(T)`** |

The last two rows are defects, not just differences:

```csharp
// BulkOperations.cs:763-769
var property = typeof(T).GetProperty(field.FieldName)
    ?? typeof(T).GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
var value = property?.GetValue(entity);
...
param.Value = value ?? DBNull.Value;
```

When `T` is `object` — which is what the ETL layer passes — every lookup returns `null`, every
parameter becomes `DBNull.Value`, and **the bulk insert writes a table full of NULLs and reports
success**. Reflect on `entity.GetType()`, as the CRUD path does.

And because no `DbType` is set, nulls arrive untyped (SqlClient infers `NVarChar`, Oracle throws)
and `byte[]`, `Guid`, `decimal` and `DateTime` values are left entirely to provider inference.

Schema concatenation without a `.` appears at four sites here — `:746`, `:885`, `:907`, `:927` —
producing `INSERT INTO dboCustomers`.

## Temp tables

`BuildSqlServerTempTableCreate` (`:945`), `BuildMySqlTempTableCreate` (`:961`) and
`BuildPostgreSqlTempTableCreate` (`:977`) differ only in the `CREATE` / `CREATE TEMPORARY` /
`CREATE TEMP` keyword. All three share the same broken column loop:

```csharp
var columns = DataStruct.Fields.Select(f => $"{GetFieldName(f.FieldName)} {f.Fieldtype}");
```

`EntityField.Fieldtype` holds a **.NET** type name, so this emits
`CREATE TABLE #t (Name System.String, Id System.Int32)` — rejected by every server.
`GenerateCreateEntityScript` already solved exactly this problem at `DMLGeneration.cs:436-452` using
`GetFallbackDbType` and `NormalizeDbTypeForProvider`; that fix was never carried across. With
`EnableBulkOptimizations` defaulting to `true`, `BulkUpdateEntities` therefore fails on its first
statement on all three engines that `SupportsTempTables()` claims to support.

Further temp-table problems:

- **The name uses SQL Server's `#` prefix everywhere** (`:528`, `:577`), which is not a legal
  unquoted identifier start in MySQL or PostgreSQL. At `44 + entityName.Length` characters it also
  exceeds their 63/64-character limits for a moderately named table, and `entityName` is
  interpolated raw.
- **The created columns and the populated columns differ.** The `CREATE` uses all
  `DataStruct.Fields`; `InsertIntoTempTable` reuses `BuildMultiRowInsertCommand`, which filters
  `!f.IsAutoIncrement`. If the key is auto-increment it is never populated, so the MERGE's
  `ON target.Id = source.Id` matches nothing — zero rows updated, reported as `Errors.Failed`.
  `BuildMergeUpdateQuery` uses a third rule again (`!f.IsKey && !f.IsAutoIncrement`).
- **`DROP TABLE` has no `IF EXISTS`** (`:999`, `:1021`) and both drop paths are wrapped in
  completely empty catches (`:1003`, `:1030`), so a failed drop leaves the temp table for the life of
  the connection with no log line.

## Batch sizing

`MaxParametersPerBatch` is applied to inserts (`:92-93`, `:150-151`) but **not** to updates:
`BulkUpdateEntities:439-448` passes the raw `batchSize` into `InsertIntoTempTable`, which builds a
multi-row insert across *all* `DataStruct.Fields`. At the default 1000 rows × 20 columns that is
20,000 parameters, far past SQL Server's 2,100 limit.

Batching itself is `O(n²)`: every path uses `entities.Skip(i).Take(batchSize).ToList()` on a
`List<T>` (`:191, 247, 310, 368, 538, 587, 633, 691`), and `Skip` re-enumerates from the start each
iteration. `GetRange` would be `O(batch)`.

## Success accounting

Insert and update use different criteria:

```csharp
// :110  insert
ErrorObject.Flag = successfulRows == totalRows ? Errors.Ok : Errors.Failed;
// :453 and :504  update
ErrorObject.Flag = successfulRows > 0 ? Errors.Ok : Errors.Failed;
```

So a bulk update of 10,000 rows that updated one reports success. Worse, `successfulRows` in the
batched paths counts rows where `UpdateEntity` returned `Errors.Ok` — which, per
[04-crud-dml.md](04-crud-dml.md), **includes rows where zero rows were affected**. Decide the
zero-rows contract before touching either.

The insert criterion has its own flaw: it sums `ExecuteNonQuery()` return values, which are `-1`
under `SET NOCOUNT ON` and inflated by triggers, so a fully successful insert can report
`Errors.Failed`.

The partial-failure message is always wrong:

```csharp
// :112-116
catch (Exception ex)
{
    HandleDatabaseError(ex, entityName, "BulkInsert");
    ErrorObject.Message += $" ({successfulRows}/{totalRows} rows inserted before error)";
}
```

`successfulRows` is only assigned when the helper *returns*, so an exception thrown inside it leaves
it at `0` — the message reads "0/N rows inserted" precisely when batches have already been
committed and the true count matters.

## Cancellation

Partial at best:

- The token is checked only at batch boundaries (`:245`, `:366`, `:585`, `:689`).
- It is **not** passed to the per-row work — `InsertEntityAsync` and `UpdateEntityAsync` have no
  token parameter at all.
- The temp-table cleanup at `:612` sits in a `finally` and is handed the **already-cancelled** token,
  so it throws immediately and the empty catch at `:1030` swallows it: **the temp table leaks on
  every cancellation**.
- The synchronous bulk methods have no cancellation at all.
- `ConfigureAwait(false)` is now on every `await` in the file, as it is across the class. It was
  absent everywhere, in a library its own CLAUDE.md notes is called from WinForms and WPF hosts —
  the classic shape of a UI deadlock.

## Other notes

- `BulkInsertEntitiesAsync` drops the null-structure guard its sync twin has (`:72-77`), then
  dereferences `DataStruct.Fields` at `:150`. Both update overloads omit it too and dereference
  `DataStruct.PrimaryKeys` at `:867`.
- `GetDataCommand()` returning `null` is reported as *"Database command does not support async
  operations"* at `:249-251`, `:594-596`, `:813-815`, `:846-848`.
- The rollback-in-catch pattern at `:209`, `:268`, `:326`, `:384`, `:649`, `:706` can mask the
  original exception when `Rollback()` itself throws on a dead connection, and nothing is logged at
  that level.
- `ReportProgress` (`:1068`) fires once per batch, never at 0%, and never emits a final completion
  event on the batched paths. It sets `ParameterString1` to the entity name, whereas `CRUD.cs:345`
  uses the same field for an error message — a consumer cannot tell them apart.
