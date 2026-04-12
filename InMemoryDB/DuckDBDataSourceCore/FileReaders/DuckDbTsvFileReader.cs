using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>TSV via <c>read_csv_auto</c> with tab delimiter.</summary>
    [FileReader(DataSourceType.TSV, "DuckDB TSV", "tsv")]
    public sealed class DuckDbTsvFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.TSV;

        public override string GetDefaultExtension() => "tsv";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadCsvAutoTab(filePath, HasHeader);
    }
}
