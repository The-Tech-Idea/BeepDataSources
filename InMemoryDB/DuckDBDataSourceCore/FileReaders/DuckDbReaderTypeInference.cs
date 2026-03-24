using System;
using System.Globalization;

namespace DuckDBDataSourceCore.FileReaders
{
    /// <summary>
    /// Lightweight type widening for <see cref="TheTechIdea.Beep.FileManager.Readers.IFileFormatReader.InferFieldType"/>.
    /// Mirrors <c>TypeInferenceHelper</c> in DataManagementEngine (internal) so DuckDB readers work cross-assembly.
    /// </summary>
    internal static class DuckDbReaderTypeInference
    {
        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy",
            "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ",
            "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss"
        };

        private static readonly string StringFullName = typeof(string).FullName!;

        public static string Widen(string? current, string? rawValue)
        {
            if (current == StringFullName)
                return current!;

            string candidate = Classify(rawValue);
            if (current == null)
                return candidate;

            return Wider(current, candidate);
        }

        private static string Classify(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return StringFullName;

            if (bool.TryParse(raw, out _))
                return typeof(bool).FullName!;

            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return typeof(long).FullName!;

            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                return typeof(decimal).FullName!;

            if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
                return typeof(DateTime).FullName!;

            return StringFullName;
        }

        private static string Wider(string a, string b)
        {
            int rank(string t) => t switch
            {
                var x when x == typeof(bool).FullName => 0,
                var x when x == typeof(long).FullName => 1,
                var x when x == typeof(decimal).FullName => 2,
                var x when x == typeof(DateTime).FullName => 3,
                _ => 4
            };
            return rank(a) >= rank(b) ? a : b;
        }
    }
}
