using System;
using System.Threading;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource
    {
        private readonly AsyncLocal<object?> _legacyOwnedTransaction = new();
        private bool CanOwnLegacyTransaction => this is IOwnedTransactionDataSource owned &&
            owned.SupportsOwnedTransactions && HasDefaultNativeTransactionHooks;

        // Default native form/enlisted callers use the same owner protocol as UoW commits.
        // An unrelated flow cannot adopt the form's transaction through the old API.
        private IErrorsInfo BeginLegacyOwnedTransaction()
        {
            lock (_transactionOwnerGate)
            {
                var owner = _legacyOwnedTransaction.Value;
                if (owner == null || !IsTransactionOwner(owner)) owner = new object();
                var result = BeginTransactionForOwner(owner);
                if (IsTransactionOwner(owner)) _legacyOwnedTransaction.Value = owner;
                return result;
            }
        }
        private IErrorsInfo? FinishLegacyOwnedTransaction(bool commit)
        {
            lock (_transactionOwnerGate)
            {
                if (_ownedTransactionOwner == null) return null;
                var owner = _legacyOwnedTransaction.Value;
                if (owner == null || !IsTransactionOwner(owner))
                    return RefuseTransactionOwner("Only the coordinating execution flow may finish its explicit transaction.");
                var result = FinishTransactionForOwner(owner, commit);
                if (!IsTransactionOwner(owner)) _legacyOwnedTransaction.Value = null;
                return result;
            }
        }
    }
}
