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



        private object ConvertToDbTypeValue(object value, string fieldType)
        {
            switch (fieldType)
            {
                case "System.DateTime":
                    //if (value is DateTime dateTimeValue)
                    //{
                    //    return dateTimeValue;
                    //}
                    DateTime dateTimeValue;
                    if (DateTime.TryParse(value?.ToString(), out dateTimeValue))
                    {
                        return dateTimeValue;
                    }
                    break;
                case "System.Int32":
                    if (value is int intValue)
                    {
                        return intValue;
                    }
                    if (int.TryParse(value?.ToString(), out intValue))
                    {
                        return intValue;
                    }
                    break;
                case "System.Int64":
                    if (value is long longValue)
                    {
                        return longValue;
                    }
                    if (long.TryParse(value?.ToString(), out longValue))
                    {
                        return longValue;
                    }
                    break;
                case "System.Decimal":
                    if (value is decimal decimalValue)
                    {
                        return decimalValue;
                    }
                    if (decimal.TryParse(value?.ToString(), out decimalValue))
                    {
                        return decimalValue;
                    }
                    break;
                case "System.Boolean":
                    if (value is bool boolValue)
                    {
                        return boolValue;
                    }
                    if (bool.TryParse(value?.ToString(), out boolValue))
                    {
                        return boolValue;
                    }
                    break;
                case "System.Double":
                    if (value is double doubleValue)
                    {
                        return doubleValue;
                    }
                    if (double.TryParse(value?.ToString(), out doubleValue))
                    {
                        return doubleValue;
                    }
                    break;
                case "System.Single":
                    if (value is float floatValue)
                    {
                        return floatValue;
                    }
                    if (float.TryParse(value?.ToString(), out floatValue))
                    {
                        return floatValue;
                    }
                    break;
                case "System.Byte":
                    if (value is byte byteValue)
                    {
                        return byteValue;
                    }
                    if (byte.TryParse(value?.ToString(), out byteValue))
                    {
                        return byteValue;
                    }
                    break;
                case "System.SByte":
                    if (value is sbyte sbyteValue)
                    {
                        return sbyteValue;
                    }
                    if (sbyte.TryParse(value?.ToString(), out sbyteValue))
                    {
                        return sbyteValue;
                    }
                    break;
                case "System.Int16":
                    if (value is short shortValue)
                    {
                        return shortValue;
                    }
                    if (short.TryParse(value?.ToString(), out shortValue))
                    {
                        return shortValue;
                    }
                    break;
                case "System.UInt16":
                    if (value is ushort ushortValue)
                    {
                        return ushortValue;
                    }
                    if (ushort.TryParse(value?.ToString(), out ushortValue))
                    {
                        return ushortValue;
                    }
                    break;
                case "System.UInt32":
                    if (value is uint uintValue)
                    {
                        return uintValue;
                    }
                    if (uint.TryParse(value?.ToString(), out uintValue))
                    {
                        return uintValue;
                    }
                    break;
                case "System.UInt64":
                    if (value is ulong ulongValue)
                    {
                        return ulongValue;
                    }
                    if (ulong.TryParse(value?.ToString(), out ulongValue))
                    {
                        return ulongValue;
                    }
                    break;
                case "System.Char":
                    if (value is char charValue)
                    {
                        return charValue;
                    }
                    if (char.TryParse(value?.ToString(), out charValue))
                    {
                        return charValue;
                    }
                    break;
                case "System.Guid":
                    if (value is Guid guidValue)
                    {
                        return guidValue;
                    }
                    if (Guid.TryParse(value?.ToString(), out guidValue))
                    {
                        return guidValue;
                    }
                    break;
                case "System.TimeSpan":
                    if (value is TimeSpan timeSpanValue)
                    {
                        return timeSpanValue;
                    }
                    if (TimeSpan.TryParse(value?.ToString(), out timeSpanValue))
                    {
                        return timeSpanValue;
                    }
                    break;
                case "System.String":
                    return value?.ToString();
                default:
                    return value;
            }
            return value;
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
