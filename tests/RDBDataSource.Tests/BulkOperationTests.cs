using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 5 bulk fixes (K5, K8, K9 and the batch-sizing gap), driven
/// through a real <see cref="TheTechIdea.Beep.DataBase.RDBSource"/> against in-memory SQLite.
/// </summary>
public class BulkOperationTests
{
    internal sealed class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }
    }

    private static SqliteHarness ProductsHarness(DataSourceType engine = DataSourceType.SqlLite)
    {
        var h = SqliteHarness.Create(engine, typeof(Product));
        h.Execute("CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT, Sku TEXT)");
        h.Register(SqliteHarness.Entity("Products",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name"),
            SqliteHarness.Field("Sku")));
        return h;
    }

    private static List<Product> Products(int count) =>
        Enumerable.Range(1, count)
                  .Select(i => new Product { Id = i, Name = $"name-{i}", Sku = $"sku-{i}" })
                  .ToList();

    [Fact]
    public void BulkInsert_WritesTheActualValues_NotNulls()
    {
        using var h = ProductsHarness();

        var result = h.Source.BulkInsertEntities("Products", Products(5));

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(5, h.ScalarLong("SELECT COUNT(*) FROM Products"));

        // K5: the builder reflected on typeof(T), so an IEnumerable<object> wrote a table of NULLs
        // and reported success. Assert the values actually landed.
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM Products WHERE Name IS NULL"));
        Assert.Equal("name-3", h.ScalarString("SELECT Name FROM Products WHERE Id = 3"));
    }

    [Fact]
    public void BulkInsert_ThroughIEnumerableOfObject_StillWritesValues()
    {
        // This is the shape the ETL layer passes, and the one that produced an all-NULL table:
        // typeof(T) is object, so every property lookup returned null.
        using var h = ProductsHarness();

        IEnumerable<object> rows = Products(4).Cast<object>().ToList();
        var result = h.Source.BulkInsertEntities("Products", rows);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(4, h.ScalarLong("SELECT COUNT(*) FROM Products"));
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM Products WHERE Name IS NULL"));
        Assert.Equal("sku-2", h.ScalarString("SELECT Sku FROM Products WHERE Id = 2"));
    }

    [Fact]
    public void BulkInsert_BatchedPath_RunsInsideItsTransaction()
    {
        // K8: the batched path opens a transaction but InsertEntity builds its command through
        // GetDataCommand, which only attaches ActiveTransaction — so the transaction was invisible
        // to it. Forcing the batched path here by disabling the multi-row optimisation.
        using var h = ProductsHarness();
        h.Source.EnableBulkOptimizations = false;

        var result = h.Source.BulkInsertEntities("Products", Products(3));

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(3, h.ScalarLong("SELECT COUNT(*) FROM Products"));

        // And the transaction must not be left dangling on the datasource afterwards.
        Assert.Null(h.Source.ActiveTransaction);
    }

    [Fact]
    public void BulkInsert_InsideACallersTransaction_DoesNotNestOrHijackIt()
    {
        using var h = ProductsHarness();
        h.Source.EnableBulkOptimizations = false;

        h.Source.BeginTransaction(null);
        var outer = h.Source.ActiveTransaction;
        Assert.NotNull(outer);

        h.Source.BulkInsertEntities("Products", Products(2));

        // The caller still owns the same transaction — the bulk path joined it rather than opening
        // its own and committing the caller's work out from under them.
        Assert.Same(outer, h.Source.ActiveTransaction);

        h.Source.Commit(null);
        Assert.Equal(2, h.ScalarLong("SELECT COUNT(*) FROM Products"));
    }

    [Fact]
    public void BulkInsert_LeavesNoActiveTransactionBehind()
    {
        using var h = ProductsHarness();

        h.Source.BulkInsertEntities("Products", Products(3));

        Assert.Null(h.Source.ActiveTransaction);
    }

    [Fact]
    public void BulkUpdate_BatchedPath_AppliesTheChanges()
    {
        using var h = ProductsHarness();
        h.Source.EnableBulkOptimizations = false;
        h.Source.BulkInsertEntities("Products", Products(3));

        var updated = Products(3);
        foreach (var p in updated) p.Name = $"updated-{p.Id}";

        var result = h.Source.BulkUpdateEntities("Products", updated);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("updated-2", h.ScalarString("SELECT Name FROM Products WHERE Id = 2"));
        Assert.Null(h.Source.ActiveTransaction);
    }

    [Fact]
    public void BulkInsert_ClampsBatchSizeToTheParameterBudget()
    {
        // 3 fields, budget of 6 parameters => 2 rows per batch. The point is that it completes
        // correctly rather than building one oversized command.
        using var h = ProductsHarness();
        h.Source.MaxParametersPerBatch = 6;

        var result = h.Source.BulkInsertEntities("Products", Products(7));

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(7, h.ScalarLong("SELECT COUNT(*) FROM Products"));
    }
}
