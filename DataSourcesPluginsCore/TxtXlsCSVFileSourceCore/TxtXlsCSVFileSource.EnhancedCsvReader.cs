using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Result of CSV analysis including detected properties
    /// </summary>
    internal class CsvAnalysisResult
    {
        public char DetectedDelimiter { get; set; }
        public Encoding DetectedEncoding { get; set; }
        public bool HasHeader { get; set; }
        public int ColumnCount { get; set; }
        public List<string> SampleRow { get; set; }
        public string AnalysisNotes { get; set; }
    }

    /// <summary>
    /// Enhanced CSV reader with intelligent features including:
    /// - Delimiter auto-detection
    /// - Encoding detection  
    /// - Header presence detection
    /// - Field trimming and BOM stripping
    /// - Column type inference
    /// - Malformed data error reporting
    /// </summary>
    internal class EnhancedCsvReader
    {
        private readonly IDMLogger _logger;
        private readonly TxtXlsCSVFileSourceHelper _helper;

        public EnhancedCsvReader(IDMLogger logger, TxtXlsCSVFileSourceHelper helper)
        {
            _logger = logger;
            _helper = helper;
        }

        /// <summary>
        /// Detects whether the CSV file has a header row by analyzing content patterns
        /// Heuristics: Headers typically have non-numeric values, consistent types, no nulls
        /// </summary>
        public bool DetectHeader(string filePath, char delimiter, int sampleLines = 5)
        {
            try
            {
                var encoding = DetectEncoding(filePath);
                var lines = new List<List<string>>();

                using (var sr = new StreamReader(filePath, encoding))
                {
                    string line;
                    int lineCount = 0;
                    while ((line = sr.ReadLine()) != null && lineCount < sampleLines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        line = StripBom(line);

                        var values = ParseCsvLine(line, delimiter, trimFields: true);
                        if (values.Any(v => !string.IsNullOrWhiteSpace(v)))
                            lines.Add(values);
                        lineCount++;
                    }
                }

                if (lines.Count < 2) return false; // Need at least 2 rows to compare

                var firstRow = lines[0];
                var secondRow = lines[1];

                // Check if column counts match (malformed CSVs with headers often have consistent column count)
                if (firstRow.Count != secondRow.Count)
                    return false;

                // Check heuristics for header detection
                int headerScore = 0;

                // Heuristic 1: Check if first row contains non-numeric values while second row has numbers
                int firstRowNumericCount = 0, secondRowNumericCount = 0;
                for (int i = 0; i < Math.Min(firstRow.Count, secondRow.Count); i++)
                {
                    if (IsNumericValue(firstRow[i])) firstRowNumericCount++;
                    if (IsNumericValue(secondRow[i])) secondRowNumericCount++;
                }

                if (firstRowNumericCount < secondRowNumericCount)
                    headerScore += 3; // Strong indicator

                // Heuristic 2: First row likely has shorter, text-like values (typical column names)
                int firstRowShortCount = 0, secondRowShortCount = 0;
                for (int i = 0; i < Math.Min(firstRow.Count, secondRow.Count); i++)
                {
                    if (firstRow[i].Length < 30 && !IsNumericValue(firstRow[i]))
                        firstRowShortCount++;
                    if (secondRow[i].Length < 30 && !IsNumericValue(secondRow[i]))
                        secondRowShortCount++;
                }

                if (firstRowShortCount > secondRowShortCount)
                    headerScore += 2;

                // Heuristic 3: Check for common header patterns (no duplicate values in first row)
                if (firstRow.Distinct().Count() == firstRow.Count)
                    headerScore += 2; // Headers usually unique

                // Heuristic 4: First row contains common column name patterns
                int headerPatternMatch = 0;
                var commonPatterns = new[] { "id", "name", "date", "time", "value", "amount", "count", "description", "title", "email", "phone" };
                foreach (var val in firstRow)
                {
                    if (commonPatterns.Any(p => val.ToLower().Contains(p)))
                        headerPatternMatch++;
                }
                if (headerPatternMatch > firstRow.Count / 2)
                    headerScore += 3;

                return headerScore >= 4; // Threshold for header detection
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error detecting header: {ex.Message}");
                return true; // Default to true for safety
            }
        }

        /// <summary>
        /// Analyzes CSV file and returns comprehensive detection results
        /// </summary>
        public CsvAnalysisResult AnalyzeCsv(string filePath)
        {
            try
            {
                var encoding = DetectEncoding(filePath);
                var delimiter = DetectDelimiter(filePath);
                var hasHeader = DetectHeader(filePath, delimiter);

                var lines = new List<List<string>>();
                using (var sr = new StreamReader(filePath, encoding))
                {
                    string line;
                    int lineCount = 0;
                    while ((line = sr.ReadLine()) != null && lineCount < 10)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        line = StripBom(line);
                        var values = ParseCsvLine(line, delimiter, trimFields: true);
                        lines.Add(values);
                        lineCount++;
                    }
                }

                var sampleRow = hasHeader && lines.Count > 1 ? lines[1] : (lines.Count > 0 ? lines[0] : new List<string>());
                var notes = $"Delimiter: '{delimiter}' | Encoding: {encoding.EncodingName} | Columns: {(lines.Count > 0 ? lines[0].Count : 0)}";

                return new CsvAnalysisResult
                {
                    DetectedDelimiter = delimiter,
                    DetectedEncoding = encoding,
                    HasHeader = hasHeader,
                    ColumnCount = lines.Count > 0 ? lines[0].Count : 0,
                    SampleRow = sampleRow,
                    AnalysisNotes = notes
                };
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error analyzing CSV: {ex.Message}");
                return new CsvAnalysisResult { DetectedDelimiter = ',', DetectedEncoding = Encoding.UTF8, HasHeader = true };
            }
        }

        /// <summary>
        /// Checks if a string value represents a numeric value
        /// </summary>
        private bool IsNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return int.TryParse(value, out _) || double.TryParse(value, out _) || decimal.TryParse(value, out _);
        }

        /// <summary>
        /// Detects the most likely delimiter by sampling the file
        /// </summary>
        public char DetectDelimiter(string filePath, int sampleLines = 5)
        {
            var delimiters = new[] { ',', ';', '\t', '|' };
            var delimiterScores = new Dictionary<char, int>();

            try
            {
                using (var sr = new StreamReader(filePath, Encoding.UTF8))
                {
                    int lineCount = 0;
                    string line;
                    while ((line = sr.ReadLine()) != null && lineCount < sampleLines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        foreach (var delim in delimiters)
                        {
                            int count = line.Count(c => c == delim);
                            if (count > 0)
                            {
                                if (!delimiterScores.ContainsKey(delim))
                                    delimiterScores[delim] = 0;
                                delimiterScores[delim] += count;
                            }
                        }
                        lineCount++;
                    }
                }

                // Return most common delimiter, default to comma
                return delimiterScores.Any() ? delimiterScores.OrderByDescending(x => x.Value).First().Key : ',';
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error detecting delimiter: {ex.Message}");
                return ','; // Default fallback
            }
        }

        /// <summary>
        /// Detects file encoding by analyzing byte order marks and content
        /// </summary>
        public Encoding DetectEncoding(string filePath)
        {
            try
            {
                byte[] buffer = new byte[4];
                using (var file = File.OpenRead(filePath))
                {
                    file.Read(buffer, 0, 4);
                }

                // Check for BOM (Byte Order Mark)
                if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                    return Encoding.UTF8; // UTF-8 with BOM
                if (buffer[0] == 0xff && buffer[1] == 0xfe && buffer[2] == 0 && buffer[3] == 0)
                    return Encoding.UTF32; // UTF-32 LE
                if (buffer[0] == 0xff && buffer[1] == 0xfe)
                    return Encoding.Unicode; // UTF-16 LE
                if (buffer[0] == 0xfe && buffer[1] == 0xff)
                    return Encoding.BigEndianUnicode; // UTF-16 BE

                // Default to UTF8
                return Encoding.UTF8;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error detecting encoding: {ex.Message}");
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Strips BOM from string if present
        /// </summary>
        public string StripBom(string line)
        {
            if (line == null) return line;
            if (line.Length > 0 && line[0] == '\ufeff')
                return line.Substring(1);
            return line;
        }

        /// <summary>
        /// Parses CSV line with support for quoted fields and escaping
        /// </summary>
        public List<string> ParseCsvLine(string line, char delimiter, bool trimFields = true)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        // Toggle quote state
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == delimiter && !insideQuotes)
                {
                    // Field separator
                    string field = currentField.ToString();
                    if (trimFields) field = field.Trim();
                    result.Add(field);
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add the last field
            string lastField = currentField.ToString();
            if (trimFields) lastField = lastField.Trim();
            result.Add(lastField);

            return result;
        }

        /// <summary>
        /// Infers column types from data sample
        /// </summary>
        public List<string> InferColumnTypes(List<List<string>> sampleRows, int minConfidence = 80)
        {
            if (sampleRows == null || sampleRows.Count == 0)
                return null;

            int colCount = sampleRows[0].Count;
            var columnTypes = new List<string>();

            for (int colIdx = 0; colIdx < colCount; colIdx++)
            {
                var columnValues = sampleRows.Select(r => colIdx < r.Count ? r[colIdx] : null).ToList();
                string inferredType = InferColumnType(columnValues, minConfidence);
                columnTypes.Add(inferredType);
            }

            return columnTypes;
        }

        /// <summary>
        /// Infers a single column's type from sample values
        /// </summary>
        private string InferColumnType(List<string> values, int minConfidence = 80)
        {
            if (values == null || values.Count == 0)
                return "System.String";

            int intCount = 0, doubleCount = 0, dateCount = 0, boolCount = 0, stringCount = 0;
            int validCount = 0;

            foreach (var val in values)
            {
                if (string.IsNullOrWhiteSpace(val)) continue;

                validCount++;

                if (bool.TryParse(val, out _)) boolCount++;
                else if (int.TryParse(val, out _)) intCount++;
                else if (double.TryParse(val, out _)) doubleCount++;
                else if (DateTime.TryParse(val, out _)) dateCount++;
                else stringCount++;
            }

            if (validCount == 0) return "System.String";

            // Return type with highest count if above confidence threshold
            if (boolCount > 0 && (boolCount * 100 / validCount) >= minConfidence)
                return "System.Boolean";
            if (dateCount > 0 && (dateCount * 100 / validCount) >= minConfidence)
                return "System.DateTime";
            if (doubleCount > 0 && (doubleCount * 100 / validCount) >= minConfidence)
                return "System.Double";
            if (intCount > 0 && (intCount * 100 / validCount) >= minConfidence)
                return "System.Int32";

            return "System.String";
        }

        /// <summary>
        /// Validates column count consistency in CSV data
        /// </summary>
        public List<string> ValidateColumns(List<List<string>> rows, int expectedColumnCount)
        {
            var issues = new List<string>();
            
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Count != expectedColumnCount)
                {
                    issues.Add($"Row {i + 1}: Expected {expectedColumnCount} columns, got {rows[i].Count}");
                }
            }

            return issues;
        }

        /// <summary>
        /// Reads CSV with intelligent features (auto-detection, trimming, type inference)
        /// </summary>
        public DataTable ReadCsvSmart(string filePath, bool hasHeader = true, int startRow = 0, int endRow = -1)
        {
            var dt = new DataTable();

            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                // Auto-detect encoding and delimiter
                var encoding = DetectEncoding(filePath);
                char delimiter = DetectDelimiter(filePath);

                var dataRows = new List<List<string>>();

                using (var sr = new StreamReader(filePath, encoding))
                {
                    string line;
                    int lineNumber = 0;
                    bool headerProcessed = false;

                    while ((line = sr.ReadLine()) != null)
                    {
                        // Strip BOM from first line
                        if (lineNumber == 0)
                            line = StripBom(line);

                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            lineNumber++;
                            continue;
                        }

                        // Skip comment lines (starting with #)
                        if (line.StartsWith("#"))
                        {
                            lineNumber++;
                            continue;
                        }

                        // Apply row filtering
                        if (startRow > 0 && lineNumber < startRow)
                        {
                            lineNumber++;
                            continue;
                        }

                        if (endRow > 0 && lineNumber > endRow)
                            break;

                        var values = ParseCsvLine(line, delimiter, trimFields: true);

                        if (!headerProcessed && hasHeader)
                        {
                            // Create columns from header
                            foreach (var val in values)
                                dt.Columns.Add(val ?? $"Column{dt.Columns.Count}", typeof(object));
                            headerProcessed = true;
                        }
                        else
                        {
                            // Collect data rows for type inference
                            dataRows.Add(values);
                            if (!headerProcessed && !hasHeader)
                            {
                                // Create auto-numbered columns
                                for (int i = 0; i < values.Count; i++)
                                    dt.Columns.Add($"Column{i}", typeof(object));
                                headerProcessed = true;
                            }
                        }

                        lineNumber++;
                    }
                }

                // Infer types from sample
                var inferredTypes = InferColumnTypes(dataRows.Take(Math.Min(100, dataRows.Count)).ToList());
                if (inferredTypes != null)
                {
                    for (int i = 0; i < Math.Min(inferredTypes.Count, dt.Columns.Count); i++)
                    {
                        try
                        {
                            dt.Columns[i].DataType = Type.GetType(inferredTypes[i]) ?? typeof(string);
                        }
                        catch { }
                    }
                }

                // Add data rows
                foreach (var row in dataRows)
                {
                    var dr = dt.NewRow();
                    for (int i = 0; i < Math.Min(row.Count, dt.Columns.Count); i++)
                    {
                        dr[i] = row[i] ?? "";
                    }
                    dt.Rows.Add(dr);
                }

                // Validate columns
                var validationIssues = ValidateColumns(dataRows, dt.Columns.Count);
                if (validationIssues.Any())
                {
                    _logger?.WriteLog($"CSV validation warnings: {string.Join("; ", validationIssues.Take(5))}");
                }

                return dt;
            }
            catch (Exception ex)
            {
                _logger?.WriteLog($"Error reading CSV smartly: {ex.Message}");
                throw;
            }
        }
    }
}
