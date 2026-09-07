# Lifecycle — construction, connection, state, disposal

Covers `RDBSource.cs`, `RDBSource.Connection.cs`, `RDBSource.Dispose.cs`, `RDBDataConnection.cs`,
and the command-creation half of `RDBSource.Utilities.cs`.

## Construction

```csharp
public RDBSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor,
                 DataSourceType databasetype, IErrorsInfo per)
```

The constructor (`RDBSource.cs:149-162`) assigns the five arguments, sets
`Category = DatasourceCategory.RDBMS`, and creates the connection wrapper:

```csharp
Dataconnection = new RDBDataConnection(DMEEditor)
{
    Logger = logger,
    ErrorObject = ErrorObject,
};
```

Two things it does **not** do, which every driver must account for:

- **No argument validation.** `per` and `pDMEEditor` are stored without null checks. Every
  `ErrorObject.Flag = Errors.Ok;` in the class then NREs if a driver passed null.
- **No connection wiring.** `Dataconnection.ConnectionProp` and `Dataconnection.DataSourceDriver`
  are never populated here. Until something sets them, `OpenConn()` finds `DataSourceDriver == null`,
  falls through to `DbConn == null`, and returns `ConnectionState.Broken`. In the normal flow
  `DataSourceLifecycleHelper` resolves these from `ConfigEditor.DataConnections` after construction.

## Properties worth knowing

