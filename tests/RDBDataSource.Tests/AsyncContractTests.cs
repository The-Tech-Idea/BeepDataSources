using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Phase 10: the async surface doing what its signatures say.
///
/// Three things were wrong. <c>GetEntityAsync</c> offloaded an iterator, so the task completed
/// having done nothing and every byte of I/O happened later on the consuming thread. Four call
/// sites wrapped synchronous methods in <c>Task.Run</c> and then blocked on <c>.Wait()</c>, which
/// gains nothing, burns a pool thread, and rewraps failures in <c>AggregateException</c>. And no
/// <c>await</c> in the class carried <c>ConfigureAwait(false)</c>, in a library whose own
/// CLAUDE.md notes it is called from WinForms and WPF hosts.
/// </summary>
public class AsyncContractTests
{
    internal sealed class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private static SqliteHarness ProductsHarness()
    {
        var h = SqliteHarness.Create(DataSourceType.SqlLite, typeof(Product));
        h.Execute("CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Register(SqliteHarness.Entity("Products",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));
        return h;
    }

    private static int Count(IEnumerable rows)
    {
        int n = 0;
        foreach (var _ in rows) n++;
        return n;
    }

    // ---------------------------------------------------------------- K20

    [Fact]
    public async Task GetEntityAsync_CompletesWithTheRowsAlreadyRead()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name) VALUES (1,'a'),(2,'b'),(3,'c')");

        var rows = await h.Source.GetEntityAsync("Products", null);

        // Task.Run over an iterator handed back the un-enumerated sequence: the await completed in
        // microseconds having opened nothing and read nothing.
        Assert.NotNull(rows);
        Assert.IsAssignableFrom<IList<object>>(rows);
        Assert.Equal(3, Count(rows));
    }

    [Fact]
    public async Task GetEntityAsync_RowsSurviveTheConnectionClosing()
    {
        // The sharp end of the same defect. Awaiting used to leave a reader still attached to the
        // shared connection, so the caller's data depended on that connection still being usable
        // whenever it got round to enumerating.
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name) VALUES (1,'a'),(2,'b')");

        var rows = await h.Source.GetEntityAsync("Products", null);

        h.Connection.Close();

        Assert.Equal(2, Count(rows));
    }

    [Fact]
    public async Task GetEntityAsync_OnAMissingEntity_ReturnsAnEmptySequenceNotNull()
    {
        using var h = ProductsHarness();

        var rows = await h.Source.GetEntityAsync("NoSuchTable", null);

        Assert.NotNull(rows);
        Assert.Equal(0, Count(rows));
    }

    // ---------------------------------------------------------------- RunScript

    [Fact]
    public void RunScript_ExecutesTheDdlAndReportsThroughItsReturnValue()
    {
        // Was: Task.Run(() => ExecuteSql(...)).Wait(), then DMEEditor.ErrorObject = t.Result.
        // Two problems in three lines — a pointless blocking offload, and the same engine-global
        // rebinding as K38, here in the base class. With no IDMEEditor attached the assignment threw
        // outright, so this test could not have been written before.
        using var h = ProductsHarness();

        var result = h.Source.RunScript(new ETLScriptDet
        {
            Ddl = "CREATE TABLE Made_By_RunScript (Id INTEGER PRIMARY KEY)"
        });

        Assert.NotNull(result);
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Made_By_RunScript'"));
    }

    [Fact]
    public void RunScript_OnBadDdl_ReportsFailureAndStampsTheScript()
    {
        using var h = ProductsHarness();
        var script = new ETLScriptDet { Ddl = "CREATE TABLE ( this is not sql" };

        var result = h.Source.RunScript(script);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.False(string.IsNullOrWhiteSpace(script.ErrorMessage));
    }

    // ---------------------------------------------------------------- async open

    [Fact]
    public async Task OpenconnectionAsync_OnAnOpenConnection_ReportsOpen()
    {
        using var h = ProductsHarness();

        var state = await h.Source.OpenconnectionAsync();

        Assert.Equal(ConnectionState.Open, state);
        Assert.Equal(Errors.Ok, h.Source.ErrorObject.Flag);
    }

    [Fact]
    public async Task OpenconnectionAsync_ReopensAClosedConnectionThroughOpenAsync()
    {
        // This is the case the resilience pipeline actually hits: CloseConn() calls Close() and keeps
        // the connection object, so reopening it is DbConnection.OpenAsync — a real awaitable open
        // that honours the token — rather than Task.Run parked on a blocking one.
        using var h = ProductsHarness();
        h.Connection.Close();
        Assert.Equal(ConnectionState.Closed, h.Connection.State);

        var state = await h.Source.OpenconnectionAsync();

        Assert.Equal(ConnectionState.Open, state);
        Assert.Equal(ConnectionState.Open, h.Connection.State);
    }
}
