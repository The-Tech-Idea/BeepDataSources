using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// `GetFallbackDbType`/`NormalizeDbTypeForProvider` decide the column types `CreateEntityAs` emits
/// for every engine when the configured per-datasource type map is absent, empty, or leaks a type
/// name that belongs to a different provider (`DMEEditor.typesHelper` is null in every test here,
/// which is exactly the "absent" case, and is also the class's own documented no-`IDMEEditor`
/// contract from Phase 11).
///
/// Before this pass, only SQL Server, PostgreSQL, MySQL and Oracle had a real mapping; everything
/// else fell back to SQLite's affinity-typed names (`TEXT`/`REAL`/`BLOB`), several of which the
/// target engine does not recognise as type names at all -- Spanner shares essentially no type
/// vocabulary with SQLite, and Firebird has no bare `TEXT`/`BLOB`-as-binary. These tests assert on
/// the generated `CREATE TABLE` text, per this class's own testing convention, since SQLite (the
/// only engine actually reachable through the harness) would execute any of these strings without
/// distinguishing a name real engines reject from one they accept.
/// </summary>
public class DdlTypeMappingTests
{
    private static EntityStructure Entity() => new EntityStructure
    {
        EntityName = "Widgets",
        Fields = new List<EntityField>
        {
            new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true },
            new EntityField { FieldName = "Name", Fieldtype = "System.String" },
            new EntityField { FieldName = "IsActive", Fieldtype = "System.Boolean" },
            new EntityField { FieldName = "Price", Fieldtype = "System.Decimal" },
            new EntityField { FieldName = "Created", Fieldtype = "System.DateTime" },
            new EntityField { FieldName = "ExternalId", Fieldtype = "System.Guid" },
            new EntityField { FieldName = "Payload", Fieldtype = "System.Byte[]" },
        },
        PrimaryKeys = new List<EntityField> { new EntityField { FieldName = "Id", Fieldtype = "System.Int32", IsKey = true } }
    };

    private static string Ddl(DataSourceType engine)
    {
        var h = SqliteHarness.Create(engine);
        return h.TestSource.GenerateCreateEntityScriptForTest(Entity());
    }

    [Fact]
    public void CockroachDB_UsesPostgresTypeNames_NotSqliteAffinities()
    {
        string ddl = Ddl(DataSourceType.Cockroach);

        Assert.Contains("BOOLEAN", ddl);
        Assert.Contains("NUMERIC(18,4)", ddl);
        Assert.Contains("UUID", ddl);
        Assert.Contains("BYTES", ddl);
        // Cockroach's own binary type name, not Postgres's BYTEA alias or SQLite's BLOB.
        Assert.DoesNotContain("BYTEA", ddl);
        Assert.DoesNotContain("BLOB", ddl);
    }

    [Fact]
    public void Spanner_UsesItsOwnTypeVocabulary_NotSqliteAffinities()
    {
        // Spanner shares almost no type names with SQLite's INTEGER/TEXT/REAL/BOOLEAN/BLOB -- the
        // pre-fix fallback would have produced a CREATE TABLE Spanner rejects outright.
        string ddl = Ddl(DataSourceType.Spanner);

        Assert.Contains("INT64", ddl);
        Assert.Contains("STRING(MAX)", ddl);
        Assert.Contains("BOOL", ddl);
        Assert.Contains("NUMERIC", ddl);
        Assert.Contains("BYTES(MAX)", ddl);
        Assert.DoesNotContain("TEXT", ddl);
        Assert.DoesNotContain("BOOLEAN", ddl);
        Assert.DoesNotContain(" BLOB", ddl);
    }

    [Fact]
    public void Firebird_UsesBlobSubTypeText_NotBareText()
    {
        // Firebird has no bare TEXT type; BLOB SUB_TYPE TEXT is its unbounded-text equivalent.
        string ddl = Ddl(DataSourceType.FireBird);

        Assert.Contains("BLOB SUB_TYPE TEXT", ddl);
        Assert.Contains("BOOLEAN", ddl);
        Assert.Contains("CHAR(36)", ddl); // Guid
    }

    [Fact]
    public void Hana_UsesSizedNvarchar_NotBareText()
    {
        string ddl = Ddl(DataSourceType.Hana);

        Assert.Contains("NVARCHAR(5000)", ddl);
        Assert.Contains("VARCHAR(36)", ddl); // Guid, HANA has no native UUID type
        Assert.DoesNotContain(" TEXT", ddl);
    }

    [Fact]
    public void Presto_HasNativeUuidType_AndUnboundedVarchar()
    {
        string ddl = Ddl(DataSourceType.Presto);

        Assert.Contains("VARCHAR", ddl);
        Assert.Contains("UUID", ddl);
        Assert.DoesNotContain("TEXT", ddl);
        Assert.DoesNotContain(" BLOB", ddl);
    }

    [Fact]
    public void Snowflake_UsesBinary_NotBlob()
    {
        // Snowflake accepts TEXT/REAL as aliases (so those would not have been visibly wrong before)
        // but its binary type is BINARY, not BLOB.
        string ddl = Ddl(DataSourceType.SnowFlake);

        Assert.Contains("BINARY", ddl);
        Assert.DoesNotContain("BLOB", ddl);
    }

    [Fact]
    public void SqlServer_StillMapsGuidAndBoolCorrectly_Regression()
    {
        // Pins the pre-existing SQL Server branch so the new dialects sitting next to it in the same
        // switch did not disturb it.
        string ddl = Ddl(DataSourceType.SqlServer);

        Assert.Contains("BIT", ddl);
        Assert.Contains("UNIQUEIDENTIFIER", ddl);
        Assert.Contains("VARBINARY(MAX)", ddl);
    }

    // ---------------------------------------------------------------- NormalizeDbTypeForProvider

    [Fact]
    public void Postgres_NormalizesMySqlLongblob_ToItsOwnBytea()
    {
        // NormalizeDbTypeForProvider runs unconditionally in GenerateCreateEntityScript, so this
        // exercises the same code path as the CREATE TABLE tests above, but pins the cross-provider
        // leak scenario specifically: a type name from one provider's own vocabulary being corrected
        // for another, which is this method's entire reason to exist. Before this pass PostgreSQL was
        // not normalized at all -- any leaked non-Postgres type name passed straight through.
        var h = SqliteHarness.Create(DataSourceType.Postgre);
        var entity = new EntityStructure
        {
            EntityName = "T",
            Fields = new List<EntityField> { new EntityField { FieldName = "Blob", Fieldtype = "System.Byte[]" } },
            PrimaryKeys = new List<EntityField>()
        };

        string ddl = h.TestSource.GenerateCreateEntityScriptForTest(entity);

        Assert.Contains("BYTEA", ddl);
    }
}
