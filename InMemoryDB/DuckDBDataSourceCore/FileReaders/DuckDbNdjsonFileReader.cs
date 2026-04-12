using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>
    /// Newline-delimited JSON via <c>read_ndjson</c>.
    /// Uses <see cref="DataSourceType.FlatFile"/> so it does not replace the default JSON reader; set connection type to FlatFile for <c>.jsonl</c> / <c>.ndjson</c> when using DuckDB.
    /// </summary>
    [FileReader(DataSourceType.FlatFile, "DuckDB NDJSON", "jsonl")]
    public sealed class DuckDbNdjsonFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.FlatFile;

        public override string GetDefaultExtension() => "jsonl";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadNdjson(filePath);
    }
}
