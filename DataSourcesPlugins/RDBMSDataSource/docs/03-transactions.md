# Transactions

Covers `RDBSource.Transaction.cs` (155 lines).

This is the most recently repaired part of the class, and the only one whose source carries a full
explanation of what was wrong before. Read the remarks block at `Transaction.cs:17-39` — it is
accurate.

## The contract

| Method | Meaning |
|---|---|
| `BeginTransaction(PassedArgs)` | Starts a local transaction on the open connection and holds it. |
| `Commit(PassedArgs)` | Commits the held transaction, then disposes it. |
| `EndTransaction(PassedArgs)` | **Rolls back** the held transaction, then disposes it. |

`EndTransaction` means rollback, not "finish". That is the callers' contract:
`UnitofWork.Commit` calls `Commit` when every item succeeded and `EndTransaction` when one failed or
an exception escaped, and `UnitofWork.Rollback` calls `EndTransaction` directly. The remarks at
`:90-95` say so explicitly. Do not "fix" it to commit.

`FormsManager.TransactionCoordination` in BeepDM drives the same trio across multiple datasources
(`../../../BeepDM/DataManagementEngineStandard/Editor/Forms/FormsManager.TransactionCoordination.cs:119,197,224`),
so transaction semantics here are load-bearing for the Forms subsystem.

## How the transaction reaches commands

The transaction is held in a field and exposed through a guarded property:

```csharp
private IDbTransaction _activeTransaction;

public IDbTransaction ActiveTransaction =>
    _activeTransaction?.Connection != null ? _activeTransaction : null;
```

A transaction whose `Connection` has gone null has already been completed by the provider, so it is
not returned.

`GetDataCommand()` attaches it to every command it creates (`Utilities.cs:105-109`), and
`SetObjects` does the same for the command it caches (`Query.cs:1020-1021`). Those are the only two
places commands are created for the ordinary CRUD and query paths, which is why the attachment is
centralised there.

## What was broken before (2026-08-03, commit `c3901629`)

`BeginTransaction` called `DbConn.BeginTransaction()` and **discarded** the returned
`IDbTransaction`. `Commit` and `EndTransaction` then tried to recover it by reflecting a
`"Transaction"` property off the *connection*, which ADO.NET connections do not expose. Both were
silent no-ops: nothing was ever committed or rolled back.

Worse, the connection was left holding a pending local transaction, so providers that enforce the
command/transaction association rejected every subsequent command:

> ExecuteNonQuery requires the command to have a transaction when the connection assigned to the
> command is in a pending local transaction. The Transaction property of the command has not been
> initialized.

That is every client/server RDBMS this class backs — SQL Server, PostgreSQL, Oracle, MySQL.
`System.Data.SQLite` does not enforce the association, which is why testing against a single
file-based driver never showed it.

**Keep this in mind when testing anything transactional here: SQLite cannot reproduce this class of
bug.**

## Current behaviour, method by method

### `BeginTransaction`

```csharp
if (RDBMSConnection?.DbConn == null || RDBMSConnection.DbConn.State != ConnectionState.Open)
{
    DMEEditor.AddLogMessage("Beep", "Error in Begin Transaction: the connection is not open", ...);
    return DMEEditor.ErrorObject;
}

if (ActiveTransaction != null)
    return DMEEditor.ErrorObject;        // reuse, don't shadow

_activeTransaction = RDBMSConnection.DbConn.BeginTransaction();
```

Reusing an already-open transaction rather than shadowing it is deliberate — most providers throw on
a second concurrent local transaction, and shadowing would orphan the first exactly as the old code
did.

### `Commit` and `EndTransaction`

Both are the same shape: act on `ActiveTransaction?`, catch and log, and dispose in a `finally`:

```csharp
try     { ActiveTransaction?.Commit(); }      // or .Rollback()
catch   { ...AddLogMessage(..., Errors.Failed); }
finally { DisposeActiveTransaction(); }
```

`DisposeActiveTransaction()` disposes and nulls the field whatever happened, so the connection is
never left with a pending local transaction.

## Known problems

These are open; see [10-known-issues.md](10-known-issues.md).

- **Calling `Commit` without `BeginTransaction` silently succeeds.** The `?.` makes it a no-op, and
  there is no "no transaction to commit" diagnostic. Same for `EndTransaction`.
- **The return value is not a reliable signal.** All three methods set `ErrorObject.Flag = Errors.Ok`
  at entry and return `DMEEditor.ErrorObject`, which is never set to `Ok` on success — so it carries
  whatever the previous unrelated operation left. See [01-error-model.md](01-error-model.md).
- **A late `Dispose()` throw inverts a successful commit.** If `Commit()` succeeds and the
  `Dispose()` in `DisposeActiveTransaction` then throws, the catch logs `Errors.Failed` (`:147`).
  The data is committed; the caller is told it failed.
- **No connection-state check on `Commit`/`EndTransaction`.** Only `BeginTransaction` checks. If the
  connection dropped, `ActiveTransaction` returns null and the commit is a silent no-op.
- **`_activeTransaction` is unsynchronised.** It is read and written from all four methods here plus
  `GetDataCommand` and `SetObjects`, with no lock and no `volatile`. The `ActiveTransaction` getter
  reads the field twice, so it can return a transaction that `DisposeActiveTransaction` nulled
  between the two reads.
- **`Closeconnection` and `Dispose` do not roll it back.** See [02-lifecycle.md](02-lifecycle.md).

## Paths that bypass the transaction entirely

Two parts of the class create or use commands without going through `GetDataCommand()`, so they do
**not** participate in an open transaction:

- **`RDBSource.Dapper.cs`** — `GetData<T>` and `SaveData<T>` call
  `RDBMSConnection.DbConn.Query<T>(sql)` / `.ExecuteAsync(...)` directly, passing no `transaction:`
  argument. Inside a `BeginTransaction`/`Commit` scope on SQL Server, PostgreSQL, Oracle or MySQL,
  these fail with the exact error quoted above.
- **`RDBSource.BulkOperations.cs`** — the batched paths open their *own* local transaction
  (`:312`, `:370`, `:635`, `:693`) but never publish it to `_activeTransaction`, so the row-level
  `InsertEntity`/`UpdateEntity` calls inside the batch get commands with no transaction attached.
  `:202` also overwrites a caller's outer transaction on the command. See [07-bulk.md](07-bulk.md).

One further interaction worth knowing: `InMemoryRDBSource.GetDeleteAllSql` returns **TRUNCATE**,
which is DDL on Oracle and MySQL and performs an implicit commit — silently ending any transaction
opened by `BeginTransaction`.
