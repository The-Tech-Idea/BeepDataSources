using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Integration tests that drive a real <see cref="TheTechIdea.Beep.DataBase.RDBSource"/> against
/// in-memory SQLite through <see cref="SqliteHarness"/>.
///
/// The previous version of this file hand-wrote SQL against SQLite and never instantiated the class
/// under test — its own comments said "Simulates RDBSource.InsertEntity pattern". Twenty-five tests
/// asserted that SQLite behaves like SQLite, so every defect in docs/10-known-issues.md was
/// invisible to them and they would have stayed green through all of it. These exercise the real
/// CRUD, query, schema, paging and transaction paths instead.
///
/// Defect-specific regressions live in the focused suites: ErrorModelTests, ParameterBindingTests,
/// IdentifierQuotingTests, FilterInjectionTests, SchemaQualificationTests, PagingDialectTests,
/// WriteContractTests and BulkOperationTests.
/// </summary>
public class RDBSourceIntegrationTests
{
    internal sealed class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Price { get; set; }
    }

    private static SqliteHarness ProductsHarness(bool autoIncrementKey = false)
    {
        var h = SqliteHarness.Create(entityType: typeof(Product));
        h.Execute(autoIncrementKey
            ? "CREATE TABLE Products (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Price REAL)"
            : "CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT, Price REAL)");
        h.Register(SqliteHarness.Entity("Products",
            SqliteHarness.Field("Id", "System.Int32", isKey: true, isAutoIncrement: autoIncrementKey),
            SqliteHarness.Field("Name"),
            SqliteHarness.Field("Price", "System.Double")));
        return h;
    }

    private static int Count(IEnumerable rows) => rows.Cast<object>().Count();

    // ---- CRUD ----

    [Fact]
    public void InsertEntity_PersistsTheRow()
    {
        using var h = ProductsHarness();

        var result = h.Source.InsertEntity("Products", new Product { Id = 1, Name = "Widget", Price = 9.5 });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("Widget", h.ScalarString("SELECT Name FROM Products WHERE Id = 1"));
    }

    [Fact]
    public void InsertEntity_AutoIncrementKey_IsOmittedFromTheStatementAndReadBack()
    {
        using var h = ProductsHarness(autoIncrementKey: true);

        var product = new Product { Id = 0, Name = "Generated", Price = 1.0 };
        var result = h.Source.InsertEntity("Products", product);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM Products"));

        // The server assigned the key and the fetch-back wrote it onto the caller's object. That
        // read-back is gated on the primary key actually being auto-increment — without the guard,
        // SQLite's implicit ROWID overwrote client-generated keys on every insert.
        Assert.True(product.Id > 0, "the generated identity should have been written back");
    }

    [Fact]
    public void UpdateEntity_ChangesOnlyTheKeyedRow()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0)");

        var result = h.Source.UpdateEntity("Products", new Product { Id = 2, Name = "changed", Price = 2.0 });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("changed", h.ScalarString("SELECT Name FROM Products WHERE Id = 2"));
        Assert.Equal("a", h.ScalarString("SELECT Name FROM Products WHERE Id = 1"));
    }

    [Fact]
    public void DeleteEntity_RemovesOnlyTheKeyedRow()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0)");

        var result = h.Source.DeleteEntity("Products", new Product { Id = 1 });

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM Products WHERE Id = 2"));
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM Products WHERE Id = 1"));
    }

    // ---- reads ----

    [Fact]
    public void GetEntity_ReturnsEveryRow()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0),(3,'c',3.0)");

        Assert.Equal(3, Count(h.Source.GetEntity("Products", null)));
    }

    [Fact]
    public void GetEntity_WithAFilter_ReturnsOnlyMatches()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0),(3,'c',3.0)");

        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Name", Operator = "=", FilterValue = "b" }
        };

        Assert.Equal(1, Count(h.Source.GetEntity("Products", filters)));
    }

    [Fact]
    public void GetEntityPaged_ReturnsThePageAndTheTotal()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1),(2,'b',2),(3,'c',3),(4,'d',4),(5,'e',5)");

        var page = h.Source.GetEntity("Products", null, pageNumber: 2, pageSize: 2);

        Assert.NotNull(page);
        Assert.Equal(2, Count((IEnumerable)page.Data));
        Assert.Equal(5, page.TotalRecords);
    }

    [Fact]
    public void GetScalar_ReturnsTheValue()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0)");

        Assert.Equal(2.0, h.Source.GetScalar("SELECT COUNT(*) FROM Products"));
    }

    [Fact]
    public void RunQuery_ReturnsRows()
    {
        using var h = ProductsHarness();
        h.Execute("INSERT INTO Products (Id, Name, Price) VALUES (1,'a',1.0),(2,'b',2.0)");

        Assert.Equal(2, h.Source.RunQuery("SELECT * FROM Products").Count());
    }

    // ---- schema ----

    [Fact]
    public void GetEntityStructure_ReadsColumnsAndTheKeyFromTheDatabase()
    {
        using var h = ProductsHarness();

        var structure = h.Source.GetEntityStructure("Products", refresh: true);

        Assert.NotNull(structure);
        Assert.Equal(3, structure.Fields.Count);
        Assert.Contains(structure.Fields, f => f.FieldName.Equals("Name", System.StringComparison.OrdinalIgnoreCase));
        Assert.Single(structure.PrimaryKeys);
        Assert.Equal("Id", structure.PrimaryKeys[0].FieldName, ignoreCase: true);
    }

    [Fact]
    public void GetEntityStructure_WithoutRefresh_ServesTheCachedStructure()
    {
        using var h = ProductsHarness();

        var first = h.Source.GetEntityStructure("Products", refresh: true);
        var second = h.Source.GetEntityStructure("Products", refresh: false);

        // EntityStructureCache has no invalidation and no eviction, so a second lookup returns the
        // same instance — which is also why a schema change is invisible until someone passes
        // refresh: true. Documented as K18; pinned here so a future change to the cache is a
        // deliberate one rather than an accident.
        Assert.Same(first, second);
    }

    [Fact]
    public void GetTableSchema_ReturnsOneRowPerColumn()
    {
        using var h = ProductsHarness();

        var schema = h.Source.GetTableSchema("Products", false);

        Assert.NotNull(schema);
        Assert.Equal(3, schema.Rows.Count);
    }

    // ---- transactions ----

    [Fact]
    public void Commit_PersistsWorkDoneInsideTheTransaction()
    {
        using var h = ProductsHarness();

        h.Source.BeginTransaction(null);
        h.Source.InsertEntity("Products", new Product { Id = 1, Name = "committed", Price = 1.0 });
        var result = h.Source.Commit(null);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM Products"));
        Assert.Null(h.Source.ActiveTransaction);
    }

    [Fact]
    public void EndTransaction_RollsBack()
    {
        using var h = ProductsHarness();

        h.Source.BeginTransaction(null);
        h.Source.InsertEntity("Products", new Product { Id = 1, Name = "rolled back", Price = 1.0 });
        var result = h.Source.EndTransaction(null);

        // EndTransaction means ROLLBACK — that is the UnitofWork contract, not a misnomer.
        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM Products"));
        Assert.Null(h.Source.ActiveTransaction);
    }

    [Fact]
    public void CommandsCreatedWhileATransactionIsOpen_CarryIt()
    {
        using var h = ProductsHarness();

        h.Source.BeginTransaction(null);
        try
        {
            using var cmd = h.Source.GetDataCommand();

            // Providers that enforce the command/transaction association reject any command that
            // does not carry the pending local transaction. SQLite does not enforce it, which is
            // exactly why that defect survived — so assert the attachment directly rather than
            // relying on execution to fail.
            Assert.NotNull(cmd);
            Assert.Same(h.Source.ActiveTransaction, cmd.Transaction);
        }
        finally
        {
            h.Source.EndTransaction(null);
        }
    }

    // ---- connection ----

    [Fact]
    public void CheckConnectionHealth_ReportsAnOpenConnectionAsHealthy()
    {
        using var h = ProductsHarness();

        Assert.True(h.Source.CheckConnectionHealth());
    }

    [Fact]
    public void ExecuteSql_RunsDdl()
    {
        using var h = ProductsHarness();

        var result = h.Source.ExecuteSql("CREATE TABLE Extra (Id INTEGER)");

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(1, h.ScalarLong("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Extra'"));
    }
}
