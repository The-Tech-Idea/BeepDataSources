
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DataManagementModels.DriversConfigurations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

using TheTechIdea.Logger;
using TheTechIdea.Util;

using System.Text.RegularExpressions;

namespace TheTechIdea.Beep.DataBase
{
    [AddinAttribute(Category = DatasourceCategory.RDBMS, DatasourceType =  DataSourceType.Oracle)]
    class OracleDataSource : RDBSource
    {
        public OracleDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per) : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
             _ParameterDelimiter = ":";
             _ColumnDelimiter = "\"";

        }
        private string _ParameterDelimiter = ":";
        private string _ColumnDelimiter = "''";
        public override string ParameterDelimiter { get => _ParameterDelimiter; set =>_ParameterDelimiter = value; }
        public override string ColumnDelimiter { get =>_ColumnDelimiter; set => _ColumnDelimiter = value; }
        public override string DisableFKConstraints(EntityStructure t1)
        {
            try
            {
                string consname = null;
               
                foreach (var item in this.GetTablesFKColumnList(t1.EntityName, GetSchemaName(), null))
                {
                    consname = item.RalationName;
                    this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE CONSTRAINT {consname}");
                }
               
                DMEEditor.ErrorObject.Message = "successfull Disabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Diabling Oracle FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
        }
        public override string EnableFKConstraints( EntityStructure t1)
        {
            try
            {
                string consname = null;
                foreach (var item in this.GetTablesFKColumnList(t1.EntityName,GetSchemaName(),null))
                {
                    consname = item.RalationName;
                    this.ExecuteSql($"ALTER TABLE {t1.EntityName} DISABLE CONSTRAINT {consname}");
                }
               
                DMEEditor.ErrorObject.Message = "successfull Enabled Oracle FK Constraints";
                DMEEditor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", "Enabing Oracle FK Constraints" + ex.Message, DateTime.Now, 0, t1.EntityName, Errors.Failed);
            }
            return DMEEditor.ErrorObject.Message;
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
        public override  object GetEntity(string EntityName, List<AppFilter> Filter)
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
                var retval=GetDataTableUsingReaderAsync(qrystr, Filter);
                return retval.Result;
            }

            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Fail", $"Error in getting entity Data({ ex.Message})", DateTime.Now, 0, "", Errors.Failed);

                return null;
            }


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
            OracleCommand command = GetDataCommandForOracle();
            Type enttype = GetEntityType(EntityName);
            var ti = Activator.CreateInstance(enttype);

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
        //public override IErrorsInfo DeleteEntity(string EntityName, object DeletedDataRow)
        //{

        //    ErrorObject.Flag = Errors.Ok;
        //    EntityStructure DataStruct = GetEntityStructure(EntityName, true);
        //    string msg;
        //    DataRowView dv;
        //    DataTable tb;
        //    DataRow dr;
        //    var sqlTran = (OracleTransaction) RDBMSConnection.DbConn.BeginTransaction();
        //    OracleCommand command = GetDataCommandForOracle();
        //    Type enttype = GetEntityType(EntityName);
        //    var ti = Activator.CreateInstance(enttype);
        //    if (recEntity != EntityName)
        //    {
        //        recNumber = 1;
        //        recEntity = EntityName;
        //    }
        //    else
        //        recNumber += 1;

        //    dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, DeletedDataRow, DataStruct);
        //    try
        //    {
        //        string updatestring = GetDeleteString(EntityName, DataStruct);
        //        command.Transaction = sqlTran;
        //        command.CommandText = updatestring;

        //        command = CreateCommandParameters(command, dr, DataStruct);
        //        int rowsUpdated = command.ExecuteNonQuery();
        //        if (rowsUpdated > 0)
        //        {
        //            msg = $"Successfully Deleted  Record  to {EntityName} : {updatestring}";
        //            //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
        //        }
        //        else
        //        {
        //            msg = $"Fail to Delete Record  from {EntityName} : {updatestring}";
        //            DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
        //        }
        //        sqlTran.Commit();
        //        command.Dispose();


        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorObject.Ex = ex;

        //        command.Dispose();
        //        try
        //        {
        //            // Attempt to roll back the transaction.
        //            sqlTran.Rollback();
        //            msg = "Unsuccessfully no Data has been written to Data Source,Rollback Complete";
        //        }
        //        catch (Exception exRollback)
        //        {
        //            // Throws an InvalidOperationException if the connection
        //            // is closed or the transaction has already been rolled
        //            // back on the server.
        //            // Console.WriteLine(exRollback.Message);
        //            msg = "Unsuccessfully no Data has been written to Data Source,Rollback InComplete";
        //            ErrorObject.Ex = exRollback;
        //        }
        //        msg = "Unsuccessfully no Data has been written to Data Source";
        //        DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);

        //    }

        //    return ErrorObject;
        //}
        //public override IErrorsInfo InsertEntity(string EntityName, object InsertedData)
        //{
        //    // DataRow tb = object UploadDataRow;
        //    ErrorObject.Flag = Errors.Ok;
        //    EntityStructure DataStruct = GetEntityStructure(EntityName, true);
        //    //DataRowView dv;
        //    //DataTable tb;
        //    DataRow dr;
        //    string msg = "";
        //    //   var sqlTran = Dataconnection.DbConn.BeginTransaction();
        //    OracleCommand command = GetDataCommandForOracle();
        //    Type enttype = GetEntityType(EntityName);
        //    var ti = Activator.CreateInstance(enttype);
        //    string updatestring = "";
        //    if (recEntity != EntityName)
        //    {
        //        recNumber = 1;
        //        recEntity = EntityName;
        //    }
        //    else
        //        recNumber += 1;

        //    dr = DMEEditor.Utilfunction.GetDataRowFromobject(EntityName, enttype, InsertedData, DataStruct);
        //    try
        //    {
        //        updatestring = GetInsertString(EntityName, DataStruct);


        //        command.CommandText = updatestring;
        //        command = CreateCommandParameters(command, dr, DataStruct);

        //        int rowsUpdated = command.ExecuteNonQuery();
        //        if (rowsUpdated > 0)
        //        {
        //            msg = $"Successfully Inserted  Record  to {EntityName} ";
        //            DMEEditor.ErrorObject.Message = msg;
        //            DMEEditor.ErrorObject.Flag = Errors.Ok;
        //            // DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Ok);
        //        }
        //        else
        //        {
        //            msg = $"Fail to Insert  Record  to {EntityName} : {updatestring}";
        //            DMEEditor.ErrorObject.Message = msg;
        //            DMEEditor.ErrorObject.Flag = Errors.Failed;


        //            //  DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, null, Errors.Failed);
        //        }
        //        // DMEEditor.AddLogMessage("Success",$"Successfully Written Data to {EntityName}",DateTime.Now,0,null, Errors.Ok);

        //    }
        //    catch (Exception ex)
        //    {
        //        msg = $"Fail to Insert  Record  to {EntityName} : {ex.Message}";
        //        ErrorObject.Ex = ex;
        //        DMEEditor.ErrorObject.Message = msg;
        //        DMEEditor.ErrorObject.Flag = Errors.Failed;
        //        command.Dispose();

        //        DMEEditor.AddLogMessage("Beep", $"{msg} ", DateTime.Now, 0, updatestring, Errors.Failed);

        //    }

        //    return ErrorObject;
        //}
        //public override string GetInsertString(string EntityName, EntityStructure DataStruct)
        //{
        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    //    EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Insertstr = "insert into " + EntityName + " (";
        //    string Valuestr = ") values (";
        //    var insertfieldname = "";
        //    // string datafieldname = "";
        //    string typefield = "";
        //    int i = DataStruct.Fields.Count();
        //    int t = 0;
        //    foreach (EntityField item in DataStruct.Fields)
        //    {

        //        Insertstr += " " + item.fieldname + "],";
        //        Valuestr += ":p_" + Regex.Replace(item.fieldname, @"\s+", "_") + ",";

        //        t += 1;
        //    }
        //    Insertstr = Insertstr.Remove(Insertstr.Length - 1);
        //    Valuestr = Valuestr.Remove(Valuestr.Length - 1);
        //    Valuestr += ")";
        //    return Insertstr + Valuestr;
        //}
        //public override string GetUpdateString(string EntityName, EntityStructure DataStruct)
        //{
        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    // List<Mapping_rep_fields> map = new List < Mapping_rep_fields >()  ; 
        //    //   map= Mapping.FldMapping;
        //    //     EntityName = Regex.Replace(EntityName, @"\s+", "");
        //    string Updatestr = @"Update " + EntityName + "  set " + Environment.NewLine;
        //    string Valuestr = "";

        //    int i = DataStruct.Fields.Count();
        //    int t = 0;
        //    foreach (EntityField item in DataStruct.Fields)
        //    {
        //        if (!DataStruct.PrimaryKeys.Any(l => l.fieldname == item.fieldname))
        //        {

        //            //     insertfieldname = Regex.Replace(item.fieldname, @"\s+", "_");
        //            Updatestr += " " + item.fieldname + " =";
        //            Updatestr += ":p_" + item.fieldname + ",";

        //        }


        //        t += 1;
        //    }

        //    Updatestr = Updatestr.Remove(Updatestr.Length - 1);

        //    Updatestr += @" where " + Environment.NewLine;
        //    i = DataStruct.PrimaryKeys.Count();
        //    t = 1;
        //    foreach (EntityField item in DataStruct.PrimaryKeys)
        //    {

        //        if (t == 1)
        //        {
        //            Updatestr += " " + item.fieldname + " =";
        //        }
        //        else
        //        {
        //            Updatestr += " and [" + item.fieldname + " =";
        //        }
        //        Updatestr += ":p_" + item.fieldname + "";

        //        t += 1;
        //    }
        //    //  Updatestr = Updatestr.Remove(Valuestr.Length - 1);
        //    return Updatestr;
        //}
        //public override string GetDeleteString(string EntityName, EntityStructure DataStruct)
        //{

        //    List<EntityField> SourceEntityFields = new List<EntityField>();
        //    List<EntityField> DestEntityFields = new List<EntityField>();
        //    string Updatestr = @"Delete from " + EntityName + "  ";
        //    int i = DataStruct.Fields.Count();
        //    int t = 0;
        //    Updatestr += @" where ";
        //    i = DataStruct.PrimaryKeys.Count();
        //    t = 1;
        //    foreach (EntityField item in DataStruct.PrimaryKeys)
        //    {

        //        if (t == 1)
        //        {
        //            Updatestr += " " + item.fieldname + " =";
        //        }
        //        else
        //        {
        //            Updatestr += " and [" + item.fieldname + " =";
        //        }
        //        Updatestr += ":p_" + item.fieldname + "";
        //        t += 1;
        //    }
        //    return Updatestr;
        //}
        #region "Command "
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
        public  OracleCommand GetDataCommandForOracle()
        {
            OracleCommand cmd = null;
            ErrorObject.Flag = Errors.Ok;
            OracleConnection conn =(OracleConnection) RDBMSConnection.DbConn;
            try
            {
                if (Dataconnection.OpenConnection() == ConnectionState.Open)
                {
                    cmd =conn.CreateCommand();
                    
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
        private async Task<DataTable> GetDataTableUsingReaderAsync(string Sql, List<AppFilter> Filter = null)
        {
            DataTable retval = new DataTable();
            OracleDataReader reader;
            OracleCommand cmd = (OracleCommand)GetDataCommandForOracle();
          
            try
            {
                // Get Filterd Query with parameters
                if (Filter != null)
                {
                    if (Filter.Count > 0)
                    {
                        if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                        {
                            foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                            {
                                OracleParameter parameter = cmd.CreateParameter();
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
                                    OracleParameter parameter1 = cmd.CreateParameter();
                                    parameter1.ParameterName = "p_" + item.FieldName + "1";
                                    parameter1.DbType = DbType.DateTime;
                                    string dr1 = Filter.Where(i => i.FieldName == item.FieldName).FirstOrDefault().FilterValue1;
                                    parameter1.Value = DateTime.Parse(dr1).ToShortDateString();
                                    cmd.Parameters.Add(parameter1);
                                }

                                //  parameter.DbType = TypeToDbType(tb.Columns[item.fieldname].DataType);
                                cmd.Parameters.Add(parameter);

                            }

                        }
                    }

                }
                // Get Table from Reader
                CancellationToken cancellationToken = new CancellationToken();
                cmd.CommandText = Sql;
               
                reader = (OracleDataReader)await cmd.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
                //reader.SuppressGetDecimalInvalidCastException = true;

                retval = new DataTable();
                if (reader.HasRows)
                {
                    retval.Load(reader);
                }
            
                reader.Close();
                cmd.Dispose();
             
            }
            catch (Exception ex)
            {

                return null;
            }
            return retval;
        }
        public   OracleDataAdapter GetDataAdapterForOracle(string Sql, List<AppFilter> Filter = null)
        {
            OracleConnection conn = null;
            OracleDataAdapter adp =null;
            OracleCommandBuilder cmdb =null ;
          
            try
            {
                ConnectionDriversConfig driversConfig = DMEEditor.Utilfunction.LinkConnection2Drivers(Dataconnection.ConnectionProp);


                //string adtype = Dataconnection.DataSourceDriver.AdapterType;
                //string cmdtype = Dataconnection.DataSourceDriver.CommandBuilderType;
                //string cmdbuildername = driversConfig.CommandBuilderType;
                //Type adcbuilderType = Type.GetType("OracleCommandBuilder");
                //List<ConstructorInfo> lsc = DMEEditor.assemblyHandler.GetInstance(adtype).GetType().GetConstructors().ToList(); ;
                //List<ConstructorInfo> lsc2 = DMEEditor.assemblyHandler.GetInstance(cmdbuildername).GetType().GetConstructors().ToList(); ;

                //ConstructorInfo ctor = lsc[GetCtorForAdapter(lsc)];
                //ConstructorInfo BuilderConstructer = lsc2[GetCtorForCommandBuilder(adcbuilderType.GetConstructors().ToList())];
                //ObjectActivator<Oracle.ManagedDataAccess.Client.OracleDataAdapter> adpActivator = GetActivator<Oracle.ManagedDataAccess.Client.OracleDataAdapter>(ctor);
                //ObjectActivator<Oracle.ManagedDataAccess.Client.OracleCommandBuilder> cmdbuilderActivator = GetActivator<Oracle.ManagedDataAccess.Client.OracleCommandBuilder>(BuilderConstructer);
                //create an instance:
                // adp = OracleDataAdapter( RDBMSConnection.DbConn);
                conn = (OracleConnection)RDBMSConnection.DbConn;
                adp = new OracleDataAdapter(Sql, conn);
                cmdb = new OracleCommandBuilder(adp);
               
                try
                {
                    //Oracle.ManagedDataAccess.Client.OracleCommand cmdBuilder = cmdbuilderActivator(adp);
                    if (Filter != null)
                    {
                        if (Filter.Count > 0)
                        {
                            if (Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue) && !string.IsNullOrEmpty(p.Operator) && !string.IsNullOrWhiteSpace(p.Operator)).Any())
                            {

                                foreach (AppFilter item in Filter.Where(p => !string.IsNullOrEmpty(p.FilterValue) && !string.IsNullOrWhiteSpace(p.FilterValue)))
                                {

                                    OracleParameter parameter = adp.SelectCommand.CreateParameter();
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
                                        OracleParameter parameter1 = adp.SelectCommand.CreateParameter();
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
                   
                    }
                
                    //  adp.ReturnProviderSpecificTypes = true;
                    adp.SuppressGetDecimalInvalidCastException = true;
                    adp.InsertCommand = cmdb.GetInsertCommand(true);
                    adp.UpdateCommand = cmdb.GetUpdateCommand(true);
                    adp.DeleteCommand = cmdb.GetDeleteCommand(true);


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

                DMEEditor.AddLogMessage("Fail", $"Error in Creating Adapter {ex.Message}", DateTime.Now, -1, ex.Message, Errors.Failed);
                adp = null;
            }

            return adp;
        }
        private OracleCommand CreateCommandParameters(OracleCommand command, DataRow r, EntityStructure DataStruct)
        {
            command.Parameters.Clear();
            command.BindByName = true;
            foreach (EntityField item in DataStruct.Fields.OrderBy(o => o.fieldname))
            {

                if (!command.Parameters.Contains("p_" + Regex.Replace(item.fieldname, @"\s+", "_")))
                {
                    OracleParameter parameter = command.CreateParameter();
                    if (!item.fieldtype.Equals("System.String", StringComparison.OrdinalIgnoreCase) && !item.fieldtype.Equals("System.DateTime", StringComparison.OrdinalIgnoreCase))
                    {
                        if (r[item.fieldname] == DBNull.Value || r[item.fieldname].ToString() == "")
                        {
                            parameter.Value = Convert.ToDecimal(null);
                        }
                        else
                        {
                            parameter.Value = r[item.fieldname];
                        }
                    }
                    else
                        if (item.fieldtype.Equals("System.DateTime", StringComparison.OrdinalIgnoreCase))
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

                                parameter.Value = Oracle.ManagedDataAccess.Types.OracleTimeStamp.Null;
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
        #endregion
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                OracleConnection conn = (OracleConnection)RDBMSConnection.DbConn;
                conn.BeginTransaction();
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                OracleConnection conn = (OracleConnection)RDBMSConnection.DbConn;
                
            }
            catch (Exception ex)
            {

                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
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
    }
}
