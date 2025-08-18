using System;
using System.Collections.Generic;
using System.Data;

using System.Threading.Tasks;
using System.Linq;
using Dapper;
using System.Reflection;
using System.Data.Common;

using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Report;
using System.Data.SqlTypes;
using TheTechIdea.Beep.Helpers;
using System.Diagnostics;

using System.ComponentModel;
using Newtonsoft.Json.Linq;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using static TheTechIdea.Beep.Utilities.Util;
using TheTechIdea.Beep.DriversConfigurations;
using System.Text;
using System.Collections;

namespace TheTechIdea.Beep.DataBase
{
    public class RDBSource : IRDBSource
    {
        HashSet<string> usedParameterNames = new HashSet<string>();
        List<EntityField> UpdateFieldSequnce=new List<EntityField>();
        public event EventHandler<PassedArgs> PassEvent;
        // Static random number generator used for various purposes within the class.
        static Random r = new Random();

        /// <summary>
        /// Unique identifier for the RDBSource instance, generated using Guid.
        /// </summary>
        public string GuidID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// General identifier of the RDBSource instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the data source.
        /// </summary>
        public string DatasourceName { get; set; }

        /// <summary>
        /// Type of the data source, indicating the specific relational database system (e.g., SQL Server, MySQL).
        /// </summary>
        public DataSourceType DatasourceType { get; set; }

        /// <summary>
        /// Current state of the database connection.
        /// </summary>
        public ConnectionState ConnectionStatus { get => Dataconnection.ConnectionStatus; set { } }

        /// <summary>
        /// Category of the data source, typically RDBMS for relational databases.
        /// </summary>
        public DatasourceCategory Category { get; set; } = DatasourceCategory.RDBMS;

        /// <summary>
        /// Object to handle error information.
        /// </summary>
        public IErrorsInfo ErrorObject { get; set; }

        /// <summary>
        /// Logger instance for logging activities and events.
        /// </summary>
        public IDMLogger Logger { get; set; }

        /// <summary>
        /// List of names of entities (e.g., tables) available in the database.
        /// </summary>
        public List<string> EntitiesNames { get; set; } = new List<string>();

        /// <summary>
        /// Editor instance for managing various database operations.
        /// </summary>
        public IDMEEditor DMEEditor { get; set; }

        /// <summary>
        /// List of entity structures representing database schemas.
        /// </summary>
        public List<EntityStructure> Entities { get; set; } = new List<EntityStructure>();

        /// <summary>
        /// Connection object to interact with the database.
        /// </summary>
        public IDataConnection Dataconnection { get; set; }

        /// <summary>
        /// Specialized connection object for relational databases.
        /// </summary>
        public RDBDataConnection RDBMSConnection { get { return (RDBDataConnection)Dataconnection; } }

        /// <summary>
        /// Delimiter used for columns in queries, specific to the database syntax.
        /// </summary>
        public virtual string ColumnDelimiter { get; set; } = "''";

        /// <summary>
        /// Delimiter used for parameters in queries, specific to the database syntax.
        /// </summary>
        public virtual string ParameterDelimiter { get; set; } = "@";

        /// <summary>
        /// Initializes a new instance of the RDBSource class.
        /// </summary>
        /// <param name="datasourcename">Name of the data source.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="pDMEEditor">DMEEditor instance for database operations.</param>
        /// <param name="databasetype">Type of the database.</param>
        /// <param name="per">Error information object.</param>
        protected static int recNumber = 0;
        protected string recEntity = "";

