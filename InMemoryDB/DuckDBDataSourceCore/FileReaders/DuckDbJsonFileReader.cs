using TheTechIdea.Beep.Addin;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>JSON (auto-detect) via <c>read_json_auto</c>.</summary>
    public sealed class DuckDbJsonFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.Json;

        public override string GetDefaultExtension() => "json";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadJsonAuto(filePath);
    }
}
