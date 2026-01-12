using System;
using System.Collections.Generic;
using System.Data;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal static class DataStreamer
    {
        public static IEnumerable<Dictionary<string, object>> Stream(IDataReader reader)
        {
            if (reader == null) yield break;
            using (reader)
            {
                int fieldCount = reader.FieldCount;
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>(fieldCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < fieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    yield return row;
                }
            }
        }
    }
}
