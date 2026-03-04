using System.Data;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource
    {
        public virtual IErrorsInfo BeginTransaction(PassedArgs args)
        {
            try
            {
                if (Openconnection() != ConnectionState.Open)
                {
                    return SetError(Errors.Failed, "Cannot begin transaction because connection is not open.");
                }

                if (_transactionStarted)
                {
                    return SetError(Errors.Ok, "Transaction already started.");
                }

                ExecuteSql("BEGIN TRANSACTION;");
                _transactionStarted = true;
                return SetError(Errors.Ok, "Transaction started.");
            }
            catch (Exception ex)
            {
                return SetError(Errors.Failed, "Error beginning transaction.", ex);
            }
        }

        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                if (!_transactionStarted)
                {
                    return SetError(Errors.Ok, "No active transaction to commit.");
                }

                ExecuteSql("COMMIT;");
                _transactionStarted = false;
                return SetError(Errors.Ok, "Transaction committed.");
            }
            catch (Exception ex)
            {
                return SetError(Errors.Failed, "Error committing transaction.", ex);
            }
        }

        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            try
            {
                if (_transactionStarted)
                {
                    ExecuteSql("ROLLBACK;");
                    _transactionStarted = false;
                    return SetError(Errors.Ok, "Transaction rolled back and ended.");
                }

                return SetError(Errors.Ok, "No active transaction.");
            }
            catch (Exception ex)
            {
                return SetError(Errors.Failed, "Error ending transaction.", ex);
            }
        }
    }
}
