using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Regression tests for the Phase 3 schema-qualification fixes (K10, K14, K15).
/// </summary>
public class SchemaQualificationTests
{
    private static RDBSource SourceWithSchema(string schema)
    {
        var source = new RDBSource("schema-probe", null, null, DataSourceType.SqlServer,
                                   new ErrorsInfo { Flag = Errors.Ok });
        source.Dataconnection.ConnectionProp.SchemaName = schema;
        return source;
    }

    private static EntityStructure Orders() =>
        new EntityStructure
        {
            EntityName = "Orders",
            Fields = new List<EntityField>
            {
                new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
                new EntityField { FieldName = "Total", Fieldtype = "System.Decimal" }
            },
            PrimaryKeys = new List<EntityField>
            {
                new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true }
            }
        };

    [Fact]
    public void Insert_Update_And_Delete_AllQualifyWithTheSameSchema()
    {
        var source = SourceWithSchema("dbo");
        var entity = Orders();

        // Each call allocates parameters into the same instance, which is fine — only the table
        // reference matters here.
        string insert = source.GetInsertString("Orders", entity);
        string update = source.GetUpdateString("Orders", entity);
        string delete = source.GetDeleteString("Orders", entity);

        // Schema qualification was previously applied to INSERT only: it was commented out for
        // UPDATE and absent from DELETE, so the three statements addressed different objects.
        Assert.Contains("dbo.Orders", insert);
        Assert.Contains("dbo.Orders", update);
        Assert.Contains("dbo.Orders", delete);

        // And never with the separator missing, which is what the bulk and async paths produced.
        Assert.DoesNotContain("dboOrders", insert);
        Assert.DoesNotContain("dboOrders", update);
        Assert.DoesNotContain("dboOrders", delete);
    }

    [Fact]
    public void NoSchemaConfigured_LeavesTheNameBare()
    {
        var source = SourceWithSchema(null);

        string insert = source.GetInsertString("Orders", Orders());

        Assert.Contains("Orders", insert);
        Assert.DoesNotContain(".Orders", insert);
    }

    [Fact]
    public void SchemaEqualToTheUserId_IsNotPrepended()
    {
        // Mirrors GetTableName's long-standing rule, so statements built here qualify exactly when
        // statements rewritten there would.
        var source = SourceWithSchema("APPUSER");
        source.Dataconnection.ConnectionProp.UserID = "appuser";

        string insert = source.GetInsertString("Orders", Orders());

        Assert.DoesNotContain("APPUSER.Orders", insert);
    }

    [Fact]
    public void InsertStatement_KeepsItsIdentifierCase()
    {
        // GetTableName lowercased the whole fragment before qualifying it —
        // GetTableName(Insertstr.ToLower()) — which folds table and column names on
        // case-sensitive servers.
        var source = SourceWithSchema("MySchema");

        string insert = source.GetInsertString("Orders", Orders());

        Assert.Contains("MySchema.Orders", insert);
        Assert.DoesNotContain("myschema.orders", insert);
        Assert.Contains("Total", insert);
    }

    [Fact]
    public void EntityWhoseNameContainsAKeyword_ProducesAnUncorruptedSelect()
    {
        // GetTableName dispatched on IndexOf("insert"/"update"/"delete") over the whole statement,
        // so "select * from updates" matched the UPDATE branch and rewrote token[1] — the "*" —
        // yielding "select  dbo.*  from updates".
        using var h = SqliteHarness.Create(entityType: typeof(Row));
        h.Source.Dataconnection.ConnectionProp.SchemaName = "main";
        h.Execute("CREATE TABLE updates (Id INTEGER PRIMARY KEY, Note TEXT)");
        h.Execute("INSERT INTO updates (Id, Note) VALUES (1, 'hello')");
        h.Register(SqliteHarness.Entity("updates",
            SqliteHarness.Field("Id", "System.Int32", isKey: true),
            SqliteHarness.Field("Note")));

        var rows = h.Source.GetEntity("updates", null).ToList();

        Assert.Single(rows);
    }

    internal sealed class Row
    {
        public int Id { get; set; }
        public string? Note { get; set; }
    }
}
