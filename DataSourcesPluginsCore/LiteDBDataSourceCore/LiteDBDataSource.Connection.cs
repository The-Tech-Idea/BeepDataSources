using System;
using System.Data;
using System.IO;
using LiteDB;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using DataManagementModels.Editor;

namespace LiteDBDataSourceCore
{
    public partial class LiteDBDataSource
    {
        public IErrorsInfo BeginTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB uses file-based transactions and checkpoints.
                if (!EnsureConnectionReady(nameof(BeginTransaction)))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Database connection is not open.";
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB commits automatically when operations complete
                // Explicit commit may not be necessary, but we can ensure consistency
                if (db != null)
                {
                    db.Checkpoint();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                // LiteDB transactions end automatically
                // Ensure checkpoint is called if needed
                if (db != null)
                {
                    db.Checkpoint();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor?.AddLogMessage("Beep", $"Error in End Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return ErrorObject;
        }

        public ConnectionState Openconnection()
        {
            try
            {
                InitDataConnection();
                if (string.IsNullOrWhiteSpace(_connectionString))
                {
                    ErrorObject ??= new ErrorsInfo();
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = "Connection string is empty";
                    ConnectionStatus = ConnectionState.Closed;
                    return ConnectionStatus;
                }

                string directory = Path.GetDirectoryName(_connectionString);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (db == null)
                {
                    db = new LiteDatabase(_connectionString);
                }

                ConnectionStatus = ConnectionState.Open;
                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Connection opened successfully.";
            }
            catch (Exception ex)
            {
                if (db != null)
                {
                    db.Dispose();
                    db = null;
                }

                ErrorObject ??= new ErrorsInfo();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                DMEEditor.AddLogMessage("Beep", $"Failed to open LiteDB connection: " + ex.Message, DateTime.Now, -1, null, Errors.Failed);
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }

        public ConnectionState Closeconnection()
        {
            try
            {
                if (db != null)
                {
                    db.Dispose();
                    db = null;
                }
            }
            finally
            {
                ConnectionStatus = ConnectionState.Closed;
            }
            return ConnectionStatus;
        }
    }
}
