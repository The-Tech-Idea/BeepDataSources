using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 3 identifier-quoting fix (K3).
///
/// <c>GetFieldName</c> used to quote with <c>ColumnDelimiter</c> — base default <c>"''"</c>, six
/// drivers override it to <c>"'"</c> — so a column named <c>Cust Id</c> rendered as
/// <c>'Cust Id'</c>: a string literal, not an identifier. It also only quoted names containing a
/// space, so reserved words went out bare.
/// </summary>
public class IdentifierQuotingTests
{
    private static RDBSource NewSource(DataSourceType type) =>
        new RDBSource("quote-probe", null, null, type, new ErrorsInfo { Flag = Errors.Ok });

    private static EntityStructure OneColumn(string columnName) =>
        new EntityStructure
        {
            EntityName = "T",
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = columnName, Fieldtype = "System.String" }
            },
            PrimaryKeys = new List<EntityField>()
        };

    [Theory]
    [InlineData(DataSourceType.SqlServer, "[Cust Id]")]
    [InlineData(DataSourceType.AzureSQL, "[Cust Id]")]
    [InlineData(DataSourceType.Mysql, "`Cust Id`")]
    [InlineData(DataSourceType.MariaDB, "`Cust Id`")]
    [InlineData(DataSourceType.Postgre, "\"Cust Id\"")]
    [InlineData(DataSourceType.Oracle, "\"Cust Id\"")]
    [InlineData(DataSourceType.SqlLite, "\"Cust Id\"")]
    public void ColumnWithASpace_IsQuotedWithTheDialectsOwnCharacters(DataSourceType type, string expected)
    {
        string sql = NewSource(type).GetInsertString("T", OneColumn("Cust Id"));

        Assert.Contains(expected, sql);

        // The old rendering was a string literal on every driver but SQLite.
        Assert.DoesNotContain("'Cust Id'", sql);
    }

    [Theory]
    [InlineData(DataSourceType.SqlServer, "[Order]")]
    [InlineData(DataSourceType.Mysql, "`Order`")]
    [InlineData(DataSourceType.Postgre, "\"Order\"")]
    public void ReservedWordColumn_IsQuoted(DataSourceType type, string expected)
    {
        // Previously never quoted at all — quoting only kicked in for names containing a space —
        // so a column called Order or User produced a syntax error on every driver.
        string sql = NewSource(type).GetInsertString("T", OneColumn("Order"));

        Assert.Contains(expected, sql);
    }

    [Theory]
    [InlineData(DataSourceType.SqlServer)]
    [InlineData(DataSourceType.Postgre)]
    [InlineData(DataSourceType.Oracle)]
    [InlineData(DataSourceType.Mysql)]
    public void OrdinaryColumn_IsLeftBare(DataSourceType type)
    {
        // Deliberate: quoting an ordinary name would change the SQL for every driver, and on
        // PostgreSQL and Oracle a quoted identifier is case-SENSITIVE while a bare one folds. Any
        // hand-authored EntityStructure whose column case does not match the stored case works
        // today only because the name goes out unquoted.
        string sql = NewSource(type).GetInsertString("T", OneColumn("CustomerName"));

        Assert.Contains("CustomerName", sql);
        Assert.DoesNotContain("[CustomerName]", sql);
        Assert.DoesNotContain("\"CustomerName\"", sql);
        Assert.DoesNotContain("`CustomerName`", sql);
    }

    [Fact]
    public void ClosingDelimiterInsideTheName_IsEscaped()
    {
        // A name containing the closing character must not be able to terminate the quoting early.
        string sql = NewSource(DataSourceType.SqlServer).GetInsertString("T", OneColumn("we]rd col"));

        Assert.Contains("[we]]rd col]", sql);
    }

    [Fact]
    public void AlreadyQuotedName_IsNotDoubleQuoted()
    {
        string sql = NewSource(DataSourceType.SqlServer).GetInsertString("T", OneColumn("[Already]"));

        Assert.Contains("[Already]", sql);
        Assert.DoesNotContain("[[Already]]", sql);
    }
}
