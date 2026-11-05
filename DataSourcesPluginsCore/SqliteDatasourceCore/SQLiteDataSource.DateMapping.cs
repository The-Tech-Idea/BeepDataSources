using System.Data;
using System.Globalization;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// SQLite-specific DateTime parameter handling.
    ///
    /// The shared RDBMS mapping has to assume a provider with a native DateTime type. SQLite does not
    /// have one: the provider would parse a DateTime parameter and drop its offset, so a local midnight
    /// came back as the previous day after a UTC round trip. This override keeps the exact wall clock and
    /// offset as ISO-8601 text, and declares the parameter as text so the provider does not reformat it.
    /// The shared read paths already use <c>DateTimeStyles.RoundtripKind</c>, which restores the local
    /// kind from the offset; UTC values keep their <c>Z</c> marker and unmarked values stay unmarked.
    /// </summary>
    public partial class SQLiteDataSource
    {
        protected override DbType GetDbType(string fieldType)
            => string.Equals(fieldType, "System.DateTime", StringComparison.OrdinalIgnoreCase)
                ? DbType.String
                : base.GetDbType(fieldType);

        protected override object ConvertToDbTypeValue(object value, string fieldType)
        {
            if (string.Equals(fieldType, "System.DateTime", StringComparison.OrdinalIgnoreCase))
            {
                if (value is DateTime dateTime)
                {
                    return FormatDateTime(dateTime);
                }

                if (value is string text &&
                    DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    return FormatDateTime(parsed);
                }
            }

            return base.ConvertToDbTypeValue(value, fieldType);
        }

        private static string FormatDateTime(DateTime value) => value.Kind switch
        {
            DateTimeKind.Local => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK", CultureInfo.InvariantCulture),
            DateTimeKind.Utc => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
            _ => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture)
        };
    }
}
