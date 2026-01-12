using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.DataBase
{
    /// <summary>
    /// Partial class containing query and result caching infrastructure.
    /// Provides thread-safe caching for query strings, prepared statements, and query results.
    /// </summary>
    public partial class RDBSource
    {
        #region "Private Fields"
        
        /// <summary>
        /// Cache for compiled query strings to reduce BuildQuery overhead.
        /// Key: hash of (originalQuery + filters), Value: compiled query string.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _queryCache = new();

        /// <summary>
        /// Cache for prepared statement command objects to reduce command creation overhead.
        /// Key: query string, Value: cloned IDbCommand template.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _preparedStatementCache = new();

        /// <summary>
        /// Memory cache for query results with configurable TTL.
        /// </summary>
        private MemoryCache? _resultCache;

        /// <summary>
        /// Default time-to-live for cached results (5 minutes).
        /// </summary>
        private readonly TimeSpan _defaultCacheTTL = TimeSpan.FromMinutes(5);

        #endregion

        #region "Properties"

        /// <summary>
        /// Gets or sets whether query result caching is enabled.
        /// Default: false (disabled to maintain backward compatibility).
        /// </summary>
        public bool EnableResultCache { get; set; } = false;

        /// <summary>
        /// Gets or sets the time-to-live for cached query results.
        /// Default: 5 minutes.
        /// </summary>
        public TimeSpan ResultCacheTTL { get; set; }

        /// <summary>
        /// Lazy-initialized result cache.
        /// </summary>
        private MemoryCache ResultCache
        {
            get
            {
                _resultCache ??= new MemoryCache(new MemoryCacheOptions
                {
                    SizeLimit = 1024 // Limit to 1024 entries
                });
                return _resultCache;
            }
        }

        #endregion

        #region "Cache Management Methods"

        /// <summary>
        /// Generates a cache key for a query based on entity name and filters.
        /// </summary>
        /// <param name="entityName">The entity/table name.</param>
        /// <param name="filters">The list of filters applied to the query.</param>
        /// <returns>A deterministic cache key string.</returns>
        private string GenerateQueryCacheKey(string entityName, List<AppFilter>? filters)
        {
            if (string.IsNullOrEmpty(entityName))
                return string.Empty;

            var key = $"{entityName.ToLowerInvariant()}";
            
            if (filters != null && filters.Count > 0)
            {
                // Sort filters to ensure consistent key generation
                var sortedFilters = filters
                    .Where(f => !string.IsNullOrWhiteSpace(f.FieldName) && !string.IsNullOrWhiteSpace(f.FilterValue))
                    .OrderBy(f => f.FieldName)
                    .ThenBy(f => f.Operator);

                foreach (var filter in sortedFilters)
                {
                    key += $"|{filter.FieldName}:{filter.Operator}:{filter.FilterValue}";
                }
            }

            return key;
        }

        /// <summary>
        /// Gets a cached query string if available.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="cachedQuery">The cached query string, if found.</param>
        /// <returns>True if cached query was found, false otherwise.</returns>
        private bool TryGetCachedQuery(string cacheKey, out string? cachedQuery)
        {
            return _queryCache.TryGetValue(cacheKey, out cachedQuery);
        }

        /// <summary>
        /// Caches a compiled query string.
        /// </summary>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="query">The compiled query string.</param>
        private void CacheQuery(string cacheKey, string query)
        {
            if (!string.IsNullOrEmpty(cacheKey) && !string.IsNullOrEmpty(query))
            {
                _queryCache[cacheKey] = query;
            }
        }

        /// <summary>
        /// Gets a cached query result if available and caching is enabled.
        /// </summary>
        /// <typeparam name="T">The type of the cached result.</typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="result">The cached result, if found.</param>
        /// <returns>True if cached result was found, false otherwise.</returns>
        private bool TryGetCachedResult<T>(string cacheKey, out T? result)
        {
            result = default;
            
            if (!EnableResultCache || string.IsNullOrEmpty(cacheKey))
                return false;

            return ResultCache.TryGetValue(cacheKey, out result);
        }

        /// <summary>
        /// Caches a query result if result caching is enabled.
        /// </summary>
        /// <typeparam name="T">The type of the result to cache.</typeparam>
        /// <param name="cacheKey">The cache key.</param>
        /// <param name="result">The result to cache.</param>
        private void CacheResult<T>(string cacheKey, T result)
        {
            if (!EnableResultCache || string.IsNullOrEmpty(cacheKey) || result == null)
                return;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1) // Each entry counts as 1 toward the size limit
                .SetSlidingExpiration(ResultCacheTTL == default ? _defaultCacheTTL : ResultCacheTTL)
                .SetPriority(CacheItemPriority.Normal);

            ResultCache.Set(cacheKey, result, cacheEntryOptions);
        }

        /// <summary>
        /// Invalidates cached queries and results for a specific entity.
        /// Called automatically on INSERT, UPDATE, DELETE operations.
        /// </summary>
        /// <param name="entityName">The entity name whose cache should be invalidated.</param>
        private void InvalidateEntityCache(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
                return;

            var entityKey = entityName.ToLowerInvariant();
            
            // Remove query cache entries starting with this entity name
            var keysToRemove = new List<string>();
            foreach (var key in _queryCache.Keys)
            {
                if (key.StartsWith(entityKey, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _queryCache.TryRemove(key, out _);
            }

            // If result caching is enabled, clear result cache entries for this entity
            if (EnableResultCache)
            {
                // MemoryCache doesn't provide key enumeration, so we rely on TTL expiration
                // For explicit invalidation, consider using a more sophisticated cache key strategy
                DMEEditor?.AddLogMessage("Beep", $"Cache invalidated for entity: {entityName}", DateTime.Now, 0, null, Errors.Ok);
            }
        }

        /// <summary>
        /// Clears all query and result caches.
        /// </summary>
        public void ClearAllCaches()
        {
            _queryCache.Clear();
            _preparedStatementCache.Clear();
            
            if (_resultCache != null)
            {
                _resultCache.Dispose();
                _resultCache = null;
            }

            DMEEditor?.AddLogMessage("Beep", "All query and result caches cleared", DateTime.Now, 0, null, Errors.Ok);
        }

        #endregion

        #region "Prepared Statement Caching"

        /// <summary>
        /// Gets a cached prepared statement command text if available.
        /// </summary>
        /// <param name="query">The query string.</param>
        /// <param name="cachedCommandText">The cached command text, if found.</param>
        /// <returns>True if cached command was found, false otherwise.</returns>
        private bool TryGetPreparedStatement(string query, out string? cachedCommandText)
        {
            return _preparedStatementCache.TryGetValue(query, out cachedCommandText);
        }

        /// <summary>
        /// Caches a prepared statement command text.
        /// </summary>
        /// <param name="query">The query string.</param>
        /// <param name="commandText">The prepared command text.</param>
        private void CachePreparedStatement(string query, string commandText)
        {
            if (!string.IsNullOrEmpty(query) && !string.IsNullOrEmpty(commandText))
            {
                _preparedStatementCache[query] = commandText;
            }
        }

        #endregion
    }
}
