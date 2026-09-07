using System;
using System.Data;
using Microsoft.Data.Sqlite;
using TheTechIdea.Beep.ConfigUtil;
using Xunit;

namespace RDBDataSource.Tests;

/// <summary>
/// GetDataReader is an IRDBSource contract method that returns a reader and nothing else, while the
/// command behind it comes from GetDataCommand(), which creates a fresh one on every call. The
/// caller therefore had no handle on the command and no way to dispose it: one leaked IDbCommand
/// per call, held for the lifetime of the connection.
///
/// The reader now owns the command. These tests pin both halves of that — the command is disposed
/// when the reader is, and not a moment before, since disposing it early finalises the statement
/// handle SQLite is still reading through.
/// </summary>
public class ReaderOwnershipTests
{
    private static SqliteHarness NewHarness()
    {
        var h = SqliteHarness.Create();
        h.Execute("create table Widget (Id integer primary key, Name text)");
        h.Execute("insert into Widget (Id, Name) values (1, 'first'), (2, 'second')");
        return h;
    }

    [Fact]
    public void GetDataReader_ReaderStillWorksWhileOpen()
    {
        using var h = NewHarness();

        using IDataReader reader = h.Source.GetDataReader("select Id, Name from Widget order by Id");

        Assert.NotNull(reader);
        Assert.True(reader.Read());
        Assert.Equal("first", reader.GetString(reader.GetOrdinal("Name")));
        Assert.True(reader.Read());
        Assert.Equal("second", reader.GetString(reader.GetOrdinal("Name")));
        Assert.False(reader.Read());
    }

    [Fact]
    public void GetDataReader_DisposingTheReader_DisposesTheCommand()
    {
        using var h = NewHarness();

        IDataReader reader = h.Source.GetDataReader("select Id from Widget");
        var command = (SqliteCommand)h.TestSource.LastCommand!;
        bool disposedRaised = false;
        command.Disposed += (_, _) => disposedRaised = true;

        // Still live while the reader is open. Disposing the command at the end of GetDataReader
        // would have finalised the statement handle this Read is going through.
        Assert.True(reader.Read());
        Assert.False(disposedRaised);

        reader.Dispose();

        // Without the fix the raw provider reader came back, nothing owned the command, and the
        // command was still live here — leaked for the lifetime of the connection.
        Assert.True(disposedRaised);
    }

    [Fact]
    public void GetDataReader_OnAClosedConnection_ReturnsNullAndFlagsFailure()
    {
        using var h = NewHarness();
        var rdb = (TheTechIdea.Beep.DataBase.RDBDataConnection)h.Source.Dataconnection;
        rdb.ConnectionStatus = ConnectionState.Closed;

        IDataReader reader = h.Source.GetDataReader("select Id from Widget");

        Assert.Null(reader);
        Assert.Equal(Errors.Failed, h.Source.ErrorObject.Flag);
    }
}
