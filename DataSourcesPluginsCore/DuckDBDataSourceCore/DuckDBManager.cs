using System;
using System.Data.Odbc;

public class DuckDBManager
{
    private OdbcConnection _connection;

    public DuckDBManager(string connectionString)
    {
        _connection = new OdbcConnection(connectionString);
    }
    public DuckDBManager(bool inMemory = true)
    {
        var connectionString = inMemory
            ? "Driver={DuckDB ODBC Driver};Database=:memory:;Server=localhost;"
            : "Driver={DuckDB ODBC Driver};Database=myDatabase;Server=localhost;";

        _connection = new OdbcConnection(connectionString);
    }
    public void OpenConnection()
    {
        _connection.Open();
    }

    public void CloseConnection()
    {
        _connection.Close();
    }

    public void ExecuteNonQuery(string commandText)
    {
        using (var command = new OdbcCommand(commandText, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public object ExecuteScalar(string commandText)
    {
        using (var command = new OdbcCommand(commandText, _connection))
        {
            return command.ExecuteScalar();
        }
    }

    public OdbcDataReader ExecuteReader(string commandText)
    {
        using (var command = new OdbcCommand(commandText, _connection))
        {
            return command.ExecuteReader();
        }
    }

    public void CreateDatabase(string databaseName)
    {
        ExecuteNonQuery($"CREATE DATABASE {databaseName}");
    }

    public void DeleteDatabase(string databaseName)
    {
        ExecuteNonQuery($"DROP DATABASE {databaseName}");
    }
}
