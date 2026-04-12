using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.FileManager.Attributes;
using TheTechIdea.Beep.Utilities;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>JSON (auto-detect) via <c>read_json_auto</c>.</summary>
    [FileReader(DataSourceType.Json, "DuckDB JSON", "json")]
    public sealed class DuckDbJsonFileReader : DuckDbFileReaderBase
    {
        public override DataSourceType SupportedType => DataSourceType.Json;

        public override string GetDefaultExtension() => "json";

        protected override string BuildSelectSql(string filePath)
            => DuckDbFileReaderSql.ReadJsonAuto(filePath);
    }
}
