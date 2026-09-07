# The error model

Read this first. How `RDBSource` reports success and failure is unusual, and it explains why
several behaviours elsewhere in the class look the way they do.

## The three channels

A method in this class can report an outcome through any of three channels, and different methods
pick different ones:

| Channel | What it is |
|---|---|
| `ErrorObject` | The `IErrorsInfo` handed to the constructor as `per` and stored on the instance (`RDBSource.cs:84`). |
| `DMEEditor.ErrorObject` | The engine-wide error object. |
| The return value | Most methods return `IErrorsInfo` — sometimes `ErrorObject`, sometimes `DMEEditor.ErrorObject`. |

`HandleDatabaseError` (`Utilities.cs:27-42`) is the only helper that treats all three coherently:

```csharp
ErrorObject.Ex = ex;
ErrorObject.Flag = Errors.Failed;
ErrorObject.Message = errorMsg;
DMEEditor.ErrorObject.Message = errorMsg;
DMEEditor.ErrorObject.Flag = Errors.Failed;
DMEEditor.AddLogMessage("Beep", logMessage, DateTime.Now, 0, entityName, Errors.Failed);
```

It is used from `CRUD.cs:88,140,230,472` and `BulkOperations.cs:114,169,457,508`. Notably, neither
`GetDataCommand` nor `GetDataAdapter` — in the same file that defines it — uses it. That
inconsistency is the root of most of the flag problems below.

## Are `ErrorObject` and `DMEEditor.ErrorObject` the same object?

Usually yes, by accident rather than design.

The standard construction path passes the engine's own error object as `per`
(`../../../BeepDM/DataManagementEngineStandard/Helpers/DataSourceLifecycleHelper.cs:386`):

```csharp
var args = new object[] { connection.ConnectionName, editor.Logger, editor,
                          connection.DatabaseType, editor.ErrorObject };
```

So for a datasource created the normal way, `this.ErrorObject` and `DMEEditor.ErrorObject` are the
same instance, and setting one does set the other. `DMEEditor.ErrorObject` itself is assigned once
in the engine's constructor and nulled on dispose.

**Do not rely on this.** It is an aliasing coincidence, not a contract:

- A driver or test that constructs an `RDBSource` directly with its own `IErrorsInfo` breaks it.
  `RDBSource`'s constructor does not validate `per` and does not reconcile the two.
- Anything that assigns a *different* instance to `DMEEditor.ErrorObject` breaks it for every
  datasource that was constructed earlier.
- `InMemoryRDBSource.cs:355,396,509` do `DMEEditor.ErrorObject = ExecuteSql(sql);`. `ExecuteSql`
  returns *this datasource's* `ErrorObject`, so on those lines the engine-wide error object is
  re-pointed at one datasource's field. Today that happens to preserve the alias; with a
  differently-constructed datasource it would hijack engine-global state.

Because of the aliasing, code that sets one object and returns the other mostly works today. Treat
every such site as latent rather than fixed.

## The unconditional problem: a null logger swallows failures

This one does not depend on aliasing.

`DMEEditor.AddLogMessage` returns **before** it records the flag when no logger is attached
(`../../../BeepDM/DataManagementEngineStandard/Editor/DM/DMEEditor.cs:200-210`):

```csharp
public void AddLogMessage(string logType, string logMessage, DateTime logDate,
                          int recordId, string miscData, Errors flag)
{
    try
    {
        if (Logger == null)
            return;                       // ← returns here

        string formattedMessage = $"{logType}: {logMessage}";

        ErrorObject.Flag = flag;          // ← never reached
        ErrorObject.Message = formattedMessage;
        ...
```

Across `RDBSource` roughly forty failure paths report *only* by calling
`AddLogMessage(..., Errors.Failed)` and never set a flag themselves. In any configuration without a
logger — headless services, tests, tooling — every one of those failures reports success.

## The pattern to recognise

Most methods here open with an optimistic flag:

```csharp
ErrorObject.Flag = Errors.Ok;      // set before doing any work
```

…and then never set `Errors.Failed` on their own failure branches, relying on `AddLogMessage` to do
it. When that call is a no-op, the optimistic `Ok` is what the caller sees.

Known instances, all verified:

