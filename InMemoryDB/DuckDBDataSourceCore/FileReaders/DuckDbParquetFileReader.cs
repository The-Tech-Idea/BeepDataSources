using TheTechIdea.Beep.Addin;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>Parquet via <c>read_parquet</c> / <c>parquet_scan</c> (glob).</summary>
    public sealed class DuckDbParquetFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.Parquet;

        public override string GetDefaultExtension() => "parquet";

        protected override string BuildSelectSql(string filePath)
        {
            if (filePath.IndexOfAny(new[] { '*', '?' }) >= 0)
                return DuckDbFileReaderSql.ReadParquetScan(filePath);
            return DuckDbFileReaderSql.ReadParquet(filePath);
        }
    }
}
