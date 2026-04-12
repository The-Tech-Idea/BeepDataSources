using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>CSV via <c>read_csv_auto</c> (DuckDB type / delimiter detection).</summary>
    [FileReader(DataSourceType.CSV, "DuckDB CSV", "csv")]
    public sealed class DuckDbCsvFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.CSV;

        public override string GetDefaultExtension() => "csv";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadCsvAuto(filePath, HasHeader);
    }
}
