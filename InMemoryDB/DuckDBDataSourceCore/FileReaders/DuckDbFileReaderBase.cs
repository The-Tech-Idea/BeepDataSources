using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using DuckDB.NET.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.FileManager.Readers;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>
    /// Base <see cref="IFileFormatReader"/> that delegates reads to DuckDB table functions
    /// (<c>read_parquet</c>, <c>read_csv_auto</c>, etc.) using a short-lived in-memory database per operation.
    /// </summary>
    public abstract class DuckDbFileReaderBase : IFileFormatReader
    {
        private readonly List<RowDiagnostic> _diagnostics = new();

        public abstract DataSourceType SupportedType { get; }

        public bool HasHeader { get; set; } = true;

        public ParseMode ParseMode { get; set; } = ParseMode.Lenient;

        public IReadOnlyList<RowDiagnostic> LastDiagnostics => _diagnostics;

        public void ClearDiagnostics() => _diagnostics.Clear();

        public abstract string GetDefaultExtension();

        public virtual void Configure(IConnectionProperties? props) { }

        /// <summary>Full SQL text: <c>SELECT * FROM read_*('path'…)</c> (no LIMIT).</summary>
        protected abstract string BuildSelectSql(string filePath);

        public string InferFieldType(string? current, string? rawValue)
            => DuckDbReaderTypeInference.Widen(current, rawValue);

        public string[] ReadHeaders(string filePath) => GetColumnNames(filePath);

        public EntityStructure? GetEntityStructure(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            string entityName = Path.GetFileNameWithoutExtension(filePath);
            string[] names = GetColumnNames(filePath);
            if (names.Length == 0)
                return FileReaderEntityHelper.BuildEntityStructure(entityName, Array.Empty<string>());

            return FileReaderEntityHelper.BuildEntityStructure(entityName, names);
        }

        public IEnumerable<string[]> ReadRows(string filePath)
        {
            if (!File.Exists(filePath))
                yield break;

            ClearDiagnostics();
            var sql = BuildSelectSql(filePath);
            DuckDBConnection? conn = null;
            try
            {
                conn = new DuckDBConnection("DataSource=:memory:");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                long rowIndex = 0;
                while (reader.Read())
                {
                    rowIndex++;
                    var row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[i] = reader.IsDBNull(i) ? string.Empty : ConvertToCellString(reader.GetValue(i));
                    yield return row;
                }
            }
            catch (Exception ex)
            {
                _diagnostics.Add(new RowDiagnostic
                {
                    RowIndex = -1,
                    Code = "DUCKDB_READ_ERROR",
                    Message = ex.Message,
                    Severity = DiagnosticSeverity.Error
                });

                if (ParseMode == ParseMode.Strict)
                    throw new InvalidOperationException($"DuckDB read failed for '{filePath}'. {ex.Message}", ex);
            }
            finally
            {
                conn?.Dispose();
            }
        }

        private static string ConvertToCellString(object? value)
        {
            if (value == null) return string.Empty;
            if (value is byte[] bytes)
                return Convert.ToHexString(bytes);

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private string[] GetColumnNames(string filePath)
        {
            if (!File.Exists(filePath))
                return Array.Empty<string>();

            var sql = BuildSelectSql(filePath);
            try
            {
                using var conn = new DuckDBConnection("DataSource=:memory:");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql + " LIMIT 0";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.FieldCount > 0)
                        return ReadNames(reader);
                }

                cmd.CommandText = sql + " LIMIT 1";
                using var reader2 = cmd.ExecuteReader();
                return reader2.FieldCount > 0 ? ReadNames(reader2) : Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _diagnostics.Add(new RowDiagnostic
                {
                    RowIndex = -1,
                    Code = "DUCKDB_SCHEMA_ERROR",
                    Message = ex.Message,
                    Severity = DiagnosticSeverity.Error
                });

                if (ParseMode == ParseMode.Strict)
                    throw new InvalidOperationException($"DuckDB schema probe failed for '{filePath}'. {ex.Message}", ex);

                return Array.Empty<string>();
            }
        }

        private static string[] ReadNames(IDataRecord reader)
        {
            var names = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                names[i] = reader.GetName(i);
            return names;
        }

      
      
        public virtual bool RewriteFile(string filePath, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows) => false;
    }
}
