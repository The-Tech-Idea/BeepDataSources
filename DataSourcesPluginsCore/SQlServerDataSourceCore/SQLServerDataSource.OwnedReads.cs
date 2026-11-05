using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLServerDataSource : IOwnedEntityReadDataSource, IOwnedEntityProjectionDataSource
    {
        public virtual bool SupportsOwnedEntityReads => HasDefaultNativeReadHooks;
        public virtual Task<EntityReadResult> ReadEntityAsync(string entityName, List<AppFilter> filters,
            CancellationToken cancellationToken = default) => SupportsOwnedEntityReads
                ? ReadOwnedEntityAsync(entityName, filters, cancellationToken)
                : Task.FromResult(new EntityReadResult(new ErrorsInfo { Flag = Errors.Failed,
                    Message = "This provider's read overrides require an explicit owned-read implementation." }, System.Array.Empty<object>()));
        public virtual bool SupportsOwnedEntityProjections => HasDefaultNativeReadHooks &&
            GetType().GetMethod(nameof(GetEntity), new[] { typeof(string), typeof(List<AppFilter>) })?.DeclaringType == typeof(RDBSource);
        public virtual Task<EntityReadResult> ReadEntityProjectionAsync(string entityName, List<AppFilter> filters,
            IReadOnlyList<string> requiredColumns, CancellationToken cancellationToken = default) => SupportsOwnedEntityProjections
                ? ReadOwnedEntityAsync(entityName, filters, cancellationToken, requiredColumns ?? System.Array.Empty<string>())
                : Task.FromResult(new EntityReadResult(new ErrorsInfo { Flag = Errors.Failed,
                    Message = "This provider's read overrides require an explicit owned-projection implementation." }, System.Array.Empty<object>()));
    }
}
