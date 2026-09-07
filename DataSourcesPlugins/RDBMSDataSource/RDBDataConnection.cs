
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
        /// <summary>
        /// Opens the connection without blocking a thread on the network round trip.
        /// </summary>
        /// <remarks>
        /// <see cref="OpenConn"/> builds a fresh <c>DbConn</c> through the driver, and that
        /// construction has no async equivalent. But the common case after <see cref="CloseConn"/>
        /// is a <c>DbConn</c> that already exists and is merely <c>Closed</c> -- <c>CloseConn</c>
        /// calls <c>Close()</c> and keeps the object -- and reopening that is exactly what
        /// <see cref="DbConnection.OpenAsync(CancellationToken)"/> is for: a genuinely awaitable
        /// open that honours the token, rather than a pool thread parked on a blocking one.
        ///
        /// Anything else falls back to the synchronous path, including a failed async open: when the
        /// existing connection object is unusable, rebuilding it from the driver is the right
        /// recovery, and that is what <see cref="OpenConn"/> does.
        /// </remarks>
        public virtual async Task<ConnectionState> OpenConnAsync(CancellationToken cancellationToken = default)
        {
            if (DbConn is DbConnection dbConn)
            {
                if (dbConn.State == ConnectionState.Open)
                {
                    ConnectionStatus = ConnectionState.Open;
                    return ConnectionStatus;
                }

                if (dbConn.State == ConnectionState.Closed && !string.IsNullOrWhiteSpace(dbConn.ConnectionString))
                {
                    try
                    {
                        await dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
                        ConnectionStatus = dbConn.State;
                        if (ErrorObject != null)
                        {
                            ErrorObject.Flag = Errors.Ok;
                        }
                        return ConnectionStatus;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        DMEEditor?.AddLogMessage("Beep",
                            $"Async reopen of {ConnectionProp?.ConnectionName ?? "the connection"} failed ({ex.Message}); rebuilding it from the driver.",
                            DateTime.Now, -1, "", Errors.Warning);
                    }
                }
            }

            return OpenConn();
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
                    // Dispose the previous connection before replacing it.
                    //
                    // The early return above only fires when the existing connection is Open, so a
                    // Closed or Broken one fell through to here and was overwritten with no Dispose.
                    // CloseConn() only calls Close() and never nulls the field, so every
                    // open→close→open cycle stranded one provider connection object — and for
                    // Oracle, ODBC and SQLite, Dispose() releases native handles that Close() does
                    // not.
                    if (DbConn != null)
                    {
                        try
                        {
                            DbConn.Dispose();
                        }
                        catch (Exception disposeEx)
                        {
                            Logger?.WriteLog($"Could not dispose the previous connection for " +
                                             $"{ConnectionProp?.ConnectionName}: {disposeEx.Message}");
                        }
                        DbConn = null;
                    }

                    DbConn = (IDbConnection)DMEEditor.assemblyHandler.GetInstance(DataSourceDriver.DbConnectionType);
                }

                if (DbConn != null)
                {
                    // Assign the built connection string, but do NOT read it back into
                    // ConnectionProp. ADO.NET providers strip the password from the ConnectionString
                    // getter unless Persist Security Info=true, so writing it back replaced the
                    // stored connection string with a password-less copy — and once ConfigEditor
                    // persisted that, the password was gone for good and every later connect failed.
                    DbConn.ConnectionString = ReplaceValueFromConnectionString();
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
                            // Set the session's default schema. Oracle only.
                            //
                            // There used to be a SQL Server arm here running
                            //     ALTER LOGIN {UserID} with DEFAULT_DATABASE = {Database}
                            // on EVERY connection open where SchemaName was non-null. That is not a
                            // session setting: it permanently rewrites the login object server-side,
                            // changing the default database for every future connection by that login
                            // from any application, and it needs ALTER ANY LOGIN, which an
                            // application account normally should not hold. It also did not do what
                            // it was reaching for — switching the session's database needs USE, and
                            // the database is already selected by Initial Catalog in the connection
                            // string. On top of that the branch was gated on SchemaName but used
                            // Database, and interpolated both identifiers unquoted.
                            //
                            // Removed rather than corrected: SQL Server connections get their
                            // database from the connection string, so there is nothing to do here.
                            if (ConnectionProp.DatabaseType == DataSourceType.Oracle &&
                                !string.IsNullOrWhiteSpace(ConnectionProp.SchemaName))
                            {
                                using (IDbCommand cmd = DbConn.CreateCommand())
                                {
                                    // Identifiers cannot be parameterised. Reject anything that is not
                                    // a plain identifier rather than interpolating it blind.
                                    string schema = ConnectionProp.SchemaName.Trim();
                                    if (Regex.IsMatch(schema, @"^[A-Za-z_][A-Za-z0-9_$#]*$"))
                                    {
                                        cmd.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {schema}";
                                        try
                                        {
                                            cmd.ExecuteNonQuery();
                                            ConnectionStatus = DbConn.State;
                                        }
                                        catch (Exception e)
                                        {
                                            DMEEditor.AddLogMessage("Warning", $"Could not set the current schema for {ConnectionProp.ConnectionName}: {e.Message}", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Warning);
                                        }
                                    }
                                    else
                                    {
                                        DMEEditor.AddLogMessage("Warning", $"Schema name '{schema}' for {ConnectionProp.ConnectionName} is not a plain identifier; the session schema was left unchanged.", DateTime.Now, 0, ConnectionProp.ConnectionName, Errors.Warning);
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
                ConnectionStatus = DbConn?.State ?? ConnectionState.Broken;
            }

            return ConnectionStatus;
        }
        public virtual ConnectionState CloseConn()
        {
            if (DbConn != null)
            {
                if (DbConn.State == ConnectionState.Open)
                {
                    try
                    {
                        DbConn.Close();
                        ConnectionStatus = ConnectionState.Closed;

                        // Set AFTER the close, not before. The optimistic assignment used to sit
                        // outside the try, and the catch below relies on AddLogMessage to record a
                        // failure — which does nothing when no logger is attached. A close that
                        // threw therefore reported Errors.Ok. ErrorObject is also not assigned by
                        // this class's constructor, so the old unguarded write could NRE outright
                        // for anyone constructing RDBDataConnection directly.
                        if (ErrorObject != null)
                        {
                            ErrorObject.Flag = Errors.Ok;
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = $"Could not close the connection to {ConnectionProp?.ConnectionName ?? "the database"}: {ex.Message}";
                        if (ErrorObject != null)
                        {
                            ErrorObject.Flag = Errors.Failed;
                            ErrorObject.Message = message;
                            ErrorObject.Ex = ex;
                        }
                        if (DMEEditor?.ErrorObject != null)
                        {
                            DMEEditor.ErrorObject.Flag = Errors.Failed;
                            DMEEditor.ErrorObject.Message = message;
                        }
                        DMEEditor?.AddLogMessage("Fail", message, DateTime.Now, 0, ConnectionProp?.ConnectionName, Errors.Failed);
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
                return ConnectionStatus;
            }




        }

    }
}
