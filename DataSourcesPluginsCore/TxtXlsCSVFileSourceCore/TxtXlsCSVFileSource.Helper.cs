using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Logger;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Helper class for file operation utilities shared between CSV and Excel readers/writers
    /// </summary>
    internal class TxtXlsCSVFileSourceHelper
    {
        private IDMLogger Logger { get; set; }

        public TxtXlsCSVFileSourceHelper(IDMLogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Escapes CSV field values that contain delimiters, quotes, or newlines
        /// </summary>
        public string EscapeCsvValue(string value, char delimiter)
        {
            if (value == null) return string.Empty;
            bool mustQuote = value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (value.Contains('"'))
            {
                value = value.Replace("\"", "\"\"");
                mustQuote = true;
            }
            if (mustQuote)
            {
                return '"' + value + '"';
            }
            return value;
        }

        /// <summary>
        /// Serializes an object (DataRow, IDictionary, or POCO) to a CSV row string
        /// </summary>
        public string SerializeObjectToCsvRow(object obj, EntityStructure entity, char delimiter)
        {
            if (entity == null || entity.Fields == null || entity.Fields.Count == 0) return string.Empty;
            var parts = new List<string>();
            foreach (var fld in entity.Fields)
            {
                object val = null;
                try
                {
                    if (obj is DataRow dr)
                    {
                        if (dr.Table.Columns.Contains(fld.Originalfieldname))
                            val = dr[fld.Originalfieldname];
                        else if (dr.Table.Columns.Contains(fld.FieldName))
                            val = dr[fld.FieldName];
                    }
                    else if (obj is IDictionary<string, object> dict)
                    {
                        if (dict.ContainsKey(fld.FieldName)) val = dict[fld.FieldName];
                        else if (dict.ContainsKey(fld.Originalfieldname)) val = dict[fld.Originalfieldname];
                    }
                    else
                    {
                        var pi = obj.GetType().GetProperty(fld.FieldName) ?? obj.GetType().GetProperty(fld.Originalfieldname);
                        if (pi != null) val = pi.GetValue(obj);
                    }
                }
                catch (Exception ex)
                {
                    Logger?.WriteLog($"Error serializing field '{fld.FieldName}': {ex.Message}");
                }
                string sval = val?.ToString() ?? string.Empty;
                parts.Add(EscapeCsvValue(sval, delimiter));
            }
            return string.Join(delimiter.ToString(), parts);
        }

        /// <summary>
        /// Converts a Type to TypeCode for type conversion operations
        /// </summary>
        public TypeCode ToTypeCode(Type dest)
        {
            TypeCode retval = TypeCode.String;
            switch (dest.ToString())
            {
                case "System.String":
                    retval = TypeCode.String;
                    break;
                case "System.Decimal":
                    retval = TypeCode.Decimal;
                    break;
                case "System.DateTime":
                    retval = TypeCode.DateTime;
                    break;
                case "System.Char":
                    retval = TypeCode.Char;
                    break;
                case "System.Boolean":
                    retval = TypeCode.Boolean;
                    break;
                case "System.DBNull":
                    retval = TypeCode.DBNull;
                    break;
                case "System.Byte":
                    retval = TypeCode.Byte;
                    break;
                case "System.Int16":
                    retval = TypeCode.Int16;
                    break;
                case "System.Double":
                    retval = TypeCode.Double;
                    break;
                case "System.Int32":
                    retval = TypeCode.Int32;
                    break;
                case "System.Int64":
                    retval = TypeCode.Int64;
                    break;
                case "System.Single":
                    retval = TypeCode.Single;
                    break;
                case "System.Object":
                    retval = TypeCode.String;
                    break;
            }
            return retval;
        }

        /// <summary>
        /// Infers data type from field values in a DataTable
        /// </summary>
        public Type InferFieldType(DataTable dt, string fieldName, int sampleSize = 100)
        {
            try
            {
                if (!dt.Columns.Contains(fieldName))
                    return typeof(string);

                int samplesToCheck = Math.Min(sampleSize, dt.Rows.Count);
                bool hasNumber = false;
                bool hasDate = false;
                bool hasBoolean = false;

                for (int i = 0; i < samplesToCheck; i++)
                {
                    object val = dt.Rows[i][fieldName];
                    if (val == null || val is DBNull)
                        continue;

                    string strVal = val.ToString().Trim();
                    if (string.IsNullOrEmpty(strVal))
                        continue;

                    // Check boolean
                    if (bool.TryParse(strVal, out _))
                    {
                        hasBoolean = true;
                    }

                    // Check date
                    if (DateTime.TryParse(strVal, out _))
                    {
                        hasDate = true;
                    }

                    // Check number
                    if (decimal.TryParse(strVal, out _))
                    {
                        hasNumber = true;
                    }
                }

                // Determine type based on samples
                if (hasBoolean && !hasNumber && !hasDate)
                    return typeof(bool);
                if (hasDate && !hasNumber)
                    return typeof(DateTime);
                if (hasNumber)
                    return typeof(decimal);

                return typeof(string);
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error inferring field type for '{fieldName}': {ex.Message}");
                return typeof(string);
            }
        }

        /// <summary>
        /// Converts a DataTable to use proper types based on field definitions
        /// </summary>
        public DataTable ConvertDataTableTypes(DataTable source, EntityStructure entity, IDMLogger logger)
        {
            try
            {
                if (entity == null || entity.Fields == null || entity.Fields.Count == 0)
                    return source;

                DataTable target = new DataTable(source.TableName);

                // Create columns with proper types
                foreach (var field in entity.Fields)
                {
                    Type pFieldtype = Type.GetType(field.Fieldtype) ?? typeof(string);
                    target.Columns.Add(field.FieldName, pFieldtype);
                }

                // Convert and copy rows
                foreach (DataRow srcRow in source.Rows)
                {
                    DataRow tgtRow = target.NewRow();
                    foreach (var field in entity.Fields)
                    {
                        try
                        {
                            object srcVal = null;
                            if (source.Columns.Contains(field.Originalfieldname))
                                srcVal = srcRow[field.Originalfieldname];
                            else if (source.Columns.Contains(field.FieldName))
                                srcVal = srcRow[field.FieldName];

                            if (srcVal != null && srcVal != DBNull.Value)
                            {
                                string strVal = srcVal.ToString().Trim();
                                if (!string.IsNullOrEmpty(strVal))
                                {
                                    Type targetType = Type.GetType(field.Fieldtype) ?? typeof(string);
                                    tgtRow[field.FieldName] = Convert.ChangeType(strVal, targetType);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.WriteLog($"Error converting field '{field.FieldName}': {ex.Message}");
                        }
                    }
                    target.Rows.Add(tgtRow);
                }

                return target;
            }
            catch (Exception ex)
            {
                logger?.WriteLog($"Error converting DataTable types: {ex.Message}");
                return source;
            }
        }
    }
}
