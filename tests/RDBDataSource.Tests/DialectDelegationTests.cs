using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Helpers.RDBMSHelpers;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Phase 9: dialect decisions that used to live in a <c>switch (DatasourceType)</c> in this class
/// now come from BeepDM's shared helper library, so every consumer gets the same answer.
///
/// These pin the two that changed behaviour: which engines accept a multi-row INSERT, and how long
/// a generated parameter name may be.
/// </summary>
public class DialectDelegationTests
{
    private static RDBSource NewSource(DataSourceType engine) =>
        new RDBSource("dialect-probe", null, null, engine, new ErrorsInfo { Flag = Errors.Ok });

    private static EntityField Field(string name, bool isKey = false) =>
        new EntityField { FieldName = name, Fieldtype = "System.String", IsKey = isKey };

    // ---------------------------------------------------------------- multi-row INSERT

    [Theory]
    [InlineData(DataSourceType.SqlServer)]
    [InlineData(DataSourceType.Mysql)]
    [InlineData(DataSourceType.Postgre)]
    [InlineData(DataSourceType.SqlLite)]
    // The four above were the whole of the old hardcoded list. These are the ones it missed:
    [InlineData(DataSourceType.MariaDB)]
    [InlineData(DataSourceType.AzureSQL)]
    [InlineData(DataSourceType.Cockroach)]
    [InlineData(DataSourceType.DuckDB)]
    [InlineData(DataSourceType.DB2)]
    [InlineData(DataSourceType.SnowFlake)]
    [InlineData(DataSourceType.Spanner)]
    [InlineData(DataSourceType.Presto)]
    [InlineData(DataSourceType.Trino)]
    public void EnginesThatAcceptMultiRowValues_AreReportedSupported(DataSourceType engine)
    {
        Assert.True(RDBMSHelper.SupportsFeature(engine, DatabaseFeature.MultiRowInsert),
            $"{engine} accepts INSERT ... VALUES (..),(..) and should take the batched path.");
    }

    [Theory]
    // Not throughput limits — these engines reject the grammar outright and need a different
    // statement shape (Oracle INSERT ALL, Firebird INSERT INTO ... SELECT ... UNION ALL). Claiming
    // support here would turn every bulk insert into a syntax error.
    [InlineData(DataSourceType.Oracle)]
    [InlineData(DataSourceType.FireBird)]
    [InlineData(DataSourceType.Hana)]
    [InlineData(DataSourceType.SqlCompact)]
    [InlineData(DataSourceType.VistaDB)]
    public void EnginesThatRejectMultiRowValues_AreReportedUnsupported(DataSourceType engine)
    {
        Assert.False(RDBMSHelper.SupportsFeature(engine, DatabaseFeature.MultiRowInsert),
            $"{engine} does not accept INSERT ... VALUES (..),(..).");
    }

    [Fact]
    public void BulkInsert_OnAnEngineTheOldListMissed_StillWritesTheRightRows()
    {
        // MariaDB reported false before, so it fell back to a statement per row. It now takes the
        // multi-row path — this proves that path produces working SQL for it, not just a flag flip.
        using var h = SqliteHarness.Create(DataSourceType.MariaDB, typeof(Row));
        h.Execute("CREATE TABLE Rows_ (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Register(SqliteHarness.Entity("Rows_",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));

        var rows = Enumerable.Range(1, 6).Select(i => new Row { Id = i, Name = $"n{i}" }).ToList();
        var result = h.Source.BulkInsertEntities("Rows_", rows);

        Assert.Equal(Errors.Ok, result.Flag);
        Assert.Equal(6, h.ScalarLong("SELECT COUNT(*) FROM Rows_"));
        Assert.Equal(0, h.ScalarLong("SELECT COUNT(*) FROM Rows_ WHERE Name IS NULL"));
        Assert.Equal("n4", h.ScalarString("SELECT Name FROM Rows_ WHERE Id = 4"));

        // And prove the batched path is the one that ran: a multi-row INSERT has more than one
        // parenthesised VALUES group. Before the delegation MariaDB reported false and this was a
        // single-row statement issued six times.
        string lastSql = h.TestSource.LastCommand?.CommandText ?? string.Empty;
        Assert.Contains("VALUES", lastSql, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("), (", lastSql);
    }

    internal sealed class Row
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    // ---------------------------------------------------------------- identifier length

    /// <summary>A field name long enough to be clamped on every engine tested here.</summary>
    private const string LongName =
        "Customer_Account_Reconciliation_Adjustment_Reference_Number_Extended";

    private static EntityStructure LongFieldEntity() => new EntityStructure
    {
        EntityName = "Wide",
        Fields = new List<EntityField> { Field("Id", isKey: true), Field(LongName) },
        PrimaryKeys = new List<EntityField> { Field("Id", isKey: true) }
    };

    [Theory]
    [InlineData(DataSourceType.SqlServer, 128)]
    [InlineData(DataSourceType.Mysql, 64)]
    [InlineData(DataSourceType.Postgre, 63)]
    [InlineData(DataSourceType.Oracle, 30)]
    [InlineData(DataSourceType.FireBird, 31)]
    public void GeneratedParameterNames_FitTheProvidersIdentifierLimit(DataSourceType engine, int limit)
    {
        string sql = NewSource(engine).GetInsertString("Wide", LongFieldEntity());

        // The emitted form is {delimiter}p_{name}; the delimiter is not part of the identifier.
        foreach (string token in sql.Split(new[] { ' ', ',', '(', ')', '\r', '\n', '\t' },
                                           System.StringSplitOptions.RemoveEmptyEntries))
        {
            int idx = token.IndexOf("p_", System.StringComparison.Ordinal);
            if (idx <= 0) continue;

            string identifier = token.Substring(idx);
            Assert.True(identifier.Length <= limit,
                $"Parameter identifier '{identifier}' is {identifier.Length} characters, over {engine}'s limit of {limit}.");
        }
    }

    [Fact]
    public void GeneratedParameterNames_AreNoLongerClampedToOraclesLimitOnEveryEngine()
    {
        // The clamp was hardcoded at 30 — Oracle's pre-12.2 limit — so SQL Server, MySQL and
        // PostgreSQL all lost the tail of any name over 28 characters, and each truncation is
        // another chance for two distinct fields to collide onto one parameter.
        string sqlServer = NewSource(DataSourceType.SqlServer).GetInsertString("Wide", LongFieldEntity());
        string oracle = NewSource(DataSourceType.Oracle).GetInsertString("Wide", LongFieldEntity());

        Assert.Contains("p_" + LongName, sqlServer);          // 128: fits whole
        Assert.DoesNotContain("p_" + LongName, oracle);       // 30: still clamped, correctly
        Assert.Contains("p_" + LongName.Substring(0, 28), oracle);
    }
}
