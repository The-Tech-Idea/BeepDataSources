using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLServerDataSource : IOwnedEntityReadDataSource
    {
        public virtual bool SupportsOwnedEntityReads => HasDefaultNativeReadHooks;
        public virtual Task<EntityReadResult> ReadEntityAsync(string entityName, List<AppFilter> filters,
            CancellationToken cancellationToken = default) => SupportsOwnedEntityReads
                ? ReadOwnedEntityAsync(entityName, filters, cancellationToken)
                : Task.FromResult(new EntityReadResult(new ErrorsInfo { Flag = Errors.Failed,
                    Message = "This provider's read overrides require an explicit owned-read implementation." }, System.Array.Empty<object>()));
    }
}
