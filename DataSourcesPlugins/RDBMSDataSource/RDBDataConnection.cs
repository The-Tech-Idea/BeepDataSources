﻿
using System;
using System.Data;
using System.IO;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Helpers;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DriversConfigurations;

namespace TheTechIdea.Beep.DataBase
{
    public class RDBDataConnection : IDataConnection
    {
        public bool InMemory { get; set; } = false;
        public int ID { get; set; }
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public IDbConnection DbConn { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Closed;
        public ConnectionDriversConfig DataSourceDriver { get; set; }
        public IDMLogger Logger { get; set; }
        public IErrorsInfo ErrorObject { get; set; }

        string ConnString { get; set; }
        public IConnectionProperties ConnectionProp { get; set; } = new ConnectionProperties();
        public RDBDataConnection(IDMEEditor pDMEEditor)
        {
            DMEEditor = pDMEEditor;

        }
        public virtual ConnectionState OpenConnection(DataSourceType dbtype, string connectionstring)
        {
            ConnectionProp.DatabaseType = dbtype;
            ConnectionProp.ConnectionString = connectionstring;
            return OpenConn();
        }
        public virtual ConnectionState OpenConnection(DataSourceType dbtype, string host, int port, string database, string userid, string password, string parameters)
        {
            ConnectionProp.DatabaseType = dbtype;
            ConnectionProp.Host = host;
            ConnectionProp.Port = port;
            ConnectionProp.Database = database;
            ConnectionProp.UserID = userid;
            ConnectionProp.Password = password;
            ConnectionProp.Parameters = parameters;
            return OpenConn();
        }
        public string ReplaceValueFromConnectionString()
        {
            //string rep="";
            //if (string.IsNullOrWhiteSpace(ConnString) == false )
            //{

            //    rep = ConnString.Replace("{Host}", ConnectionProp.Host);
            //    rep = rep.Replace("{UserID}", ConnectionProp.UserID);
            //    rep = rep.Replace("{Password}", ConnectionProp.Password);
            //    rep = rep.Replace("{Database}", ConnectionProp.Database);
            //    rep = rep.Replace("{Port}", ConnectionProp.Port.ToString());


            //    if (rep.Contains("{Url}"))
            //    {
            //        rep = rep.Replace("{Url}", ConnectionProp.Url);
            //    }
            //    if (!string.IsNullOrEmpty(ConnectionProp.FilePath))
            //    {
            //        if (ConnectionProp.FilePath.StartsWith(".") || ConnectionProp.FilePath.Equals("./") || ConnectionProp.FilePath.Equals(".\\"))
            //        {
            //            ConnectionProp.FilePath = ConnectionProp.FilePath.Replace(".", DMEEditor.ConfigEditor.ExePath);
            //        }
            //    }

            //    if (rep.Contains("{File}"))
            //    {
            //        string file = ConnectionProp.FileName;
            //        string dirpath= ConnectionProp.FilePath;
            //        string filename = string.Empty;
            //        if(string.IsNullOrEmpty(dirpath))
            //        {
            //            filename = file;
            //        }else
            //            filename=Path.Combine(dirpath, file);
            //        rep = rep.Replace("{File}", filename);
            //    }
            //}

            return ConnectionHelper.ReplaceValueFromConnectionString(DataSourceDriver, ConnectionProp, DMEEditor);
        }
        public virtual ConnectionState OpenConnection()
        {

            ConnectionStatus = OpenConn();
            return ConnectionStatus;
        }
        public virtual ConnectionState OpenConn()
        {
            if (DbConn != null)
            {
                if (DbConn.State == ConnectionState.Open)
                {

                    ConnectionStatus = DbConn.State;
                    DMEEditor.AddLogMessage("Success", $"RDBMS already Open {ConnectionProp.ConnectionName}", DateTime.Now, -1, "", Errors.Ok);
                    return DbConn.State;
                }

            }
            try
            {
           
                if (DataSourceDriver != null)
                {
                    DbConn = (IDbConnection)DMEEditor.assemblyHandler.GetInstance(DataSourceDriver.DbConnectionType);
                }

                if (DbConn != null)
                {
                    //if (!string.IsNullOrEmpty(ConnectionProp.ConnectionString))
                    //{
                    //    ConnString = ConnectionProp.ConnectionString;
                    //}
                    //else ConnString = DataSourceDriver.ConnectionString;

                    DbConn.ConnectionString = ReplaceValueFromConnectionString(); //ConnectionProp.ConnectionString;
                    ConnectionProp.ConnectionString = DbConn.ConnectionString;
                }
                else
                {
                    ConnectionStatus = ConnectionState.Broken;
                    DMEEditor.AddLogMessage("Fail", $"Could Find DataSource Drivers {ConnectionProp.ConnectionName}", DateTime.Now, 0, null, Errors.Failed);
                    return ConnectionState.Broken;
                }

            }
            catch (Exception e)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not get instance Driver for {ConnectionProp.ConnectionName}- {e.Message}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Failed);

            }

