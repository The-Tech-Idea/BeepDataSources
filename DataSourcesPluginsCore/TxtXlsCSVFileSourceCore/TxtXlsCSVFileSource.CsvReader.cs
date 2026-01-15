using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Partial class for CSV read operations using StreamReader
    /// </summary>
    public partial class TxtXlsCSVFileSource : IDataSource
    {
        /// <summary>
        /// Reads a CSV file into a DataTable with intelligent features:
        /// - Auto-detects delimiter and encoding
        /// - Auto-detects header presence
        /// - Trims fields and strips BOM
        /// - Infers column types
        /// - Validates column consistency
        /// </summary>
        private DataTable ReadCsvFileHelper(string filePath, char delimiter, bool hasHeader, int startRow = 0, int endRow = -1)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                // Use enhanced reader for smart CSV processing
                var enhancedReader = new EnhancedCsvReader(Logger, _helper);
                
                // Auto-detect delimiter if not explicitly provided
                char effectiveDelimiter = (delimiter == '\0' || delimiter == ',' && !filePath.Contains(',')) 
                    ? enhancedReader.DetectDelimiter(filePath) 
                    : delimiter;

                // Auto-detect header presence if not explicitly specified
                bool effectiveHasHeader = hasHeader;
                // Use smart detection if parameter indicates default/uncertain
                if (!hasHeader)
                {
                    effectiveHasHeader = enhancedReader.DetectHeader(filePath, effectiveDelimiter);
                }

                // Use smart reader with auto-detection
                DataTable dt = enhancedReader.ReadCsvSmart(filePath, effectiveHasHeader, startRow, endRow);

                return dt;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading CSV file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parses a CSV line handling quoted fields, escaping, and proper delimiter splitting
        /// Note: For enhanced parsing with trimming, use EnhancedCsvReader.ParseCsvLine instead
        /// </summary>
        private List<string> ParseCsvLine(string line, char delimiter)
        {
            var enhancedReader = new EnhancedCsvReader(Logger, _helper);
            return enhancedReader.ParseCsvLine(line, delimiter, trimFields: false);
        }

        /// <summary>
        /// Gets all CSV sheets (returns single sheet with file name) with intelligent delimiter and header detection
        /// </summary>
        private void GetSheetsCsv()
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(FilePath, FileName);
                }

                if (!File.Exists(CombineFilePath))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"File not found: {CombineFilePath}";
                    return;
                }

                Entities.Clear();
                EntitiesNames.Clear();

                // For CSV, we treat the file as a single sheet
                string entityName = Path.GetFileNameWithoutExtension(FileName);
                EntitiesNames.Add(entityName);

                try
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    // Use enhanced reader for smart analysis
                    var enhancedReader = new EnhancedCsvReader(Logger, _helper);
                    var analysis = enhancedReader.AnalyzeCsv(CombineFilePath);

                    Logger?.WriteLog($"CSV Analysis: {analysis.AnalysisNotes}");

                    char delimiter = Delimiter != '\0' ? Delimiter : (Dataconnection?.ConnectionProp?.Delimiter ?? analysis.DetectedDelimiter);
                    bool hasHeader = analysis.HasHeader; // Use smart detection result

                    using (var sr = new StreamReader(CombineFilePath, analysis.DetectedEncoding))
                    {
                        string firstLine = sr.ReadLine();
                        if (!string.IsNullOrEmpty(firstLine))
                        {
                            firstLine = enhancedReader.StripBom(firstLine);
                            var fieldNames = enhancedReader.ParseCsvLine(firstLine, delimiter, trimFields: true);

                            // If no header, skip first line (already read)
                            string dataLine = hasHeader ? sr.ReadLine() : firstLine;

                            var entity = new EntityStructure
                            {
                                EntityName = entityName,
                                DatasourceEntityName = entityName,
                                OriginalEntityName = entityName,
                                StartRow = hasHeader ? 1 : 0,
                                EndRow = CountLinesInFile(CombineFilePath),
                                Fields = new List<EntityField>()
                            };

                            List<string> headerNames;
                            if (hasHeader)
                            {
                                headerNames = fieldNames;
                            }
                            else
                            {
                                // Generate column names if no header
                                headerNames = Enumerable.Range(0, fieldNames.Count)
                                    .Select(i => $"Column{i}")
                                    .ToList();
                            }

                            int colIdx = 0;
                            foreach (var fieldName in headerNames)
                            {
                                var field = new EntityField
                                {
                                    fieldname = fieldName?.Trim() ?? $"Column{colIdx}",
                                    Originalfieldname = fieldName?.Trim() ?? $"Column{colIdx}",
                                    fieldtype = "System.String",
                                    IsKey = colIdx == 0,
                                    AllowDBNull = true
                                };
                                entity.Fields.Add(field);
                                colIdx++;
                            }

                            Entities.Add(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"Error reading CSV headers: {ex.Message}");
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Ex = ex;
                }

                IsFileRead = true;
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading CSV sheets: {ex.Message}");
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Ex = ex;
            }
        }

        /// <summary>
        /// Counts the number of lines in a CSV file
        /// </summary>
        private int CountLinesInFile(string filePath)
        {
            int lineCount = 0;
            try
            {
                using (var sr = new StreamReader(filePath, Encoding.UTF8))
                {
                    while (sr.ReadLine() != null)
                    {
                        lineCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error counting lines in file: {ex.Message}");
            }
            return lineCount;
        }

        /// <summary>
        /// Reads a CSV file range into a DataTable
        /// </summary>
        private DataTable ReadDataTableCsv(string sheetName, bool hasHeader, int startRow, int endRow)
        {
            DataTable dt = new DataTable(sheetName);

            try
            {
                if (string.IsNullOrEmpty(CombineFilePath))
                {
                    CombineFilePath = Path.Combine(FilePath, FileName);
                }

                if (!File.Exists(CombineFilePath))
                {
                    throw new FileNotFoundException($"File not found: {CombineFilePath}");
                }

                char delim = Delimiter != '\0' ? Delimiter : (Dataconnection?.ConnectionProp?.Delimiter ?? ',');
                dt = ReadCsvFileHelper(CombineFilePath, delim, hasHeader, startRow, endRow);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error reading CSV data table: {ex.Message}");
                throw;
            }

            return dt;
        }

        #region "Async CSV Operations"

        /// <summary>
        /// Analyzes a CSV file to detect its properties (delimiter, encoding, header presence)
        /// Useful for understanding file structure before reading
        /// </summary>
        internal CsvAnalysisResult AnalyzeCsvFile(string filePath)
        {
            try
            {
                var enhancedReader = new EnhancedCsvReader(Logger, _helper);
                return enhancedReader.AnalyzeCsv(filePath);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error analyzing CSV file: {ex.Message}");
                return new CsvAnalysisResult 
                { 
                    DetectedDelimiter = ',', 
                    DetectedEncoding = Encoding.UTF8, 
                    HasHeader = true,
                    AnalysisNotes = "Analysis failed, using defaults"
                };
            }
        }

        /// <summary>
        /// Asynchronously analyzes a CSV file to detect its properties
        /// </summary>
        internal async Task<CsvAnalysisResult> AnalyzeCsvFileAsync(string filePath)
        {
            return await Task.Run(() => AnalyzeCsvFile(filePath));
        }

        /// <summary>
        /// Asynchronously reads a CSV file into a DataTable with proper delimiter and encoding handling
        /// </summary>
        private async Task<DataTable> ReadCsvFileHelperAsync(string filePath, char delimiter, bool hasHeader, int startRow = 0, int endRow = -1)
        {
            return await Task.Run(() => ReadCsvFileHelper(filePath, delimiter, hasHeader, startRow, endRow));
        }

        /// <summary>
        /// Asynchronously gets all CSV sheets (returns single sheet with file name)
        /// </summary>
        private async Task GetSheetsCsvAsync()
        {
            await Task.Run(() => GetSheetsCsv());
        }

        /// <summary>
        /// Asynchronously reads a CSV file range into a DataTable
        /// </summary>
        private async Task<DataTable> ReadDataTableCsvAsync(string sheetName, bool hasHeader, int startRow, int endRow)
        {
            return await Task.Run(() => ReadDataTableCsv(sheetName, hasHeader, startRow, endRow));
        }

        /// <summary>
        /// Asynchronously counts the number of lines in a CSV file
        /// </summary>
        private async Task<int> CountLinesInFileAsync(string filePath)
        {
            return await Task.Run(() => CountLinesInFile(filePath));
        }

        #endregion
    }
}
