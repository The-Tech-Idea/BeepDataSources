using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;

namespace TheTechIdea.Beep.DataBase
{
    public partial class SQLiteDataSource : IOwnedEntityProjectionDataSource
    {
        // SQLite's Openconnection override is part of this exact connector; derived providers need their own implementation.
        public bool SupportsOwnedEntityProjections => GetType() == typeof(SQLiteDataSource) && DMEEditor is IScopedEditorErrors && !InMemory &&
            GetType().GetMethod(nameof(GetEntity), new[] { typeof(string), typeof(List<AppFilter>) })?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(GetEntityAsync), new[] { typeof(string), typeof(List<AppFilter>) })?.DeclaringType == typeof(RDBSource) &&
            GetType().GetMethod(nameof(GetDataCommand), Type.EmptyTypes)?.DeclaringType == typeof(RDBSource);
        public Task<EntityReadResult> ReadEntityProjectionAsync(string entityName, List<AppFilter> filters,
            IReadOnlyList<string> requiredColumns, CancellationToken cancellationToken = default) => SupportsOwnedEntityProjections
                ? ReadOwnedEntityAsync(entityName, filters, cancellationToken, requiredColumns ?? Array.Empty<string>())
                : Task.FromResult(new EntityReadResult(new ErrorsInfo { Flag = Errors.Failed,
                    Message = "This SQLite provider cannot acknowledge the requested projection." }, Array.Empty<object>()));
    }
}
