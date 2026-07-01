using System;
using System.Collections.Generic;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        // Pagination is implemented across multiple files. This file serves as the central
        // reference for all paging-related functionality and will be the consolidation target.
        //
        // CURRENT IMPLEMENTATIONS:
        //
        // 1. RDBSource.Query.cs — GetEntity(entityName, filter, pageNumber, pageSize)
        //    Uses PaginationHelper.ApplyPaging() which delegates to RDBMSHelper.GetPagingSyntax().
        //    This is the legacy paging path used by non-async callers.
        //
        // 2. RDBSource.Modernization.cs — GetPagedQuery(), BuildOraclePagingQuery(), GetOperator()
        //    Database-specific paging (OFFSET/FETCH for SQL Server, LIMIT/OFFSET for MySQL/PostgreSQL/SQLite,
        //    ROWNUM subquery for Oracle). Used by GetEntityPagedAsync<T>() and GetEntityPagedStreamAsync<T>().
        //
        // 3. Helpers/PagedQueryExecutor.cs — Builds count query + paged query, executes both,
        //    returns (List<object> rows, int total). Regex-based count query generation.
        //
        // 4. Helpers/PaginationHelper.cs — Thin wrapper delegating to RDBMSHelper.GetPagingSyntax().
        //
        // CONSOLIDATION PLAN:
        // - Move GetPagedQuery(), BuildOraclePagingQuery(), GetOperator() from Modernization.cs here
        // - Have Query.cs delegate to this file instead of PaginationHelper
        // - Make PagedQueryExecutor the single engine for all paged queries
        // - Add a PaginationStrategy with database-specific implementations (Strategy pattern)
        //
        // TARGET: This file becomes the single source of truth for all paging logic.
    }
}
