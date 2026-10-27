using System;
using System.Data;
using System.Threading;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource
    {
        private readonly object _transactionOwnerGate = new();
        private object? _ownedTransactionOwner;
        private bool _ownedTransactionUnresolved;
        private int _ownedTransactionReaders;
        private ErrorsInfo? _ownedTransactionErrors;
        private IDisposable? _ownedTransactionEditorScope;
        private readonly AsyncLocal<object?> _ownedTransactionFlow = new();

        // A derived driver with legacy overrides must implement its owned protocol explicitly.
        // Do not silently bypass provider hooks through this default ADO.NET implementation.
        protected bool HasDefaultNativeTransactionHooks =>
            GetType().GetMethod(nameof(BeginTransaction), new[] { typeof(PassedArgs) })?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(Commit), new[] { typeof(PassedArgs) })?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(EndTransaction), new[] { typeof(PassedArgs) })?.DeclaringType == typeof(RDBSource);

        protected static IErrorsInfo RefuseTransactionOwner(string message) => new TransactionOwnershipRefusal(message);
        private static IErrorsInfo AcknowledgeOwnedTransaction(string message) => new ErrorsInfo { Flag = Errors.Ok, Message = message };
        protected bool IsTransactionOwner(object owner)
        {
            lock (_transactionOwnerGate) return owner != null && ReferenceEquals(owner, _ownedTransactionOwner);
        }
        protected IErrorsInfo BeginTransactionForOwner(object owner)
        {
            if (_entityReadContext.Value != null) return RefuseTransactionOwner("An entity reader cannot start an independent transaction.");
            if (owner == null) return RefuseTransactionOwner("An explicit transaction owner is required.");
            lock (_transactionOwnerGate)
            {
                if (_ownedTransactionOwner != null)
                {
                    if (ReferenceEquals(owner, _ownedTransactionOwner) && !_ownedTransactionUnresolved && OpenPhysicalTransaction() != null)
                    { _ownedTransactionFlow.Value = owner; return AcknowledgeOwnedTransaction("This owner already has an open transaction."); }
                    return RefuseTransactionOwner("Another transaction owner or unresolved outcome controls this datasource.");
                }
                if (_activeTransaction != null)
                    return RefuseTransactionOwner("An existing explicit or enlisted transaction cannot be adopted by a UnitOfWork commit.");
                if (RDBMSConnection?.DbConn == null || RDBMSConnection.DbConn.State != ConnectionState.Open)
                    return RefuseTransactionOwner("The native connection is not open.");
                try
                {
                    _activeTransaction = RDBMSConnection.DbConn.BeginTransaction();
                    _ownedTransactionErrors = new ErrorsInfo { Flag = Errors.Ok };
                    _ownedTransactionOwner = owner; _ownedTransactionFlow.Value = owner; _ownedTransactionUnresolved = false;
                    _ownedTransactionEditorScope = (DMEEditor as IScopedEditorErrors)?.BeginOperationErrors(_ownedTransactionErrors);
                    SetSuccess();
                    return AcknowledgeOwnedTransaction("Native transaction begun for its explicit owner.");
                }
                catch (Exception ex)
                {
                    // If a provider returned a transaction before a later failure, preserve its owner
                    // so the caller can resolve that exact scope. Never dispose a foreign transaction.
                    if (_activeTransaction != null) { _ownedTransactionOwner = owner; _ownedTransactionUnresolved = true; }
                    HandleDatabaseError(ex, DatasourceName, "begin an owned transaction on");
                    return new ErrorsInfo { Flag = Errors.Failed, Message = "Native owned transaction begin was not acknowledged.", Ex = ex };
                }
            }
        }
        protected IErrorsInfo FinishTransactionForOwner(object owner, bool commit)
        {
            lock (_transactionOwnerGate)
            {
                if (owner == null || !ReferenceEquals(owner, _ownedTransactionOwner))
                    return RefuseTransactionOwner("Only the transaction's explicit owner may finish it.");
                if (_ownedTransactionReaders != 0)
                    return RefuseTransactionOwner("The owned transaction's entity reads must drain before completion.");
                var transaction = OpenPhysicalTransaction();
                if (transaction == null || commit && _ownedTransactionUnresolved)
                    return RefuseTransactionOwner("The owned transaction outcome requires reconciliation before further commits.");
                try
                {
                    if (commit) transaction.Commit(); else transaction.Rollback();
                }
                catch (Exception ex)
                {
                    // Keep ownership and the exact handle: a failed commit must not be retried by
                    // another UoW. Its owner may attempt rollback; an unconfirmed rollback stays held.
                    _ownedTransactionUnresolved = true;
                    HandleDatabaseError(ex, DatasourceName, commit ? "commit an owned transaction on" : "roll back an owned transaction on");
                    return new ErrorsInfo { Flag = Errors.Failed, Message = "Native owned transaction completion was not acknowledged; reconcile its outcome.", Ex = ex };
                }
                // Acknowledged completion is final even if disposal/diagnostics fail.
                var outcome = AcknowledgeOwnedTransaction(commit ? "Owned transaction committed." : "Owned transaction rolled back.");
                // Keep the actual disposal exception in this operation's outcome. The legacy
                // disposer only logs it, so it cannot preserve a warning for this caller.
                try { transaction.Dispose(); SetSuccess(); }
                catch (Exception cleanupError)
                {
                    RecordConfirmedTransactionWarning(outcome, cleanupError);
                }
                try { _ownedTransactionEditorScope?.Dispose(); }
                catch (Exception scopeError) { RecordConfirmedTransactionWarning(outcome, scopeError); }
                finally
                {
                    _activeTransaction = null; _ownedTransactionOwner = null; _ownedTransactionFlow.Value = null;
                    _ownedTransactionUnresolved = false; _ownedTransactionErrors = null; _ownedTransactionEditorScope = null;
                }
                return outcome;
            }
        }
        private void RecordConfirmedTransactionWarning(IErrorsInfo outcome, Exception cleanupError)
        {
            outcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Transaction completed but cleanup failed.", Ex = cleanupError });
            try
            {
                DMEEditor?.AddLogMessage("TransactionOwnership", "Confirmed transaction cleanup failed: " + cleanupError,
                    DateTime.Now, 0, DatasourceName, Errors.Warning);
            }
            catch (Exception reportingError)
            {
                outcome.Errors.Add(new ErrorsInfo { Flag = Errors.Warning, Message = "Transaction completed but cleanup diagnostics could not be recorded.", Ex = reportingError });
            }
        }
        private void CheckBulkTransactionOwner()
        {
            if (_ownedTransactionOwner != null &&
                (_ownedTransactionUnresolved || !ReferenceEquals(_ownedTransactionOwner, _ownedTransactionFlow.Value)))
                throw new InvalidOperationException("A bulk operation cannot join another owner's or unresolved transaction.");
        }
    }
}
