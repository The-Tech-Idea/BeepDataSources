using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        private DbType GetDbType(string fieldType)
        {
            // Convert field type to DbType
            switch (fieldType)
            {
                case "System.String":
                    return DbType.String;
                case "System.Int32":
                    return DbType.Int32;
                case "System.Int64":
                    return DbType.Int64;
                case "System.Int16":
                    return DbType.Int16;
                case "System.Byte":
                    return DbType.Byte;
                case "System.Boolean":
                    return DbType.Boolean;
                case "System.DateTime":
                    return DbType.DateTime;
                case "System.Decimal":
                    return DbType.Decimal;
                case "System.Double":
                    return DbType.Double;
                case "System.Single":
                    return DbType.Single;
                case "System.Guid":
                    return DbType.Guid;
                case "System.TimeSpan":
                    return DbType.Time;
                case "System.Byte[]":
                    return DbType.Binary;
                case "System.UInt16":
                    return DbType.UInt16;
                case "System.UInt32":
                    return DbType.UInt32;
                case "System.UInt64":
                    return DbType.UInt64;
                case "System.SByte":
                    return DbType.SByte;
                case "System.Object":
                    return DbType.Object;
                case "System.Xml.XmlDocument":
                    return DbType.Xml;
                case "System.Data.SqlTypes.SqlBinary":
                    return DbType.Binary;
                case "System.Data.SqlTypes.SqlBoolean":
                    return DbType.Boolean;
                case "System.Data.SqlTypes.SqlByte":
                    return DbType.Byte;
                case "System.Data.SqlTypes.SqlDateTime":
                    return DbType.DateTime;
                case "System.Data.SqlTypes.SqlDecimal":
                    return DbType.Decimal;
                case "System.Data.SqlTypes.SqlDouble":
                    return DbType.Double;
                case "System.Data.SqlTypes.SqlGuid":
                    return DbType.Guid;
                case "System.Data.SqlTypes.SqlInt16":
                    return DbType.Int16;
                case "System.Data.SqlTypes.SqlInt32":
                    return DbType.Int32;
                case "System.Data.SqlTypes.SqlInt64":
                    return DbType.Int64;
                case "System.Data.SqlTypes.SqlMoney":
                    return DbType.Currency;
                case "System.Data.SqlTypes.SqlSingle":
                    return DbType.Single;
                case "System.Data.SqlTypes.SqlString":
                    return DbType.String;
                default:
                    return DbType.String; // Default to string if type is unknown
            }
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
