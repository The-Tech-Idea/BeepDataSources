using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Extensions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 3 injection fix (K4).
///
/// The fix is in BeepDM
/// (<c>DataManagementModelsStandard/Extensions/DataSourceAppFilterExtensions.cs</c>); the reach is
/// through <c>RDBSource.GetEntity</c>, which builds its WHERE clause with
/// <c>BuildSelectQueryDefinition</c>.
///
/// Filter <em>values</em> were always parameterised. <c>Operator</c> and <c>FieldName</c> were not:
/// <c>NormalizeOperator</c> passed unknown operators through verbatim and the WHERE builder's
/// default arm concatenated them, while <c>QuoteIdentifier</c> returned a field name UNQUOTED
/// exactly when it contained the characters that make quoting necessary.
///
/// These assert on the GENERATED SQL, not on row counts. Row counts cannot distinguish the two
/// outcomes: a successful injection of "OR 1=1" returns every row, and so does the fix, because a
/// rejected operator drops the filter. Only the SQL text tells them apart.
/// </summary>
public class FilterInjectionTests
{
    private static RDBSource NewSource() =>
        new RDBSource("injection-probe", null, null, DataSourceType.SqlServer,
                      new ErrorsInfo { Flag = Errors.Ok });

    private static string WhereOf(string fieldName, string op, string value)
    {
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = fieldName, Operator = op, FilterValue = value }
        };
        return NewSource().BuildSelectQueryDefinition("Secrets", filters).WhereClause ?? string.Empty;
    }

    [Theory]
    [InlineData("= 'visible' OR 1=1 --")]
    [InlineData("; DROP TABLE Secrets --")]
    [InlineData("= 1 OR 1=1")]
    [InlineData("UNION SELECT")]
    public void CraftedOperator_NeverReachesTheWhereClause(string craftedOperator)
    {
        string where = WhereOf("Name", craftedOperator, "visible");

        // The operator is rejected, so the filter is dropped and the clause is empty. What must
        // never happen is any fragment of the crafted operator appearing in the SQL.
        Assert.DoesNotContain("1=1", where);
        Assert.DoesNotContain("--", where);
        Assert.DoesNotContain("DROP", where, System.StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UNION", where, System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("=")]
    [InlineData("eq")]
    [InlineData("equals")]
    [InlineData("==")]
    [InlineData(">")]
    [InlineData("gte")]
    public void LegitimateOperators_StillProduceAPredicate(string op)
    {
        string where = WhereOf("Name", op, "visible");

        Assert.False(string.IsNullOrWhiteSpace(where), $"operator '{op}' should still produce a predicate");
        Assert.Contains("Name", where);
    }

    [Fact]
    public void CraftedFieldName_IsQuotedAndEscaped_NotInjected()
    {
        // A field name that tries to close its own delimiter and append SQL.
        string where = WhereOf("Name\" OR \"1\"=\"1", "=", "visible");

        // SQL Server quoting is used here, so the name must come out bracketed with any closing
        // bracket doubled — never as bare SQL.
        Assert.DoesNotContain("OR \"1\"=\"1\" ", where);
        Assert.StartsWith("[", where.TrimStart());
    }

    [Fact]
    public void CraftedFieldNameWithABracket_CannotTerminateItsOwnQuoting()
    {
        string where = WhereOf("Name] OR [1", "=", "visible");

        // The closing bracket inside the name must be escaped by doubling.
        Assert.Contains("]]", where);
    }

    [Fact]
    public void FilterValues_RemainParameterised()
    {
        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Name", Operator = "=", FilterValue = "'; DROP TABLE Secrets --" }
        };

        var definition = NewSource().BuildSelectQueryDefinition("Secrets", filters);

        // Values were never the problem, but pin it: the payload belongs in the parameter map, not
        // in the SQL text.
        Assert.DoesNotContain("DROP", definition.WhereClause, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains(definition.Parameters.Values, v => v is string s && s.Contains("DROP"));
    }

    [Fact]
    public void EndToEnd_CraftedOperatorDoesNotExecuteAttackerSql()
    {
        using var h = SqliteHarness.Create(entityType: typeof(Secret));
        h.Execute("CREATE TABLE Secrets (Id INTEGER PRIMARY KEY, Name TEXT)");
        h.Execute("INSERT INTO Secrets (Id, Name) VALUES (1, 'visible'), (2, 'hidden')");
        h.Register(SqliteHarness.Entity("Secrets",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Name")));

        var filters = new List<AppFilter>
        {
            new AppFilter { FieldName = "Name", Operator = "= 'visible'; DROP TABLE Secrets --", FilterValue = "visible" }
        };

        _ = h.Source.GetEntity("Secrets", filters).ToList();

        // The table must still be there.
        Assert.Equal(2, h.ScalarLong("SELECT COUNT(*) FROM Secrets"));
    }

    internal sealed class Secret
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
