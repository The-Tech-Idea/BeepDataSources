using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// The temp-table bulk-update path generated its temp table name as
/// <c>$"#TempUpdate_{entityName}_{Guid.NewGuid():N}"</c> for every engine unconditionally. The
/// leading <c>#</c> is SQL Server's own local-temp-table sigil; on PostgreSQL and CockroachDB it is
/// a syntax error, and on MySQL <c>#</c> starts a to-end-of-line comment, so
/// <c>CREATE TEMPORARY TABLE #TempUpdate_...</c> became <c>CREATE TEMPORARY TABLE</c> with no name
/// at all. SupportsTempTables() has always listed MySQL and PostgreSQL alongside SQL Server, so this
/// broke the temp-table path on two of the three engines it was written for from the start — masked
/// because nothing before this exercised it against a real, non-SqlServer execution.
///
/// Only PostgreSQL's generated SQL happens to also be valid SQLite syntax end to end (its
/// <c>UPDATE ... FROM</c> join-update form is one SQLite supports too). MySQL's merge query uses
/// <c>UPDATE ... INNER JOIN ... SET</c>, which is not SQLite syntax at all — a real MySQL server
/// understands it fine, SQLite as a stand-in does not — so that case, and SQL Server's (SQLite does
/// not accept a bare <c>#</c> in an identifier under any circumstances), are asserted on the
/// generated SQL text rather than on end-to-end execution, per this suite's own established
/// practice for exactly this situation.
/// </summary>
public class TempTableNamingTests
{
    internal sealed class Row
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private static SqliteHarness Harness(DataSourceType engine)
    {
        var h = SqliteHarness.Create(engine, typeof(Row));
        h.Execute("CREATE TABLE Rows_ (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Register(SqliteHarness.Entity("Rows_",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));
        h.Execute("INSERT INTO Rows_ (Id, Name) VALUES (1,'a'),(2,'b')");
        return h;
    }

    [Fact]
    public void BulkUpdate_OnPostgres_ExecutesEndToEnd_WithNoHashSigil()
    {
        using var h = Harness(DataSourceType.Postgre);

        var result = h.Source.BulkUpdateEntities("Rows_", new List<Row>
        {
            new Row { Id = 1, Name = "updated-1" },
            new Row { Id = 2, Name = "updated-2" },
        });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("updated-1", h.ScalarString("SELECT Name FROM Rows_ WHERE Id = 1"));
        Assert.Equal("updated-2", h.ScalarString("SELECT Name FROM Rows_ WHERE Id = 2"));

        string lastSql = h.TestSource.LastCommand?.CommandText ?? string.Empty;
        Assert.DoesNotContain("#", lastSql);
    }

    [Fact]
    public void BulkUpdate_OnMySql_TempTableNameHasNoHashSigil()
    {
        // MySQL's own UPDATE ... INNER JOIN ... SET merge statement is not SQLite syntax, so this
        // asserts on the CREATE TEMPORARY TABLE statement -- captured before the merge step runs and
        // fails -- rather than on the whole operation succeeding.
        using var h = Harness(DataSourceType.Mysql);

        h.Source.BulkUpdateEntities("Rows_", new List<Row> { new Row { Id = 1, Name = "updated-1" } });

        string createSql = h.TestSource.LastCommandTextSeen(containing: "CREATE TEMPORARY TABLE") ?? string.Empty;
        Assert.Contains("CREATE TEMPORARY TABLE TempUpdate_", createSql);
        Assert.DoesNotContain("#", createSql);
    }

    [Fact]
    public void BulkUpdate_OnSqlServer_StillUsesTheHashSigil_Regression()
    {
        // Pins that fixing the other engines did not touch the one engine "#" is actually correct
        // for. SQLite rejects a bare "#" in an identifier outright, so this asserts on the CREATE
        // TABLE statement SQL Server's own path attempted, not on it executing successfully.
        using var h = Harness(DataSourceType.SqlServer);

        h.Source.BulkUpdateEntities("Rows_", new List<Row> { new Row { Id = 1, Name = "updated-1" } });

        string createSql = h.TestSource.LastCommandTextSeen(containing: "CREATE TABLE") ?? string.Empty;
        Assert.Contains("#TempUpdate_", createSql);
    }
}
