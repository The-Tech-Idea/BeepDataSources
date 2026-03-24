using System;
using System.IO;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>Path escaping and DuckDB read_* SQL fragments shared by file readers.</summary>
    public static class DuckDbFileReaderSql
    {
        /// <summary>Normalizes to forward slashes and escapes single quotes for SQL string literals.</summary>
        public static string EscapePathLiteral(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            try
            {
                var full = Path.GetFullPath(path);
                var s = full.Replace('\\', '/');
                return s.Replace("'", "''", StringComparison.Ordinal);
            }
            catch
            {
                return path.Replace('\\', '/').Replace("'", "''", StringComparison.Ordinal);
            }
        }

        public static string ReadParquet(string path)
        {
            var p = EscapePathLiteral(path);
            return $"SELECT * FROM read_parquet('{p}')";
        }

        public static string ReadParquetScan(string pathOrGlob)
        {
            var p = EscapePathLiteral(pathOrGlob);
            return $"SELECT * FROM parquet_scan('{p}')";
        }

        public static string ReadCsvAuto(string path, bool header)
        {
            var p = EscapePathLiteral(path);
            var h = header ? "true" : "false";
            return $"SELECT * FROM read_csv_auto('{p}', header={h})";
        }

        public static string ReadCsvAutoTab(string path, bool header)
        {
            var p = EscapePathLiteral(path);
            var h = header ? "true" : "false";
            return $"SELECT * FROM read_csv_auto('{p}', delim='\\t', header={h})";
        }

        public static string ReadJsonAuto(string path)
        {
            var p = EscapePathLiteral(path);
            return $"SELECT * FROM read_json_auto('{p}')";
        }

        /// <summary>NDJSON / JSON lines — requires DuckDB read_ndjson.</summary>
        public static string ReadNdjson(string path)
        {
            var p = EscapePathLiteral(path);
            return $"SELECT * FROM read_ndjson('{p}', auto_detect=true)";
        }
    }
}
