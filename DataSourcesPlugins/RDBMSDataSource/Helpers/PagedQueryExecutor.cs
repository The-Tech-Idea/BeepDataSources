using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal class PagedQueryExecutor
    {
        private readonly Func<IDbCommand> _commandFactory;
        private readonly IDMEEditor _editor;
        private readonly IErrorsInfo _errorObject;

        public PagedQueryExecutor(Func<IDbCommand> commandFactory, IDMEEditor editor, IErrorsInfo errorObject)
        {
            _commandFactory = commandFactory;
            _editor = editor;
            _errorObject = errorObject;
        }

        public (List<object> rows, int total) Execute(string baseQuery, IEnumerable<AppFilter> filters, DataSourceType dbType, int pageNumber, int pageSize, Func<string,string> sanitizeParam)
        {
            var rows = new List<object>();
            int total = 0;
            try
            {
                string filtered = SqlQueryBuilder.BuildFilteredQuery(baseQuery, filters, null, "@", sanitizeParam);
                string countSql = BuildCountQuery(baseQuery, filtered);
                string pagedSql = PaginationHelper.ApplyPaging(filtered, dbType, pageNumber, pageSize);

                using (var countCmd = _commandFactory())
                {
                    if (countCmd == null) return (rows, 0);
                    countCmd.CommandText = countSql;
                    FilterParameterBinder.Bind(countCmd, filters, sanitizeParam);
                    object scalar = countCmd.ExecuteScalar();
                    if (scalar != null && int.TryParse(Convert.ToString(scalar), out var c)) total = c; else total = Convert.ToInt32(scalar);
                }
                using (var dataCmd = _commandFactory())
                {
                    if (dataCmd == null) return (rows, total);
                    dataCmd.CommandText = pagedSql;
                    FilterParameterBinder.Bind(dataCmd, filters, sanitizeParam);
                    using var reader = dataCmd.ExecuteReader(CommandBehavior.SequentialAccess);
                    int fieldCount = reader.FieldCount;
                    while (reader.Read())
                    {
                        var dict = new Dictionary<string, object>(fieldCount, StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < fieldCount; i++)
                        {
                            dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        rows.Add(dict);
                    }
                }
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("Fail", $"Paged execution failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                _errorObject.Flag = Errors.Failed;
                _errorObject.Message = ex.Message;
            }
            return (rows, total);
        }

        private string BuildCountQuery(string originalBase, string filtered)
        {
            var match = Regex.Match(originalBase, @"^\s*SELECT\s+\*\s+FROM\s+([a-zA-Z0-9_\.]+)", RegexOptions.IgnoreCase);
            if (match.Success && !Regex.IsMatch(originalBase, @"\bjoin\b", RegexOptions.IgnoreCase))
            {
                string table = match.Groups[1].Value;
                string whereClause = ExtractWhere(filtered);
                return $"SELECT COUNT(*) FROM {table} {whereClause}".Trim();
            }
            string filteredNoOrder = Regex.Replace(filtered, @"order\s+by[\s\S]*$", string.Empty, RegexOptions.IgnoreCase).Trim();
            return $"SELECT COUNT(*) FROM ( {filteredNoOrder} ) q";
        }

        private string ExtractWhere(string sql)
        {
            var m = Regex.Match(sql, @"\bwhere\b[\s\S]*", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string chunk = m.Value;
                var stop = Regex.Match(chunk, @"\b(group\s+by|having|order\s+by)\b", RegexOptions.IgnoreCase);
                if (stop.Success)
                {
                    return chunk.Substring(0, stop.Index).Trim();
                }
                return chunk.Trim();
            }
            return string.Empty;
        }
    }
}
