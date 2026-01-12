using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase.Helpers;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        /// <summary>
        /// Converts a .NET type string to DbType using the DbTypeMapper helper.
        /// This eliminates 70+ lines of duplicate switch logic by using the centralized helper.
        /// </summary>
        private DbType GetDbType(string fieldType)
        {
            return DbTypeMapper.ToDbType(fieldType);
        }



        /// <summary>
        /// Converts a value to the specified database type using pattern matching for better performance and readability.
        /// Falls back to Convert.ChangeType for unsupported types.
        /// </summary>
        private object ConvertToDbTypeValue(object value, string fieldType)
        {
            if (value == null || value == DBNull.Value)
                return DBNull.Value;

            // Pattern matching approach (C# 7+) - more concise and performant
            return fieldType switch
            {
                "System.DateTime" when value is DateTime dt => dt,
                "System.DateTime" when DateTime.TryParse(value.ToString(), out var dt) => dt,
                
                "System.Int32" when value is int i => i,
                "System.Int32" when int.TryParse(value.ToString(), out var i) => i,
                
                "System.Int64" when value is long l => l,
                "System.Int64" when long.TryParse(value.ToString(), out var l) => l,
                
                "System.Decimal" when value is decimal dec => dec,
                "System.Decimal" when decimal.TryParse(value.ToString(), out var dec) => dec,
                
                "System.Boolean" when value is bool b => b,
                "System.Boolean" when bool.TryParse(value.ToString(), out var b) => b,
                
                "System.Double" when value is double d => d,
                "System.Double" when double.TryParse(value.ToString(), out var d) => d,
                
                "System.Single" when value is float f => f,
                "System.Single" when float.TryParse(value.ToString(), out var f) => f,
                
                "System.Byte" when value is byte by => by,
                "System.Byte" when byte.TryParse(value.ToString(), out var by) => by,
                
                "System.SByte" when value is sbyte sb => sb,
                "System.SByte" when sbyte.TryParse(value.ToString(), out var sb) => sb,
                
                "System.Int16" when value is short sh => sh,
                "System.Int16" when short.TryParse(value.ToString(), out var sh) => sh,
                
                "System.UInt16" when value is ushort us => us,
                "System.UInt16" when ushort.TryParse(value.ToString(), out var us) => us,
                
                "System.UInt32" when value is uint ui => ui,
                "System.UInt32" when uint.TryParse(value.ToString(), out var ui) => ui,
                
                "System.UInt64" when value is ulong ul => ul,
                "System.UInt64" when ulong.TryParse(value.ToString(), out var ul) => ul,
                
                "System.Char" when value is char c => c,
                "System.Char" when char.TryParse(value.ToString(), out var c) => c,
                
                "System.Guid" when value is Guid g => g,
                "System.Guid" when Guid.TryParse(value.ToString(), out var g) => g,
                
                "System.TimeSpan" when value is TimeSpan ts => ts,
                "System.TimeSpan" when TimeSpan.TryParse(value.ToString(), out var ts) => ts,
                
                "System.String" => value.ToString(),
                
                _ => value // Return as-is if no conversion needed
            };
        }


        private DbType TypeToDbType(Type type)
        {
            // Add more mappings as necessary
            if (type == typeof(string)) return DbType.String;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(DateTime)) return DbType.DateTime;
            if (type == typeof(bool)) return DbType.Boolean;
            // Add other type mappings as necessary

            return DbType.String; // Default type
        }
    }
}