| Property | Note |
|---|---|
| `ConnectionStatus` | `get => Dataconnection.ConnectionStatus; set { }` (`RDBSource.cs:74`). **The setter is a no-op.** Assignments to it are silently discarded. |
| `RDBMSConnection` | `(RDBDataConnection)Dataconnection` — an unguarded cast (`:114`). A driver that substitutes a different `IDataConnection` gets `InvalidCastException` on every access, including inside the `if (RDBMSConnection != null)` guards, because the cast runs before the null test. |
| `ColumnDelimiter` | Defaults to `"''"`. Used only by `GetFieldName`, and not as an identifier quote in practice; see [09-extending.md](09-extending.md#columndelimiter). |
| `ParameterDelimiter` | Defaults to `"@"`. Oracle uses `":"`, SQLite `"$"`, and Hana/Snowflake/Presto `"?"` — which does not work; see [10-known-issues.md](10-known-issues.md). |
| `Entities`, `EntitiesNames` | Plain `List<T>`, mutated from many paths with no synchronisation. `Dispose` now `Clear()`s them; it used to set them to `null`, so any post-disposal access threw. |

## Shared mutable state

This is the most important thing to understand about the class. `RDBSource.cs:32-34` and `:134-147`
declare per-operation working state as instance (and one static) fields:

```csharp
// Note: usedParameterNames is reset at the beginning of each operation, making it thread-safe per-operation
HashSet<string> usedParameterNames = new HashSet<string>();
List<EntityField> UpdateFieldSequnce = new List<EntityField>();
...
protected static int recNumber = 0;
protected string recEntity = "";
...
EntityStructure DataStruct = null;
IDbCommand command = null;
Type enttype = null;
bool ObjectsCreated = false;
string lastentityname = null;
```

**The comment on line 32 is wrong.** Resetting a shared field at the start of each operation is what
makes it *unsafe*, not safe: `usedParameterNames` is reassigned at `CRUD.cs:65,117,169,420,497,544`,
so a second concurrent call on the same datasource swaps the collection out from under the first one
mid-build. The same applies to `UpdateFieldSequnce`, and to the `DataStruct` / `command` / `enttype`
group, which `SetObjects` (`Query.cs:995-1026`) writes and seventeen CRUD, bulk and paged call sites
read.

`recNumber` is `protected static` — one counter shared by every instance of every driver in the
process — while its key, `recEntity`, is per-instance. The pairing
`if (recEntity != EntityName) { recNumber = 1; recEntity = EntityName; }` is therefore incoherent
across datasources.

`CRUD.cs:363` disposes the shared `command` field without clearing `ObjectsCreated`, so the next
`SetObjects` for the same entity short-circuits (`Query.cs:997`) and hands back a disposed command.

**Practical rule: an `RDBSource` instance is not safe for concurrent use.** Serialise access per
datasource, or give each caller its own instance.

## Opening and closing

`RDBSource.Connection.cs` is thin delegation:

```csharp
public virtual ConnectionState Openconnection()
{
    if (RDBMSConnection != null)
        ConnectionStatus = RDBMSConnection.OpenConnection();
    return ConnectionStatus;
}
```

The assignment on line 13 is dead — `ConnectionStatus`'s setter discards. It works only because
`RDBDataConnection.OpenConnection` sets its own field, which the getter reads through. `Closeconnection`
has the same dead assignment and calls `CloseConn()` **three times** on the same object (lines 21,
22 and 27 — `RDBMSConnection` *is* `Dataconnection`). Neither method has a try/catch, and neither
touches `ErrorObject`.

Neither method rolls back an open transaction. `Dispose(bool)` handles that itself now — it rolls
back and disposes `_activeTransaction` before closing — but a bare `Closeconnection()` call still
closes the connection out from under a live transaction.

`OpenconnectionAsync(CancellationToken)` sits alongside `Openconnection` and records its outcome the
same way. It is additive: nothing on `IDataSource` declares it, so no driver has to implement it. It
exists so the resilience pipeline has something real to await — that used to be
`Task.Run(() => Openconnection(), ct)`, which parks a pool thread on a blocking open and cannot be
cancelled once the open has started. Underneath, `RDBDataConnection.OpenConnAsync` uses
`DbConnection.OpenAsync(ct)` when `DbConn` already exists and is merely `Closed` — which is what
`CloseConn` leaves behind, and the case a reconnect actually hits — and otherwise falls back to the
synchronous `OpenConn()`, which rebuilds the connection from the driver. A failed async open falls
back the same way, since rebuilding is the right recovery for an unusable connection object.

### `RDBDataConnection.OpenConn()`

The real work is in `RDBDataConnection.cs:98-211`:

1. If `DbConn` exists **and is Open**, return early.
2. Instantiate the provider connection from the driver config via the assembly handler (`:116`).
3. Build and assign the connection string (`:127`).
4. Open, either the file-based branch (`:150`) or the server branch (`:163`).
5. For Oracle and SQL Server with a non-null `SchemaName`, run a context statement (`:169-191`).

Three hazards in that sequence:

- **Connection leak on reconnect** (`:100-116`). The early return fires only for `Open`. A `Closed`
  or `Broken` `DbConn` falls through to line 116 and is **overwritten without being disposed**.
  `CloseConn()` calls `Close()`, never `Dispose()`, and never nulls the field. `DbConn` is not
  disposed anywhere in the solution, so every open→close→open cycle strands one provider connection
  object — which matters for Oracle, ODBC and SQLite, where `Dispose()` releases native handles that
  `Close()` does not.
- **The stored password is destroyed** (`:128`):
  ```csharp
  DbConn.ConnectionString = ReplaceValueFromConnectionString();
  ConnectionProp.ConnectionString = DbConn.ConnectionString;
  ```
  ADO.NET providers strip the password from the `ConnectionString` *getter* unless
  `Persist Security Info=true`. Line 128 writes that stripped string back into `ConnectionProp`; when
  `ConfigEditor` next persists the connection, the password is gone for good.
- **The SQL Server context statement is the wrong statement** (`:180`):
  ```csharp
  cmd.CommandText = $"ALTER LOGIN {ConnectionProp.UserID} with DEFAULT_DATABASE = {ConnectionProp.Database}";
  ```
  This is a permanent, server-wide change to the login object, affecting every future connection by
  that login from any application. It does not change the current session's database — that needs
  `USE`. The branch is gated on `SchemaName != null` but uses `Database`; the identifiers are
  interpolated with no quoting; the `IDbCommand` is never disposed; and a failure is logged as
  `Errors.Warning` and ignored, leaving a connection that reports `Open` while pointing at the wrong
  context.

## Command creation

Every command in the class comes from `GetDataCommand()` (`Utilities.cs:91-132`), which is also
where an open transaction is attached:

```csharp
cmd = RDBMSConnection.DbConn.CreateCommand();
var tx = ActiveTransaction;
if (tx != null) cmd.Transaction = tx;
ConfigureCommand(cmd);
```

`ConfigureCommand(IDbCommand)` (`:139`) is the one `protected virtual` hook for provider-specific
command setup; Oracle overrides it to set `BindByName`.

**`GetDataCommand()` returns `null` when the connection is not open**, with `ErrorObject.Flag` left
at `Ok`. Eleven call sites across `CRUD.cs` and `BulkOperations.cs` do not check for it. Because
`using (null)` is legal C#, the failure surfaces one line later as "Object reference not set",
losing the real cause. `GetDataCommand` now records the reason on `ErrorObject` and `GetDataReader`
returns `null` rather than dereferencing it, but `BulkOperations.cs:249-251` still reports it as
*"Database command does not support async operations"* and the `CRUD.cs` / `BulkOperations.cs` call
sites are still unguarded (K21).

## Disposal

`RDBSource.Dispose.cs` releases, in order:

| Step | Why the order matters |
|---|---|
| `_activeTransaction` | Rolled back, then disposed — **before** the connection closes. A pending local transaction whose connection is closed underneath it leaves the connection unusable on pooled providers. |
| `command` (`RDBSource.cs:144`) | The retained `IDbCommand`, plus `ObjectsCreated` and `lastentityname`, so nothing hands back a disposed command. |
| `_entityCache` | Cleared and nulled. Its loader closure captures `this`, so a surviving reference to the cache kept the disposed source alive. |
| `Closeconnection()` | The normal close path. |
| `RDBMSConnection.DbConn` | `Dispose()`d, not merely `Close()`d. `Close()` returns a pooled connection; `Dispose()` releases the native handles Oracle, ODBC and SQLite hold. |
| `Entities`, `EntitiesNames` | `Clear()`ed, **not** nulled. |

Every step runs inside a `SafelyDispose(Action step, string what)` wrapper that logs and continues:
`Dispose` must not throw, and one failing step must not skip the rest. Previously `Closeconnection()`
could throw — from the `RDBMSConnection` cast, or from an `ErrorObject` NRE inside
`RDBDataConnection.CloseConn` — and when it did, `_rdsDisposed = true` never ran,
`GC.SuppressFinalize` never ran, and the exception escaped `Dispose()`, breaking `using` blocks and
container teardown.

`Entities` and `EntitiesNames` are declared non-nullable, so nulling them turned every post-disposal
access into a `NullReferenceException`. Clearing gives callers an empty list instead. There is still
no `ObjectDisposedException` guard on individual members.

`DisposeAsync` is implemented: it disposes the provider connection through
`DbConnection.DisposeAsync` where the provider supports it, then runs the synchronous path for
everything else.

`InMemoryRDBSource` overrides `Dispose(bool)` and calls `SaveStructure()`, which issues
`GetEntitesList()` and `CreateEntityAs()` — **database round trips and `CREATE TABLE` statements,
plus a config-file write** — from inside disposal. The empty catch around it is gone, so a failure
there is now logged, but the round trips themselves remain: SQLite and DuckDB rely on that call to
persist their structure at teardown, and removing it would lose data rather than fix a defect.
Moving it to an explicit call the host drives is a contract change for both drivers and is tracked
as K23. See [06-schema-types.md](06-schema-types.md).
