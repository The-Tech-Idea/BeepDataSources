using TheTechIdea.Beep.FileManager;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>
    /// Registers DuckDB-backed <see cref="TheTechIdea.Beep.FileManager.Readers.IFileFormatReader"/> instances with <see cref="FileReaderFactory"/>.
    /// </summary>
    public static class DuckDbFileReaderRegistration
    {
        /// <summary>
        /// Registers readers that do <b>not</b> overlap built-in defaults: Parquet + NDJSON (as <see cref="TheTechIdea.Beep.Addin.DataSourceType.FlatFile"/>).
        /// Safe to call after <see cref="FileReaderFactory.RegisterDefaults"/>.
        /// </summary>
        public static void RegisterNonConflicting()
        {
            FileReaderFactory.Register(new DuckDbParquetFileReader());
            FileReaderFactory.Register(new DuckDbNdjsonFileReader());
        }

        /// <summary>
        /// Registers all P0 DuckDB readers. Replaces default readers for CSV, TSV, and Json if those were already registered.
        /// Call <see cref="FileReaderFactory.RegisterDefaults"/> first, then this method, if you intend to override.
        /// </summary>
        public static void RegisterAllPrimaryFormats()
        {
            RegisterNonConflicting();
            FileReaderFactory.Register(new DuckDbCsvFileReader());
            FileReaderFactory.Register(new DuckDbTsvFileReader());
            FileReaderFactory.Register(new DuckDbJsonFileReader());
        }
    }
}
