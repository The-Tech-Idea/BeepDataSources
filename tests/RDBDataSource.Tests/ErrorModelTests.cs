using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 1 error-model fixes.
///
/// These are the first tests in this project that instantiate <see cref="RDBSource"/> itself
/// rather than hand-writing SQL against SQLite, so they are the only ones that would catch a
/// regression in the class under test.
///
/// Every case runs with no logger and no <c>IDMEEditor</c>, which is the configuration that made
/// these defects invisible: <c>DMEEditor.AddLogMessage</c> returns before it assigns
/// <c>ErrorObject.Flag</c> when no logger is attached, so ~40 failure paths that reported only
/// through it used to hand the caller back the optimistic <c>Errors.Ok</c> set on method entry.
/// </summary>
public class ErrorModelTests
{
    /// <summary>
    /// Builds a datasource whose connection is closed and which has no logging attached at all —
    /// the configuration under which a failure has to be self-reporting or it is lost.
    /// </summary>
    private static RDBSource CreateUnconnectedSource(out IErrorsInfo errors)
    {
        errors = new ErrorsInfo { Flag = Errors.Ok };
        return new RDBSource("phase1-probe", null, null, DataSourceType.SqlLite, errors);
    }

    [Fact]
    public void ExecuteSql_OnClosedConnection_ReportsFailure()
    {
        var source = CreateUnconnectedSource(out _);

        // Before the fix: GetDataCommand returned null with the flag still Ok, ExecuteSql had no
        // `else` on `if (cmd != null)`, and this returned Errors.Ok having executed nothing.
        // CreateEntityAs then logged "Entity created successfully" for a table that was never made.
        var result = source.ExecuteSql("CREATE TABLE probe (id INTEGER)");

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void GetDataCommand_OnClosedConnection_ReportsFailure()
    {
        var source = CreateUnconnectedSource(out var errors);

        var cmd = source.GetDataCommand();

        Assert.Null(cmd);
        Assert.Equal(Errors.Failed, errors.Flag);
    }

    [Fact]
    public void GetDataReader_OnClosedConnection_ReturnsNullRatherThanThrowing()
    {
        var source = CreateUnconnectedSource(out var errors);

        // Previously dereferenced the null command and threw a bare NullReferenceException out of
        // an IRDBSource contract method.
        var reader = source.GetDataReader("select 1");

        Assert.Null(reader);
        Assert.Equal(Errors.Failed, errors.Flag);
    }

    [Fact]
    public void RunQuery_OnClosedConnection_ReportsFailureRatherThanEmptySuccess()
    {
        var source = CreateUnconnectedSource(out var errors);

        var rows = source.RunQuery("select 1");

        // An empty sequence is also what a query with no matches returns, so the flag is the only
        // thing that distinguishes "nothing matched" from "nothing ran".
        Assert.Empty(rows);
        Assert.Equal(Errors.Failed, errors.Flag);
    }

    [Fact]
    public void BeginTransaction_OnClosedConnection_ReportsFailure()
    {
        var source = CreateUnconnectedSource(out _);

        var result = source.BeginTransaction(null);

        Assert.Equal(Errors.Failed, result.Flag);
    }

    [Fact]
    public void Commit_WithNoOpenTransaction_ReportsSuccessExplicitly()
    {
        var source = CreateUnconnectedSource(out var errors);

        // Seed a stale failure from a notionally unrelated earlier operation. Commit used to return
        // DMEEditor.ErrorObject without ever setting it to Ok on success, so it handed back whatever
        // flag was already there — and UnitofWork branches on that value.
        errors.Flag = Errors.Failed;
        errors.Message = "stale failure from an earlier operation";

        var result = source.Commit(null);

        Assert.Equal(Errors.Ok, result.Flag);
    }

    [Fact]
    public void EndTransaction_WithNoOpenTransaction_ReportsSuccessExplicitly()
    {
        var source = CreateUnconnectedSource(out var errors);

        errors.Flag = Errors.Failed;

        var result = source.EndTransaction(null);

        Assert.Equal(Errors.Ok, result.Flag);
    }

    [Fact]
    public void Openconnection_WithNoDriverConfigured_ReportsFailure()
    {
        var source = CreateUnconnectedSource(out var errors);

        // No ConnectionProp or DataSourceDriver has been wired up, so the open cannot succeed.
        // Neither Openconnection nor Closeconnection touched ErrorObject at all before.
        var state = source.Openconnection();

        Assert.NotEqual(ConnectionState.Open, state);
        Assert.Equal(Errors.Failed, errors.Flag);
    }

    [Fact]
    public void GetScalar_OnClosedConnection_DistinguishesFailureFromARealZero()
    {
        var source = CreateUnconnectedSource(out var errors);

        var value = source.GetScalar("select count(*) from probe");

        // 0.0 is a legitimate result, so the return value alone cannot carry the outcome.
        Assert.Equal(0.0, value);
        Assert.Equal(Errors.Failed, errors.Flag);
    }
}
