using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.Text.RegularExpressions;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "RDBSSource Database Methods"

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
    }
}
