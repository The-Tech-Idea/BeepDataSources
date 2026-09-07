using System;
using System.Data;
using System.Globalization;
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
        /// </summary>
        /// <remarks>
        /// protected virtual, not private: this and <see cref="ConvertToDbTypeValue"/> decide the
        /// DbType and the CLR value of every parameter on every INSERT, UPDATE and DELETE, yet a
        /// driver could not adjust either. Oracle (NUMBER, CLOB, TIMESTAMP WITH TIME ZONE),
        /// PostgreSQL (jsonb, arrays, uuid), DuckDB, Snowflake and Firebird all have mappings the
        /// shared table does not cover.
        ///
        /// Note the fallback in <c>DbTypeMapper</c> is <see cref="DbType.String"/> for anything it
        /// does not recognise, and <c>EntityField.Fieldtype</c> often carries a SQL type name rather
        /// than a .NET one — so byte[], Guid and decimal can arrive typed as text. Overriding this
        /// is the supported way to correct that per driver.
        /// </remarks>
        protected virtual DbType GetDbType(string fieldType)
        {
            return DbTypeMapper.ToDbType(fieldType);
        }



        /// <summary>
        /// Converts a value to the CLR type implied by <paramref name="fieldType"/>.
        /// </summary>
        /// <remarks>
        /// All parsing is culture-INVARIANT. It previously used the ambient culture: every arm
        /// called <c>Convert.ToString(value, CultureInfo.InvariantCulture)</c> and <c>TryParse(string)</c> with no format provider, so on
        /// a machine set to de-DE or fr-FR "1.5" and "1,5" swapped meaning and "03/04/2026" changed
        /// month. For a class that serialises values into database parameters that is a silent
        /// data-corruption path, and it depends on the machine the process happens to run on.
        ///
        /// The XML doc here used to claim a Convert.ChangeType fallback. There is none — the default
        /// arm returns the value unchanged, so a failed parse hands the provider an unconverted
        /// object and the failure surfaces as an opaque provider error instead of a clean one.
        /// </remarks>
        protected virtual object ConvertToDbTypeValue(object value, string fieldType)
        {
            if (value == null || value == DBNull.Value)
                return DBNull.Value;

            // Pattern matching approach (C# 7+) - more concise and performant
            return fieldType switch
            {
                "System.DateTime" when value is DateTime dt => dt,
                "System.DateTime" when DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) => dt,
                
                "System.Int32" when value is int i => i,
                "System.Int32" when int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) => i,
                
                "System.Int64" when value is long l => l,
                "System.Int64" when long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var l) => l,
                
                "System.Decimal" when value is decimal dec => dec,
                "System.Decimal" when decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var dec) => dec,
                
                "System.Boolean" when value is bool b => b,
                "System.Boolean" when bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var b) => b,
                
                "System.Double" when value is double d => d,
                "System.Double" when double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
                
                "System.Single" when value is float f => f,
                "System.Single" when float.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var f) => f,
                
                "System.Byte" when value is byte by => by,
                "System.Byte" when byte.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var by) => by,
                
                "System.SByte" when value is sbyte sb => sb,
                "System.SByte" when sbyte.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var sb) => sb,
                
                "System.Int16" when value is short sh => sh,
                "System.Int16" when short.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var sh) => sh,
                
                "System.UInt16" when value is ushort us => us,
                "System.UInt16" when ushort.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var us) => us,
                
                "System.UInt32" when value is uint ui => ui,
                "System.UInt32" when uint.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var ui) => ui,
                
                "System.UInt64" when value is ulong ul => ul,
                "System.UInt64" when ulong.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var ul) => ul,
                
                "System.Char" when value is char c => c,
                "System.Char" when char.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var c) => c,
                
                "System.Guid" when value is Guid g => g,
                "System.Guid" when Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var g) => g,
                
                "System.TimeSpan" when value is TimeSpan ts => ts,
                "System.TimeSpan" when TimeSpan.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, out var ts) => ts,
                
                "System.String" => Convert.ToString(value, CultureInfo.InvariantCulture),
                
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
