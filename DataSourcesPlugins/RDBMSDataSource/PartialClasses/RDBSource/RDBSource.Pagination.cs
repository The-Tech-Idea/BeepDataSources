namespace TheTechIdea.Beep.DataBase
{
    public partial class RDBSource : IRDBSource
    {
        // Paging has a single dialect source: RDBMSHelper.GetPagingSyntax.
        //
        // Both live paths now use it — RDBSource.Query.cs for the synchronous paged GetEntity, and
        // RDBSource.Modernization.cs for GetEntityPagedAsync / GetEntityPagedStreamAsync. Add a new
        // engine's syntax there and both paths get it.
        //
        // This file previously held no code at all: 36 lines of comment describing four competing
        // paging implementations and a consolidation that was never carried out (and describing them
        // inaccurately — it claimed Query.cs went through PaginationHelper, which it never did).
        // Of those four, two were dead and have been deleted (Helpers/PagedQueryExecutor.cs and
        // Helpers/PaginationHelper.cs), and Modernization's private switch has been replaced by the
        // shared helper.
        //
        // Kept as a signpost rather than deleted, because "where does paging live?" is the question
        // this file's name invites.
    }
}
