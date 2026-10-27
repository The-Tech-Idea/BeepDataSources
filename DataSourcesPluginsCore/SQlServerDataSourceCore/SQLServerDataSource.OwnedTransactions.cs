using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLServerDataSource : IOwnedTransactionDataSource
    {
        public virtual bool SupportsOwnedTransactions => HasDefaultNativeTransactionHooks;
        public virtual bool OwnsTransaction(object owner) => IsTransactionOwner(owner);
        public virtual IErrorsInfo BeginOwnedTransaction(object owner) => HasDefaultNativeTransactionHooks
            ? BeginTransactionForOwner(owner)
            : RefuseTransactionOwner("This driver's transaction overrides require an explicit owned-transaction implementation.");
        public virtual IErrorsInfo CommitOwnedTransaction(object owner) => FinishTransactionForOwner(owner, true);
        public virtual IErrorsInfo RollbackOwnedTransaction(object owner) => FinishTransactionForOwner(owner, false);
    }
}
