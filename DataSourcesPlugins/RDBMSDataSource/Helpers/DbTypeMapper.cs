using System;
using System.Collections.Generic;
using System.Data;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal static class DbTypeMapper
    {
        private static readonly Dictionary<string, DbType> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["System.String"] = DbType.String,
            ["System.Int32"] = DbType.Int32,
            ["System.Int64"] = DbType.Int64,
            ["System.Int16"] = DbType.Int16,
            ["System.Byte"] = DbType.Byte,
            ["System.Boolean"] = DbType.Boolean,
            ["System.DateTime"] = DbType.DateTime,
            ["System.Decimal"] = DbType.Decimal,
            ["System.Double"] = DbType.Double,
            ["System.Single"] = DbType.Single,
            ["System.Guid"] = DbType.Guid,
            ["System.TimeSpan"] = DbType.Time,
            ["System.Byte[]"] = DbType.Binary,
            ["System.UInt16"] = DbType.UInt16,
            ["System.UInt32"] = DbType.UInt32,
            ["System.UInt64"] = DbType.UInt64,
            ["System.SByte"] = DbType.SByte,
            ["System.Object"] = DbType.Object,
            ["System.Char"] = DbType.String
        };

        public static DbType ToDbType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return DbType.String;
            return Map.TryGetValue(typeName, out var dbType) ? dbType : DbType.String;
        }
    }
}