            try
            {
                if (DbConn != null)
                {
                    if (ConnectionProp.FilePath != null && ConnectionProp.FileName != null && !string.IsNullOrEmpty(ConnectionProp.FilePath) && !string.IsNullOrEmpty(ConnectionProp.FileName))
                    {
                        if (System.IO.File.Exists(Path.Combine(ConnectionProp.FilePath, ConnectionProp.FileName)))
                        {
                            DbConn.Open();
                            //       DMEEditor.AddLogMessage("Success", $"Open RDBMS Connection to {ConnectionProp.ConnectionName}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Ok);
                            ConnectionStatus = DbConn.State;
                        }
                        else
                        {
                            ConnectionStatus = ConnectionState.Broken;
                        }
                    }
                    else
                    {
                        DbConn.Open();
                        DMEEditor.AddLogMessage("Success", $"Open RDBMS Connection to {ConnectionProp.ConnectionName}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Ok);
                        ConnectionStatus = DbConn.State;
                        if (ConnectionStatus == ConnectionState.Open)
                        {
                            // Check if need to change schema name
                            if (ConnectionProp.DatabaseType == DataSourceType.Oracle || ConnectionProp.DatabaseType == DataSourceType.SqlServer)
                            {
                                if (ConnectionProp.SchemaName != null)
                                {
                                    IDbCommand cmd = DbConn.CreateCommand();
                                    switch (ConnectionProp.DatabaseType)
                                    {
                                        case DataSourceType.Oracle:

                                            cmd.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {ConnectionProp.SchemaName}";

                                            break;
                                        case DataSourceType.SqlServer:
                                            cmd.CommandText = $"ALTER LOGIN {ConnectionProp.UserID} with DEFAULT_DATABASE = {ConnectionProp.Database}";
                                            break;

                                    }
                                    try
                                    {
                                        var x = cmd.ExecuteNonQuery();

                                        ConnectionStatus = DbConn.State;
                                    }

                                    catch (Exception e)
                                    {
                                        DMEEditor.AddLogMessage("Fail", $"Could not alter Schema for RDBMS  to {ConnectionProp.ConnectionName}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Failed);

                                    }

                                }
                            }
                        }

                    }

                }
                else
                {
                    DMEEditor.AddLogMessage("Fail", $"Could not get Drivers for RDBMS Connection to {ConnectionProp.ConnectionName}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Failed);
                    ConnectionStatus = ConnectionState.Closed;
                }
            }
            catch (Exception e)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not Open RDBMS Connection to {ConnectionProp.ConnectionName}- {e.Message}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Failed);
                ConnectionStatus = DbConn.State;
            }

            return ConnectionStatus;
        }
        public virtual ConnectionState CloseConn()
        {
            if (DbConn != null)
            {
                if (DbConn.State == ConnectionState.Open)
                {
                    ErrorObject.Flag = Errors.Ok;

                    try
                    {
                        DbConn.Close();
                        ConnectionStatus = ConnectionState.Closed;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor.AddLogMessage("Fail", $"Could not close Connetion Database Function End {ex.Message}", DateTime.Now, 0, null, Errors.Failed);

                    }

                    return DbConn.State;
                }
                else
                {
                    ConnectionStatus = ConnectionState.Closed;
                    return ConnectionStatus;
                }
            }
            else
            {
                ConnectionStatus = ConnectionState.Closed;
                DMEEditor.AddLogMessage("Fail", $"Closing RDBMS Connection  {ConnectionProp.ConnectionName}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Ok);
                return ConnectionStatus;
            }




        }

    }
}
