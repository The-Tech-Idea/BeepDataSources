using System;
using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        /// <summary>
        /// The transaction opened by <see cref="BeginTransaction"/>, held so that
        /// commands can carry it and <see cref="Commit"/> / <see cref="EndTransaction"/>
        /// can finish it.
        /// </summary>
        /// <remarks>
        /// This used to be thrown away. <c>BeginTransaction</c> called
        /// <c>DbConn.BeginTransaction()</c> and dropped the returned
        /// <see cref="IDbTransaction"/> on the floor; <c>Commit</c> and
        /// <c>EndTransaction</c> then tried to recover it by reflecting a
        /// "Transaction" property off the CONNECTION, which ADO.NET connections do
        /// not expose. Both were therefore silent no-ops, and the transaction was
        /// never committed or rolled back.
        ///
        /// Worse, the connection was left with a pending local transaction, so
        /// providers that enforce the command/transaction association rejected
        /// every subsequent command:
        ///
        ///   "ExecuteNonQuery requires the command to have a transaction when the
        ///    connection assigned to the command is in a pending local transaction.
        ///    The Transaction property of the command has not been initialized."
        ///
        /// That is every client/server RDBMS this class backs — SQL Server,
        /// Postgres, Oracle, MySQL. System.Data.SQLite does not enforce the
        /// association, which is why testing against a single file-based driver
        /// never showed it. Found by the SQL Server example in Beep.Desktop.
        /// (2026-08-03)
        /// </remarks>
        private IDbTransaction _activeTransaction;

        /// <summary>
        /// The transaction currently open on this source, or null when there is
        /// none. A transaction whose Connection has gone null has already been
        /// completed and is not returned.
        /// </summary>
        public IDbTransaction ActiveTransaction =>
            _activeTransaction?.Connection != null ? _activeTransaction : null;

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
                if (RDBMSConnection?.DbConn == null ||
                    RDBMSConnection.DbConn.State != ConnectionState.Open)
                {
                    DMEEditor.AddLogMessage("Beep",
                        "Error in Begin Transaction: the connection is not open",
                        DateTime.Now, 0, DatasourceName, Errors.Failed);
                    return DMEEditor.ErrorObject;
                }

                // Reuse an already-open transaction rather than shadowing it.
                // Most providers throw on a second concurrent local transaction,
                // and shadowing would orphan the first one exactly as before.
                if (ActiveTransaction != null)
                    return DMEEditor.ErrorObject;

                _activeTransaction = RDBMSConnection.DbConn.BeginTransaction();
            }
            catch (Exception ex)
            {
                _activeTransaction = null;
                DMEEditor.AddLogMessage("Beep", $"Error in Begin Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Ends a database transaction by ROLLING IT BACK.
        /// </summary>
        /// <param name="args">Optional arguments related to the transaction.</param>
        /// <returns>An IErrorsInfo object indicating the success or failure of ending the transaction.</returns>
        /// <remarks>
        /// Rollback, not commit — this is the callers' contract. UnitofWork.Commit
        /// calls <see cref="Commit"/> when every item succeeded and this when one
        /// failed or an exception escaped, and UnitofWork.Rollback calls this
        /// directly. Do not "fix" it to commit.
        /// </remarks>
        public virtual IErrorsInfo EndTransaction(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                ActiveTransaction?.Rollback();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in end Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            finally
            {
                DisposeActiveTransaction();
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Commits the open transaction.
        /// </summary>
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            ErrorObject.Flag = Errors.Ok;
            try
            {
                ActiveTransaction?.Commit();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Commit Transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            finally
            {
                DisposeActiveTransaction();
            }
            return DMEEditor.ErrorObject;
        }

        /// <summary>
        /// Disposes the held transaction and clears it, so the connection is left
        /// with no pending local transaction whatever happened above.
        /// </summary>
        private void DisposeActiveTransaction()
        {
            try
            {
                _activeTransaction?.Dispose();
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error disposing transaction {ex.Message} ", DateTime.Now, 0, null, Errors.Failed);
            }
            finally
            {
                _activeTransaction = null;
            }
        }
    }
}
