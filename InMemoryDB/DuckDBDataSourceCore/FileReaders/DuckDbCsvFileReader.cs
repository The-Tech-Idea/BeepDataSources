using TheTechIdea.Beep.Addin;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>CSV via <c>read_csv_auto</c> (DuckDB type / delimiter detection).</summary>
    public sealed class DuckDbCsvFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.CSV;

        public override string GetDefaultExtension() => "csv";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadCsvAuto(filePath, HasHeader);
    }
}