        /// <summary>
        /// Get List of Tables that connection has that is not on that same user
        /// </summary>
        ///
        public string GetListofEntitiesSql { get; set; } = string.Empty;
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        public RDBSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per)
        {
            DatasourceName = datasourcename;
            Logger = logger;
            ErrorObject = per;
            DMEEditor = pDMEEditor;
            DatasourceType = databasetype;
            Category = DatasourceCategory.RDBMS;
            Dataconnection = new RDBDataConnection(DMEEditor)
            {
                Logger = logger,
                ErrorObject = ErrorObject,
            };
        }
        #region "IDataSource Interface Methods"
        public virtual ConnectionState Openconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.OpenConnection();
            }
            return ConnectionStatus;
        }
        public virtual ConnectionState Closeconnection()
        {
            if (RDBMSConnection != null)
            {
                ConnectionStatus = RDBMSConnection.CloseConn();
                Dataconnection.CloseConn();
            }
            if (Dataconnection != null)
            {

                Dataconnection.CloseConn();
            }
            return ConnectionStatus;
        }
        #region "Repo Methods"
        /// <summary>
        /// Executes a SQL query and retrieves the first column of the first row in the result set.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <returns>The scalar value as a double. Returns 0.0 if the query fails or doesn't return a valid double.</returns>
        /// <remarks>
        /// This method is used to retrieve a single value, such as an aggregate or a count, from the database.
        /// </remarks>
        public virtual Task<double> GetScalarAsync(string query)
        {
            return Task.Run(() => GetScalar(query));
        }
        /// <summary>
        /// Asynchronously retrieves a single scalar value from the database.
        /// </summary>
        /// <param name="query">The SQL query to be executed.</param>
        /// <returns>A task representing the asynchronous operation, resulting in the scalar value.</returns>
        public virtual double GetScalar(string query)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Assuming you have a database connection and command objects.

                using (var command = GetDataCommand())
                {
                    command.CommandText = query;
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var result = reader.GetDecimal(0); // Assuming the result is a decimal value
                            return Convert.ToDouble(result);
                        }
                    }
                }


                // If the query executed successfully but didn't return a valid double, you can handle it here.
                // You might want to log an error or throw an exception as needed.
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in executing scalar query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }

            // Return a default value or throw an exception if the query failed.
            return 0.0; // You can change this default value as needed.
        }
        /// <summary>
        /// Executes a SQL command that does not return a result set.
        /// </summary>
        /// <param name="sql">The SQL command to execute.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// Use this method for SQL commands like INSERT, UPDATE, DELETE, etc.
        /// </remarks>
        public virtual IErrorsInfo ExecuteSql(string sql)
        {
            ErrorObject.Flag = Errors.Ok;
            // CurrentSql = sql;
            IDbCommand cmd = GetDataCommand();
            if (cmd != null)
            {
                try
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    //    DMEEditor.AddLogMessage("Success", "Executed Sql Successfully", DateTime.Now, -1, "Ok", Errors.Ok);
                }
                catch (Exception ex)
                {

                    cmd.Dispose();
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $" Could not run Script - {sql} -" + ex.Message;
                    DMEEditor.AddLogMessage("Fail", $" Could not run Script - {sql} -" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);

                }

            }

            return ErrorObject;
        }
        /// <summary>
        /// Executes a SQL query and returns the result set.
        /// </summary>
        /// <param name="qrystr">The SQL query string.</param>
        /// <returns>A DataTable containing the query results or null if an error occurs.</returns>
        /// <remarks>
        /// This method is suitable for queries that return multiple rows.
        /// </remarks>
        public virtual IBindingList RunQuery(string qrystr)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                if (string.IsNullOrWhiteSpace(qrystr))
                {
                    DMEEditor.AddLogMessage("Fail", "RunQuery: query string is null or empty", DateTime.Now, 0, "", Errors.Failed);
                    return new DataTable().DefaultView;
                }

                if (Dataconnection.ConnectionStatus != ConnectionState.Open)
                {
                    Openconnection();
                }

                using (var cmd = GetDataCommand())
                {
                    if (cmd == null)
                    {
                        DMEEditor.AddLogMessage("Fail", "RunQuery: failed to create data command", DateTime.Now, 0, "", Errors.Failed);
                        return new DataTable().DefaultView;
                    }

                    cmd.CommandText = qrystr;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.Default))
                    {
                        var dt = new DataTable();
                        dt.Load(reader);
                        return dt.DefaultView; // DataView implements IBindingList
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error executing query ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return new DataTable().DefaultView;
            }
        }
        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="args">Optional arguments related to the transaction.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of beginning the transaction.</returns>
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                RDBMSConnection.DbConn.BeginTransaction();
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>
        /// Ends a database transaction.
        /// </summary>
        /// <param name="args">Optional arguments related to the transaction.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of ending the transaction.</returns>
        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>
        /// Creates and adds parameters to a database command based on the provided DataRow and EntityStructure.
        /// </summary>
        /// <param name="command">The database command to add parameters to.</param>
        /// <param name="r">The DataRow containing parameter values.</param>
        /// <param name="DataStruct">The EntityStructure defining the structure of the entity.</param>
        /// <returns>The updated IDbCommand with parameters added.</returns>
        private IDbCommand CreateCommandParameters(IDbCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    switch (item.fieldtype)
                    {
                        case "System.DateTime":
                            parameter.DbType = DbType.DateTime;  // Set this once as it's common for both branches

                            if (r[item.fieldname] == DBNull.Value || string.IsNullOrWhiteSpace(r[item.fieldname].ToString()))
                            {
                                parameter.Value = DBNull.Value;
                            }
                            else
                            {
                                if (DateTime.TryParse(r[item.fieldname].ToString(), out DateTime dateValue))
                                {
                                    // Ensuring the DateTime Kind is correctly set
                                    if (dateValue.Kind == DateTimeKind.Unspecified)
                                    {
                                        // Assuming the unspecified DateTime is in UTC as required by PostgreSQL
                                        dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                                    }
                                    else if (dateValue.Kind == DateTimeKind.Local)
                                    {
                                        // Convert local DateTime to UTC
                                        dateValue = dateValue.ToUniversalTime();
                                    }
                                    parameter.Value = dateValue;
                                }
                                else
                                {
                                    parameter.Value = DBNull.Value;
                                }
                            }

                            break;
                        case "System.Double":
                            parameter.DbType = DbType.Double;
                            parameter.Value = Convert.ToDouble(r[item.fieldname]);
                            break;
                        case "System.Single": // Single is equivalent to float in C#
                            parameter.DbType = DbType.Single;
                            parameter.Value = Convert.ToSingle(r[item.fieldname]);
                            break;
                        case "System.Byte":
                            parameter.DbType = DbType.Byte;
                            parameter.Value = Convert.ToByte(r[item.fieldname]);
                            break;
                        case "System.Guid":
                            parameter.DbType = DbType.Guid;
                            parameter.Value = Guid.Parse(r[item.fieldname].ToString());
                            break;
                        case "System.String":  // For VARCHAR2 and NVARCHAR2
                            parameter.DbType = DbType.String;
                            parameter.Value = r[item.fieldname] ?? DBNull.Value;
                            break;
                        case "System.Decimal":  // For NUMBER without scale
                            parameter.DbType = DbType.Decimal;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToDecimal(r[item.fieldname]);
                            break;
                        case "System.Int32":  // For NUMBER that fits into Int32
                            parameter.DbType = DbType.Int32;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToInt32(r[item.fieldname]);
                            break;
                        case "System.Int64":  // For NUMBER that fits into Int64
                            parameter.DbType = DbType.Int64;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToInt64(r[item.fieldname]);
                            break;
                        case "System.Boolean":  // If you have a boolean in .NET mapped to VARCHAR2(3 CHAR) in Oracle
                            parameter.DbType = DbType.Boolean;
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : (object)Convert.ToBoolean(r[item.fieldname]);
                            break;
                        // Add more cases as needed for other types
                        default:
                            parameter.Value = r.IsNull(item.fieldname) ? DBNull.Value : r[item.fieldname];
                            break;
                    }
                    //if (item.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //    if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                    //    {

                    //        parameter.Value = DBNull.Value;
                    //        parameter.DbType = DbType.DateTime;
                    //    }
                    //    else
                    //    {
                    //        parameter.DbType = DbType.DateTime;
                    //        try
                    //        {
                    //            parameter.Value = DateTime.Parse(r[item.fieldname].ToString());
                    //        }
                    //        catch (FormatException formatex)
                    //        {

                    //            parameter.Value = SqlDateTime.Null;
                    //        }
                    //    }
                    //}
                    //else
                    //    parameter.Value = r[item.fieldname];
                    parameter.ParameterName = "p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                    //   parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }
        private IDbCommand CreateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {

            foreach (var field in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                // Skip auto-increment (identity) fields
                if (field.IsAutoIncrement)
                {
                    continue;
                }

                var property = InsertedData.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = InsertedData.GetType().GetProperty(field.fieldname).GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();
                   
                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;
                    
                    
                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }
        private IDbCommand CreateUpdateCommandParameters(IDbCommand command, object InsertedData, EntityStructure DataStruct)
        {
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField field= UpdateFieldSequnce[i];
                // Skip auto-increment (identity) fields
                if (field.IsAutoIncrement)
                {
                    continue;
                }

                var property = InsertedData.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = InsertedData.GetType().GetProperty(field.fieldname).GetValue(InsertedData) ?? DBNull.Value;
                    var parameter = command.CreateParameter();

                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;


                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parameter);
                }
            }
            //foreach (var field in UpdateFieldSequnce.OrderBy(o => o.fieldname))
            //{
              
            //}

            return command;
        }
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
        /// <summary>
        /// Creates parameters for a DELETE database command based on the provided DataRow and EntityStructure.
        /// </summary>
        /// <param name="command">The DELETE database command to add parameters to.</param>
        /// <param name="r">The DataRow containing parameter values for the DELETE operation.</param>
        /// <param name="DataStruct">The EntityStructure defining the primary keys for the DELETE operation.</param>
        /// <returns>The updated IDbCommand with parameters added.</returns>
        private IDbCommand CreateDeleteCommandParameters(IDbCommand command, object r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField field in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {

                var property = r.GetType().GetProperty(field.fieldname);
                if (property != null)
                {
                    var value = property.GetValue(r);
                    var parameter = command.CreateParameter();

                    // Find the corresponding parameter name in usedParameterNames
                    string paramName = Regex.Replace(field.fieldname, @"\s+", "_");
                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    string matchingParamName = usedParameterNames.FirstOrDefault(p => p.StartsWith(paramName));
                    if (string.IsNullOrEmpty(matchingParamName))
                    {
                        throw new InvalidOperationException($"Parameter name for field '{field.fieldname}' not found in usedParameterNames.");
                    }

                    parameter.ParameterName = $"{ParameterDelimiter}p_" + matchingParamName;
                    parameter.Value = value ?? DBNull.Value;
                    parameter.DbType = GetDbType(field.fieldtype);
                    if (value != DBNull.Value && value.GetType() != typeof(DBNull))
                    {
                        parameter.Value = ConvertToDbTypeValue(value, field.fieldtype);
                    }
                    else
                    {
                        parameter.Value = DBNull.Value;
                    }

                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {

            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        /// <summary>
        /// Updates a specific record in the database for the given entity based on provided data.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) in which the record will be updated.</param>
        /// <param name="UploadDataRow">The data row that contains the updated values for the entity.</param>
        /// <returns>IErrorsInfo object containing information about the operation's success or failure.</returns>
        /// <remarks>
        /// This method constructs and executes an SQL update command based on the provided data row. 
        /// It also handles transaction management and logs the operation's outcome.
        /// </remarks>

        public virtual IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;

            //DataRowView dv;
            //DataTable tb;
            //DataRow dr;
            string msg = "";
            // dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, UploadDataRow, DataStruct);
            try
            {
                UpdateFieldSequnce = new List<EntityField>();
                usedParameterNames = new HashSet<string>();
                string updatestring = GetUpdateString(EntityName, DataStruct);
                command = GetDataCommand();
                command.CommandText = updatestring;
                command = CreateUpdateCommandParameters(command, UploadDataRow, DataStruct);


                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Updated  Record  to {EntityName} : {updatestring}";
                    // DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Updated  Record  to {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }


            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;

                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    //     sqlTran.Rollback();
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                msg = "Unsuccessfully no Data has been written to Data Source";
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

            }

            return ErrorObject;
        }
        /// <summary>
        /// Deletes a specific record from the database for the given entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) from which the record will be deleted.</param>
        /// <param name="DeletedDataRow">The data row that identifies the record to be deleted.</param>
        /// <returns>IErrorsInfo object containing information about the success or failure of the operation.</returns>
        /// <remarks>
        /// This method constructs and executes an SQL delete command. It uses transactions to ensure data integrity and logs the outcome of the operation.
        /// </remarks>
        public virtual IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;

            string msg;
         //   DataRowView dv;
         //   DataTable tb;
         //   DataRow dr;
            //var sqlTran = RDBMSConnection.DbConn.BeginTransaction();
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

         //   dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, DeletedDataRow, DataStruct);
            try
            {
                usedParameterNames = new HashSet<string>();
                string updatestring = GetDeleteString(EntityName, DataStruct);
                command = GetDataCommand();
            //    command.Transaction = sqlTran;
                command.CommandText = updatestring;
                command = CreateDeleteCommandParameters(command, DeletedDataRow, DataStruct);
                //command = CreateDeleteCommandParameters(command, dr, DataStruct);
                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Deleted  Record  to {EntityName} : {updatestring}";
                    //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Delete Record  from {EntityName} : {updatestring}";
                    DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
             //   sqlTran.Commit();
                command.Dispose();


            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;

                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                  //  sqlTran.Rollback();
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
                }
                catch (Exception exRollback)
                {
                    // Throws an InvalidOperationException if the connection
                    // is closed or the transaction has already been rolled
                    // back on the server.
                    // Console.WriteLine(exRollback.Message);
                    msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
                    ErrorObject.Ex = exRollback;
                }
                msg = "Unsuccessfully no Data has been written to Data Source";
                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

            }

            return ErrorObject;
        }
        /// <summary>
        /// Inserts a new record into the database for the specified entity.
        /// </summary>
        /// <param name="EntityName">The name of the entity (e.g., table name) in which the new record will be inserted.</param>
        /// <param name="InsertedData">The data row representing the new record to be inserted.</param>
        /// <returns>IErrorsInfo object with information about the success or failure of the insert operation.</returns>
        /// <remarks>
        /// This method prepares and executes an SQL insert command based on the data provided. It logs the operation's outcome for debugging and error handling purposes.
        /// </remarks>
        public virtual IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;
            DataRow dr;
            string msg = "";
            string updatestring = "";
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

            //     dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, InsertedData, DataStruct);
            try
            {
                usedParameterNames = new HashSet<string>();
                updatestring = GetInsertString(EntityName, DataStruct);
                command = GetDataCommand();
                command.CommandText = updatestring;
                command = CreateCommandParameters(command, InsertedData, DataStruct);

                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Inserted  Record  to {EntityName} ";
                    DMEEditor.ErrorObject.Message = msg;
                    DMEEditor.ErrorObject.Flag = Errors.Ok;
                    string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
                    if (fetchIdentityQuery.ToUpper().Contains("SELECT") && DataStruct.PrimaryKeys.Count() > 0)
                    {
                        command.CommandText = fetchIdentityQuery;
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            var primaryKeyProperty = InsertedData.GetType().GetProperty(DataStruct.PrimaryKeys.First().fieldname);
                            if (primaryKeyProperty != null && primaryKeyProperty.CanWrite)
                            {
                                var primaryKeyType = primaryKeyProperty.PropertyType;
                                Type underlyingType = Nullable.GetUnderlyingType(primaryKeyType) ?? primaryKeyType;

                                // Convert the identity to the appropriate type
                                var convertedIdentity = Convert.ChangeType(result, underlyingType);
                                primaryKeyProperty.SetValue(InsertedData, convertedIdentity);

                                msg = $"Successfully Inserted Record to {EntityName} with ID {convertedIdentity}";
                                DMEEditor.ErrorObject.Message = msg;
                                DMEEditor.ErrorObject.Flag = Errors.Ok;
                            }
                        }
                        else
                        {
                            msg = "Failed to retrieve the identity of the inserted record.";
                            DMEEditor.ErrorObject.Message = msg;
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                        }
                    }

                    // DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
                }
                else
                {
                    msg = $"Fail to Insert  Record  to {EntityName} : {updatestring}";
                    DMEEditor.ErrorObject.Message = msg;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;


                    //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
                }
                // DMEEditor.AddLogMessage("Success",$"Successfully Written Data to {EntityName}",DateTime.Now,0,null, Errors.Ok);

            }
            catch (Exception ex)
            {
                msg = $"Fail to Insert  Record  to {EntityName} : {ex.Message}";
                ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message = msg;
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                command.Dispose();

                DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, updatestring, Errors.Failed);

            }

            return ErrorObject;
        }
        /// <summary>
        /// Sets up necessary objects and structures for database operations based on the provided entity name.
        /// </summary>
        /// <param name="Entityname">The name of the entity for which the database command objects will be set up.</param>
        /// <remarks>
        /// This method is essential for initializing and reusing database commands and structures, improving efficiency and maintainability.
        /// </remarks>
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, false);
                command = RDBMSConnection.DbConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        // <summary>
        /// Dynamically builds an SQL query based on the original query and provided filters.
        /// </summary>
        /// <param name="originalquery">The base SQL query string.</param>
        /// <param name="Filter">List of filters to be applied to the query.</param>
        /// <returns>The dynamically built SQL query string.</returns>
        /// <remarks>
        /// This method enhances flexibility in data retrieval by allowing dynamic query modifications based on runtime conditions and parameters.
        /// </remarks>
        /// /// <summary>
        /// Dynamically builds an SQL query based on the original query and provided filters.
        /// </summary>
        /// <param name="originalquery">The base SQL query string.</param>
        /// <param name="Filter">List of filters to be applied to the query.</param>
        /// <returns>The dynamically built SQL query string.</returns>
        /// <remarks>
        /// This method creates flexible, database-agnostic queries by properly handling 
        /// SQL syntax, filter operators, and parameter names for prepared statements.
        /// </remarks>

        private string BuildQuery(string originalquery, List<AppFilter> Filter)
        {
            string retval;
            string[] stringSeparators;
            string[] sp;
            string qrystr = "Select ";
            bool FoundWhere = false;
            QueryBuild queryStructure = new QueryBuild();
            try
            {
                //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
                // Get Selected Fields
                originalquery = GetTableName(originalquery.ToLower());
                stringSeparators = new string[] { "select", "from", "where", "group by", "having", "order by" };
                sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                queryStructure.FieldsString = sp[0];
                string[] Fieldsp = sp[0].Split(',');
                queryStructure.Fields.AddRange(Fieldsp);
                // Get From  Tables
                queryStructure.EntitiesString = sp[1];
                string[] Tablesdsp = sp[1].Split(',');
                queryStructure.Entities.AddRange(Tablesdsp);

                if (GetSchemaName() == null)
                {
                    qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
                }
                else
                    qrystr += queryStructure.FieldsString + $" from {GetSchemaName().ToLower()}." + queryStructure.EntitiesString;

                qrystr += Environment.NewLine;

                if (Filter != null)
                {
                    if (Filter.Count > 0)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            qrystr += Environment.NewLine;
                            if (FoundWhere == false)
                            {
                                qrystr += " where " + Environment.NewLine;
                                FoundWhere = true;
                            }

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
                            {
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                    if (item.Operator.ToLower() == "between")
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + $" and  {ParameterDelimiter}p_" + item.FieldName + "1 " + Environment.NewLine;
                                    }
                                    else
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + " " + Environment.NewLine;
                                    }

                                }



                            }
                        }
                    }
                }
                if (originalquery.ToLower().Contains("where"))
                {
                    qrystr += Environment.NewLine;

                    string[] whereSeparators = new string[] { "where", "group by", "having", "order by" };

                    string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.WhereCondition = spwhere[0];
                    if (FoundWhere == false)
                    {
                        qrystr += " where " + Environment.NewLine;
                        FoundWhere = true;
                    }
                    qrystr += spwhere[1];
                    qrystr += Environment.NewLine;



                }
                if (originalquery.ToLower().Contains("group by"))
                {
                    string[] groupbySeparators = new string[] { "group by", "having", "order by" };

                    string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.GroupbyCondition = groupbywhere[1];
                    qrystr += " group by " + groupbywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("having"))
                {
                    string[] havingSeparators = new string[] { "having", "order by" };

                    string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.HavingCondition = havingywhere[1];
                    qrystr += " having " + havingywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("order by"))
                {
                    string[] orderbySeparators = new string[] { "order by" };

                    string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.OrderbyCondition = orderbywhere[1];
                    qrystr += " order by " + orderbywhere[1];

                }

            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
            }
            return qrystr;
        }
        //private string BuildQuery(string originalquery, List<AppFilter> Filter)
        //{
        //    if (string.IsNullOrEmpty(originalquery))
        //    {
        //        DMEEditor.AddLogMessage("Fail", "Cannot build query from null or empty query string", DateTime.Now, 0, "Error", Errors.Failed);
        //        return string.Empty;
        //    }

        //    try
        //    {
        //        // Normalize query to lowercase for parsing
        //        string queryLower = originalquery.ToLower();

        //        // Apply schema name to table if needed
        //        string queryWithSchema = GetTableName(queryLower);

        //        // Parse the query into components
        //        QueryBuild queryStructure = ParseQueryComponents(queryWithSchema);

        //        // Start building the new query
        //        StringBuilder queryBuilder = new StringBuilder();

        //        // Add SELECT and FROM clauses
        //        string schemaPrefix = GetSchemaPrefix();
        //        queryBuilder.AppendLine($"SELECT {queryStructure.FieldsString} FROM {schemaPrefix}{queryStructure.EntitiesString}");

        //        // Process WHERE clause with filters
        //        bool hasWhereClause = queryLower.Contains("where");
        //        string whereClause = BuildWhereClause(queryStructure, Filter, hasWhereClause);
        //        if (!string.IsNullOrEmpty(whereClause))
        //        {
        //            queryBuilder.AppendLine(whereClause);
        //        }

        //        // Add remaining clauses in correct order
        //        AppendClauseIfExists(queryBuilder, queryStructure.GroupbyCondition, "GROUP BY");
        //        AppendClauseIfExists(queryBuilder, queryStructure.HavingCondition, "HAVING");
        //        AppendClauseIfExists(queryBuilder, queryStructure.OrderbyCondition, "ORDER BY");

        //        return queryBuilder.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.AddLogMessage("Fail", $"Unable to build query object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
        //        return originalquery; // Return original query on failure
        //    }
        //}

        /// <summary>
        /// Parses SQL query components into a QueryBuild structure.
        /// </summary>
        private QueryBuild ParseQueryComponents(string query)
        {
            QueryBuild queryStructure = new QueryBuild();

            // Define the SQL clause keywords to split by
            string[] clauseKeywords = { "select", "from", "where", "group by", "having", "order by" };

            // Split the query by clause keywords
            string[] parts = query.Split(clauseKeywords, StringSplitOptions.RemoveEmptyEntries);

            // Parse SELECT clause
            if (parts.Length > 0)
            {
                queryStructure.FieldsString = parts[0].Trim();
                queryStructure.Fields.AddRange(parts[0].Split(',').Select(f => f.Trim()));
            }

            // Parse FROM clause
            if (parts.Length > 1)
            {
                queryStructure.EntitiesString = parts[1].Trim();
                queryStructure.Entities.AddRange(parts[1].Split(',').Select(e => e.Trim()));
            }

            // Extract additional clauses if present in original query
            if (query.Contains("where"))
            {
                int wherePos = query.IndexOf("where", StringComparison.OrdinalIgnoreCase) + 5;
                int endPos = FindNextClausePosition(query, wherePos, new[] { "group by", "having", "order by" });
                queryStructure.WhereCondition = query.Substring(wherePos, endPos - wherePos).Trim();
            }

            if (query.Contains("group by"))
            {
                int groupByPos = query.IndexOf("group by", StringComparison.OrdinalIgnoreCase) + 8;
                int endPos = FindNextClausePosition(query, groupByPos, new[] { "having", "order by" });
                queryStructure.GroupbyCondition = query.Substring(groupByPos, endPos - groupByPos).Trim();
            }

            if (query.Contains("having"))
            {
                int havingPos = query.IndexOf("having", StringComparison.OrdinalIgnoreCase) + 6;
                int endPos = FindNextClausePosition(query, havingPos, new[] { "order by" });
                queryStructure.HavingCondition = query.Substring(havingPos, endPos - havingPos).Trim();
            }

            if (query.Contains("order by"))
            {
                int orderByPos = query.IndexOf("order by", StringComparison.OrdinalIgnoreCase) + 8;
                queryStructure.OrderbyCondition = query.Substring(orderByPos).Trim();
            }

            return queryStructure;
        }

        /// <summary>
        /// Finds the position of the next SQL clause in the query.
        /// </summary>
        private int FindNextClausePosition(string query, int startPos, string[] clauses)
        {
            int nextPos = query.Length;

            foreach (string clause in clauses)
            {
                int pos = query.IndexOf(clause, startPos, StringComparison.OrdinalIgnoreCase);
                if (pos > 0 && pos < nextPos)
                {
                    nextPos = pos;
                }
            }

            return nextPos;
        }

        /// <summary>
        /// Gets the schema prefix for the query.
        /// </summary>
        private string GetSchemaPrefix()
        {
            string schemaName = GetSchemaName();
            return !string.IsNullOrEmpty(schemaName) ? $"{schemaName}." : string.Empty;
        }

        /// <summary>
        /// Builds the WHERE clause including any filters.
        /// </summary>
        private string BuildWhereClause(QueryBuild queryStructure, List<AppFilter> filters, bool hasExistingWhere)
        {
            StringBuilder whereBuilder = new StringBuilder();
            bool hasFilters = filters != null && filters.Any(f => IsValidFilter(f));

            // Determine if we need to add a WHERE clause
            if (hasExistingWhere || hasFilters)
            {
                whereBuilder.Append("WHERE ");

                // Add filters if present
                if (hasFilters)
                {
                    bool firstFilter = true;
                    foreach (AppFilter filter in filters.Where(IsValidFilter))
                    {
                        if (!firstFilter)
                        {
                            whereBuilder.AppendLine(" AND ");
                        }

                        whereBuilder.Append(FormatFilterCondition(filter));
                        firstFilter = false;
                    }
                }

                // Add existing where clause if present
                if (hasExistingWhere && !string.IsNullOrEmpty(queryStructure.WhereCondition))
                {
                    if (hasFilters)
                    {
                        whereBuilder.AppendLine(" AND ");
                    }
                    whereBuilder.Append(queryStructure.WhereCondition);
                }
            }

            return whereBuilder.ToString();
        }

        /// <summary>
        /// Checks if an AppFilter has valid values for SQL generation.
        /// </summary>
        private bool IsValidFilter(AppFilter filter)
        {
            return filter != null &&
                   !string.IsNullOrEmpty(filter.FieldName) &&
                   !string.IsNullOrWhiteSpace(filter.FieldName) &&
                   !string.IsNullOrEmpty(filter.Operator) &&
                   !string.IsNullOrWhiteSpace(filter.Operator) &&
                   !string.IsNullOrEmpty(filter.FilterValue) &&
                   !string.IsNullOrWhiteSpace(filter.FilterValue);
        }

        /// <summary>
        /// Formats a filter condition for SQL.
        /// </summary>
        private string FormatFilterCondition(AppFilter filter)
        {
            string fieldName = filter.FieldName;
            string paramName = SanitizeParameterName(filter.FieldName);

            if (filter.Operator.ToLower() == "between")
            {
                return $"{fieldName} BETWEEN {ParameterDelimiter}p_{paramName} AND {ParameterDelimiter}p_{paramName}1";
            }
            else
            {
                return $"{fieldName} {filter.Operator} {ParameterDelimiter}p_{paramName}";
            }
        }

        /// <summary>
        /// Sanitizes a parameter name to ensure it's valid for SQL.
        /// </summary>
        private string SanitizeParameterName(string fieldName)
        {
            // Replace spaces with underscores and ensure name is valid
            string paramName = Regex.Replace(fieldName, @"\s+", "_");

            // Truncate if needed (for databases with name length limits)
            if (paramName.Length > 30 && (DatasourceType == DataSourceType.Oracle || DatasourceType == DataSourceType.Postgre))
            {
                paramName = paramName.Substring(0, 30);
            }

            return paramName;
        }

        /// <summary>
        /// Appends a SQL clause to the query builder if it exists.
        /// </summary>
        private void AppendClauseIfExists(StringBuilder queryBuilder, string clauseContent, string clauseName)
        {
            if (!string.IsNullOrEmpty(clauseContent))
            {
                queryBuilder.AppendLine($"{clauseName} {clauseContent}");
            }
        }

        //private string BuildQuery(string originalquery, List<AppFilter> Filter)
        //{
        //    string retval;
        //    string[] stringSeparators;
        //    string[] sp;
        //    string qrystr = "Select ";
        //    bool FoundWhere = false;
        //    QueryBuild queryStructure = new QueryBuild();
        //    try
        //    {
        //        //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
        //        // Get Selected Fields
        //        originalquery = GetTableName(originalquery.ToLower());
        //        stringSeparators = new string[] { "select", "from", "where", "group by", "having", "order by" };
        //        sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
        //        queryStructure.FieldsString = sp[0];
        //        string[] Fieldsp = sp[0].Split(',');
        //        queryStructure.Fields.AddRange(Fieldsp);
        //        // Get From  Tables
        //        queryStructure.EntitiesString = sp[1];
        //        string[] Tablesdsp = sp[1].Split(',');
        //        queryStructure.Entities.AddRange(Tablesdsp);

        //        if (GetSchemaName() == null)
        //        {
        //            qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
        //        }
        //        else
        //            qrystr += queryStructure.FieldsString + $" from {GetSchemaName().ToLower()}." + queryStructure.EntitiesString;

        //        qrystr += Environment.NewLine;

        //        if (Filter != null)
        //        {
        //            if (Filter.Count > 0)
        //            {
        //                if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
        //                {
        //                    qrystr += Environment.NewLine;
        //                    if (FoundWhere == false)
        //                    {
        //                        qrystr += " where " + Environment.NewLine;
        //                        FoundWhere = true;
        //                    }

        //                    foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)))
        //                    {
        //                        if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
        //                        {
        //                            //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
        //                            if (item.Operator.ToLower() == "between")
        //                            {
        //                                qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + $" and  {ParameterDelimiter}p_" + item.FieldName + "1 " + Environment.NewLine;
        //                            }
        //                            else
        //                            {
        //                                qrystr += item.FieldName + " " + item.Operator + $" {ParameterDelimiter}p_" + item.FieldName + " " + Environment.NewLine;
        //                            }

        //                        }



        //                    }
        //                }
        //            }
        //        }
        //        if (originalquery.ToLower().Contains("where"))
        //        {
        //            qrystr += Environment.NewLine;

        //            string[] whereSeparators = new string[] { "where", "group by", "having", "order by" };

        //            string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
        //            queryStructure.WhereCondition = spwhere[0];
        //            if (FoundWhere == false)
        //            {
        //                qrystr += " where " + Environment.NewLine;
        //                FoundWhere = true;
        //            }
        //            qrystr += spwhere[1];
        //            qrystr += Environment.NewLine;



        //        }
        //        if (originalquery.ToLower().Contains("group by"))
        //        {
        //            string[] groupbySeparators = new string[] { "group by", "having", "order by" };

        //            string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
        //            queryStructure.GroupbyCondition = groupbywhere[1];
        //            qrystr += " group by " + groupbywhere[1];
        //            qrystr += Environment.NewLine;
        //        }
        //        if (originalquery.ToLower().Contains("having"))
        //        {
        //            string[] havingSeparators = new string[] { "having", "order by" };

        //            string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
        //            queryStructure.HavingCondition = havingywhere[1];
        //            qrystr += " having " + havingywhere[1];
        //            qrystr += Environment.NewLine;
        //        }
        //        if (originalquery.ToLower().Contains("order by"))
        //        {
        //            string[] orderbySeparators = new string[] { "order by" };

        //            string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
        //            queryStructure.OrderbyCondition = orderbywhere[1];
        //            qrystr += " order by " + orderbywhere[1];

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
        //    }
        //    return qrystr;
        //}
        /// <summary>
        /// Retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// This method supports both direct table queries and custom queries. It uses dynamic SQL generation and can adapt to different database types. The method also converts the retrieved DataTable to a list of objects based on the entity's structure and type.
        /// </remarks>
        /// <returns>An object representing the data retrieved, which could be a list or another type based on the entity structure.</returns>
        /// <exception cref="Exception">Catches and logs any exceptions that occur during the data retrieval process.</exception>
        public virtual IBindingList GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;
            bool IsQuery = false;
            string inname = "";
            string qrystr = "select * from ";
            SetObjects(EntityName);
            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.ToLower().Contains("select") && !EntityName.ToLower().Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    qrystr = GetTableName(qrystr.ToLower());
                    inname = EntityName;
                }
                else
                {
                    EntityName = GetTableName(EntityName);
                    string[] stringSeparators = new string[] { " from ", " where ", " group by ", " order by " };
                    string[] sp = EntityName.ToLower().Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }

            }
            EntityStructure ent = GetEntityStructure(inname);
            if (ent != null)
            {
                if (!string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    qrystr = ent.CustomBuildQuery;
                    IsQuery = true;
                }
                else
                    IsQuery = false;
            }
            qrystr = BuildQuery(qrystr, Filter);
            try
            {
                if (enttype == null)
                {
                    enttype = GetEntityType(inname);
                }
                IDataAdapter adp = GetDataAdapter(qrystr, Filter);
                DataSet dataSet = new DataSet();
                // Temporarily disable constraints during fill operation
                dataSet.EnforceConstraints = false;
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];
                Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
                // Prepare the arguments for the constructor
                object[] constructorArgs = new object[] { dt };

                // Create an instance of UnitOfWork<T> with the specific constructor
                // Dynamically handle the instance since we can't cast to a specific IUnitofWork<T> at compile time
                object uowInstance = Activator.CreateInstance(uowGenericType, constructorArgs);
                return (IBindingList)uowInstance;//DMEEditor.Utilfunction.ConvertTableToList(dt,GetEntityStructure(EntityName),GetEntityType(EntityName));
            }

            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
        }
        /// <summary>
        /// Retrieves data for a specified entity from the database with pagination support.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <returns>A PagedResult object containing the data and pagination metadata.</returns>
        public virtual PagedResult GetEntity(string EntityName, List<AppFilter> Filter, int pageNumber, int pageSize)
        {
            ErrorObject.Flag = Errors.Ok;

            try
            {
                // Validate parameters
                if (pageNumber < 1)
                    pageNumber = 1;

                if (pageSize < 1)
                    pageSize = 20; // Set a reasonable default

                string entityNameToUse = EntityName?.Trim();
                bool isQuery = false;
                string inname = "";
                string baseQuery = "";

                // Step 1: Determine if we're dealing with a table name or a query
                if (string.IsNullOrEmpty(entityNameToUse))
                {
                    DMEEditor.AddLogMessage("Fail", "Entity name cannot be null or empty", DateTime.Now, 0, "", Errors.Failed);
                    return null;
                }

                if (entityNameToUse.ToLower().Contains("select") && entityNameToUse.ToLower().Contains("from"))
                {
                    // This is a custom query
                    isQuery = true;
                    baseQuery = entityNameToUse;

                    // Extract the entity name from the query for metadata purposes
                    string[] stringSeparators = new string[] { " from ", " where ", " group by ", " order by " };
                    string[] parts = entityNameToUse.ToLower().Split(stringSeparators, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        inname = parts[1].Trim();
                    }
                }
                else
                {
                    // This is a table name
                    baseQuery = $"SELECT * FROM {entityNameToUse}";
                    inname = entityNameToUse;
                }

                // Step 2: Get entity structure for metadata
                EntityStructure ent = GetEntityStructure(inname);
                if (ent != null && !string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    baseQuery = ent.CustomBuildQuery;
                    isQuery = true;
                }

                // Step 3: Build the query with filters
                string countQuery = "";
                string finalQuery = "";

                if (isQuery && !baseQuery.ToLower().Contains("order by"))
                {
                    // When processing a complex query without ORDER BY, we need to ensure proper pagination
                    // by adding a default ordering, ideally by primary key
                    string orderByClause = "";
                    if (ent != null && ent.PrimaryKeys != null && ent.PrimaryKeys.Count > 0)
                    {
                        orderByClause = $" ORDER BY {GetFieldName(ent.PrimaryKeys[0].fieldname)}";
                    }

                    baseQuery += orderByClause;
                }

                // Build the main query with filters
                finalQuery = BuildQuery(baseQuery, Filter);

                // Create a count query to get total records (for pagination metadata)
                if (!isQuery)
                {
                    // Simple table query - we can COUNT(*)
                    countQuery = $"SELECT COUNT(*) FROM {GetTableName(inname.ToLower())}";

                    // Add WHERE clause if filters exist
                    if (Filter != null && Filter.Count > 0)
                    {
                        string whereClause = ExtractWhereClause(finalQuery);
                        if (!string.IsNullOrEmpty(whereClause))
                        {
                            countQuery += $" {whereClause}";
                        }
                    }
                }

                // Step 4: Apply database-specific pagination
                // Get the appropriate paging syntax for this database type
                string pagingSyntax = RDBMSHelper.GetPagingSyntax(DatasourceType, pageNumber, pageSize);
                string pagingQuery = $"{finalQuery} {pagingSyntax}";

                // Step 5: Execute query and get result
                int totalRecords = 0;

                // Get total count if count query is available
                if (!string.IsNullOrEmpty(countQuery))
                {
                    try
                    {
                        var countResult = GetScalar(countQuery);
                        totalRecords = Convert.ToInt32(countResult);
                    }
                    catch (Exception countEx)
                    {
                        DMEEditor.AddLogMessage("Warning", $"Could not get total record count: {countEx.Message}", DateTime.Now, 0, "", Errors.Warning);
                        // Continue with query execution even if count fails
                    }
                }

                // Execute the main query with pagination
                IDataAdapter adp = GetDataAdapter(pagingQuery, Filter);
                DataSet dataSet = new DataSet();

                try
                {
                    dataSet.EnforceConstraints = false;
                    adp.Fill(dataSet);
                }
                catch (Exception fillEx)
                {
                    DMEEditor.AddLogMessage("Fail", $"Error executing paginated query: {fillEx.Message}", DateTime.Now, 0, pagingQuery, Errors.Failed);
                    throw;
                }

                DataTable dt = dataSet.Tables[0];

                // Step 6: Create and return the paged result
                if (enttype == null)
                {
                    enttype = GetEntityType(inname);
                }

                // Create the result object with pagination metadata
                Type uowGenericType = typeof(ObservableBindingList<>).MakeGenericType(enttype);
                object[] constructorArgs = new object[] { dt };
                object data = Activator.CreateInstance(uowGenericType, constructorArgs);

                var result = new PagedResult
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 0,
                    HasNextPage = pageNumber * pageSize < totalRecords,
                    HasPreviousPage = pageNumber > 1
                };

                return result;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting paginated entity data: {ex.Message}", DateTime.Now, 0, "", Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Asynchronously retrieves data for a specified entity from the database, with the option to apply filters.
        /// </summary>
        /// <param name="EntityName">The name of the entity (table) to retrieve data from.</param>
        /// <param name="Filter">A list of filters to apply to the query.</param>
        /// <remarks>
        /// This method is an asynchronous wrapper around GetEntity, providing the same functionality but in an async manner. It is particularly useful for operations that might take a longer time to complete, ensuring that the application remains responsive.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation, which, when completed, will return an object representing the data retrieved.</returns>
        public virtual Task<IBindingList> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<IBindingList>)GetEntity(EntityName, Filter);
        }
        public virtual IErrorsInfo CreateEntities(List<EntityStructure> entities)
        {
            try
            {
                foreach (var item in entities)
                {
                    try
                    {
                        CreateEntityAs(item);
                    }
                    catch (Exception ex)
                    {
                        ErrorObject.Flag = Errors.Failed;
                        ErrorObject.Message = ex.Message;
                        DMEEditor.AddLogMessage("Fail", $"Could not Create Entity {item.EntityName}" + ex.Message, DateTime.Now, -1, ex.Message, Errors.Failed);
                    }

                }
            }
            catch (Exception ex1)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex1.Message;
                DMEEditor.AddLogMessage("Fail", " Could not Complete Create Entities" + ex1.Message, DateTime.Now, -1, ex1.Message, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }
        public virtual IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            SetObjects(EntityName);

            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            if (UploadData != null)
            {
                IList<object> srcList = null;


                //           DMTypeBuilder.CreateNewObject(DMEEditor, null, srcentitystructure.EntityName, SourceFields);
                if (UploadData.GetType().FullName.Contains("DataTable"))
                {
                    srcList = DMEEditor.Utilfunction.GetListByDataTable((DataTable)UploadData, DMTypeBuilder.MyType, DataStruct);

                }
                else
                 if (UploadData.GetType().FullName.Contains("ObservableBindingList"))
                {
                    IBindingListView t = (IBindingListView)UploadData;
                    srcList = new List<object>();

                    foreach (var item in t)
                    {
                        srcList.Add((object)item);
                    }

                }
                else
                if (UploadData.GetType().FullName.Contains("List"))
                {
                    srcList = (IList<object>)UploadData;

                }
                else
                if (UploadData.GetType().FullName.Contains("IEnumerable"))
                {
                    srcList = (IList<object>)UploadData;
                }





                #region "Update Code"

                ErrorObject.Flag = Errors.Ok;

                string str = "";
                string errorstring = "";
                int CurrentRecord = 0;
                DMEEditor.ETL.CurrentScriptRecord = 0;
                DMEEditor.ETL.ScriptCount += srcList.Count;
                int highestPercentageReached = 0;
                int numberToCompute = DMEEditor.ETL.ScriptCount;
                try
                {
                    if (srcList != null)
                    {
                        numberToCompute = srcList.Count;
                        // int i = 0;

                        for (int i = 0; i < srcList.Count; i++)
                        {
                            try
                            {
                                object r = srcList[i];

                                DMEEditor.ErrorObject = InsertEntity(EntityName, r);
                                CurrentRecord = i;


                                string msg = "";
                                //int rowsUpdated = command.ExecuteNonQuery();
                                int percentComplete = (int)((float)CurrentRecord / (float)numberToCompute * 100);
                                if (percentComplete > highestPercentageReached)
                                {
                                    highestPercentageReached = percentComplete;

                                }
                                PassedArgs args = new PassedArgs
                                {
                                    CurrentEntity = EntityName,
                                    DatasourceName = DatasourceName,
                                    DataSource = this,
                                    EventType = "UpdateEntity",
                                };
                                args.ParameterInt1 = percentComplete;
                                //         UpdateEvents(EntityName, msg, highestPercentageReached, CurrentRecord, numberToCompute, this);
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { Messege = msg, ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = null };
                                    progress.Report(ps);
                                }
                                //   PassEvent?.Invoke(this, args);
                                //   DMEEditor.RaiseEvent(this, args);
                            }
                            catch (Exception er)
                            {
                                string msg = $"Fail to I/U/D  Record {i} to {EntityName} ";
                                if (progress != null)
                                {
                                    PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = msg };
                                    progress.Report(ps);
                                }
                                DMEEditor.AddLogMessage("Fail", msg, DateTime.Now, i, EntityName, Errors.Failed);
                            }
                        }
                        DMEEditor.ETL.CurrentScriptRecord = DMEEditor.ETL.ScriptCount;
                        //command.Dispose();
                        DMEEditor.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);


                    }


                }
                catch (Exception ex)
                {
                    ErrorObject.Ex = ex;
                    command.Dispose();


                }
                #endregion
            }
            return ErrorObject;
        }
        #endregion
        #region "Get Entity Structure"
        // <summary>
        /// Retrieves the detailed structure of an entity, including its fields, primary keys, and relationships.
        /// It optionally refreshes the entity structure if the 'refresh' parameter is true.
        /// </summary>
        /// <param name="fnd">The entity structure to be filled or refreshed.</param>
        /// <param name="refresh">Boolean flag indicating whether to refresh the entity's metadata.</param>
        /// <returns>The updated or refreshed EntityStructure object.</returns>
        public virtual EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            EntityStructure retval = new EntityStructure();

            if (Entities.Count == 0)
            {
                GetEntitesList();
            }
            retval = Entities.FirstOrDefault(d => d.EntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase));
            //if (retval == null)
            //{
            //    List<EntityStructure> ls = Entities.Where(d => !string.IsNullOrEmpty(d.OriginalEntityName)).ToList();
            //    retval = ls.Where(d => d.OriginalEntityName.Equals(EntityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            //}

            if (retval == null)
            {
                retval = new EntityStructure();
                refresh = true;
                retval.DataSourceID = DatasourceName;
                retval.EntityName = EntityName;
                retval.DatasourceEntityName = EntityName;
                retval.Caption = EntityName;

                if (RDBMSHelper.IsSqlStatementValid(EntityName))
                {
                    retval.Viewtype = ViewType.Query;
                    retval.CustomBuildQuery = EntityName;
                }
                else
                {
                    retval.Viewtype = ViewType.Table;
                    retval.CustomBuildQuery = null;
                }
                refresh = true;
            }



            return GetEntityStructure(retval, refresh);
        }
        private bool GetBooleanField(DataRow r, string fieldName)
        {
            try
            {
                return r.Field<bool>(fieldName);
            }
            catch
            {
                return false;
            }
        }

        private bool IsNumericType(string fieldType)
        {
            return fieldType == "System.Decimal" || fieldType == "System.Float" || fieldType == "System.Double";
        }
        // helper to read a typed column only if it exists (otherwise return default)
        private static T SafeField<T>(DataRow row, string colName, T defaultValue = default)
        {
            if (row.Table.Columns.Contains(colName) && !row.IsNull(colName))
                return row.Field<T>(colName);
            return defaultValue;
        }

        public virtual EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
        {
            DataTable tb = new DataTable();
            string entname = fnd.EntityName;
            if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
            {
                fnd.DatasourceEntityName = fnd.EntityName;
            }
            //if (fnd.Created == false && fnd.Viewtype!= ViewType.Table)
            //{
            //    fnd.Created = false;
            //    fnd.Drawn = false;
            //    fnd.Editable = true;
            //    return fnd;

            //}
            if (refresh)
            {
                if (!fnd.EntityName.Equals(fnd.DatasourceEntityName, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(fnd.DatasourceEntityName))
                {
                    entname = fnd.DatasourceEntityName;
                }
                if (string.IsNullOrEmpty(fnd.DatasourceEntityName))
                {
                    fnd.DatasourceEntityName = entname;
                }
                if (string.IsNullOrEmpty(fnd.Caption))
                {
                    fnd.Caption = entname;
                }
                //fnd.DataSourceID = DatasourceName;
                //  fnd.EntityName = EntityName;
                if (fnd.Viewtype == ViewType.Query)
                {
                    tb = GetTableSchema(fnd.CustomBuildQuery, true);
                }
                else
                {

                    tb = GetTableSchema(entname, false);
                }
                if (tb.Rows.Count > 0)
                {
                    fnd.Fields = new List<EntityField>();
                    fnd.PrimaryKeys = new List<EntityField>();
                    DataRow rt = tb.Rows[0];
                    fnd.IsCreated = true;
                    fnd.EntityType = EntityType.Table;
                    fnd.Editable = false;
                    fnd.Drawn = true;
                    foreach (DataRow r in rt.Table.Rows)
                    {
                        EntityField x = new EntityField();
                        try
                        {
                            x.fieldname = SafeField<string>(r, "ColumnName");
                            x.fieldtype = SafeField<Type>(r, "DataType")?.ToString() ?? "System.String";

                            // Oracle FLOAT → .NET mapping
                            if (DatasourceType == DataSourceType.Oracle
                             && x.fieldtype.Equals("FLOAT", StringComparison.OrdinalIgnoreCase))
                            {
                                int precision = GetFloatPrecision(x.EntityName, x.fieldname);
                                x.fieldtype = MapOracleFloatToDotNetType(precision);
                            }

                            x.Size1 = SafeField<int>(r, "ColumnSize");
                            x.IsAutoIncrement = SafeField<bool>(r, "IsAutoIncrement");
                            x.AllowDBNull = SafeField<bool>(r, "AllowDBNull");
                            x.IsIdentity = SafeField<bool>(r, "IsIdentity");
                            x.IsKey = SafeField<bool>(r, "IsKey");
                            x.IsUnique = SafeField<bool>(r, "IsUnique");
                            x.OrdinalPosition = SafeField<int>(r, "OrdinalPosition");  // no more exception

                            x.IsReadOnly = SafeField<bool>(r, "IsReadOnly");
                            x.IsRowVersion = SafeField<bool>(r, "IsRowVersion");
                            x.IsLong = SafeField<bool>(r, "IsLong");
                            x.DefaultValue = SafeField<string>(r, "DefaultValue", null);
                            x.Expression = SafeField<string>(r, "Expression", null);
                            x.BaseTableName = SafeField<string>(r, "BaseTableName", null);
                            x.BaseColumnName = SafeField<string>(r, "BaseColumnName", null);

                            // MaxLength is same as ColumnSize
                            x.MaxLength = x.Size1;
                            x.IsFixedLength = SafeField<bool>(r, "IsFixedLength");
                            x.IsHidden = SafeField<bool>(r, "IsHidden");

                            // NumericPrecision/Scale only if the schema provides them
                            if (IsNumericType(x.fieldtype))
                            {
                                x.NumericPrecision = SafeField<short>(r, "NumericPrecision");
                                x.NumericScale = SafeField<short>(r, "NumericScale");
                            }
                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage(
                              "Fail",
                              $"Error creating Field metadata for {entname}.{x.fieldname}: {ex.Message}",
                              DateTime.Now, 0, entname, Errors.Failed
                            );
                        }

                        if (x.IsKey)
                        {
                            fnd.PrimaryKeys.Add(x);
                        }
                        fnd.Fields.Add(x);
                    }
                    if (fnd.Viewtype == ViewType.Table)
                    {
                        if ((fnd.Relations.Count == 0) || refresh)
                        {
                            fnd.Relations = new List<RelationShipKeys>();
                            fnd.Relations = GetEntityforeignkeys(entname, Dataconnection.ConnectionProp.SchemaName);
                        }
                    }

                    //   EntityStructure exist = Entities.Where(d => d.EntityName.Equals(fnd.EntityName,StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    int idx = Entities.FindIndex(o => o.EntityName.Equals(fnd.EntityName, StringComparison.InvariantCultureIgnoreCase));
                    if (idx == -1)
                    {
                        Entities.Add(fnd);
                    }
                    else
                    {

                        Entities[idx].IsCreated = true;
                        Entities[idx].Editable = false;
                        Entities[idx].Drawn = true;
                        Entities[idx].Fields = fnd.Fields;
                        Entities[idx].Relations = fnd.Relations;
                        Entities[idx].PrimaryKeys = fnd.PrimaryKeys;

                    }
                }
                else
                {
                    fnd.IsCreated = false;
                }

            }
            return fnd;
        }
        /// <summary>
        /// <summary>
        /// Retrieves the structure of a specific entity (e.g., a database table) using a database connection.
        /// </summary>
        /// <param name="connection">Database connection to access the schema.</param>
        /// <param name="tableName">The name of the table for which the structure is required.</param>
        /// <returns>An EntityStructure representing the table's schema.</returns>
        public EntityStructure GetEntityStructureForQuery(DbConnection connection, string query)
        {
            EntityStructure entityStructure = new EntityStructure();
            // Assuming entityStructure properties are appropriately set

            DataTable schemaTable = new DataTable();
            using (DbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    schemaTable = reader.GetSchemaTable();
                }
            }

            // Now you can map schema information to your EntityStructure or EntityField instances
            // ...

            return GetEntityStructure(schemaTable);
        }
        /// <summary>
        /// Creates an entity structure from a given schema table.
        /// </summary>
        /// <param name="schemaTable">A DataTable containing schema information.</param>
        /// <returns>The constructed EntityStructure based on the schema table.</returns>
        private EntityStructure GetEntityStructure(DataTable schemaTable)
        {
            EntityStructure entityStructure = new EntityStructure();
            string columnNameKey = "COLUMN_NAME";
            string dataTypeKey = "DATA_TYPE";
            string maxLengthKey = "CHARACTER_MAXIMUM_LENGTH";
            string numericPrecisionKey = "NUMERIC_PRECISION";
            string numericScaleKey = "NUMERIC_SCALE";
            string isNullableKey = "IS_NULLABLE";
            string isAutoIncrementKey = "AUTOINCREMENT";
            string isKeyKey = "PRIMARY_KEY";
            string isUniqueKey = "UNIQUE";
            // Add more keys for other properties

            foreach (DataRow row in schemaTable.Rows)
            {
                EntityField field = new EntityField();
                field.fieldname = row[columnNameKey].ToString();
                field.fieldtype = row[dataTypeKey].ToString();
                field.Size1 = Convert.ToInt32(row[maxLengthKey]);
                field.NumericPrecision = Convert.ToInt16(row[numericPrecisionKey]);
                field.NumericScale = Convert.ToInt16(row[numericScaleKey]);
                field.AllowDBNull = row[isNullableKey].ToString() == "YES";
                field.IsAutoIncrement = row[isAutoIncrementKey].ToString() == "YES";
                field.IsKey = row[isKeyKey].ToString() == "YES";
                field.IsUnique = row[isUniqueKey].ToString() == "YES";
                // Map other schema properties to the EntityField instance
                // ...

                entityStructure.Fields.Add(field);
            }
            return entityStructure;
        }
        public EntityStructure GetEntityStructure(DbConnection connection, string tableName)
        {
            EntityStructure entityStructure = new EntityStructure();
            entityStructure.EntityName = tableName;

            DataTable schemaTable = connection.GetSchema("Columns", new[] { null, null, tableName, null });




            return GetEntityStructure(schemaTable);
        }
        #endregion "Get Entity Structure"

        public virtual Type GetEntityType(string EntityName)
        {
            EntityStructure x = GetEntityStructure(EntityName);
            DMTypeBuilder.CreateNewObject(DMEEditor, DatasourceName, DatasourceName, EntityName, x.Fields);
            enttype = DMTypeBuilder.MyType;
            return DMTypeBuilder.MyType;
        }
        /// <summary>
        /// Retrieves a list of all entity names (like tables) from the database.
        /// </summary>
        /// <remarks>
        /// This method queries the database to get a list of all tables. It handles different schema configurations
        /// and adapts to various database types as defined in the Dataconnection's properties.
        /// </remarks>
        /// <returns>A List of strings, each representing the name of a table in the database.</returns>
        public virtual List<string> GetEntitesList()
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            IDbDataAdapter adp;
            DataTable tb = new DataTable();
            try
            {
                if (Dataconnection != null)
                {
                    if (Dataconnection.ConnectionProp != null)
                    {
                        if (Dataconnection.ConnectionProp.SchemaName != null)
                        {
                            if (Dataconnection.ConnectionProp.SchemaName.Contains(','))
                            {
                                string[] schemas = Dataconnection.ConnectionProp.SchemaName.Split(',');
                            }
                        }
                    }
                }
                string sql = GetListofEntitiesSql;
                if (String.IsNullOrEmpty(sql))
                {
                    sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getlistoftables, null, Dataconnection.ConnectionProp.SchemaName, null, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                }

                adp = GetDataAdapter(sql, null);
                adp.Fill(ds);
#if DEBUG
                DMEEditor.AddLogMessage("Beep", $"Get Tables List Query {sql}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                Debug.WriteLine($" -- Get Tables List Query {sql}");
#endif

                tb = ds.Tables[0];
                EntitiesNames = new List<string>();
                int i = 0;
                foreach (DataRow row in tb.Rows)
                {
                    EntitiesNames.Add(row.Field<string>("TABLE_NAME").ToUpper());

                    i += 1;
                }
                List<string> EntitiesnotinEntitiesNames = new List<string>();
                if (Entities.Count > 0)
                {
                    EntitiesnotinEntitiesNames = Entities.Where(p => !EntitiesNames.Contains(p.EntityName)).Select(p => p.EntityName).ToList();
                    foreach (string item in EntitiesnotinEntitiesNames)
                    {
                        int idx = Entities.FindIndex(p => p.EntityName == item);
                        Entities[idx].IsCreated=false;
                        Entities[idx].EntityType= EntityType.InMemory;
                        Entities[idx].Drawn = false;
                        // update EntitiesNames and add to the list
                        if (!EntitiesNames.Contains(item))
                        {
                            EntitiesNames.Add(item);
                        }
                    }
                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting  Table List ({ex.Message})", DateTime.Now, 0, DatasourceName, Errors.Failed);

            }
            tb = null;
            adp = null;
            return EntitiesNames;



        }
        // <summary>
        /// Adds a new entity to the system.
        /// </summary>
        /// <param name="entityName">The name of the new entity.</param>
        /// <param name="schemaname">The database schema name associated with the entity.</param>
        /// <returns>A string message indicating the result of the operation.</returns>
        /// <remarks>
        /// This method validates the input and adds the entity to the collection if it doesn't already exist.
        /// </remarks>
        public virtual string AddNewEntity(string entityName, string schemaname)
        {
            if (entityName == null)
            {
                return "Entity Name is null";
            }
            if (schemaname == null)
            {
                return "schema Name is null";
            }
            if (!string.IsNullOrEmpty(schemaname))
            {
                int ent = Entities.FindIndex(p => p.EntityName.ToUpper() == entityName.ToUpper());
                if (ent > -1)
                {
                    return "Entity Exist";
                }

            }
            EntityStructure entity = new EntityStructure();
            entity.EntityName = entityName;
            entity.SchemaOrOwnerOrDatabase = schemaname;
            Entities.Add(entity);
            return null;
        }
        /// <summary>
        /// Retrieves the schema name from the connection properties.
        /// </summary>
        /// <returns>The schema name as a string.</returns>
        /// <remarks>
        /// If the schema name is not explicitly set in the connection properties, defaults are used based on the database type.
        /// </remarks>
        public virtual string GetSchemaName()
        {
            string schemaname = null;

            if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = Dataconnection.ConnectionProp.SchemaName.ToUpper();
            }
            if (Dataconnection.ConnectionProp.DatabaseType == DataSourceType.SqlServer && string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
            {
                schemaname = "dbo";
            }
            return schemaname;
        }
        /// <summary>
        /// Checks if an entity exists in the system.
        /// </summary>
        /// <param name="EntityName">The name of the entity to check.</param>
        /// <returns>True if the entity exists, otherwise false.</returns>
        /// <remarks>
        /// This method checks for the existence of an entity by its name in the Entities collection.
        /// </remarks>
        public bool CheckEntityExist(string EntityName)
        {
            bool retval = false;
            GetEntitesList();
            if (EntitiesNames.Count == 0)
            {
                retval = false;
            }
            if (Entities.Count > 0)
            {
                retval = Entities.Any(p => p.EntityName == EntityName || p.OriginalEntityName == EntityName || p.DatasourceEntityName == EntityName);
            }

            return retval;
        }
        /// <summary>
        /// Retrieves the index of a specific entity in the Entities collection.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <returns>The index of the entity or -1 if not found.</returns>
        public int GetEntityIdx(string entityName)
        {
            if (Entities.Count > 0)
            {
                return Entities.FindIndex(p => p.EntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase) || p.DatasourceEntityName.Equals(entityName, StringComparison.InvariantCultureIgnoreCase));
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// Creates an entity in the database as per the specified structure.
        /// </summary>
        /// <param name="entity">The entity structure to create in the database.</param>
        /// <returns>True if creation is successful, otherwise false.</returns>
        /// <remarks>
        /// This method attempts to create a new entity in the database if it does not already exist.
        /// </remarks>
        public virtual bool CreateEntityAs(EntityStructure entity)
        {
            bool retval = false;
            if (CheckEntityExist(entity.EntityName) == false)
            {
                string createstring = CreateEntity(entity);
                DMEEditor.ErrorObject = ExecuteSql(createstring);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    Entities.Add(entity);
                    EntitiesNames.Add(entity.EntityName);
                    retval = true;
                }
            }


            return retval;
        }
        /// <summary>
        /// Retrieves foreign key relationships for a specific entity.
        /// </summary>
        /// <param name="entityname">The name of the entity to retrieve foreign keys for.</param>
        /// <param name="SchemaName">The database schema name.</param>
        /// <returns>A list of foreign key relationships.</returns>
        /// <remarks>
        /// This method fetches foreign key information for the given entity from the database.
        /// </remarks>
        public virtual List<RelationShipKeys> GetEntityforeignkeys(string entityname, string SchemaName)
        {
            List<RelationShipKeys> fk = new List<RelationShipKeys>();
            ErrorObject.Flag = Errors.Ok;
            try
            {
                List<ChildRelation> ds = GetTablesFKColumnList(entityname, GetSchemaName(), null);
                //-------------------------------
                // Create Parent Record First
                //-------------------------------
                if (ds != null)
                {
                    if (ds.Count > 0)
                    {
                        foreach (ChildRelation r in ds)
                        {
                            RelationShipKeys rfk = new RelationShipKeys
                            {
                                RelatedEntityID = r.parent_table,
                                RelatedEntityColumnID = r.parent_column,
                                EntityColumnID = r.child_column,
                            };
                            try
                            {
                                rfk.RalationName = r.Constraint_Name;
                            }
                            catch (Exception ex)
                            {
                                ErrorObject.Flag = Errors.Failed;
                                ErrorObject.Ex = ex;
                            }
                            fk.Add(rfk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not get forgien key  for {entityname} ({ex.Message})", DateTime.Now, 0, entityname, Errors.Failed);
            }
            return fk;
        }
        /// <summary>
        /// Retrieves a list of child tables related to the specified table.
        /// </summary>
        /// <param name="tablename">The name of the table to find child tables for.</param>
        /// <param name="SchemaName">The database schema name.</param>
        /// <param name="Filterparamters">Additional filter parameters.</param>
        /// <returns>A list of child relations.</returns>
        /// <remarks>
        /// This method provides information about child tables related to a specified table.
        /// </remarks>
        public virtual List<ChildRelation> GetChildTablesList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getChildTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting  child entities for {tablename} ({ex.Message})", DateTime.Now, 0, tablename, Errors.Failed);
                return null;
            }
        }
        /// <summary>
        /// Executes a provided SQL script.
        /// </summary>
        /// <param name="scripts">The script details to execute.</param>
        /// <returns>IErrorsInfo object with information about the execution outcome.</returns>
        /// <remarks>
        /// This method runs an SQL script and provides detailed information about its execution.
        /// </remarks>
        public virtual IErrorsInfo RunScript(ETLScriptDet scripts)
        {
            var t = Task.Run<IErrorsInfo>(() => { return ExecuteSql(scripts.ddl); });
            t.Wait();
            DMEEditor.ErrorObject = t.Result;
            scripts.errormessage = DMEEditor.ErrorObject.Message;

            return DMEEditor.ErrorObject;
        }
        /// <summary>
        /// Generates SQL scripts for creating entities based on their structure.
        /// </summary>
        /// <param name="entities">A list of entities to generate scripts for.</param>
        /// <returns>A list of ETLScriptDet containing the SQL create scripts.</returns>
        /// <remarks>
        /// This method is useful for generating database creation scripts from entity structures.
        /// </remarks>
        public virtual List<ETLScriptDet> GetCreateEntityScript(List<EntityStructure> entities)
        {
            return GetDDLScriptfromDatabase(entities);
        }
        #endregion
        #region "RDBSSource Database Methods"
        // Helper method to extract WHERE clause from a query
        private string ExtractWhereClause(string query)
        {
            string lowerQuery = query.ToLower();
            int wherePos = lowerQuery.IndexOf(" where ");

            if (wherePos >= 0)
            {
                // Find the position after "where"
                int startPos = wherePos + 7; // length of " where "

                // Find the next clause, if any
                int endPos = lowerQuery.Length;
                string[] endClauses = { " group by ", " having ", " order by " };

                foreach (string clause in endClauses)
                {
                    int pos = lowerQuery.IndexOf(clause, startPos);
                    if (pos >= 0 && pos < endPos)
                    {
                        endPos = pos;
                    }
                }

                return "WHERE " + query.Substring(startPos, endPos - startPos).Trim();
            }

            return string.Empty;
        }
        private string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += " " + t1.EntityName + "\n(";

                if (t1.Fields.Count == 0)
                {
                    // Empty fields collection, add error log
                    DMEEditor.AddLogMessage("Fail", $"No fields defined for entity {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                // Filter out fields with empty names before calculating total
                var validFields = t1.Fields.Where(p => !string.IsNullOrEmpty(p.fieldname?.Trim())).ToList();
                int totalValidFields = validFields.Count;

                if (totalValidFields == 0)
                {
                    DMEEditor.AddLogMessage("Fail", $"All field names are empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                    return createtablestring + ")";
                }

                int processedFields = 0;

                foreach (EntityField dbf in t1.Fields)
                {
                    // Skip fields with empty names
                    if (string.IsNullOrEmpty(dbf.fieldname))
                    {
                        DMEEditor.AddLogMessage("Fail", $"Field Name is empty for {t1.EntityName}", DateTime.Now, 0, t1.EntityName, Errors.Failed);
                        continue;
                    }

                    string fieldName = dbf.fieldname;
                    if (DatasourceType == DataSourceType.Mysql)
                    {
                        fieldName = fieldName.Replace(" ", "_");
                        fieldName = "`" + fieldName + "`";
                    }

                    createtablestring += "\n " + fieldName + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";

                    if (dbf.IsAutoIncrement)
                    {
                        string autonumberstring = CreateAutoNumber(dbf);
                        if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                        {
                            createtablestring += autonumberstring;
                        }
                        else
                        {
                            throw new Exception(ErrorObject.Message);
                        }
                    }

                    if (dbf.AllowDBNull == false)
                    {
                        createtablestring += " NOT NULL ";
                    }

                    if (dbf.IsUnique == true)
                    {
                        createtablestring += " UNIQUE ";
                    }

                    processedFields++;

                    // Only add comma if this is not the last valid field
                    if (processedFields < totalValidFields)
                    {
                        createtablestring += ",";
                    }
                }

                // Add primary key constraint if there are primary keys
                if (t1.PrimaryKeys != null && t1.PrimaryKeys.Count > 0)
                {
                    // Add comma before primary key only if we have valid fields
                    if (totalValidFields > 0)
                    {
                        createtablestring += ",";
                    }
                    createtablestring += "\n" + CreatePrimaryKeyString(t1);
                }

                // Close the CREATE TABLE statement
                createtablestring += ")";
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Creating Entity {t1.EntityName} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                createtablestring = "";
            }

            return createtablestring;
        }

        public List<ETLScriptDet> GenerateCreatEntityScript(List<EntityStructure> entities)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            int i = 0;
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First
                foreach (EntityStructure item in entities)
                {
                    ETLScriptDet x = new ETLScriptDet();
                    x.destinationdatasourcename = DatasourceName;

                    x.ddl = CreateEntity(item);
                    x.sourceentityname = item.EntityName;
                    x.sourceDatasourceEntityName = item.DatasourceEntityName;
                    x.scriptType = DDLScriptType.CreateEntity;
                    rt.Add(x);
                    rt.AddRange(CreateForKeyRelationScripts(item));
                    i += 1;
                }
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        public List<ETLScriptDet> GenerateCreatEntityScript(EntityStructure entity)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;

            int i = 0;

            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                // Generate Create Table First

                ETLScriptDet x = new ETLScriptDet();
                x.destinationdatasourcename = DatasourceName;
                x.ddl = CreateEntity(entity);
                x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                x.sourceentityname = entity.EntityName;
                x.scriptType = DDLScriptType.CreateEntity;
                rt.Add(x);
                rt.AddRange(CreateForKeyRelationScripts(entity));
            }
            catch (Exception ex)
            {
                string errmsg = "Error in Generating Script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);

            }
            return rt;

        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(string entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                var t = Task.Run<EntityStructure>(() => { return GetEntityStructure(entity, true); });
                t.Wait();
                EntityStructure entstructure = t.Result;
                entstructure.IsCreated = false;
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    Entities[Entities.FindIndex(x => x.EntityName == entity)] = entstructure;

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Error getting entity structure for {entity}", DateTime.Now, entstructure.Id, entstructure.DataSourceID, Errors.Failed);
                }
                var t2 = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
                t2 = Task.Run<List<ETLScriptDet>>(() => { return CreateForKeyRelationScripts(entstructure); });
                t2.Wait();
                rt.AddRange(t2.Result);
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private List<ETLScriptDet> GetDDLScriptfromDatabase(List<EntityStructure> structureentities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                if (structureentities.Count > 0)
                {
                    var t = Task.Run<List<ETLScriptDet>>(() => { return GenerateCreatEntityScript(structureentities); });
                    t.Wait();
                    rt.AddRange(t.Result);
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting entities from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private string CreatePrimaryKeyString(EntityStructure t1)
        {
            string retval = null;
            try
            {
                if (t1.PrimaryKeys.Count > 0)
                {
                    retval = @" PRIMARY KEY ( ";
                }
                else
                {
                    return string.Empty;
                }

                ErrorObject.Flag = Errors.Ok;
                int i = 0;
                foreach (EntityField dbf in t1.PrimaryKeys)
                {
                    retval += dbf.fieldname + ",";

                    i += 1;
                }
                if (retval.EndsWith(","))
                {
                    retval = retval.Remove(retval.Length - 1, 1);
                }
                retval += ")\n";
                return retval;
            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not  Create Primery Key" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        private string CreateAlterRalationString(EntityStructure t1)
        {
            string retval = "";
            ErrorObject.Flag = Errors.Ok;
            try
            {
                int i = 0;
                foreach (string item in t1.Relations.Select(o => o.RelatedEntityID).Distinct())
                {
                    string forkeys = "";
                    string refkeys = "";
                    foreach (RelationShipKeys fk in t1.Relations.Where(p => p.RelatedEntityID == item))
                    {
                        forkeys += fk.EntityColumnID + ",";
                        refkeys += fk.RelatedEntityColumnID + ",";
                    }
                    i += 1;
                    forkeys = forkeys.Remove(forkeys.Length - 1, 1);
                    refkeys = refkeys.Remove(refkeys.Length - 1, 1);
                    retval += @" ALTER TABLE " + t1.EntityName + " ADD CONSTRAINT " + t1.EntityName + i + r.Next(10, 1000) + "  FOREIGN KEY (" + forkeys + ")  REFERENCES " + item + "(" + refkeys + "); \n";
                }
                if (i == 0)
                {
                    retval = "";
                }
                return retval;
            }

            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, "Could not Create Relation" + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };
        }
        private List<ETLScriptDet> CreateForKeyRelationScripts(EntityStructure entity)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();
            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                if (entity.Relations != null)
                {
                    if (entity.Relations.Count > 0)
                    {
                        string relations = CreateAlterRalationString(entity);
                        string[] rels = relations.Split(';');
                        foreach (string rl in rels)
                        {
                            ETLScriptDet x = new ETLScriptDet();
                            x.destinationdatasourcename = DatasourceName;
                            ds = DMEEditor.GetDataSource(entity.DataSourceID);
                            x.sourceDatasourceEntityName = entity.DatasourceEntityName;
                            x.ddl = rl;
                            x.sourceentityname = entity.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                        }
                        i += 1;
                    }
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);
            }
            return rt;
        }
        private List<ETLScriptDet> CreateForKeyRelationScripts(List<EntityStructure> entities)
        {
            List<ETLScriptDet> rt = new List<ETLScriptDet>();

            try
            {
                int i = 0;
                IDataSource ds;
                // Generate Forign Keys
                foreach (EntityStructure item in entities)
                {
                    if (item.Relations != null)
                    {
                        if (item.Relations.Count > 0)
                        {
                            ETLScriptDet x = new ETLScriptDet();
                            x.destinationdatasourcename = item.DataSourceID;
                            ds = DMEEditor.GetDataSource(item.DataSourceID);
                            x.sourceDatasourceEntityName = item.DatasourceEntityName;
                            x.ddl = CreateAlterRalationString(item);
                            x.sourceentityname = item.EntityName;
                            x.scriptType = DDLScriptType.AlterFor;
                            rt.Add(x);
                            //alteraddForignKey.Add(x);
                            i += 1;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting For. Keys from Database ({ex.Message})", DateTime.Now, -1, "CopyDatabase", Errors.Failed);

            }
            return rt;
        }
        public virtual string CreateAutoNumber(EntityField f)
        {
            ErrorObject.Flag = Errors.Ok;
            string AutnumberString = "";
            try
            {
                if (f.IsAutoIncrement)
                {
                    switch (Dataconnection.ConnectionProp.DatabaseType)
                    {
                        //case DataSourceType.Excel:
                        //    break;
                        case DataSourceType.Mysql:
                            AutnumberString = "NULL AUTO_INCREMENT";
                            break;
                        case DataSourceType.Oracle:
                            AutnumberString = " GENERATED BY DEFAULT ON NULL AS IDENTITY";// "CREATE SEQUENCE " + f.fieldname + "_seq MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 1 ";
                            break;
                        case DataSourceType.SqlCompact:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        case DataSourceType.SqlLite:
                            AutnumberString = "AUTOINCREMENT";
                            break;
                        case DataSourceType.SqlServer:
                            AutnumberString = "IDENTITY(1,1)";
                            break;
                        default:
                            AutnumberString = "";
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Creating Auto number Field {f.EntityName} and {f.fieldname} ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);

            }
            return AutnumberString;
        }
        private string CreateEntity(EntityStructure t1)
        {
            string createtablestring = null;
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                createtablestring = GenerateCreateEntityScript(t1);
            }
            catch (System.Exception ex)
            {
                createtablestring = null;
                DMEEditor.AddLogMessage("Fail", $"Error in  Creating Table " + t1.EntityName + "   ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
            }
            return createtablestring;
        }
        public virtual string GetInsertString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            string Insertstr = "INSERT INTO " + EntityName + " (";
            Insertstr = GetTableName(Insertstr.ToLower());
            string Valuestr = ") VALUES (";

            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
                if (!(item.IsAutoIncrement))
                {
                    string fieldName = GetFieldName(item.fieldname);
                    string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);

                    Insertstr += $"{fieldName},";
                    Valuestr += $"{ParameterDelimiter}p_" + paramName + ",";
                }

                t += 1;
            }
            Insertstr = Insertstr.Remove(Insertstr.Length - 1);
            Valuestr = Valuestr.Remove(Valuestr.Length - 1);
            Valuestr += ")";
            return Insertstr + Valuestr;
        }
        public virtual string GetUpdateString(string EntityName, EntityStructure DataStruct)
        {
            List<EntityField> SourceEntityFields = new List<EntityField>();
            List<EntityField> DestEntityFields = new List<EntityField>();

            string Updatestr = @"Update " + EntityName + " set " + Environment.NewLine;
      //      Updatestr = GetTableName(Updatestr.ToLower());
            string Valuestr = "";
            // i want a new list of fields that are the primary key at the end of the list
            UpdateFieldSequnce = new List<EntityField>();
            for (int i = 0; i < DataStruct.Fields.Count; i++)
            {
                EntityField field = DataStruct.Fields[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == field.fieldname))
                {
                    UpdateFieldSequnce.Add(field);
                }
            }
            for (int i = 0; i < UpdateFieldSequnce.Count; i++)
            {
                EntityField item= UpdateFieldSequnce[i];
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    string fieldName = GetFieldName(item.fieldname);
                    string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);

                    Updatestr += $"{GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName},";
                }
            }

           

            Updatestr = Updatestr.Remove(Updatestr.Length - 1); // Remove the trailing comma
            UpdateFieldSequnce.AddRange(DataStruct.PrimaryKeys);
            Updatestr += @" where " + Environment.NewLine;
            int t = 1;
            for (int i = 0; i < DataStruct.PrimaryKeys.Count; i++)
            {
                EntityField item = DataStruct.PrimaryKeys[i];
                string fieldName = GetFieldName(item.fieldname);
                string paramName = Regex.Replace(item.fieldname, @"\s+", "_");
                if (usedParameterNames.Contains(paramName))
                {
                    paramName = usedParameterNames.FirstOrDefault(p => p.Contains(paramName));
                }
                else
                {
                    // Ensure the field name and parameter name are within the Oracle identifier length limit
                    if (fieldName.Length > 30)
                    {
                        fieldName = fieldName.Substring(0, 30);
                    }

                    if (paramName.Length > 30)
                    {
                        paramName = paramName.Substring(0, 30);
                    }

                    // Ensure unique parameter names
                    int suffix = 1;
                    string originalParamName = paramName;
                    while (usedParameterNames.Contains(paramName))
                    {
                        paramName = originalParamName + "_" + suffix++;
                    }
                    usedParameterNames.Add(paramName);
                }

                if (t == 1)
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName}";
                }
                else
                {
                    Updatestr += $" and {GetFieldName(item.fieldname)}= {ParameterDelimiter}p_{paramName}";
                }
                t += 1;
            }
           
            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName, EntityStructure DataStruct)
        {
            string deleteStr = $"DELETE FROM {EntityName} WHERE ";
            int t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {
                string fieldName = GetFieldName(item.fieldname);
                string paramName = Regex.Replace(item.fieldname, @"\s+", "_");

                // Ensure the field name and parameter name are within the Oracle identifier length limit
                if (fieldName.Length > 30)
                {
                    fieldName = fieldName.Substring(0, 30);
                }

                if (paramName.Length > 30)
                {
                    paramName = paramName.Substring(0, 30);
                }

                // Ensure unique parameter names
                int suffix = 1;
                string originalParamName = paramName;
                while (usedParameterNames.Contains(paramName))
                {
                    paramName = originalParamName + "_" + suffix++;
                }
                usedParameterNames.Add(paramName);
                if (t > 1)
                {
                    deleteStr += " AND ";
                }
                deleteStr += $"{GetFieldName(item.fieldname)} = {ParameterDelimiter}p_{paramName}";
                t += 1;
            }
            return deleteStr;
        }

        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();

            return dt;

        }
        private string GetUniqueParameterName(string baseName, HashSet<string> usedParameterNames)
        {
            string parameterName = "p_" + Regex.Replace(baseName, @"\s+", "_");
            string uniqueParameterName = parameterName;
            int counter = 1;

            while (usedParameterNames.Contains(uniqueParameterName))
            {
                uniqueParameterName = $"{parameterName}_{counter}";
                counter++;
            }

            usedParameterNames.Add(uniqueParameterName);
            return uniqueParameterName;
        }

        public virtual string GetFieldName(string fieldname)
        {
            string retval = fieldname;
            if (fieldname.IndexOf(" ") != -1)
            {
                if (ColumnDelimiter.Length == 2) //(ColumnDelimiter.Contains("[") || ColumnDelimiter.Contains("]"))
                {

                    retval = $"{ColumnDelimiter[0]}{fieldname}{ColumnDelimiter[1]}";
                }
                else
                {
                    retval = $"{ColumnDelimiter}{fieldname}{ColumnDelimiter}";
                }

            }
            return retval;
        }
        #region "Dapper"
        public virtual List<T> GetData<T>(string sql)
        {
            // DMEEditor.OpenDataSource(ds.DatasourceName);
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                List<T> t = RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();

                return t;
            }
            else
                return null;
        }
        public virtual Task SaveData<T>(string sql, T parameters)
        {
            if (Dataconnection.ConnectionStatus == ConnectionState.Open)
            {
                return RDBMSConnection.DbConn.ExecuteAsync(sql, parameters);
            }
            else
                return null;


        }
        #endregion
        private int GetCtorForAdapter(List<ConstructorInfo> ls)
        {

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 2)
                {
                    if (d[0].ParameterType == System.Type.GetType("System.String"))
                    {
                        if (d[1].ParameterType != System.Type.GetType("System.String"))
                        {
                            return i;
                        }
                    }
                }

                i += 1;
            }
            return i;

        }
        private int GetCtorForCommandBuilder(List<ConstructorInfo> ls)
        {

            int i = 0;
            foreach (ConstructorInfo c in ls)
            {
                ParameterInfo[] d = c.GetParameters();
                if (d.Length == 1)
                {
                    return i;

                }

                i += 1;
            }
            return i;

        }
        public virtual IDbCommand GetDataCommand()
        {
            IDbCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    cmd = RDBMSConnection.DbConn.CreateCommand();
                }
                else
                {
                    cmd = null;

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command, Cannot get DataSource", DateTime.Now, -1, DatasourceName, Errors.Failed);
                }



            }
            catch (Exception ex)
            {

                cmd = null;

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);

            }
            return cmd;
        }
        public virtual IDbDataAdapter GetDataAdapter(string Sql, List<AppFilter> Filter = null)
        {
            IDbDataAdapter adp = null;

            try
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);
                string adtype = Dataconnection.DataSourceDriver.AdapterType;
                string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                string cmdbuildername = driversConfig.CommandBuilderType;
                if (string.IsNullOrEmpty(cmdbuildername))
                {
                    return null;
                }
                Type adcbuilderType = DMEEditor.assemblyHandler.GetType(cmdbuildername);
                List<ConstructorInfo> lsc = DMEEditor.assemblyHandler.GetInstance(adtype).GetType().GetConstructors().ToList(); ;
                List<ConstructorInfo> lsc2 = DMEEditor.assemblyHandler.GetInstance(cmdbuildername).GetType().GetConstructors().ToList(); ;

                ConstructorInfo ctor = lsc[GetCtorForAdapter(lsc)];
                ConstructorInfo BuilderConstructer = lsc2[GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList())];
                ObjectActivator<IDbDataAdapter> adpActivator = GetActivator<IDbDataAdapter>(ctor);
                ObjectActivator<DbCommandBuilder> cmdbuilderActivator = GetActivator<DbCommandBuilder>(BuilderConstructer);

                //create an instance:
                adp = (IDbDataAdapter)adpActivator(Sql, RDBMSConnection.DbConn);
                try
                {
                    DbCommandBuilder cmdBuilder = cmdbuilderActivator(adp);
                    if (Filter != null)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {

                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                            {

                                IDbDataParameter parameter = adp.SelectCommand.CreateParameter();
                                string dr = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue;
                                parameter.ParameterName = "p_" + item.FieldName;
                                if (item.valueType == "System.DateTime")
                                {
                                    parameter.DbType = DbType.DateTime;
                                    parameter.Value = DateTime.Parse(dr).ToShortDateString();

                                }
                                else
                                { parameter.Value = dr; }

                                if (item.Operator.ToLower() == "between")
                                {
                                    IDbDataParameter parameter1 = adp.SelectCommand.CreateParameter();
                                    parameter1.ParameterName = "p_" + item.FieldName + "1";
                                    parameter1.DbType = DbType.DateTime;
                                    string dr1 = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue1;
                                    parameter1.Value = DateTime.Parse(dr1).ToShortDateString();
                                    adp.SelectCommand.Parameters.Add(parameter1);
                                }

                                //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                                adp.SelectCommand.Parameters.Add(parameter);

                            }

                        }
                    }
                    adp.InsertCommand = cmdBuilder.GetInsertCommand(true);
                    adp.UpdateCommand = cmdBuilder.GetUpdateCommand(true);
                    adp.DeleteCommand = cmdBuilder.GetDeleteCommand(true);
                }
                catch (Exception ex)
                {

                    // DMEEditor.AddLogMessage("Fail", $"Error in Creating builder commands {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                }

                adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
                adp.MissingMappingAction = MissingMappingAction.Passthrough;


                ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Creating Adapter for {Sql}- {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                adp = null;
            }

            return adp;
        }
        public virtual DataTable GetTableSchema(string TableName, bool Isquery = false)
        {
            ErrorObject.Flag = Errors.Ok;
            DataTable tb = new DataTable();
            IDataReader reader;
            IDbCommand cmd = GetDataCommand();
            //  EntityStructure entityStructure = GetEntityStructure(TableName, false);
            try
            {
                string cmdtxt = "";
                if (!Isquery)
                {
                    if (!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName) && !string.IsNullOrWhiteSpace(Dataconnection.ConnectionProp.SchemaName))
                    {
                        TableName = Dataconnection.ConnectionProp.SchemaName + "." + TableName;
                    }
                    cmdtxt = "Select * from " + TableName + " where 1=2";
                }
                else
                {
                    cmdtxt = TableName;
                }
                cmd.CommandText = cmdtxt;
                reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

                tb = reader.GetSchemaTable();
                reader.Close();
                cmd.Dispose();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error Fetching Schema for {TableName} -{ex.Message}", DateTime.Now, 0, TableName, Errors.Failed);
            }

            return tb;
        }
        public virtual List<ChildRelation> GetTablesFKColumnList(string tablename, string SchemaName, string Filterparamters)
        {
            ErrorObject.Flag = Errors.Ok;
            DataSet ds = new DataSet();
            try
            {
                string sql = DMEEditor.ConfigEditor.GetSql(Sqlcommandtype.getFKforTable, tablename, SchemaName, Filterparamters, DMEEditor.ConfigEditor.QueryList, DatasourceType);
                if (!string.IsNullOrEmpty(sql) && !string.IsNullOrWhiteSpace(sql))
                {
                    return GetData<ChildRelation>(sql);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Unsuccessfully Retrieve Child tables list {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                return null;
            }
        }
        public virtual string DisableFKConstraints(EntityStructure t1)
        {
            // Disable all foreign key constraints
            return string.Empty;
        }
        public virtual string EnableFKConstraints(EntityStructure t1)
        {
            return string.Empty;
        }
        public static string MapOracleFloatToDotNetType(int precision)
        {
            if (precision <= 24)
            {
                // Fits in .NET float
                return "System.Single";
            }
            else if (precision <= 53)
            {
                // Fits in .NET double
                return "System.Double";
            }
            else
            {
                // Use .NET decimal for higher precision
                return "System.Decimal";
            }
        }
        public int GetFloatPrecision(string tableName, string fieldName)
        {
            int precision = 0;
            string query = $"SELECT DATA_PRECISION FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = '{tableName.ToUpper()}' AND COLUMN_NAME = '{fieldName.ToUpper()}'";


            IDbCommand command = GetDataCommand();
            try
            {
                command.CommandText = query;
                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // Assuming the precision is not null, adjust as needed if it could be
                    precision = reader.GetInt32(0);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine(ex.Message);
            }


            return precision;
        }

        public virtual string GetTableName(string querystring)
        {
            string schname = Dataconnection.ConnectionProp.SchemaName;
            string userid = Dataconnection.ConnectionProp.UserID;
            string schemastring = "";
            if (!string.IsNullOrEmpty(schname) && !schname.Equals(userid, StringComparison.InvariantCultureIgnoreCase))
            {
                if (schname.Length > 0)
                {
                    schemastring = schname + ".";
                }
            }
            else
                schemastring = "";
            if (querystring.IndexOf("select") > 0)
            {
                int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                int wherepos = querystring.IndexOf("where", StringComparison.InvariantCultureIgnoreCase);
                if (wherepos == 0)
                {
                    wherepos = querystring.Length - 1;

                }

                int firstcharindex = querystring.IndexOf(' ', frompos);
                int lastcharindex = querystring.IndexOf(' ', firstcharindex + 2);
                string tablename = querystring.Substring(firstcharindex + 1, lastcharindex - firstcharindex - 1);
                querystring = querystring.Replace(' ' + tablename + ' ', $" {schemastring}{tablename} ");
            }
            else if (querystring.IndexOf("insert") >= 0)
            {
                int intopos = querystring.IndexOf("into", StringComparison.InvariantCultureIgnoreCase);
                string[] instokens = querystring.Split(' ');
                querystring = querystring.Replace(instokens[2], $" {schemastring}{instokens[2]} ");
            }
            else if (querystring.IndexOf("update") >= 0)
            {
                int setpos = querystring.IndexOf("set", StringComparison.InvariantCultureIgnoreCase);
                string[] uptokens = querystring.Split(' ');
                querystring = querystring.Replace(uptokens[1], $" {schemastring}{uptokens[1]} ");
            }
            else if (querystring.IndexOf("delete") >= 0)
            {
                int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                string[] fromtokens = querystring.Split(' ');
                querystring = querystring.Replace(fromtokens[1], $" {schemastring}{fromtokens[2]} ");
            }

            return querystring;
        }
        #endregion
        #region "dispose"
        private bool disposedValue;
        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Closeconnection();
                    Entities = null;
                    EntitiesNames = null;

                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RDBSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        #endregion









    }

}

