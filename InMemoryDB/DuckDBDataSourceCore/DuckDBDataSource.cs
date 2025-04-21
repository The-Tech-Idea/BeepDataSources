﻿
using DuckDB.NET.Data;
using DuckDB.NET;
using System.Data;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;



using System.Reflection;
using System.Text;
using System.Xml;

using System.Data.Common;

using TheTechIdea.Beep.Report;

using DateTime = System.DateTime;
using System.Text.RegularExpressions;
using System.Diagnostics;
using TheTechIdea.Beep.Helpers;
using System.Data.SqlTypes;


namespace DuckDBDataSourceCore
{
    [AddinAttribute(Category = DatasourceCategory.INMEMORY, DatasourceType = DataSourceType.DuckDB)]
    public class DuckDBDataSource : InMemoryRDBSource
    {
        private bool disposedValue;
        string dbpath;
        
   
        public DuckDBConnection DuckConn { get; set; }
        DuckDBTransaction Transaction { get; set; }
        
        public DuckDBDataSource(string pdatasourcename, IDMLogger plogger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(pdatasourcename, plogger, pDMEEditor, databasetype, per)
        {
            //FormattableString str = $"Data Source={dbpath}";
            if(per == null)
            {
                per = DMEEditor.ErrorObject;
            }
            if (pdatasourcename != null)
            {
                if (Dataconnection == null)
                {
                    Dataconnection = new RDBDataConnection(DMEEditor);
                }
                Dataconnection.ConnectionProp = DMEEditor.ConfigEditor.DataConnections.FirstOrDefault(p => p.ConnectionName.Equals(pdatasourcename, StringComparison.InvariantCultureIgnoreCase)); ;
                if (Dataconnection.ConnectionProp == null)
                {
                    Dataconnection.ConnectionProp = new ConnectionProperties();
                }

            }
            Dataconnection.ConnectionProp.DatabaseType = DataSourceType.DuckDB;
            
             //   Dataconnection.ConnectionProp.IsInMemory=true;
             ColumnDelimiter = "[]";
            ParameterDelimiter = "$";
           
            dbpath = Path.Combine(DMEEditor.ConfigEditor.Config.DataFilePath, DatasourceName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (DuckConn != null)
                    {
                        DuckConn.Dispose();
                        DuckConn = null;
                    }
                    if (Transaction != null)
                    {
                        Transaction.Dispose();
                        Transaction = null;
                    }
                    if (command != null)
                    {
                        command.Dispose();
                        command = null;
                    }
                }

                // Free unmanaged resources and override finalizer
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DuckDBDataSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public IErrorsInfo OpenDatabaseInMemory(string databasename)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {
                Dataconnection.InMemory = true;
                Dataconnection.ConnectionProp.FileName = string.Empty;
                Dataconnection.ConnectionProp.ConnectionString = "DataSource=:memory:?cache=shared";
                Dataconnection.ConnectionProp.Database = databasename;
                Dataconnection.ConnectionProp.ConnectionName = databasename;
                Dataconnection.ConnectionProp.SchemaName = "main";

                DuckConn = new DuckDBConnection("DataSource=:memory:?cache=shared");
                DuckConn.Open();
                if (DuckConn.State == ConnectionState.Open)
                {
                    Dataconnection.ConnectionStatus = ConnectionState.Open;
                }
                else
                {
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    DMEEditor.ErrorObject.Flag = Errors.Failed;
                    DMEEditor.ErrorObject.Message = "Failed to open in-memory database connection";
                }
            }
            catch (Exception ex)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.AddLogMessage("Beep", $"Error opening in-memory database: {ex.Message}",
                    System.DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public string GetConnectionString()
        {
            return Dataconnection.ConnectionProp.ConnectionString;
        }

        #region "IDataSource Properties"


        #endregion "IDataSource Properties"
        #region "IDataSource Methods"
        public override ConnectionState Openconnection()
        {
         
            CancellationTokenSource token = new CancellationTokenSource();

            Dataconnection.InMemory = Dataconnection.ConnectionProp.IsInMemory;
            if (ConnectionStatus == ConnectionState.Open)
            {
                DMEEditor.AddLogMessage("Beep", $"Connection is already open", System.DateTime.Now, -1, "", Errors.Ok);
                return ConnectionState.Open;
            }
            if (Dataconnection.ConnectionProp.IsInMemory)
            {
                OpenDatabaseInMemory(Dataconnection.ConnectionProp.Database);
                base.Openconnection();
                if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                {
                    
                    return ConnectionState.Open;
                }
            }
            else
            {
                base.Openconnection();

            }

            return ConnectionStatus;
        }
        public override ConnectionState Closeconnection()
        {
            ConnectionState retval = ConnectionState.Closed;
            try
            {
                SaveStructure();
                if (DuckConn != null)
                {
                    DuckConn.Close();
                    Dataconnection.ConnectionStatus = ConnectionState.Closed;
                    DMEEditor.AddLogMessage("Success", $"Closing connection to DuckDB Database",
                        System.DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                string errmsg = "Error Closing connection to DuckDB Database";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}",
                    System.DateTime.Now, 0, null, Errors.Failed);
            }
            return retval;
        }

        public override IErrorsInfo BeginTransaction(PassedArgs args)
        {
            if (DuckConn == null || DuckConn.State != ConnectionState.Open)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Cannot begin transaction: Connection is not open";
                return DMEEditor.ErrorObject;
            }

            Transaction = DuckConn.BeginTransaction();
            return base.BeginTransaction(args);
        }

        public override IErrorsInfo Commit(PassedArgs args)
        {
            if (Transaction == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Cannot commit: No active transaction";
                return DMEEditor.ErrorObject;
            }

            Transaction.Commit();
            Transaction = null;
            return base.Commit(args);
        }

        public override IErrorsInfo EndTransaction(PassedArgs args)
        {
            if (Transaction == null)
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Cannot rollback: No active transaction";
                return DMEEditor.ErrorObject;
            }

            Transaction.Rollback();
            Transaction = null;
            return base.EndTransaction(args);
        }

        #endregion "IDataSource Methods"
        #region "DuckDB Methods"

        public string DuckDBConvert(Type netType)
        {
            if (netType == typeof(bool))
                return "BOOLEAN";
            else if (netType == typeof(sbyte))
                return "TINYINT";
            else if (netType == typeof(byte))
                return "UTINYINT";
            else if (netType == typeof(short))
                return "SMALLINT";
            else if (netType == typeof(ushort))
                return "USMALLINT";
            else if (netType == typeof(int))
                return "INTEGER";
            else if (netType == typeof(uint))
                return "UINTEGER";
            else if (netType == typeof(long))
                return "BIGINT";
            else if (netType == typeof(ulong))
                return "UBIGINT";
            else if (netType == typeof(decimal))
                return "DECIMAL";
            else if (netType == typeof(float))
                return "REAL";
            else if (netType == typeof(double))
                return "DOUBLE";
            else if (netType == typeof(System.DateTime))
                return "TIMESTAMP";
            else if (netType == typeof(string))
                return "VARCHAR";
            else if (netType == typeof(char))
                return "VARCHAR";
            else if (netType == typeof(byte[]))
                return "BLOB";
            else if (netType == typeof(XmlDocument) || netType == typeof(XmlElement) || netType == typeof(XmlNode))
                return "VARCHAR";
            else
                throw new ArgumentException("Unsupported .NET data type: " + netType);
        }
      
        public  string DuckDBConvert(string netTypeName)
        {
            Type netType = Type.GetType(netTypeName);

            if (netType == null)
                throw new ArgumentException("Unrecognized .NET data type: " + netTypeName);

            if (netType == typeof(bool))
                return "BOOLEAN";
            else if (netType == typeof(sbyte))
                return "TINYINT";
            else if (netType == typeof(byte))
                return "UTINYINT";
            else if (netType == typeof(short))
                return "SMALLINT";
            else if (netType == typeof(ushort))
                return "USMALLINT";
            else if (netType == typeof(int))
                return "INTEGER";
            else if (netType == typeof(uint))
                return "UINTEGER";
            else if (netType == typeof(long))
                return "BIGINT";
            else if (netType == typeof(ulong))
                return "UBIGINT";
            else if (netType == typeof(decimal))
                return "DECIMAL";
            else if (netType == typeof(float))
                return "REAL";
            else if (netType == typeof(double))
                return "DOUBLE";
            else if (netType == typeof(System.DateTime))
                return "TIMESTAMP";
            else if (netType == typeof(string))
                return "VARCHAR";
            else if (netType == typeof(char))
                return "VARCHAR";
            else if (netType == typeof(byte[]))
                return "BLOB";
            else if (netType == typeof(XmlDocument) || netType == typeof(XmlElement) || netType == typeof(XmlNode))
                return "VARCHAR";
            else
                throw new ArgumentException("Unsupported .NET data type: " + netTypeName);
        }
        #endregion
        #region "Insert or Update or Delete Objects"
        EntityStructure DataStruct = null;
        IDbCommand command = null;
        Type enttype = null;
        bool ObjectsCreated = false;
        string lastentityname = null;
        #endregion
        private void SetObjects(string Entityname)
        {
            if (!ObjectsCreated || Entityname != lastentityname)
            {
                DataStruct = GetEntityStructure(Entityname, true);
                command = DuckConn.CreateCommand();
                enttype = GetEntityType(Entityname);
                ObjectsCreated = true;
                lastentityname = Entityname;
            }
        }
        #region "Overirden RDBSource Methods"
        public override EntityStructure GetEntityStructure(string EntityName, bool refresh = false)
        {
            string EntityName2 = EntityName;
            EntityStructure entityStructure = new EntityStructure();
            if (Entities.Count == 0)
            {
                GetEntitesList();
            }

            entityStructure = Entities.FirstOrDefault((EntityStructure d) => d.EntityName.Equals(EntityName2, StringComparison.InvariantCultureIgnoreCase));
            if (entityStructure == null)
            {
                entityStructure = new EntityStructure();
                refresh = true;
                entityStructure.DataSourceID = DatasourceName;
                entityStructure.EntityName = EntityName2;
                entityStructure.DatasourceEntityName = EntityName2;
                entityStructure.Caption = EntityName2;
                if (RDBMSHelper.IsSqlStatementValid(EntityName2))
                {
                    entityStructure.Viewtype = ViewType.Query;
                    entityStructure.CustomBuildQuery = EntityName2;
                }
                else
                {
                    entityStructure.Viewtype = ViewType.Table;
                    entityStructure.CustomBuildQuery = null;
                }

                refresh = true;
            }

            return GetEntityStructure(entityStructure, refresh);
          
        }
        public override EntityStructure GetEntityStructure(EntityStructure fnd, bool refresh = false)
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
                    tb = this.GetTableSchemaFromQuery(entname);
                }
                else
                {

                    tb = this.GetTableSchema(entname);
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

                            x.fieldname = r.Field<string>("Column_Name");
                            x.fieldtype = DataTypeFieldMappingHelper.GetDataType(DatasourceName, r.Field<string>("Data_Type"),DMEEditor);
                           
                            try
                            {
                                x.AllowDBNull = r.Field<bool>("is_nullable");
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                x.IsAutoIncrement = r.Field<bool>("is_identity");
                                x.IsIdentity = x.IsAutoIncrement;
                            }
                            catch (Exception)
                            {
                                x.IsIdentity = false;
                            }
                           
                            try
                            {
                                if (x.fieldtype == "System.Decimal" || x.fieldtype == "System.Float" || x.fieldtype == "System.Double")
                                {
                                    var NumericPrecision = r["Numeric_Precision"];
                                    var NumericScale = r["Numeric_Scale"];
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
                           
                        }
                        catch (Exception ex)
                        {
                            DMEEditor.AddLogMessage("Fail", $"Error in Creating Field Type({ex.Message})", DateTime.Now, 0, entname, Errors.Failed);
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
        public override object GetEntity(string EntityName, List<AppFilter> Filter)
        {
            ErrorObject.Flag = Errors.Ok;
            //  int LoadedRecord;

            EntityName = EntityName.ToLower();
            string inname = "";
            string qrystr = "select * from ";

            if (!string.IsNullOrEmpty(EntityName) && !string.IsNullOrWhiteSpace(EntityName))
            {
                if (!EntityName.Contains("select") && !EntityName.Contains("from"))
                {
                    qrystr = "select * from " + EntityName;
                    inname = EntityName;
                }
                else
                {

                    string[] stringSeparators = new string[] { " from ", " where ", " group by ", " order by " };
                    string[] sp = EntityName.Split(stringSeparators, StringSplitOptions.None);
                    qrystr = EntityName;
                    inname = sp[1].Trim();
                }

            }
            // EntityStructure ent = GetEntityStructure(inname);
            qrystr = BuildQuery(qrystr, Filter);

            try
            {
                //OracleDataAdapter adp =GetDataAdapterForOracle(qrystr, Filter);
                //DataSet dataSet = new DataSet();
                //adp.Fill(dataSet);
                //DataTable dt = dataSet.Tables[0];
                var retval = RunQuery(qrystr);
                return retval;
            }

            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ex.Message})", DateTime.Now, 0, "", Errors.Failed);

                return null;
            }


        }
        public override IErrorsInfo UpdateEntities(string EntityName, object UploadData, IProgress<PassedArgs> progress)
        {
            DataTable tb = new DataTable();
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            if (UploadData != null)
            {
                SetObjects(EntityName);
                if (UploadData.GetType().FullName.Contains("DataTable"))
                {

                }
                if (UploadData.GetType().FullName.Contains("List"))
                {
                    tb = DMEEditor.Utilfunction.ToDataTable((System.Collections.IList)UploadData, enttype);
                }

                if (UploadData.GetType().FullName.Contains("IEnumerable"))
                {
                    tb = DMEEditor.Utilfunction.ToDataTable((System.Collections.IList)UploadData, enttype);
                }

                //  RunCopyDataBackWorker(EntityName,  UploadData,  Mapping );
                #region "Update Code"
                //IDbTransaction sqlTran;

                // DMEEditor.classCreator.CreateClass();
                //List<object> f = DMEEditor.Utilfunction.GetListByDataTable(tb);
                ErrorObject.Flag = Errors.Ok;
             //   EntityStructure DataStruct = GetEntityStructure(EntityName);
              //  DuckDBCommand command = GetDataCommand();
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
                        if (changes != null)
                        {
                            for (int i = 0; i < changes.Rows.Count; i++)
                            {
                                try
                                {
                                    DataRow r = changes.Rows[i];
                                    DMEEditor.ErrorObject = InsertEntity(EntityName, r);
                                    CurrentRecord = i;
                                    //switch (r.RowState)
                                    //{
                                    //    case DataRowState.Unchanged:
                                    //    case DataRowState.Added:
                                    //        updatestring = GetInsertString(EntityName, DataStruct);
                                    //        break;
                                    //    case DataRowState.Deleted:
                                    //        updatestring = GetDeleteString(EntityName, DataStruct);
                                    //        break;
                                    //    case DataRowState.Modified:
                                    //        updatestring = GetUpdateString(EntityName, DataStruct);
                                    //        break;
                                    //    default:
                                    //        updatestring = GetInsertString(EntityName, DataStruct);
                                    //        break;
                                    //}
                                    //command.CommandText = updatestring;
                                    //command = CreateCommandParameters(command, r, DataStruct);
                           //         errorstring = updatestring.Clone().ToString();
                                    //foreach (EntityField item in DataStruct.Fields)
                                    //{
                                    //    try
                                    //    {
                                    //        string s;
                                    //        string f;
                                    //        if (r[item.fieldname] == DBNull.Value)
                                    //        {
                                    //            s = "\' \'";
                                    //        }
                                    //        else
                                    //        {
                                    //            s = "\'" + r[item.fieldname].ToString() + "\'";
                                    //        }
                                    //        f = "@p_" + Regex.Replace(item.fieldname, @"\s+", "_");
                                    //        errorstring = errorstring.Replace(f, s);
                                    //    }
                                    //    catch (Exception ex1)
                                    //    {
                                    //    }
                                    //}
                                    string msg = "";
                                    //int rowsUpdated = command.ExecuteNonQuery();
                                    if (DMEEditor.ErrorObject.Flag == Errors.Ok)
                                    {
                                        msg = $"Successfully I/U/D  Record {i} to {EntityName}" ;
                                    }
                                    else
                                    {
                                        msg = $"Fail to I/U/D  Record {i} to {EntityName} ";
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
                                        PassedArgs ps = new PassedArgs { Messege = msg, ParameterInt1 = CurrentRecord, ParameterInt2 = DMEEditor.ETL.ScriptCount, ParameterString1 = null };
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
                           // command.Dispose();
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
        public override IErrorsInfo UpdateEntity(string EntityName, object UploadDataRow)
        {
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            // DataRow tb = object UploadDataRow;
            ErrorObject.Flag = Errors.Ok;
            EntityStructure DataStruct = GetEntityStructure(EntityName, true);
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            string msg = "";
            //   var sqlTran = Dataconnection.DbConn.BeginTransaction();
            DuckDBCommand command = GetDataCommand();
            Type enttype = GetEntityType(EntityName);
            //  var ti = Activator.CreateInstance(enttype);

            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, UploadDataRow, DataStruct);
            try
            {
                string updatestring = GetUpdateString(EntityName, DataStruct);
                command.CommandText = updatestring;
                command = CreateCommandParameters(command, dr, DataStruct);
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
        public override IErrorsInfo InsertEntity(string EntityName, object InsertedData)
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
            DuckDBCommand command = GetDataCommand();
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
                    string fetchIdentityQuery = RDBMSHelper.GenerateFetchLastIdentityQuery(DatasourceType);
                    if (fetchIdentityQuery.ToUpper().Contains("SELECT") && DataStruct.PrimaryKeys.Count() > 0)
                    {
                        command.CommandText = fetchIdentityQuery;
                        object result = command.ExecuteScalar();
                        int identity = 0;
                        if (result != null)
                        {
                            identity = Convert.ToInt32(result);
                            // Update the primary key property of the inserted record
                            var primaryKeyProperty = InsertedData.GetType().GetProperty(DataStruct.PrimaryKeys.First().fieldname);
                            if (primaryKeyProperty != null && primaryKeyProperty.CanWrite)
                            {
                                primaryKeyProperty.SetValue(InsertedData, identity);
                            }

                            msg = $"Successfully Inserted Record to {EntityName} with ID {identity}";
                            DMEEditor.ErrorObject.Message = msg;
                            DMEEditor.ErrorObject.Flag = Errors.Ok;
                        }
                        else
                        {
                            msg = "Failed to retrieve the identity of the inserted record.";
                            DMEEditor.ErrorObject.Message = msg;
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                        }
                    }
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
        public override IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        {
            SetObjects(EntityName);
            ErrorObject.Flag = Errors.Ok;

            string msg;
            DataRowView dv;
            DataTable tb;
            DataRow dr;
            var sqlTran = RDBMSConnection?.DbConn.BeginTransaction();
            if (recEntity != EntityName)
            {
                recNumber = 1;
                recEntity = EntityName;
            }
            else
                recNumber += 1;
            DuckDBCommand command = GetDataCommand();
            dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, DeletedDataRow, DataStruct);
            try
            {
                string updatestring = GetDeleteString(EntityName, DataStruct);
                command = GetDataCommand();
               
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
        public override List<string> GetEntitesList()
        {
            EntitiesNames = new List<string>();
            using (var command = DuckConn.CreateCommand())
            {
                command.CommandText = "SHOW TABLES;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        EntitiesNames.Add(reader.GetString(0));
                    }
                }
            }


            return EntitiesNames;
        }
        public override bool CreateEntityAs(EntityStructure entity)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            bool retval = false;
            try
            {
                string sql = $"CREATE TABLE {entity.EntityName} (";
                foreach (EntityField fld in entity.Fields)
                {
                    string fieldType = DuckDBConvert(fld.fieldtype);
                    sql += $"{fld.fieldname} {fieldType}";

                    // Add constraints
                    if (!fld.AllowDBNull)
                        sql += " NOT NULL";
                    if (fld.IsKey)
                        sql += " PRIMARY KEY";
                    if (fld.IsAutoIncrement)
                        sql += " AUTOINCREMENT";

                    sql += ",";
                }
                sql = sql.TrimEnd(',') + ")";
                using (var cmd = new DuckDBCommand(sql, DuckConn))
                {
                    cmd.ExecuteNonQuery();
                }
                retval = true;
                DMEEditor.AddLogMessage("Success", $"Creating Entity {entity.EntityName}", System.DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Error in Creating Entity {entity.EntityName} {ex.Message}", System.DateTime.Now, 0, null, Errors.Failed);
            }
            return retval;
        }

        public override object RunQuery(string qrystr)
        {
            if (RDBMSHelper.IsSqlStatementValid(qrystr)) { return RunQueryOnDuckDb(qrystr); }
            else
            {
                DMEEditor.ErrorObject.Flag = Errors.Failed;
                DMEEditor.ErrorObject.Message = "Invalid Query";
                return null;
            }
            
        }
        public override IErrorsInfo ExecuteSql(string sql)
        {
            try
            {
               
                using (var command = DuckConn.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
               

                DMEEditor.ErrorObject.Flag = Errors.Ok;
                // Return a successful result
                return DMEEditor.ErrorObject; // Replace with your actual success implementation
            }
            catch (Exception ex)
            {
           
                // Return an error result
                DMEEditor.ErrorObject.Flag= Errors.Failed;
                DMEEditor.ErrorObject.Ex = ex;
                DMEEditor.ErrorObject.Message= ex.Message;
                return DMEEditor.ErrorObject; // Replace with your actual error implementation
            }
        }
        public DataTable RunQueryOnDuckDb( string qryStr)
        {
            var dataTable = new DataTable();
            DuckDBCommand command = GetDataCommand();

            command.CommandText = qryStr;
            //command.Parameters.Add(new DuckDBParameter("table_name", tableName));
            using (var reader = command.ExecuteReader())
            {
                dataTable.Load(reader);
            }
            //using (var command = new DuckDBCommand(qryStr, DuckConn))
            //{
               
            //    using (var adapter = new OdbcDataAdapter(command))
            //    {
            //        adapter.Fill(dataTable);
            //    }
               
            //}

            return dataTable;
        }
        private DuckDBCommand CreateDeleteCommandParameters(DuckDBCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();

            foreach (EntityField item in DataStruct.PrimaryKeys.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    DbParameter parameter = command.CreateParameter();
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
        private DuckDBCommand CreateCommandParameters(DuckDBCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();
         
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    DbParameter parameter = command.CreateParameter();
                    parameter.ParameterName = "p_" + Regex.Replace(item.fieldname, @"\s+", "_");

                    if (r.IsNull(item.fieldname))
                    {
                        parameter.Value = DBNull.Value;
                    }
                    else
                    {
                        switch (item.fieldtype)
                        {
                            case "System.DateTime":
                                parameter.DbType = DbType.DateTime;
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

                                        parameter.Value = null;
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
                    }
                  
            
                    
                    //   parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                    command.Parameters.Add(parameter);
                }

            }
            return command;
        }
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
        private string PagedQuery(string originalquery, List<AppFilter> Filter)
        {

            AppFilter pagesizefilter = Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            AppFilter pagenumberfilter = Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            int pagesize = Convert.ToInt32(pagesizefilter.FilterValue);
            int pagenumber = Convert.ToInt32(pagenumberfilter.FilterValue);

            string pagedquery = "SELECT * FROM " +
                             "  (SELECT a.*, rownum rn" +
                             "    FROM    (" +
                             $"             {originalquery} ) a " +
                             $"    WHERE rownum < (({pagenumber} * {pagesize}) + 1)) WHERE rn >= ((({pagenumber} - 1) * {pagesize}) + 1)";
            return pagedquery;
        }
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
                stringSeparators = new string[] { "select ", " from ", " where ", " group by ", " having ", " order by " };
                sp = originalquery.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                queryStructure.FieldsString = sp[0];
                string[] Fieldsp = sp[0].Split(',');
                queryStructure.Fields.AddRange(Fieldsp);
                // Get From  Tables
                queryStructure.EntitiesString = sp[1];
                string[] Tablesdsp = sp[1].Split(',');
                queryStructure.Entities.AddRange(Tablesdsp);
                qrystr += queryStructure.FieldsString + " " + " from " + queryStructure.EntitiesString;
                qrystr += Environment.NewLine;
                if (Filter != null)
                {
                    List<AppFilter> FilterwoPaging = Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator) && !p.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase) && !p.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (FilterwoPaging != null)
                    {
                        if (FilterwoPaging.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            qrystr += Environment.NewLine;
                            if (FoundWhere == false)
                            {
                                qrystr += " where " + Environment.NewLine;
                                FoundWhere = true;
                            }


                            int i = 0;

                            foreach (AppFilter item in FilterwoPaging)
                            {
                                //item.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase) && item.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)
                                if (!string.IsNullOrEmpty(item.FilterValue) && !string.IsNullOrWhiteSpace(item.FilterValue))
                                {
                                    //  EntityField f = ent.Fields.Where(i => i.fieldname == item.FieldName).FirstOrDefault();
                                    //>= (((pageNumber-1) * pageSize) + 1)

                                    if (item.Operator.ToLower() == "between")
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " and  :p_" + item.FieldName + "1 " + Environment.NewLine;
                                    }
                                    else
                                    {
                                        qrystr += item.FieldName + " " + item.Operator + " :p_" + item.FieldName + " " + Environment.NewLine;
                                    }
                                }

                                if (i < FilterwoPaging.Count - 1)
                                {
                                    qrystr += " and ";
                                }
                                i++;
                            }
                        }

                    }
                }

                if (originalquery.ToLower().Contains("where"))
                {
                    qrystr += Environment.NewLine;

                    string[] whereSeparators = new string[] { " where " };

                    string[] spwhere = originalquery.ToLower().Split(whereSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.WhereCondition = spwhere[0];
                    if (FoundWhere == false)
                    {
                        qrystr += " where " + Environment.NewLine;
                        FoundWhere = true;
                    }
                    qrystr += spwhere[0];
                    qrystr += Environment.NewLine;



                }
                if (originalquery.ToLower().Contains("group by"))
                {
                    string[] groupbySeparators = new string[] { " group by " };

                    string[] groupbywhere = originalquery.ToLower().Split(groupbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.GroupbyCondition = groupbywhere[1];
                    qrystr += " group by " + groupbywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("having"))
                {
                    string[] havingSeparators = new string[] { " having " };

                    string[] havingywhere = originalquery.ToLower().Split(havingSeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.HavingCondition = havingywhere[1];
                    qrystr += " having " + havingywhere[1];
                    qrystr += Environment.NewLine;
                }
                if (originalquery.ToLower().Contains("order by"))
                {
                    string[] orderbySeparators = new string[] { " order by " };

                    string[] orderbywhere = originalquery.ToLower().Split(orderbySeparators, StringSplitOptions.RemoveEmptyEntries);
                    queryStructure.OrderbyCondition = orderbywhere[1];
                    qrystr += " order by " + orderbywhere[1];

                }
                if (Filter != null)
                {
                    if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any() && Filter.Count >= 2)
                    {
                        if (Filter.Where(o => o.FieldName.Equals("Pagesize", StringComparison.OrdinalIgnoreCase)).Any() || Filter.Where(o => o.FieldName.Equals("pagenumber", StringComparison.OrdinalIgnoreCase)).Any())
                        {
                            qrystr = PagedQuery(qrystr, Filter);
                        }
                    }
                }


            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Unable Build Query Object {originalquery}", DateTime.Now, 0, "Error", Errors.Failed);
            }


            if (qrystr.TrimEnd().EndsWith("and"))
            {
                qrystr = qrystr.Substring(0, qrystr.LastIndexOf("and") - 1);
            }
            return qrystr;
        }
        public virtual DuckDBCommand GetDataCommand()
        {
            DuckDBCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            try
            {
                if (Dataconnection.ConnectionStatus == ConnectionState.Open)
                {
                    cmd = DuckConn.CreateCommand();
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
        private EntityStructure GetEntityStructure(DataTable schemaTable)
        {
            EntityStructure entityStructure = new EntityStructure();
            string columnNameKey = "column_name";
            string dataTypeKey = "data_type";
            string maxLengthKey = "character_maximum_length";
            string numericPrecisionKey = "numeric_precision";
            string numericScaleKey = "numeric_scale";
            string isNullableKey = "is_nullable";
            // Add more keys for other properties

            foreach (DataRow row in schemaTable.Rows)
            {
                EntityField field = new EntityField();
                field.fieldname = row[columnNameKey].ToString();
                field.fieldtype = row[dataTypeKey].ToString();

                // Handle possible DBNull values and check for column existence
                field.Size1 = schemaTable.Columns.Contains(maxLengthKey) && row[maxLengthKey] != DBNull.Value ? Convert.ToInt32(row[maxLengthKey]) : 0;
                field.NumericPrecision = schemaTable.Columns.Contains(numericPrecisionKey) && row[numericPrecisionKey] != DBNull.Value ? Convert.ToInt16(row[numericPrecisionKey]) : (short)0;
                field.NumericScale = schemaTable.Columns.Contains(numericScaleKey) && row[numericScaleKey] != DBNull.Value ? Convert.ToInt16(row[numericScaleKey]) : (short)0;
                field.AllowDBNull = schemaTable.Columns.Contains(isNullableKey) && row[isNullableKey].ToString().ToUpper() == "YES";

                // Default values for auto-increment, primary key, and unique
                field.IsAutoIncrement = false;
                field.IsKey = false;
                field.IsUnique = false;
                // Map other schema properties to the EntityField instance
                // ...

                entityStructure.Fields.Add(field);
            }
            return entityStructure;
        }
        public EntityStructure GetEntityStructure( string tableName)
        {
            EntityStructure entityStructure = new EntityStructure();
            entityStructure.EntityName = tableName;

            DataTable schemaTable = DuckConn.GetSchema("Columns", new[] { null, null, tableName, null });
            return GetEntityStructure(schemaTable);
        }
        public DataTable GetTableSchemaFromQuery(string sqlQuery)
        {
            DataTable schemaTable = new DataTable();


            DuckDBCommand command = GetDataCommand();
            command.CommandText = sqlQuery;
         
                    // Execute the command with SchemaOnly behavior to get only the schema
                    using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        schemaTable = reader.GetSchemaTable();
                    }
              
           
            return schemaTable;
        }
        public DataTable GetTableSchema(string tableName)
        {
            DataTable schemaTable = new DataTable();


            // Using a parameterized query to avoid SQL injection
            string query = $"SELECT * FROM information_schema.columns WHERE table_name = '{tableName}'";
            DuckDBCommand command = GetDataCommand();

            command.CommandText = query;
           // command.Parameters.Add(new DuckDBParameter("table_name", tableName));
            using (var reader = command.ExecuteReader())
            {
                schemaTable.Load(reader);
            }



            return schemaTable;
        }
        #endregion
    }
public enum compressiontype
    {
        none,gzip,zstd,auto
    }

}
