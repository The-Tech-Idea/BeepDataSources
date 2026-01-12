using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.DataBase.Helpers; // Add this line

namespace TheTechIdea.Beep.DataBase.Helpers
{
    /// <summary>
    /// Builds SQL queries with dynamic filter insertion without binding parameter values.
    /// Responsible only for textual transformation.
    /// </summary>
    internal static class SqlQueryBuilder
    {
        private static readonly string[] ClauseOrder = { "select", "from", "where", "group by", "having", "order by" };

        public static string BuildFilteredQuery(string baseSql, IEnumerable<AppFilter> filters, string schemaName, string parameterPrefix, Func<string,string> fieldSanitizer)
        {
            if (string.IsNullOrWhiteSpace(baseSql)) return baseSql;
            string work = baseSql.Trim();
            bool hasSelect = work.TrimStart().StartsWith("select", StringComparison.OrdinalIgnoreCase);
            if (!hasSelect)
            {
                // Assume table name shortcut
                work = $"SELECT * FROM {work}";
            }
            // Normalize spacing to simplify parsing
            string lower = work.ToLowerInvariant();
            // Early exit if no filters
            var validFilters = (filters ?? Enumerable.Empty<AppFilter>())
                .Where(IsValidFilter)
                .ToList();
            if (validFilters.Count == 0) return work; // nothing to add

            // If existing WHERE append AND chain, else add WHERE
            StringBuilder sb = new StringBuilder(work);
            if (Regex.IsMatch(lower, @"\bwhere\b", RegexOptions.IgnoreCase))
            {
                sb.Append('\n');
                sb.Append(" AND ");
            }
            else
            {
                sb.Append('\n');
                sb.Append(" WHERE ");
            }

            bool first = true;
            foreach (var f in validFilters)
            {
                if (!first)
                {
                    sb.Append(" AND ");
                }
                string safeField = f.FieldName; // assume already valid identifier from metadata
                string pname = fieldSanitizer(f.FieldName);
                if (f.Operator.Equals("between", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append($"{safeField} BETWEEN {parameterPrefix}p_{pname} AND {parameterPrefix}p_{pname}1");
                }
                else
                {
                    sb.Append($"{safeField} {f.Operator} {parameterPrefix}p_{pname}");
                }
                first = false;
            }
            return sb.ToString();
        }

        private static bool IsValidFilter(AppFilter f) =>
            f != null && !string.IsNullOrWhiteSpace(f.FieldName) &&
            !string.IsNullOrWhiteSpace(f.Operator) &&
            !string.IsNullOrWhiteSpace(f.FilterValue);
    }
}
