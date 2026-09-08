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
            try
            {
                if (RDBMSConnection?.DbConn == null ||
                    RDBMSConnection.DbConn.State != ConnectionState.Open)
                {
                    SetFailure("Error in Begin Transaction: the connection is not open");
                    return ErrorObject;
                }

                // Reuse an already-open transaction rather than shadowing it.
                // Most providers throw on a second concurrent local transaction,
                // and shadowing would orphan the first one exactly as before.
                if (ActiveTransaction != null)
                {
                    SetSuccess("A transaction is already open on this datasource; reusing it.");
                    return ErrorObject;
                }

                _activeTransaction = RDBMSConnection.DbConn.BeginTransaction();
                SetSuccess();
            }
            catch (Exception ex)
            {
                DisposeActiveTransaction();
                HandleDatabaseError(ex, DatasourceName, "begin a transaction on");
            }
            return ErrorObject;
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
            try
            {
                var tx = ActiveTransaction;
                if (tx == null)
                {
                    // Was a silent no-op returning whatever flag the previous operation left.
                    SetSuccess("EndTransaction: there was no open transaction to roll back.");
                }
                else
                {
                    tx.Rollback();
                    SetSuccess();
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "roll back the transaction on");
            }
            finally
            {
                DisposeActiveTransaction();
            }
            return ErrorObject;
        }

        /// <summary>
        /// Commits the open transaction.
        /// </summary>
        public virtual IErrorsInfo Commit(PassedArgs args)
        {
            try
            {
                var tx = ActiveTransaction;
                if (tx == null)
                {
                    // Was a silent no-op. UnitofWork branches on this return value, and the method
                    // used to hand back DMEEditor.ErrorObject without ever setting it to Ok — so a
                    // successful commit reported whatever flag an earlier, unrelated operation had
                    // left, and a commit with no transaction reported nothing at all.
                    SetSuccess("Commit: there was no open transaction to commit.");
                }
                else
                {
                    tx.Commit();
                    SetSuccess();
                }
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "commit the transaction on");
            }
            finally
            {
                DisposeActiveTransaction();
            }
            return ErrorObject;
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
                // Logged, deliberately WITHOUT touching ErrorObject. This runs in the `finally` of
                // Commit, so flagging a failure here turned a transaction that had already been
                // committed into a reported failure — and the caller would then typically retry or
                // roll back work the server had already durably accepted.
                Logger?.WriteLog($"Error disposing the transaction on {DatasourceName}: {ex.Message}");
            }
            finally
            {
                _activeTransaction = null;
            }
        }
    }
}
