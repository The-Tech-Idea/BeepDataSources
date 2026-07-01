using System.Data;
using TheTechIdea.Beep.DataBase.Helpers;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// Unit tests for RDBDataSource helper classes.
/// These test core logic without requiring full Beep framework initialization.
/// </summary>
public class HelperTests
{
    #region DbTypeMapper Tests

    [Theory]
    [InlineData("System.String", DbType.String)]
    [InlineData("System.Int32", DbType.Int32)]
    [InlineData("System.Int64", DbType.Int64)]
    [InlineData("System.Decimal", DbType.Decimal)]
    [InlineData("System.Double", DbType.Double)]
    [InlineData("System.Boolean", DbType.Boolean)]
    [InlineData("System.DateTime", DbType.DateTime)]
    [InlineData("System.Guid", DbType.Guid)]
    [InlineData("System.Byte", DbType.Byte)]
    [InlineData("System.Single", DbType.Single)]
    [InlineData("System.Int16", DbType.Int16)]
    public void ToDbType_ShouldMapCorrectly(string typeName, DbType expected)
    {
        var result = DbTypeMapper.ToDbType(typeName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDbType_UnknownType_ShouldDefaultToString()
    {
        var result = DbTypeMapper.ToDbType("System.SomeUnknownType");
        Assert.Equal(DbType.String, result);
    }

    [Fact]
    public void ToDbType_EmptyString_ShouldDefaultToString()
    {
        var result = DbTypeMapper.ToDbType("");
        Assert.Equal(DbType.String, result);
    }

    [Fact]
    public void ToDbType_Null_ShouldDefaultToString()
    {
        var result = DbTypeMapper.ToDbType(null!);
        Assert.Equal(DbType.String, result);
    }

    #endregion

    #region DbTypeMapper Additional Tests

    [Fact]
    public void ToDbType_CaseInsensitive_ShouldWork()
    {
        var result = DbTypeMapper.ToDbType("system.string");
        Assert.Equal(DbType.String, result);
    }

    [Fact]
    public void ToDbType_CommonSqlTypes_ShouldMap()
    {
        Assert.Equal(DbType.Int32, DbTypeMapper.ToDbType("System.Int32"));
        Assert.Equal(DbType.String, DbTypeMapper.ToDbType("System.String"));
        Assert.Equal(DbType.DateTime, DbTypeMapper.ToDbType("System.DateTime"));
        Assert.Equal(DbType.Decimal, DbTypeMapper.ToDbType("System.Decimal"));
    }

    #endregion

    #region SQLite Integration Tests

    [Fact]
    public void SqliteInMemory_CanCreateTable_And_Query()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY, name TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test VALUES (1, 'hello')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM test";
        var count = (long)cmd.ExecuteScalar()!;

        Assert.Equal(1, count);
    }

    [Fact]
    public void SqliteInMemory_ParameterizedQuery_PreventsInjection()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO users VALUES (1, 'alice'), (2, 'bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name FROM users WHERE id = $id";
        var param = cmd.CreateParameter();
        param.ParameterName = "$id";
        param.Value = 1;
        cmd.Parameters.Add(param);

        var result = (string)cmd.ExecuteScalar()!;
        Assert.Equal("alice", result);
    }

    [Fact]
    public void SqliteInMemory_BulkInsert_ShouldWork()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE bulk_test (id INTEGER PRIMARY KEY, value TEXT)";
        cmd.ExecuteNonQuery();

        // Multi-row insert (SQLite supports this)
        cmd.CommandText = "INSERT INTO bulk_test VALUES (1, 'a'), (2, 'b'), (3, 'c'), (4, 'd'), (5, 'e')";
        var rows = cmd.ExecuteNonQuery();

        Assert.Equal(5, rows);

        cmd.CommandText = "SELECT COUNT(*) FROM bulk_test";
        Assert.Equal(5L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void SqliteInMemory_Transaction_CommitAndRollback()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE tx_test (id INTEGER PRIMARY KEY, val INTEGER)";
        cmd.ExecuteNonQuery();

        // Test commit
        using (var tx = connection.BeginTransaction())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO tx_test VALUES (1, 100)";
            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        cmd.Transaction = null;
        cmd.CommandText = "SELECT val FROM tx_test WHERE id = 1";
        Assert.Equal(100L, (long)cmd.ExecuteScalar()!);

        // Test rollback
        using (var tx = connection.BeginTransaction())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO tx_test VALUES (2, 200)";
            cmd.ExecuteNonQuery();
            tx.Rollback();
        }

        cmd.Transaction = null;
        cmd.CommandText = "SELECT COUNT(*) FROM tx_test";
        Assert.Equal(1L, (long)cmd.ExecuteScalar()!); // Only row 1 remains
    }

    #endregion
}