| Method | File | What happens |
|---|---|---|
| `GetDataCommand` | `Utilities.cs:94,115,126,131` | Returns `null` when the connection is closed, flag left `Ok`. |
| `ExecuteSql` | `Query.cs:121-158` | `if (cmd != null) { … }` with **no `else`** — returns `Ok` without executing anything. |
| `GetDataAdapter` | `Utilities.cs:234-244` | Swallows the command-builder failure, then sets `Errors.Ok` unconditionally. |
| `GetDataAdapter` early exits | `Utilities.cs:154,161,169,180` | Four `return null` paths that never touch the flag. |
| `GetScalar` / `GetScalarAsync` | `Query.cs:61,105` | Return `0.0` on failure — indistinguishable from a real zero. |
| `RunQuery` | `Query.cs:187,202` | Returns empty, flag `Ok`. |
| `GetEntity` (paged) | `Query.cs:808,840` | `return null`, flag `Ok`. |
| `GetTableSchema` | `Schema.cs:796` | Logs `Failed`, never sets the flag; returns an empty `DataTable`. |
| `GetEntitesList` | `Schema.cs:434` | Logs `Failed`, never sets the flag. |
| `GetChildTablesList` / `GetTablesFKColumnList` | `Schema.cs:729,815` | `return null` on an empty SQL template, flag `Ok`. |
| `Openconnection` / `Closeconnection` | `Connection.cs` | Never touch `ErrorObject` at all. |
| `CloseConn` | `RDBDataConnection.cs:219,226-230` | Sets `Ok` *before* `Close()`, never corrects it if `Close()` throws. |
| `CreateAutoNumber` | `DMLGeneration.cs:797-800` | Flag stays `Ok`, so the caller's `if (ErrorObject.Flag == Errors.Ok)` guard passes and an empty identity clause is appended. |

### The end-to-end consequence

These compose into a specific, reproducible lie:

1. The connection is closed.
2. `GetDataCommand()` returns `null` with `Flag == Ok` (`Utilities.cs:115`).
3. `ExecuteSql` skips its `if (cmd != null)` body and returns `Ok` (`Query.cs:136`).
4. `CreateEntityAs` checks `if (DMEEditor.ErrorObject.Flag == Errors.Failed)`, sees `Ok`, adds the
   entity to `Entities` and logs **"Entity 'X' created successfully"** (`Schema.cs:635-647`) — for a
   `CREATE TABLE` that never ran.

`RunScript` reported the same for un-executed DDL, and `InMemoryRDBSource.cs:356,397` consumed the
same `Ok` as proof that a table was truncated, then reloaded data on top of rows that were still
there.

`RunScript` had a second problem on the same three lines: it ran `ExecuteSql` through
`Task.Run(...).Wait()`, so any exception reached its caller rewrapped in an `AggregateException`
whose message is "One or more errors occurred." — in the method whose entire job is to say what
happened. It calls `ExecuteSql` directly now and returns that result rather than assigning it to
`DMEEditor.ErrorObject`.

## The inverse: success reported as failure

`AddLogMessage` writes `ErrorObject.Flag = flag` on **every** call, including `Errors.Ok` and
`Errors.Warning`. Two consequences:

- **A success log clears a real failure.** `InMemoryRDBSource.SyncEntitiesNameandEntities` logs
  `Errors.Ok` per entity at `:161` and `:194`. Any `Failed` set earlier in the same call chain is
  erased before `SaveStructure` returns at `:131`.
- **A stale flag is returned as the result.** `Commit` (`Transaction.cs:117-133`) returns
  `DMEEditor.ErrorObject` but never sets it to `Ok` on success, so it carries whatever the previous
  unrelated operation left. `UnitofWork` branches on that value.
- **A late throw inverts a completed commit.** `Commit` calls `DisposeActiveTransaction()` in its
  `finally`; if `Dispose()` throws *after* `Commit()` succeeded, the catch logs `Errors.Failed`
  (`Transaction.cs:147`). The data is committed and the caller is told it failed.

Historically this shape also produced a Debug-only bug in `GetEntitesList`, where a diagnostic trace
of a *successful* query was logged as `Errors.Failed`. That one is fixed, and the fix carries a
comment explaining why (`Schema.cs:396-403`) — worth reading as the model for the rest.

## Guidance

When adding or changing code in this class:

- Call `HandleDatabaseError(ex, entityName, operation, sql)` on failure. Do not rely on
  `AddLogMessage` to set the flag.
- Set `Errors.Ok` explicitly on the success path, immediately before returning — not optimistically
  at the top of the method.
- Return the same object you set. If you set `ErrorObject`, return `ErrorObject`.
- Never log `Errors.Ok` or `Errors.Warning` in the middle of a multi-step operation; it clears any
  failure recorded so far.
- If a method can return `null` or an empty result for a non-exceptional reason, say so through the
  flag, or the caller cannot distinguish "no rows" from "no connection".

See [10-known-issues.md](10-known-issues.md) for the full defect register.
