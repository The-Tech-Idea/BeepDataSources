using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        #region "IDataSource Interface Methods"

        /// <summary>
        /// Opens the underlying connection and records the outcome on <see cref="ErrorObject"/>.
        /// </summary>
        /// <remarks>
        /// Note that the assignment to <c>ConnectionStatus</c> that used to sit here was dead:
        /// the property's setter is <c>set { }</c> (RDBSource.cs). It worked only because
        /// <see cref="RDBDataConnection.OpenConnection"/> sets its own field and the getter reads
        /// through to it. The call is kept, the misleading assignment is not.
        ///
        /// Neither this method nor <see cref="Closeconnection"/> previously touched
        /// <see cref="ErrorObject"/> at all, so a caller doing
        /// <c>Openconnection(); if (ErrorObject.Flag == Errors.Ok) …</c> read whatever the previous
        /// unrelated operation had left there.
        /// </remarks>
        public virtual ConnectionState Openconnection()
        {
            try
            {
                if (RDBMSConnection != null)
                {
                    RDBMSConnection.OpenConnection();
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    SetSuccess();
                }
                else
                {
                    SetFailure($"Could not open the connection to {DatasourceName}; state is {ConnectionStatus}.");
                }
            }
            catch (Exception ex)
            {
                // The RDBMSConnection cast can throw InvalidCastException, which used to escape
                // uncaught — including out of Dispose, which calls Closeconnection.
                HandleDatabaseError(ex, DatasourceName, "open the connection to");
            }

            return ConnectionStatus;
        }

        /// <summary>
        /// Opens the underlying connection asynchronously, recording the outcome on
        /// <see cref="ErrorObject"/> exactly as <see cref="Openconnection"/> does.
        /// </summary>
        /// <remarks>
        /// Additive: nothing on <c>IDataSource</c> declares this, so no driver has to implement it.
        /// It exists so the resilience pipeline has something real to await -- it used to call
        /// <c>Task.Run(() =&gt; Openconnection(), ct)</c>, which parks a pool thread on a blocking
        /// open and cannot be cancelled once the open has started.
        /// </remarks>
        public virtual async Task<ConnectionState> OpenconnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (RDBMSConnection != null)
                {
                    await RDBMSConnection.OpenConnAsync(cancellationToken).ConfigureAwait(false);
                }

                if (ConnectionStatus == ConnectionState.Open)
                {
                    SetSuccess();
                }
                else
                {
                    SetFailure($"Could not open the connection to {DatasourceName}; state is {ConnectionStatus}.");
                }
            }
            catch (OperationCanceledException)
            {
                // The caller asked to stop; that is not a datasource failure to record.
                throw;
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "open the connection to");
            }

            return ConnectionStatus;
        }

        /// <summary>
        /// Closes the underlying connection and records the outcome on <see cref="ErrorObject"/>.
        /// </summary>
        public virtual ConnectionState Closeconnection()
        {
            try
            {
                // One call, not three. RDBMSConnection is Dataconnection — the property is a cast of
                // the same reference — so the previous body invoked CloseConn() on the same object
                // three times.
                Dataconnection?.CloseConn();
                SetSuccess();
            }
            catch (Exception ex)
            {
                HandleDatabaseError(ex, DatasourceName, "close the connection to");
            }

            return ConnectionStatus;
        }
        #endregion
    }
}
