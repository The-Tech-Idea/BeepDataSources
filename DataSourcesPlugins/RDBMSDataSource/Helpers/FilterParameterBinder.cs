using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal static class FilterParameterBinder
    {
        public static void Bind(IDbCommand cmd, IEnumerable<AppFilter> filters, Func<string, string> nameSanitizer)
        {
            if (cmd == null || filters == null) return;

            foreach (var f in filters.Where(v => v!=null && !string.IsNullOrWhiteSpace(v.FieldName) && !string.IsNullOrWhiteSpace(v.Operator) && !string.IsNullOrWhiteSpace(v.FilterValue)))
            {
                string pname = nameSanitizer(f.FieldName);
                var p = cmd.CreateParameter();
                p.ParameterName = $"p_{pname}";
                if (f.valueType == "System.DateTime" && DateTime.TryParse(f.FilterValue, out var dt))
                {
                    p.DbType = DbType.DateTime;
                    p.Value = dt;
                }
                else
                {
                    p.Value = f.FilterValue;
                }
                cmd.Parameters.Add(p);

                if (f.Operator.Equals("between", StringComparison.OrdinalIgnoreCase))
                {
                    var p2 = cmd.CreateParameter();
                    p2.ParameterName = $"p_{pname}1";
                    if (f.valueType == "System.DateTime" && DateTime.TryParse(f.FilterValue1, out var dt2))
                    {
                        p2.DbType = DbType.DateTime;
                        p2.Value = dt2;
                    }
                    else
                    {
                        p2.Value = f.FilterValue1;
                    }
                    cmd.Parameters.Add(p2);
                }
            }
        }
    }
}
