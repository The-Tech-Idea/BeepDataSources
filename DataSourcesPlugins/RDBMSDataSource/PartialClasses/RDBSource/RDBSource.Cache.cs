using System;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Cache invalidation for the entity-structure cache.
    /// </summary>
    /// <remarks>
    /// This file used to hold three caches — a compiled-query-string cache, a MemoryCache of query
    /// results with TTL and per-entity key tracking, and a prepared-statement cache — none of which
    /// were reachable. <c>TryGetCachedResult</c>, <c>CacheResult</c>, <c>TryGetPreparedStatement</c>
    /// and <c>CachePreparedStatement</c> had no callers at all; the query-string cache had exactly
    /// one, inside the dead <c>BuildQuery</c> cluster that has since been deleted from
    /// <c>RDBSource.Query.cs</c>.
    ///
    /// <c>InvalidateEntityCache</c>, meanwhile, is called from ten places — after every INSERT,
    /// UPDATE, DELETE and bulk operation — and logged "Cache invalidated for entity: X" each time
    /// having invalidated nothing at all.
    ///
    /// The cache that does exist and does matter is <see cref="Helpers.EntityStructureCache"/>, and
    /// it had no invalidation whatsoever, so a schema change stayed invisible for the lifetime of
    /// the datasource. Those ten call sites now point at it.
    ///
    /// Result caching was not reinstated deliberately: <c>GetEntity</c> is an iterator that streams
    /// rows off an open reader, so caching its return value would cache an un-enumerated sequence,
    /// and forcing it to a list to make caching possible would throw away the streaming behaviour
    /// that the read path is built around.
    /// </remarks>
    public partial class RDBSource
    {
        /// <summary>
        /// Retained for API compatibility. Result caching is no longer implemented — see the remarks
        /// on this file.
        /// </summary>
        [Obsolete("Result caching is not implemented; this setting has no effect. It is kept so existing callers still compile.")]
        public bool EnableResultCache { get; set; } = false;

        /// <summary>
        /// Retained for API compatibility. Result caching is no longer implemented — see the remarks
        /// on this file.
        /// </summary>
        [Obsolete("Result caching is not implemented; this setting has no effect. It is kept so existing callers still compile.")]
        public TimeSpan ResultCacheTTL { get; set; }

        /// <summary>
        /// Drops the cached <see cref="EntityStructure"/> for one entity, so the next lookup reads it
        /// from the database again.
        /// </summary>
        /// <remarks>
        /// Called after every write and after DDL. Before the structure cache gained a
        /// <c>Remove</c>, this swept a query cache that was permanently empty and then logged
        /// success.
        /// </remarks>
        /// <param name="entityName">The entity whose cached structure should be dropped.</param>
        private void InvalidateEntityCache(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                return;

            _entityCache?.Remove(entityName);
        }

        /// <summary>
        /// Drops every cached entity structure on this datasource.
        /// </summary>
        public void ClearAllCaches()
        {
            _entityCache?.Clear();
        }
    }
}
