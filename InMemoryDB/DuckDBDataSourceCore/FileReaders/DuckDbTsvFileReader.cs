using TheTechIdea.Beep.Addin;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>TSV via <c>read_csv_auto</c> with tab delimiter.</summary>
    public sealed class DuckDbTsvFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.TSV;

        public override string GetDefaultExtension() => "tsv";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadCsvAutoTab(filePath, HasHeader);
    }
}
