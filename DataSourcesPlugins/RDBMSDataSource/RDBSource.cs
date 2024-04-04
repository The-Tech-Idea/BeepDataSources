using System;
using System.Collections.Generic;
using System.Data;
using TheTechIdea.Util;
using TheTechIdea.Logger;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using System.Reflection;
using System.Data.Common;
using static TheTechIdea.Beep.Util;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using DataManagementModels.DriversConfigurations;
using TheTechIdea.Beep.Report;
using System.Data.SqlTypes;
using TheTechIdea.Beep.Helpers;
using System.Diagnostics;

namespace TheTechIdea.Beep.DataBase
{
    public class RDBSource : IRDBSource
    {
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
                ConnectionStatus= RDBMSConnection.OpenConnection();
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
            return Task.Run(()=>GetScalar(query));
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
              
                    using (var command =GetDataCommand())
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
        public virtual object RunQuery(string qrystr)
        {
            ErrorObject.Flag = Errors.Ok;
            IDbCommand cmd = GetDataCommand();
            try
            {
                DataTable dt = new DataTable();
                cmd.CommandText = qrystr;
                dt.Load(cmd.ExecuteReader(CommandBehavior.Default));
                cmd.Dispose();
                if (dt != null)
                {
                    if (dt.Rows.Count == 1)
                    {
                        if (dt.Columns.Count == 1)
                            return dt.Rows[0][0];
                    }
                    else if (dt.Rows.Count > 1)
                    {
                        //EntityStructure st = DMEEditor.Utilfunction.GetEntityStructure(dt);
                        //Type type = DMEEditor.Utilfunction.GetEntityType("tab", st.Fields);
                        return dt; // DMEEditor.Utilfunction.ConvertTableToList(dt, st, type);
                    }
                }
                return null;

            }
            catch (Exception ex)
            {
                cmd.Dispose();
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);
                return null;
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

                            if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                            {
                                parameter.Value = DBNull.Value;
                            }
                            else
                            {
                                // Use TryParse for safer date parsing without exception handling
                                if (DateTime.TryParse(r[item.fieldname].ToString(), out DateTime dateValue))
                                {
                                    parameter.Value = dateValue;
                                }
                                else
                                {
                                    // If parsing fails, assign a DBNull.Value
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
        /// <summary>
        /// Creates parameters for a DELETE database command based on the provided DataRow and EntityStructure.
        /// </summary>
        /// <param name="command">The DELETE database command to add parameters to.</param>
        /// <param name="r">The DataRow containing parameter values for the DELETE operation.</param>
        /// <param name="DataStruct">The EntityStructure defining the primary keys for the DELETE operation.</param>
        /// <returns>The updated IDbCommand with parameters added.</returns>
        private IDbCommand CreateDeleteCommandParameters(IDbCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField item in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    IDbDataParameter parameter = command.CreateParameter();
                    //if (!item.fieldtype.Equals("System.String", StringComparison.InvariantCultureIgnoreCase) && !item.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                    //{
                    //    if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                    //    {
                    //        parameter.Value = Convert.ToDecimal(null);
                    //    }
                    //    else
                    //    {
                    //        parameter.Value = r[item.fieldname];
                    //    }
                    //}
                    //else
                    if (item.fieldtype.Equals("System.DateTime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                        {

                            parameter.Value = DBNull.Value;
                            parameter.DbType = DbType.DateTime;
                        }
                        else
                        {
                            parameter.DbType = DbType.DateTime;
                            try
                            {
                                parameter.Value = DateTime.Parse(r[item.fieldname].ToString());
                            }
                            catch (FormatException formatex)
                            {

                                parameter.Value = SqlDateTime.Null;
                            }
                        }
                    }
                    else
                        parameter.Value = r[item.fieldname];
                    parameter.ParameterName = "p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                    //   parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
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
         
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            string msg = "";
            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, UploadDataRow, DataStruct);
            try
            {
                command = GetDataCommand();
                string updatestring = GetUpdateString(EntityName,  DataStruct);
                command.CommandText = updatestring;
                command = CreateCommandParameters(command,dr, DataStruct);
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
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            var sqlTran = RDBMSConnection.DbConn.BeginTransaction();
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
           
            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, DeletedDataRow, DataStruct);
            try
            {
                string updatestring = GetDeleteString(EntityName, DataStruct);
                command = GetDataCommand();
                command.Transaction = sqlTran;
                command.CommandText = updatestring;

                command = CreateDeleteCommandParameters(command, dr, DataStruct);
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
                sqlTran.Commit();
                command.Dispose();
               

            }
            catch (Exception ex)
            {
                ErrorObject.Ex = ex;
               
                command.Dispose();
                try
                {
                    // Attempt to roll back the transaction.
                    sqlTran.Rollback();
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
            string updatestring="";
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;

            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, InsertedData, DataStruct);
            try
            {
                updatestring = GetInsertString(EntityName, DataStruct);
                command = GetDataCommand();

                command.CommandText = updatestring;
                command = CreateCommandParameters(command, dr, DataStruct);

                int rowsUpdated = command.ExecuteNonQuery();
                if (rowsUpdated > 0)
                {
                    msg = $"Successfully Inserted  Record  to {EntityName} ";
                    DMEEditor.ErrorObject.Message = msg;
                    DMEEditor.ErrorObject.Flag = Errors.Ok;
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
                DataStruct = GetEntityStructure(Entityname, true);
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
        private string BuildQuery(string originalquery, List<AppFilter> Filter)
        {
            string retval;
            string[] stringSeparators;
            string[] sp;
            string qrystr="Select ";
            bool FoundWhere = false;
            QueryBuild queryStructure = new QueryBuild();
            try
            {
                //stringSeparators = new string[] {"select ", " from ", " where ", " group by "," having ", " order by " };
                // Get Selected Fields
                originalquery=GetTableName(originalquery.ToLower());  
                stringSeparators = new string[] { "select", "from" , "where", "group by", "having", "order by" };
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
                    string[] groupbySeparators = new string[] { "group by","having", "order by" };

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
            catch (Exception ex )
            {
                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}- {ex.Message}", DateTime.Now, 0, "Error", Errors.Failed);
            }
            return qrystr;
        }
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
        public virtual object GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;
           
            EntityName = EntityName.ToLower();
            string inname="";
            string qrystr = "select * from ";
            
            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select") && !EntityName.Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    qrystr = GetTableName(qrystr.ToLower());
                    inname = EntityName;
                }else
                {
                    EntityName = GetTableName(EntityName);
                    string[] stringSeparators = new string[] { " from ", " where ", " group by "," order by " };
                    string[] sp = EntityName.Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }
               
            }
            EntityStructure ent = GetEntityStructure(inname);
            if(ent != null)
            {
                if (!string.IsNullOrEmpty(ent.CustomBuildQuery))
                {
                    qrystr = ent.CustomBuildQuery;
                }

            }
           
            qrystr= BuildQuery(qrystr, Filter);
          
            try
            {
                IDataAdapter adp = GetDataAdapter(qrystr,Filter);
                DataSet dataSet = new DataSet();
                adp.Fill(dataSet);
                DataTable dt = dataSet.Tables[0];

                return  DMEEditor.Utilfunction.ConvertTableToList(dt,GetEntityStructure(EntityName),GetEntityType(EntityName));
            }

            catch (Exception ex)
            {
                
                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);
             
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
        public virtual Task<object> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            return (Task<object>)GetEntity(EntityName, Filter);
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
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            if (UploadData != null)
            {
                if (UploadData.GetType().ToString() != "System.Data.DataTable")
                {
                    DMEEditor.AddLogMessage("Fail", $"Please use DataTable for this Method {EntityName}", DateTime.Now, 0, null, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }
                //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
                #region "Update Code"
                //IDbTransaction sqlTran;
                DataTable tb = (DataTable)UploadData;
                // DMEEditor.classCreator.CreateClass();
                //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
                ErrorObject.Flag = Errors.Ok;
                EntityStructure DataStruct = GetEntityStructure(EntityName);
                IDbCommand command = RDBMSConnection.DbConn.CreateCommand();
                string str = "";
                string errorstring = "";
                int CurrentRecord = 0;
                DMEEditor.ETL.CurrentScriptRecord = 0;
                DMEEditor.ETL.ScriptCount += tb.Rows.Count;
                int highestPercentageReached = 0;
                int numberToCompute = DMEEditor.ETL.ScriptCount;
                try
                {
                    if (tb != null)
                    {
                        numberToCompute = tb.Rows.Count;
                        tb.TableName = EntityName;
                        // int i = 0;
                        string updatestring = null;
                        DataTable changes = tb.GetChanges();
                        if(changes != null)
                        {
                            for (int i = 0; i < changes.Rows.Count; i++)
                            {
                                try
                                {
                                    DataRow r = changes.Rows[i];
                                    CurrentRecord = i;
                                    switch (r.RowState)
                                    {
                                        case DataRowState.Unchanged:
                                        case DataRowState.Added:
                                            updatestring = GetInsertString(EntityName, DataStruct);
                                            break;
                                        case DataRowState.Deleted:
                                            updatestring = GetDeleteString(EntityName, DataStruct);
                                            break;
                                        case DataRowState.Modified:
                                            updatestring = GetUpdateString(EntityName, DataStruct);
                                            break;
                                        default:
                                            updatestring = GetInsertString(EntityName, DataStruct);
                                            break;
                                    }
                                    command.CommandText = updatestring;
                                    command = CreateCommandParameters(command, r, DataStruct);
                                    errorstring = updatestring.Clone().ToString();
                                    foreach (EntityField item in DataStruct.Fields)
                                    {
                                        try
                                        {
                                            string s;
                                            string f;
                                            if (r[item.fieldname] == DBNull.Value)
                                            {
                                                s = "\' \'";
                                            }
                                            else
                                            {
                                                s = "\'" + r[item.fieldname].ToString() + "\'";
                                            }
                                            f = "@p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                                            errorstring = errorstring.Replace(f, s);
                                        }
                                        catch (Exception ex1)
                                        {
                                        }
                                    }
                                    string msg = "";
                                    int rowsUpdated = command.ExecuteNonQuery();
                                    if (rowsUpdated > 0)
                                    {
                                        msg = $"Successfully I/U/D  Record {i} to {EntityName} : {updatestring}";
                                    }
                                    else
                                    {
                                        msg = $"Fail to I/U/D  Record {i} to {EntityName} : {updatestring}";
                                    }
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
                                    if (DataStruct.PrimaryKeys != null)
                                    {
                                        if (DataStruct.PrimaryKeys.Count == 1)
                                        {
                                            args.ParameterString1 = r[DataStruct.PrimaryKeys[0].fieldname].ToString();
                                        }
                                        if (DataStruct.PrimaryKeys.Count == 2)
                                        {
                                            args.ParameterString2 = r[DataStruct.PrimaryKeys[1].fieldname].ToString();
                                        }
                                        if (DataStruct.PrimaryKeys.Count == 3)
                                        {
                                            args.ParameterString3 = r[DataStruct.PrimaryKeys[2].fieldname].ToString();
                                        }
                                    }
                                    args.ParameterInt1 = percentComplete;
                                    //         UpdateEvents(EntityName, msg, highestPercentageReached, CurrentRecord, numberToCompute, this);
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = null };
                                        progress.Report(ps);
                                    }
                                    //   PassEvent?.Invoke(this, args);
                                    //   DMEEditor.RaiseEvent(this, args);
                                }
                                catch (Exception er)
                                {
                                    string msg = $"Fail to I/U/D  Record {i} to {EntityName} : {updatestring}";
                                    if (progress != null)
                                    {
                                        PassedArgs ps = new PassedArgs { ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = msg };
                                        progress.Report(ps);
                                    }
                                    DMEEditor.AddLogMessage("Fail", msg, DateTime.Now, i, EntityName, Errors.Failed);
                                }
                            }
                            DMEEditor.ETL.CurrentScriptRecord = DMEEditor.ETL.ScriptCount;
                            command.Dispose();
                            DMEEditor.AddLogMessage("Success", $"Finished Uploading Data to {EntityName}", DateTime.Now, 0, null, Errors.Ok);
                        }
                       
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
            retval = Entities.FirstOrDefault(d => d.EntityName.Equals(EntityName,StringComparison.InvariantCultureIgnoreCase));
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
                        fnd.DatasourceEntityName=entname;
                    }
                    if (string.IsNullOrEmpty(fnd.Caption))
                    {
                        fnd.Caption = entname;
                    }
                    fnd.DataSourceID = DatasourceName;
                    //  fnd.EntityName = EntityName;
                    if (fnd.Viewtype == ViewType.Query)
                    {
                        tb = GetTableSchema(fnd.CustomBuildQuery,true);
                    }
                    else
                    {
                       
                        tb = GetTableSchema(entname,false);
                    }
                    if (tb.Rows.Count > 0)
                    {
                        fnd.Fields = new List<EntityField>();
                        fnd.PrimaryKeys = new List<EntityField>();
                        DataRow rt = tb.Rows[0];
                        fnd.Created = true;
                        fnd.Editable = false;
                        fnd.Drawn = true;
                        foreach (DataRow r in rt.Table.Rows)
                        {
                            EntityField x = new EntityField();
                            try
                            {

                                x.fieldname = r.Field<string>("ColumnName");
                                x.fieldtype = (r.Field<Type>("DataType")).ToString(); //"ColumnSize"
                                if( DatasourceType == DataSourceType.Oracle)
                                {
                                    if (x.fieldtype.Equals("FLOAT", StringComparison.OrdinalIgnoreCase))
                                    {
                                        int precision = GetFloatPrecision(x.EntityName, x.fieldname); // Implement GetFloatPrecision to retrieve the precision for the field
                                        x.fieldtype = MapOracleFloatToDotNetType(precision);
                                    }
                                }
                              
                            x.Size1 = r.Field<int>("ColumnSize");
                                try
                                {
                                    x.IsAutoIncrement = r.Field<bool>("IsAutoIncrement");
                                }
                                catch (Exception)
                                {
                                  x.IsAutoIncrement = false;
                                }
                                try
                                {
                                    x.AllowDBNull = r.Field<bool>("AllowDBNull");
                                }
                                catch (Exception)
                                {
                                }
                                try
                                {
                                    x.IsAutoIncrement = r.Field<bool>("IsIdentity");
                                    x.IsIdentity = x.IsAutoIncrement;
                                }
                                catch (Exception)
                                {
                                    x.IsIdentity = false;
                                }
                                try
                                {
                                    x.IsKey = r.Field<bool>("IsKey");
                                }
                                catch (Exception)
                                {

                                }
                                try
                                {
                                  if (x.fieldtype == "System.Decimal" || x.fieldtype=="System.Float" || x.fieldtype == "System.Double") 
                                    {
                                        var NumericPrecision = r["NumericPrecision"];
                                        var NumericScale = r["NumericScale"];
                                        if (NumericPrecision != System.DBNull.Value && NumericScale != System.DBNull.Value)
                                        {
                                            x.NumericPrecision = (short)NumericPrecision;
                                            x.NumericScale = (short)NumericScale;
                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }
                                try
                                {
                                    x.IsUnique = r.Field<bool>("IsUnique");
                                }
                                catch (Exception)
                                {

                                }
                            }
                            catch (Exception ex)
                            {
                                DMEEditor.AddLogMessage("Fail", $"Error in Creating Field Type({ ex.Message})", DateTime.Now, 0, entname, Errors.Failed);
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
                        if (idx==-1)
                        {
                            Entities.Add(fnd);
                        }
                        else
                        {
                           
                            Entities[idx].Created = true;
                            Entities[idx].Editable = false;
                            Entities[idx].Drawn = true;
                            Entities[idx].Fields = fnd.Fields;
                            Entities[idx].Relations = fnd.Relations;
                            Entities[idx].PrimaryKeys = fnd.PrimaryKeys;
    
                        }
                    }
                    else
                    {
                        fnd.Created = false;
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
            DMTypeBuilder.CreateNewObject(DMEEditor,"Beep."+DatasourceName, EntityName, x.Fields);
            return DMTypeBuilder.myType;
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
            try
            {
                if (Dataconnection != null)
                {
                    if(Dataconnection.ConnectionProp!=null)
                    {
                        if(Dataconnection.ConnectionProp.SchemaName!=null)
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
                
                    IDbDataAdapter adp = GetDataAdapter(sql, null);
                    adp.Fill(ds);
#if DEBUG
                    DMEEditor.AddLogMessage("Beep", $"Get Tables List Query {sql}", DateTime.Now, 0, DatasourceName, Errors.Failed);
                    Debug.WriteLine($" -- Get Tables List Query {sql}");
#endif 
                DataTable tb = new DataTable();
                    tb = ds.Tables[0];
                    EntitiesNames = new List<string>();
                    int i = 0;
                    foreach (DataRow row in tb.Rows)
                    {
                        EntitiesNames.Add(row.Field<string>("TABLE_NAME").ToUpper());
                        i += 1;
                    }
               
               
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in getting  Table List ({ ex.Message})", DateTime.Now, 0, DatasourceName, Errors.Failed);
              
            }

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
        public virtual string AddNewEntity(string entityName,string schemaname)
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
                int  ent=Entities.FindIndex(p=>p.EntityName.ToUpper()==entityName.ToUpper());
                if (ent> -1)
                {
                    return "Entity Exist";
                }

            }
            EntityStructure entity=new EntityStructure();
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
            string schemaname=null;
            
            if(!string.IsNullOrEmpty(Dataconnection.ConnectionProp.SchemaName))
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
            if (Entities.Count > 0) {
                retval = Entities.Any(p=>p.EntityName == EntityName || p.OriginalEntityName==EntityName || p.DatasourceEntityName==EntityName);
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
            }else
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
                string createstring=CreateEntity(entity);
                DMEEditor.ErrorObject=ExecuteSql(createstring);
                if (DMEEditor.ErrorObject.Flag == Errors.Failed)
                {
                    retval = false;
                }
                else
                {
                    Entities.Add(entity);
                    retval = true;
                }
            } else
            {
                if (Entities.Count > 0)
                {
                    if (Entities.Where(p => p.EntityName.Equals(entity.EntityName, StringComparison.InvariantCultureIgnoreCase) && p.Created == false).Any())
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
                            retval = true;
                        }
                    }
                }
                else
                    return false;
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
               DMEEditor.AddLogMessage("Fail", $"Could not get forgien key  for {entityname} ({ ex.Message})", DateTime.Now, 0, entityname, Errors.Failed);
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
             DMEEditor.AddLogMessage("Fail", $"Error in getting  child entities for {tablename} ({ ex.Message})", DateTime.Now, 0, tablename, Errors.Failed);
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
            scripts.errorsInfo = t.Result;
            scripts.errormessage = DMEEditor.ErrorObject.Message;
            DMEEditor.ErrorObject = scripts.errorsInfo;
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
        private string GenerateCreateEntityScript(EntityStructure t1)
        {
            string createtablestring = "Create table ";
            try
            {//-- Create Create string
                int i = 1;
                t1.EntityName = Regex.Replace(t1.EntityName, @"\s+", "_");
                createtablestring += " " +t1.EntityName + "\n(";
                if (t1.Fields.Count == 0)
                {
                    // t1=ds.GetEntityStructure()
                }
                foreach (EntityField dbf in t1.Fields)
                {

                    createtablestring += "\n " + dbf.fieldname + " " + DMEEditor.typesHelper.GetDataType(DatasourceName, dbf) + " ";
                    if (dbf.IsAutoIncrement)
                    {
                      //  dbf.fieldname = Regex.Replace(dbf.fieldname, @"\s+", "_");
                        string autonumberstring = "";
                        autonumberstring = CreateAutoNumber(dbf);
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
                    i += 1;

                    if (i <= t1.Fields.Count)
                    {
                        createtablestring += ",";
                    }

                }
                if (t1.PrimaryKeys != null)
                {
                    if (t1.PrimaryKeys.Count > 0)
                    {
                        createtablestring += $",\n" + CreatePrimaryKeyString(t1);
                    }
                }
                if (createtablestring[createtablestring.Length - 1].Equals(","))
                {
                    createtablestring = createtablestring.Remove(createtablestring.Length);
                }

                createtablestring += ")";

            }
            catch (System.Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error Creating Entity {t1.EntityName}  ({ex.Message})", DateTime.Now, 0, "", Errors.Failed);
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
                entstructure.Created = false;
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
                retval = @" PRIMARY KEY ( ";
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
                if (i ==0)
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
                        string relations=CreateAlterRalationString(entity);
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
                createtablestring= GenerateCreateEntityScript(t1);
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
            // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
            //   map= Mapping.FldMapping;
            //    EntityName = Regex.Replace(EntityName, @"\s+", "");
            string Insertstr = "insert into " + EntityName + " (";
            Insertstr = GetTableName(Insertstr.ToLower());
            string Valuestr = ") values (";
            var insertfieldname = "";
            // string datafieldname = "";
            string typefield = "";
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {
               Insertstr += $"{GetFieldName(item.fieldname)},";
               Valuestr += $"{ParameterDelimiter}p_" + Regex.Replace(item.fieldname, @"\s+", "_") + ",";
                 
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
          
            string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
            Updatestr= GetTableName(Updatestr.ToLower());
            string Valuestr = "";
           
            int i = DataStruct.Fields.Count();
            int t = 0;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o=>o.fieldname))
            {
                if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
                {
                    Updatestr += $"{GetFieldName(item.fieldname)}= ";
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + ",";
                }
                t += 1;
            }

            Updatestr = Updatestr.Remove(Updatestr.Length - 1);

            Updatestr += @" where " + Environment.NewLine;
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {
               
                    if (t == 1)
                    {
                        Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                    else
                    {
                        Updatestr += $" and {GetFieldName(item.fieldname)}= ";
                }
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";
                  
                t += 1;
            }
            //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
            return Updatestr;
        }
        public virtual string GetDeleteString(string EntityName,  EntityStructure DataStruct)
        {

          
            string Updatestr = @"Delete from " + EntityName + "  ";
            Updatestr = GetTableName(Updatestr.ToLower());
            int i = DataStruct.Fields.Count();
            int t = 0;
            Updatestr += @" where ";
            i = DataStruct.PrimaryKeys.Count();
            t = 1;
            foreach (EntityField item in DataStruct.PrimaryKeys)
            {
                
                    if (t == 1)
                    {
                        Updatestr += $"{GetFieldName(item.fieldname)}= ";
                }
                    else
                    {
                        Updatestr += $" and  {GetFieldName(item.fieldname)}= ";
                    }
                    Updatestr += $"{ParameterDelimiter}p_" + item.fieldname + "";
                t += 1;
            }
            return Updatestr;
        }
        public virtual IDataReader GetDataReader(string querystring)
        {
            IDbCommand cmd = GetDataCommand();
            cmd.CommandText = querystring;
            IDataReader dt = cmd.ExecuteReader();
            
            return dt;

        }
        public virtual string GetFieldName(string fieldname)
        {
            string retval = fieldname;
            if (fieldname.IndexOf(" ") != -1)
            {
                if (ColumnDelimiter.Length==2) //(ColumnDelimiter.Contains("[") || ColumnDelimiter.Contains("]"))
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
                return RDBMSConnection.DbConn.Query<T>(sql).AsList<T>();
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
                if(Dataconnection.ConnectionStatus== ConnectionState.Open)
                {
                    cmd = RDBMSConnection.DbConn.CreateCommand();
                }else
                {
                    cmd = null;

                    DMEEditor.AddLogMessage("Fail", $"Error in Creating Data Command, Cannot get DataSource", DateTime.Now, -1,DatasourceName, Errors.Failed);
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
        public virtual DataTable GetTableSchema(string TableName,bool Isquery=false)
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
                    cmdtxt = "Select * from " + TableName.ToLower() + " where 1=2";
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
            throw new NotImplementedException();
        }
        public virtual string EnableFKConstraints(EntityStructure t1)
        {
            throw new NotImplementedException();
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
            if (schname != null)
            {
                if (!schname.Equals(userid, StringComparison.InvariantCultureIgnoreCase))
                {
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
                        querystring = querystring.Replace(' ' + tablename + ' ', $" {schname}.{tablename} ");
                    }
                    else if (querystring.IndexOf("insert") >= 0)
                    {
                        int intopos = querystring.IndexOf("into", StringComparison.InvariantCultureIgnoreCase);
                        string[] instokens = querystring.Split(' ');
                        querystring = querystring.Replace(instokens[2], $" {schname}.{instokens[2]} ");
                    }
                    else if (querystring.IndexOf("update") >= 0)
                    {
                        int setpos = querystring.IndexOf("set", StringComparison.InvariantCultureIgnoreCase);
                        string[] uptokens = querystring.Split(' ');
                        querystring = querystring.Replace(uptokens[1], $" {schname}.{uptokens[1]} ");
                    }
                    else if (querystring.IndexOf("delete") >= 0)
                    {
                        int frompos = querystring.IndexOf("from", StringComparison.InvariantCultureIgnoreCase);
                        string[] fromtokens = querystring.Split(' ');
                        querystring = querystring.Replace(fromtokens[1], $" {schname}.{fromtokens[2]} ");
                    }
                }
                    
            }
            return querystring;
        }
        #endregion
        #region "dispose"
        private bool disposedValue;
        protected  void Dispose(bool disposing)
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

