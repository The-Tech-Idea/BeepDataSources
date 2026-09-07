using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// End-to-end tests for the Phase 2 write fixes, driving a real <see cref="TheTechIdea.Beep.DataBase.RDBSource"/>
/// against in-memory SQLite through <see cref="SqliteHarness"/>.
/// </summary>
public class WriteContractTests
{
    private static SqliteHarness PeopleHarness(DataSourceType engine = DataSourceType.SqlLite)
    {
        var h = SqliteHarness.Create(engine, typeof(Person));
        h.Execute("CREATE TABLE People (Id INTEGER PRIMARY KEY, Name TEXT, NameSuffix TEXT)");
        h.Register(SqliteHarness.Entity("People",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name"),
            SqliteHarness.Field("NameSuffix")));
        return h;
    }

    internal sealed class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? NameSuffix { get; set; }
    }

    [Fact]
    public void InsertEntity_WritesTheValuesItWasGiven()
    {
        using var h = PeopleHarness();

        var result = h.Source.InsertEntity("People", new Person { Id = 1, Name = "Ada", NameSuffix = "Jr" });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM People"));
        Assert.Equal("Ada", h.ScalarString("SELECT Name FROM People WHERE Id = 1"));
        Assert.Equal("Jr", h.ScalarString("SELECT NameSuffix FROM People WHERE Id = 1"));
    }

    [Fact]
    public void InsertEntity_FieldsSharingAPrefix_EachGetsItsOwnValue()
    {
        // The binder used to recover a field's parameter name by StartsWith over a HashSet, so
        // "Name" could resolve to "NameSuffix"'s parameter and the two values could swap or collide.
        using var h = PeopleHarness();

        h.Source.InsertEntity("People", new Person { Id = 7, Name = "first", NameSuffix = "second" });

        Assert.Equal("first", h.ScalarString("SELECT Name FROM People WHERE Id = 7"));
        Assert.Equal("second", h.ScalarString("SELECT NameSuffix FROM People WHERE Id = 7"));
    }

    [Fact]
    public void UpdateEntity_UpdatesTheRowIdentifiedByItsKey()
    {
        using var h = PeopleHarness();
        h.Execute("INSERT INTO People (Id, Name, NameSuffix) VALUES (1, 'Ada', 'Jr'), (2, 'Grace', 'Sr')");

        var result = h.Source.UpdateEntity("People", new Person { Id = 2, Name = "Grace Hopper", NameSuffix = "Sr" });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("Grace Hopper", h.ScalarString("SELECT Name FROM People WHERE Id = 2"));
        Assert.Equal("Ada", h.ScalarString("SELECT Name FROM People WHERE Id = 1"));   // untouched
    }

    [Fact]
    public void DeleteEntity_DeletesOnlyTheKeyedRow()
    {
        using var h = PeopleHarness();
        h.Execute("INSERT INTO People (Id, Name, NameSuffix) VALUES (1, 'Ada', 'Jr'), (2, 'Grace', 'Sr')");

        var result = h.Source.DeleteEntity("People", new Person { Id = 1 });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM People"));
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM People WHERE Id = 2"));
    }

    // ---- the zero-rows contract (Phase 2, T2) ----

    [Fact]
    public void UpdateEntity_MatchingNoRow_ReportsFailure()
    {
        using var h = PeopleHarness();
        h.Execute("INSERT INTO People (Id, Name, NameSuffix) VALUES (1, 'Ada', 'Jr')");

        var result = h.Source.UpdateEntity("People", new Person { Id = 999, Name = "nobody" });

        // Previously returned Errors.Ok with no logger attached, and Errors.Failed with one —
        // the outcome depended on logging configuration. Now deterministic.
        Assert.Equal(Errors.Failed, result.Flag);
    }

    [Fact]
    public void DeleteEntity_MatchingNoRow_ReportsFailure()
    {
        using var h = PeopleHarness();

        var result = h.Source.DeleteEntity("People", new Person { Id = 999 });

        Assert.Equal(Errors.Failed, result.Flag);
    }

    [Fact]
    public void UpdateEntity_MatchingNoRow_OnMySql_ReportsSuccess()
    {
        // MySQL and MariaDB report 0 affected rows for an UPDATE that matched a row but changed
        // nothing, unless the connection enables CLIENT_FOUND_ROWS. Treating 0 as failure there
        // would make every re-save of an unchanged entity fail, so the carve-out is deliberate.
        // (Executed against SQLite; only the reported engine differs.)
        using var h = PeopleHarness(DataSourceType.Mysql);

        var result = h.Source.UpdateEntity("People", new Person { Id = 999, Name = "nobody" });

        Assert.Equal(Errors.Ok, result.Flag);
    }

    [Fact]
    public void DeleteEntity_MatchingNoRow_OnMySql_StillReportsFailure()
    {
        // The carve-out is UPDATE-only: a DELETE affecting no rows is unambiguous on every engine.
        using var h = PeopleHarness(DataSourceType.Mysql);

        var result = h.Source.DeleteEntity("People", new Person { Id = 999 });

        Assert.Equal(Errors.Failed, result.Flag);
    }

    // ---- keyless entities (Phase 2, K31) ----

    [Fact]
    public void UpdateEntity_WithNoPrimaryKey_ReportsFailureInsteadOfMangledSql()
    {
        using var h = SqliteHarness.Create(entityType: typeof(Person));
        h.Execute("CREATE TABLE Keyless (Name TEXT)");
        h.Register(SqliteHarness.Entity("Keyless", SqliteHarness.Field("Name")));

        // Previously appended nothing after "where", producing a syntax error the caller saw only
        // as an opaque provider message.
        var result = h.Source.UpdateEntity("Keyless", new Person { Name = "x" });

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("primary key", result.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteEntity_WithNoPrimaryKey_ReportsFailureAndDeletesNothing()
    {
        using var h = SqliteHarness.Create(entityType: typeof(Person));
        h.Execute("CREATE TABLE Keyless (Name TEXT)");
        h.Execute("INSERT INTO Keyless (Name) VALUES ('keep me')");
        h.Register(SqliteHarness.Entity("Keyless", SqliteHarness.Field("Name")));

        var result = h.Source.DeleteEntity("Keyless", new Person { Name = "keep me" });

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM Keyless"));
    }
}
