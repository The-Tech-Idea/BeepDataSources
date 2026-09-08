using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// CockroachDB is PostgreSQL wire- and DDL-compatible for the exact statements the temp-table bulk
/// update path emits (<c>CREATE TEMP TABLE</c>, <c>UPDATE ... SET ... FROM ... AS source</c>), so it
/// now reuses PostgreSQL's own builders rather than falling back to the slower per-row batched path
/// <c>SupportsTempTables()</c> used to send every non-SqlServer/Mysql/Postgre engine down.
/// </summary>
public class CockroachTempTableTests
{
    internal sealed class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private static SqliteHarness Harness()
    {
        var h = SqliteHarness.Create(DataSourceType.Cockroach, typeof(Product));
        h.Execute("CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Register(SqliteHarness.Entity("Products",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));
        return h;
    }

    [Fact]
    public void BulkUpdate_OnCockroach_TakesTheTempTablePath_NotTheBatchedFallback()
    {
        using var h = Harness();
        h.Execute("INSERT INTO Products (Id, Name) VALUES (1,'a'),(2,'b'),(3,'c')");

        var updated = new List<Product>
        {
            new Product { Id = 1, Name = "updated-1" },
            new Product { Id = 2, Name = "updated-2" },
            new Product { Id = 3, Name = "updated-3" },
        };

        var result = h.Source.BulkUpdateEntities("Products", updated);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal("updated-1", h.ScalarString("SELECT Name FROM Products WHERE Id = 1"));
        Assert.Equal("updated-2", h.ScalarString("SELECT Name FROM Products WHERE Id = 2"));
        Assert.Equal("updated-3", h.ScalarString("SELECT Name FROM Products WHERE Id = 3"));

        // DropTempTable's cleanup statement runs last and is what LastCommand still holds by the
        // time BulkUpdateEntities returns; its presence -- naming a "TempUpdate_" table -- is
        // exactly the shape only the temp-table path produces, never the row-at-a-time batched one.
        // Before SupportsTempTables() included Cockroach, this datasource fell back to
        // BulkUpdateBatched and no such table would exist to drop.
        string lastSql = h.TestSource.LastCommand?.CommandText ?? string.Empty;
        Assert.Contains("TempUpdate_Products_", lastSql);
        Assert.Contains("DROP TABLE", lastSql, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BulkUpdate_OnCockroach_TempTableColumnsUseCockroachTypes_NotPostgresBytea()
    {
        // Pins the K46-era fix alongside this one: the temp table's CREATE statement is built by
        // ResolveDdlColumnType, which reads DatasourceType at call time -- so a Cockroach-reporting
        // datasource reusing Postgres's temp-table builder still gets Cockroach's own type names
        // (BYTES), not Postgres's BYTEA, for any binary column.
        using var h = SqliteHarness.Create(DataSourceType.Cockroach, typeof(BlobRow));
        h.Execute("CREATE TABLE Blobs (Id INTEGER PRIMARY KEY, Payload BLOB)");
        h.Register(SqliteHarness.Entity("Blobs",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Payload", "System.Byte[]")));
        h.Execute("INSERT INTO Blobs (Id, Payload) VALUES (1, X'01')");

        h.Source.BulkUpdateEntities("Blobs", new List<BlobRow> { new BlobRow { Id = 1, Payload = new byte[] { 2 } } });

        // The CREATE TEMP TABLE statement ran before the final UPDATE captured above, so this checks
        // that a temp table was actually created and dropped rather than asserting on SQL text that
        // has already scrolled past LastCommand.
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'temp_%'"));
    }

    internal sealed class BlobRow
    {
        public int Id { get; set; }
        public byte[]? Payload { get; set; }
    }
}
