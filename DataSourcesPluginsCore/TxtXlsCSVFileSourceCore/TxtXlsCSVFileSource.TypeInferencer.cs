using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Intelligent type inference engine for automatic field type detection
    /// </summary>
    internal class TypeInferencer
    {
        private IDMLogger Logger { get; set; }

        public TypeInferencer(IDMLogger logger = null)
        {
            Logger = logger;
        }

        /// <summary>
        /// Infers the best matching type for a value string
        /// Tries types in order: Decimal, Double, Int64, Int32, DateTime, Boolean, String
        /// </summary>
        public string InferType(string value, string previousType = "System.String")
        {
            if (string.IsNullOrWhiteSpace(value))
                return "System.String";

            string trimmed = value.Trim();

            // Try decimal first (most restrictive for numbers)
            if (decimal.TryParse(trimmed, out _))
                return "System.Decimal";

            // Try double
            if (double.TryParse(trimmed, out _))
                return "System.Double";

            // Try long
            if (long.TryParse(trimmed, out _))
                return "System.Int64";

            // Try int
            if (int.TryParse(trimmed, out _))
                return "System.Int32";

            // Try datetime
            if (DateTime.TryParse(trimmed, out _))
                return "System.DateTime";

            // Try boolean
            if (bool.TryParse(trimmed, out _))
                return "System.Boolean";

            // Default to string
            return "System.String";
        }

        /// <summary>
        /// Gets decimal precision and scale from a decimal value
        /// </summary>
        public (int Precision, int Scale) GetDecimalMetrics(decimal value)
        {
            if (value == 0)
                return (1, 0);

            // Get bits representation: [lo, mid, hi, flags]
            int[] bits = decimal.GetBits(value);
            int flags = bits[3];
            int scale = (flags >> 16) & 0x7F;

            // Calculate precision from significant digits
            decimal absValue = Math.Abs(value);
            int precision = Math.Max(1, (int)Math.Ceiling(Math.Log10((double)absValue + 1)) + scale);

            return (Math.Min(precision, 28), scale);
        }

        /// <summary>
        /// Converts a string value to the specified type
        /// Returns the converted value or original string if conversion fails
        /// </summary>
        public object ConvertToType(string value, string targetType)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                return targetType switch
                {
                    "System.Decimal" => decimal.Parse(value),
                    "System.Double" => double.Parse(value),
                    "System.Int64" => long.Parse(value),
                    "System.Int32" => int.Parse(value),
                    "System.Int16" => short.Parse(value),
                    "System.DateTime" => DateTime.Parse(value),
                    "System.Boolean" => bool.Parse(value),
                    "System.Byte" => byte.Parse(value),
                    "System.String" => value,
                    _ => value
                };
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error converting '{value}' to {targetType}: {ex.Message}");
                return value; // Return original string on failure
            }
        }

        /// <summary>
        /// Converts a Type to its TypeCode equivalent for type-safe operations
        /// </summary>
        public TypeCode GetTypeCode(Type type)
        {
            if (type == null)
                return TypeCode.String;

            return type.Name switch
            {
                nameof(String) => TypeCode.String,
                nameof(Decimal) => TypeCode.Decimal,
                nameof(DateTime) => TypeCode.DateTime,
                nameof(Char) => TypeCode.Char,
                nameof(Boolean) => TypeCode.Boolean,
                "DBNull" => TypeCode.DBNull,
                nameof(Byte) => TypeCode.Byte,
                nameof(Int16) => TypeCode.Int16,
                nameof(Double) => TypeCode.Double,
                nameof(Int32) => TypeCode.Int32,
                nameof(Int64) => TypeCode.Int64,
                nameof(Single) => TypeCode.Single,
                "Object" => TypeCode.String,
                _ => TypeCode.String
            };
        }

        /// <summary>
        /// Analyzes a DataTable column and infers the best type based on sample values
        /// Uses statistical approach: if >80% of non-null values parse as type, use that type
        /// </summary>
        public string InferColumnType(DataTable table, string columnName, int sampleRows = -1)
        {
            if (table?.Columns.Contains(columnName) != true)
                return "System.String";

            var typeCounts = new Dictionary<string, int>();
            int totalRows = 0;

            // Limit sample size for performance (default: all rows)
            int rowLimit = sampleRows < 0 ? table.Rows.Count : Math.Min(sampleRows, table.Rows.Count);

            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(rowLimit))
            {
                if (row[columnName] == DBNull.Value || row[columnName] == null)
                    continue;

                string value = row[columnName].ToString().Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                string inferredType = InferType(value);
                if (!typeCounts.ContainsKey(inferredType))
                    typeCounts[inferredType] = 0;
                typeCounts[inferredType]++;
                totalRows++;
            }

            if (totalRows == 0)
                return "System.String";

            // Find type with >80% confidence
            var dominantType = typeCounts
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            double confidence = (double)dominantType.Value / totalRows;
            return confidence >= 0.8 ? dominantType.Key : "System.String";
        }

        /// <summary>
        /// Gets the maximum string length in a DataTable column
        /// </summary>
        public int GetMaxStringLength(DataTable table, string columnName, int sampleRows = -1)
        {
            if (table?.Columns.Contains(columnName) != true)
                return 50;

            int maxLength = 0;
            int rowLimit = sampleRows < 0 ? table.Rows.Count : Math.Min(sampleRows, table.Rows.Count);

            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(rowLimit))
            {
                if (row[columnName] != DBNull.Value && row[columnName] != null)
                {
                    int length = row[columnName].ToString().Length;
                    if (length > maxLength)
                        maxLength = length;
                }
            }

            return Math.Max(50, maxLength); // Minimum 50, maximum 4000 for practical DB column size
        }
    }
}
