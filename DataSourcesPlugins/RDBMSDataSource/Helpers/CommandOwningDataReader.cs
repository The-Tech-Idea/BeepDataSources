using System;
using System.Data;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    /// <summary>
    /// An <see cref="IDataReader"/> that also owns the <see cref="IDbCommand"/> it was executed
    /// from, disposing it when the reader is disposed.
    /// </summary>
    /// <remarks>
    /// <c>GetDataReader</c> is an <c>IRDBSource</c> contract method that hands the caller a reader
    /// and nothing else. The command behind it came from <c>GetDataCommand()</c>, which creates a
    /// fresh <c>IDbConnection.CreateCommand()</c> on every call — so the caller had no handle on it
    /// and no way to dispose it. Every <c>GetDataReader</c> call leaked one command for the lifetime
    /// of the connection.
    ///
    /// Disposing the command directly at the end of <c>GetDataReader</c> is not an option: on
    /// several providers — SQLite among them — disposing the command finalises the statement handle
    /// the open reader is still reading through. Ownership has to travel with the reader, which is
    /// the one object the caller does hold.
    ///
    /// Order matters in <see cref="Dispose"/>: the reader closes first, then the command.
    /// </remarks>
    internal sealed class CommandOwningDataReader : IDataReader
    {
        private readonly IDataReader _inner;
        private readonly IDbCommand _command;
        private bool _disposed;

        internal CommandOwningDataReader(IDataReader inner, IDbCommand command)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _command = command;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _inner.Dispose(); }
            finally { _command?.Dispose(); }
        }

        public object this[int i] => _inner[i];
        public object this[string name] => _inner[name];

        public int Depth => _inner.Depth;
        public bool IsClosed => _inner.IsClosed;
        public int RecordsAffected => _inner.RecordsAffected;
        public int FieldCount => _inner.FieldCount;

        public void Close() => _inner.Close();
        public DataTable? GetSchemaTable() => _inner.GetSchemaTable();
        public bool NextResult() => _inner.NextResult();
        public bool Read() => _inner.Read();

        public bool GetBoolean(int i) => _inner.GetBoolean(i);
        public byte GetByte(int i) => _inner.GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
            => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        public char GetChar(int i) => _inner.GetChar(i);
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
            => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public IDataReader GetData(int i) => _inner.GetData(i);
        public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);
        public DateTime GetDateTime(int i) => _inner.GetDateTime(i);
        public decimal GetDecimal(int i) => _inner.GetDecimal(i);
        public double GetDouble(int i) => _inner.GetDouble(i);
        public Type GetFieldType(int i) => _inner.GetFieldType(i);
        public float GetFloat(int i) => _inner.GetFloat(i);
        public Guid GetGuid(int i) => _inner.GetGuid(i);
        public short GetInt16(int i) => _inner.GetInt16(i);
        public int GetInt32(int i) => _inner.GetInt32(i);
        public long GetInt64(int i) => _inner.GetInt64(i);
        public string GetName(int i) => _inner.GetName(i);
        public int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public string GetString(int i) => _inner.GetString(i);
        public object GetValue(int i) => _inner.GetValue(i);
        public int GetValues(object[] values) => _inner.GetValues(values);
        public bool IsDBNull(int i) => _inner.IsDBNull(i);
    }
}
