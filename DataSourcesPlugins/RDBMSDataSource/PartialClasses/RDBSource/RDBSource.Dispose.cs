using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource, IAsyncDisposable
    {
        #region "dispose"
        private bool _rdsDisposed;

        /// <summary>
        /// Releases everything this datasource owns.
        /// </summary>
        /// <remarks>
        /// The previous implementation closed the connection and then set <c>Entities</c> and
        /// <c>EntitiesNames</c> to <b>null</b>, releasing nothing else. Two problems with that:
        ///
        /// Nulling non-nullable collections turned any post-dispose access into a
        /// NullReferenceException rather than an empty result — <c>Clear()</c> is the correct
        /// disposal action, and callers get an empty list instead of a crash.
        ///
        /// And it left a live <see cref="IDbTransaction"/> in <c>_activeTransaction</c> with its
        /// connection closed underneath it, a live <c>IDbCommand</c> in the shared <c>command</c>
        /// field, the entity-structure cache (whose loader closure captures <c>this</c>, so holding
        /// the cache kept the disposed source alive), and the provider connection itself — which is
        /// only ever <c>Close()</c>d, never <c>Dispose()</c>d.
        ///
        /// Every step is individually guarded. <c>Closeconnection()</c> can throw — the
        /// <c>RDBMSConnection</c> cast is unguarded — and if it did, <c>_rdsDisposed</c> was never
        /// set, <c>GC.SuppressFinalize</c> never ran, and the exception escaped <c>Dispose()</c>,
        /// which breaks <c>using</c> blocks and container teardown.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_rdsDisposed)
                return;

            if (disposing)
            {
                // Roll back before closing: a pending local transaction whose connection is closed
                // underneath it leaves the connection unusable on pooled providers.
                SafelyDispose(() =>
                {
                    if (_activeTransaction != null)
                    {
                        try { _activeTransaction.Rollback(); }
                        catch { /* the provider may already have ended it */ }
                        DisposeActiveTransaction();
                    }
                }, "active transaction");

                SafelyDispose(() =>
                {
                    command?.Dispose();
                    command = null;
                    ObjectsCreated = false;
                    lastentityname = null;
                }, "cached command");

                SafelyDispose(() =>
                {
                    _entityCache?.Clear();
                    _entityCache = null;
                }, "entity structure cache");

                SafelyDispose(() => Closeconnection(), "connection close");

                SafelyDispose(() =>
                {
                    // Close() returns a pooled connection; Dispose() releases the native handles
                    // that Oracle, ODBC and SQLite hold on to.
                    if (Dataconnection is RDBDataConnection rdb && rdb.DbConn != null)
                    {
                        rdb.DbConn.Dispose();
                        rdb.DbConn = null;
                    }
                }, "provider connection");

                // Clear, do not null.
                SafelyDispose(() =>
                {
                    Entities?.Clear();
                    EntitiesNames?.Clear();
                }, "entity collections");
            }

            _rdsDisposed = true;
        }

        /// <summary>
        /// Asynchronous disposal. Disposes the provider connection through
        /// <see cref="DbConnection.DisposeAsync"/> where the provider supports it, then runs the
        /// synchronous path for everything else.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_rdsDisposed)
            {
                if (Dataconnection is RDBDataConnection rdb && rdb.DbConn is DbConnection dbConn)
                {
                    try
                    {
                        await dbConn.DisposeAsync().ConfigureAwait(false);
                        rdb.DbConn = null;
                    }
                    catch (Exception ex)
                    {
                        Logger?.WriteLog($"Error disposing the connection for {DatasourceName} asynchronously: {ex.Message}");
                    }
                }
            }

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Runs one disposal step, recording rather than propagating any failure.
        /// </summary>
        /// <remarks>
        /// Dispose must not throw, and one failing step must not skip the rest.
        /// </remarks>
        private void SafelyDispose(Action step, string what)
        {
            try
            {
                step();
            }
            catch (Exception ex)
            {
                Logger?.WriteLog($"Error disposing {what} for {DatasourceName}: {ex.Message}");
            }
        }

        #endregion
    }
}
