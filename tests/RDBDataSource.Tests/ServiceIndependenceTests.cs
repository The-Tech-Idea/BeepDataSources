using System.Collections.Generic;
using System.Data;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Phase 11: what the class does when the engine's services are not there.
///
/// `IDMEEditor` was used as a service locator throughout, and the great majority of those uses were
/// not service lookups at all — 222 of ~300 references were `ErrorObject` and `AddLogMessage`, i.e.
/// error reporting. Every unguarded one was two defects at once: a `NullReferenceException` waiting
/// for a headless caller, and an F1 instance (a failure reported only through `AddLogMessage`, which
/// returns before setting the flag when no logger is attached).
///
/// Reporting now goes through `SetFailure`/`SetSuccess`/`HandleDatabaseError`, which guard both error
/// objects. The genuine service dependencies that remain — `ConfigEditor`, `Utilfunction`,
/// `assemblyHandler`, `ETL`, `typesHelper` — are guarded at the point of use and name themselves when
/// missing, instead of surfacing as "Object reference not set".
///
/// Every test here runs with no `IDMEEditor` and no logger at all.
/// </summary>
public class ServiceIndependenceTests
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

    private static EntityStructure NewEntity(string name) => new EntityStructure
    {
        EntityName = name,
        DatasourceEntityName = name,
        Fields = new List<EntityField>
        {
            // AllowDBNull = false on the key: DatabaseEntityValidator rejects a nullable primary
            // key, and CreateEntityAs validates before it generates anything.
            new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true, AllowDBNull = false },
            new EntityField { FieldName = "Label", Fieldtype = "System.String" }
        },
        PrimaryKeys = new List<EntityField>
        {
            new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true, AllowDBNull = false }
        }
    };

    // ---------------------------------------------------------------- CreateEntityAs

    [Fact]
    public void CreateEntityAs_CreatesTheTable_WithNoEngineAttached()
    {
        // This whole path used to be unreachable without an IDMEEditor: CreateEntityAs wrote
        // DMEEditor.ErrorObject directly on three branches and assigned ExecuteSql's result to it on
        // the fourth. Any of them threw before the CREATE ran.
        using var h = ProductsHarness();

        bool created = h.Source.CreateEntityAs(NewEntity("Widgets"));

        Assert.True(created);
        Assert.Equal(Errors.Ok, h.Source.ErrorObject.Flag);
        Assert.Equal(1, h.ScalarLong(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Widgets'"));
    }

    [Fact]
    public void CreateEntityAs_ForAnEntityThatAlreadyExists_ReportsFailureDeterministically()
    {
        using var h = ProductsHarness();

        bool created = h.Source.CreateEntityAs(NewEntity("Products"));

        // False and Failed — not because creation broke, but because the entity is already there.
        // Reported through SetFailure, so the flag is set whether or not a logger is attached; it
        // used to depend on AddLogMessage, which does nothing to the flag without one.
        Assert.False(created);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.Contains("already exists", h.Source.ErrorObject.Message);
    }

    [Fact]
    public void CreateEntityAs_WithAnInvalidStructure_ReportsFailureInsteadOfThrowing()
    {
        using var h = ProductsHarness();
        var broken = new EntityStructure { EntityName = "", Fields = new List<EntityField>() };

        bool created = h.Source.CreateEntityAs(broken);

        Assert.False(created);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
    }

    // ---------------------------------------------------------------- missing services name themselves

    [Fact]
    public void GetChildTablesList_WithNoConfigEditor_SaysWhichServiceIsMissing()
    {
        using var h = ProductsHarness();

        var children = h.Source.GetChildTablesList("Products", null, null);

        Assert.Null(children);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.Contains("ConfigEditor", h.Source.ErrorObject.Message);
    }

    [Fact]
    public void GetTablesFKColumnList_WithNoConfigEditor_SaysWhichServiceIsMissing()
    {
        using var h = ProductsHarness();

        var fks = h.Source.GetTablesFKColumnList("Products", null, null);

        Assert.Null(fks);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.Contains("ConfigEditor", h.Source.ErrorObject.Message);
    }

    [Fact]
    public void GetDataAdapter_WithNoEngineServices_ReturnsNullAndSaysWhy()
    {
        using var h = ProductsHarness();

        var adapter = h.Source.GetDataAdapter("SELECT * FROM Products", null);

        Assert.Null(adapter);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.False(string.IsNullOrWhiteSpace(h.Source.ErrorObject.Message));
    }

    [Fact]
    public void GetEntitesList_WithNoConfigEditorAndNoDriverSql_SaysWhichServiceIsMissing()
    {
        using var h = ProductsHarness();

        var names = h.Source.GetEntitesList();

        // The registered names are still returned — the method's contract — but the flag and message
        // now say the refresh could not happen, instead of a swallowed NullReferenceException
        // leaving a stale list indistinguishable from a fresh one.
        Assert.NotNull(names);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
        Assert.Contains("ConfigEditor", h.Source.ErrorObject.Message);
    }

    // ---------------------------------------------------------------- ETL is optional

    [Fact]
    public void UpdateEntities_RunsWithoutTheEtlService()
    {
        // UpdateEntities dereferenced DMEEditor.ETL four times for progress bookkeeping, before
        // touching the database — so it threw on a service it does not need to do its actual work.
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name) VALUES (1,'before'),(2,'other')");

        var rows = new List<object>
        {
            new Product { Id = 1, Name = "after" }
        };

        var result = h.Source.UpdateEntities("Products", rows, null);

        Assert.NotNull(result);
        Assert.Equal("after", h.ScalarString("SELECT Name FROM Products WHERE Id = 1"));
        Assert.Equal("other", h.ScalarString("SELECT Name FROM Products WHERE Id = 2"));
    }
}
