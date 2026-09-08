using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Utilities;

namespace RDBDataSource.Tests;

/// <summary>
/// Drives a real <see cref="RDBSource"/> against an in-memory SQLite database.
///
/// The existing RDBSourceIntegrationTests hand-write SQL and never touch the class under test.
/// This wires an actual SQLite connection into the datasource instead, so CRUD goes through
/// GetInsertString / GetUpdateString / GetDeleteString, the parameter binders and GetDataCommand —
/// the code that actually ships.
///
/// It sidesteps the engine: <see cref="RDBDataConnection.DbConn"/> and
/// <see cref="RDBDataConnection.ConnectionStatus"/> are public and settable, and the entity
/// structure is placed directly in <c>Entities</c> so structure lookup does not need a live
/// IDMEEditor or ConfigEditor.
/// </summary>
/// <summary>
/// An <see cref="RDBSource"/> that resolves entity types without the engine.
/// </summary>
/// <remarks>
/// <c>SetObjects</c> calls <see cref="RDBSource.GetEntityType"/>, which goes through
/// <c>DMTypeBuilder</c> to <c>editor.classCreator</c> and needs a live <c>IDMEEditor</c> to
/// Roslyn-compile a POCO — the whole engine, for a value the write path only stores in a field.
/// Overriding that virtual hook keeps these tests on the real CRUD, SQL-generation and
/// parameter-binding code while skipping the code generator.
/// </remarks>
internal sealed class TestRDBSource : RDBSource
{
    private readonly System.Type _entityType;

    public TestRDBSource(string name, DataSourceType type, IErrorsInfo errors, System.Type entityType)
        : base(name, null, null, type, errors)
    {
        _entityType = entityType;
    }

    public override System.Type GetEntityType(string EntityName) => _entityType;

    /// <summary>Exposes the protected CREATE TABLE string builder for dialect-mapping assertions.</summary>
    public string GenerateCreateEntityScriptForTest(EntityStructure entity) => GenerateCreateEntityScript(entity);

    /// <summary>
    /// The most recent command handed out by <c>GetDataCommand()</c>.
    /// </summary>
    /// <remarks>
    /// <c>ConfigureCommand</c> is the last thing <c>GetDataCommand</c> touches before returning, so
    /// overriding it is the only way a test can get at a command the class creates internally and
    /// never exposes — which is exactly the situation <c>GetDataReader</c> leaked one in.
    /// </remarks>
    public IDbCommand LastCommand { get; private set; }

    /// <summary>
    /// Every command created via <c>GetDataCommand()</c> during this instance's lifetime, in order.
    /// A single bulk operation issues several commands in sequence (CREATE TEMP TABLE, per-batch
    /// INSERT, the merge UPDATE, the DROP), each disposed before the next runs, so <see
    /// cref="LastCommand"/> alone only ever reflects the final one. This lets a test find an
    /// intermediate step's SQL even though its command object has since been disposed.
    /// </summary>
    private readonly List<IDbCommand> _commandHistory = new();

    protected override void ConfigureCommand(IDbCommand command)
    {
        base.ConfigureCommand(command);
        LastCommand = command;
        _commandHistory.Add(command);
    }

    /// <summary>
    /// The <c>CommandText</c> of the most recent recorded command whose text contains
    /// <paramref name="containing"/>, searched newest-first. Reading <c>CommandText</c> off an
    /// already-disposed <c>SqliteCommand</c> does not throw -- it is a plain managed string field,
    /// not a handle Dispose() releases -- which is what makes this safe to call after the operation
    /// that issued the command has completed and moved on to later steps.
    /// </summary>
    public string? LastCommandTextSeen(string containing)
    {
        for (int i = _commandHistory.Count - 1; i >= 0; i--)
        {
            string? text = _commandHistory[i].CommandText;
            if (text != null && text.Contains(containing, System.StringComparison.OrdinalIgnoreCase))
                return text;
        }
        return null;
    }
}

internal sealed class SqliteHarness : System.IDisposable
{
    public RDBSource Source { get; }
    public SqliteConnection Connection { get; }
    public IErrorsInfo ErrorInfo { get; }

    /// <summary>The same instance as <see cref="Source"/>, typed for the test-only hooks.</summary>
    public TestRDBSource TestSource => (TestRDBSource)Source;

    private SqliteHarness(RDBSource source, SqliteConnection connection, IErrorsInfo errors)
    {
        Source = source;
        Connection = connection;
        ErrorInfo = errors;
    }

    /// <param name="dataSourceType">
    /// Reported engine. Lets a test exercise a dialect-specific decision (the MySQL zero-rows
    /// carve-out, say) while still executing against SQLite.
    /// </param>
    /// <param name="entityType">CLR type the datasource should report for its entities.</param>
    public static SqliteHarness Create(DataSourceType dataSourceType = DataSourceType.SqlLite,
                                       System.Type entityType = null)
    {
        var errors = new ErrorsInfo { Flag = Errors.Ok };
        var source = new TestRDBSource("sqlite-harness", dataSourceType, errors,
                                       entityType ?? typeof(object));

        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var rdb = (RDBDataConnection)source.Dataconnection;
        rdb.DbConn = connection;
        rdb.ConnectionStatus = ConnectionState.Open;

        return new SqliteHarness(source, connection, errors);
    }

    public void Execute(string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public long ScalarLong(string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        return (long)cmd.ExecuteScalar()!;
    }

    public string? ScalarString(string sql)
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// Registers an entity structure so <c>SetObjects</c> finds it without a metadata round trip.
    /// </summary>
    public void Register(EntityStructure entity)
    {
        Source.Entities.Add(entity);
        Source.EntitiesNames.Add(entity.EntityName);
    }

    public static EntityField Field(string name, string type = "System.String",
                                    bool isKey = false, bool isAutoIncrement = false) =>
        new EntityField
        {
            FieldName = name,
            Fieldtype = type,
            IsKey = isKey,
            IsAutoIncrement = isAutoIncrement
        };

    public static EntityStructure Entity(string name, params EntityField[] fields)
    {
        var list = new List<EntityField>(fields);
        var keys = new List<EntityField>();
        foreach (var f in list)
        {
            if (f.IsKey) keys.Add(f);
        }
        return new EntityStructure
        {
            EntityName = name,
            DatasourceEntityName = name,
            Fields = list,
            PrimaryKeys = keys
        };
    }

    public void Dispose() => Connection.Dispose();
}
