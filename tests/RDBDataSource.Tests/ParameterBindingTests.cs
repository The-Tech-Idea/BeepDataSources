using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 2 parameter-binding fix (K1).
///
/// Statement generation allocates a unique parameter name per field; binding then has to recover
/// which name belongs to which field. It used to do that by searching a <c>HashSet</c> for a name
/// that merely started with — or, in the UPDATE primary-key clause, merely contained — the field
/// name. A HashSet has no defined enumeration order, so the recovery could return a different
/// field's parameter and the statement bound the wrong value to the wrong column.
///
/// These assert on the generated SQL text rather than on execution, because SQLite would happily
/// run the wrong-but-valid statement and report success.
/// </summary>
public class ParameterBindingTests
{
    private static RDBSource NewSource() =>
        new RDBSource("param-probe", null, null, DataSourceType.SqlServer,
                      new ErrorsInfo { Flag = Errors.Ok });

    private static EntityField Field(string name, bool isKey = false, bool isAutoIncrement = false) =>
        new EntityField
        {
            FieldName = name,
            Fieldtype = "System.String",
            IsKey = isKey,
            IsAutoIncrement = isAutoIncrement
        };

    /// <summary>Everything after the WHERE keyword, which is where the key predicate lives.</summary>
    private static string WhereClauseOf(string sql)
    {
        int idx = sql.IndexOf("where", System.StringComparison.OrdinalIgnoreCase);
        Assert.True(idx >= 0, $"Expected a WHERE clause in: {sql}");
        return sql.Substring(idx);
    }

    [Fact]
    public void GetUpdateString_PrimaryKeyIsBoundToItsOwnParameter_NotALongerFieldsParameter()
    {
        // "Id" is a prefix of, and contained in, "ProductId" — the exact shape the old substring
        // recovery got wrong. It produced "where Id = @p_ProductId", so the UPDATE was steered onto
        // a row chosen by ProductId's value.
        var entity = new EntityStructure
        {
            EntityName = "Orders",
            Fields = new List<EntityField> { Field("Id", isKey: true), Field("ProductId"), Field("Quantity") },
            PrimaryKeys = new List<EntityField> { Field("Id", isKey: true) }
        };

        string sql = NewSource().GetUpdateString("Orders", entity);
        string where = WhereClauseOf(sql);

        Assert.Contains("@p_Id", where);
        Assert.DoesNotContain("@p_ProductId", where);
    }

    [Fact]
    public void GetUpdateString_KeyWhoseNameCollidesWithAnAllocatedParameter_GetsItsOwnParameter()
    {
        // The deterministic reproduction of the old defect.
        //
        // "My Field" normalises to "My_Field", so after the SET clause the allocated-name set
        // contains "My_Field" — which is also the primary key's own normalised name. The old WHERE
        // loop did:
        //     if (usedParameterNames.Contains(paramName))
        //         paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
        // and so REUSED the parameter already allocated for the column "My Field". The statement
        // became "SET [My Field] = @p_My_Field WHERE My_Field = @p_My_Field": one parameter for two
        // different columns, bound with the SET column's value, so the UPDATE both wrote the wrong
        // column and filtered on the wrong value.
        var entity = new EntityStructure
        {
            EntityName = "Odd",
            Fields = new List<EntityField> { Field("My Field"), Field("My_Field", isKey: true) },
            PrimaryKeys = new List<EntityField> { Field("My_Field", isKey: true) }
        };

        string sql = NewSource().GetUpdateString("Odd", entity);
        int whereIdx = sql.IndexOf("where", System.StringComparison.OrdinalIgnoreCase);
        string setClause = sql.Substring(0, whereIdx);
        string where = sql.Substring(whereIdx);

        var setParams = new HashSet<string>();
        foreach (Match m in Regex.Matches(setClause, @"@p_\w+")) setParams.Add(m.Value);
        var whereParams = new HashSet<string>();
        foreach (Match m in Regex.Matches(where, @"@p_\w+")) whereParams.Add(m.Value);

        Assert.NotEmpty(setParams);
        Assert.NotEmpty(whereParams);

        // The key predicate must not borrow a parameter that the SET clause already owns.
        whereParams.IntersectWith(setParams);
        Assert.Empty(whereParams);
    }

    [Fact]
    public void GetUpdateString_SetClauseKeepsItsOwnParameter()
    {
        var entity = new EntityStructure
        {
            EntityName = "Orders",
            Fields = new List<EntityField> { Field("Id", isKey: true), Field("ProductId") },
            PrimaryKeys = new List<EntityField> { Field("Id", isKey: true) }
        };

        string sql = NewSource().GetUpdateString("Orders", entity);
        string setClause = sql.Substring(0, sql.IndexOf("where", System.StringComparison.OrdinalIgnoreCase));

        Assert.Contains("@p_ProductId", setClause);
    }

    [Fact]
    public void GetInsertString_FieldsSharingAPrefix_GetDistinctParameters()
    {
        // "Name" is a prefix of "NameSuffix". Generation was always fine here; it was the binder's
        // StartsWith recovery that could match the wrong one. Assert the names are distinct so the
        // recovery has something unambiguous to find.
        var entity = new EntityStructure
        {
            EntityName = "People",
            Fields = new List<EntityField> { Field("Name"), Field("NameSuffix") },
            PrimaryKeys = new List<EntityField>()
        };

        string sql = NewSource().GetInsertString("People", entity);

        Assert.Contains("@p_Name,", sql + ",");   // trailing comma guards against matching @p_NameSuffix
        Assert.Contains("@p_NameSuffix", sql);
    }

    [Fact]
    public void GetInsertString_FieldsNormalisingToTheSameName_StillGetUniqueParameters()
    {
        // "My Field" normalises to "My_Field", colliding with a real "My_Field" column. The
        // allocator suffixes the second one; both must still appear exactly once.
        var entity = new EntityStructure
        {
            EntityName = "Odd",
            Fields = new List<EntityField> { Field("My Field"), Field("My_Field") },
            PrimaryKeys = new List<EntityField>()
        };

        string sql = NewSource().GetInsertString("Odd", entity);

        var parameters = Regex.Matches(sql, @"@p_\w+");
        var distinct = new HashSet<string>();
        foreach (Match m in parameters) distinct.Add(m.Value);

        Assert.Equal(2, parameters.Count);
        Assert.Equal(2, distinct.Count);
    }

    [Fact]
    public void GetDeleteString_CompositeKey_BindsEachKeyToItsOwnParameter()
    {
        var entity = new EntityStructure
        {
            EntityName = "OrderLines",
            Fields = new List<EntityField> { Field("Order", isKey: true), Field("OrderLine", isKey: true) },
            PrimaryKeys = new List<EntityField> { Field("Order", isKey: true), Field("OrderLine", isKey: true) }
        };

        string sql = NewSource().GetDeleteString("OrderLines", entity);

        var distinct = new HashSet<string>();
        foreach (Match m in Regex.Matches(sql, @"@p_\w+")) distinct.Add(m.Value);

        Assert.Equal(2, distinct.Count);
    }
}
