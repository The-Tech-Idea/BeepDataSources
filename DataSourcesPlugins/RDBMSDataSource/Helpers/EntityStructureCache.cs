using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    /// <summary>
    /// Per-datasource cache of <see cref="EntityStructure"/>, keyed by entity name.
    /// </summary>
    /// <remarks>
    /// Three things this class did not previously do, all of which cost correctness:
    ///
    /// <b>Invalidation.</b> There was no way to evict anything. After a CREATE TABLE or ALTER TABLE
    /// the pre-DDL structure was served for the lifetime of the datasource unless a caller happened
    /// to pass <c>refresh: true</c>. <see cref="Remove"/> and <see cref="Clear"/> now exist, and
    /// <c>RDBSource.InvalidateEntityCache</c> calls <see cref="Remove"/> after every write — those
    /// ten call sites previously invalidated a cache that was always empty.
    ///
    /// <b>Bounds.</b> Callers pass whole SQL statements as the key, not just entity names
    /// (see <c>RDBSource.Query.cs</c>), so an application issuing ad-hoc queries grew this
    /// dictionary without limit. It is now capped; at the cap the cache stops admitting new
    /// entries rather than evicting live ones, since a wrong structure is worse than a slow lookup.
    ///
    /// <b>Loader serialisation.</b> <c>ConcurrentDictionary.GetOrAdd</c> does not hold a lock across
    /// the value factory, so N threads asking for the same uncached entity all ran the loader — and
    /// the loader issues <c>ExecuteReader</c> on the single shared <c>IDbConnection</c>. Most
    /// providers answer that with "There is already an open DataReader associated with this
    /// Connection". Loads are now serialised per key.
    ///
    /// A null result is no longer cached: the loader returns a field-less structure when
    /// <c>GetTableSchema</c> fails, and caching that served the failure to every later caller.
    /// </remarks>
    internal class EntityStructureCache
    {
        /// <summary>
        /// Upper bound on cached entries. Generous — this exists to stop unbounded growth from
        /// ad-hoc query keys, not to keep the cache small.
        /// </summary>
        private const int MaxEntries = 2048;

        private readonly ConcurrentDictionary<string, EntityStructure> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, object> _loadLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, bool, EntityStructure> _loader;

        public EntityStructureCache(Func<string, bool, EntityStructure> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public EntityStructure Get(string name, bool refresh)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            if (refresh)
            {
                var refreshed = _loader(name, true);
                Store(name, refreshed);
                return refreshed;
            }

            if (_cache.TryGetValue(name, out var cached))
                return cached;

            // Serialise the load per key. GetOrAdd would run the factory on every racing thread,
            // and the factory opens a reader on the shared connection.
            object gate = _loadLocks.GetOrAdd(name, _ => new object());
            lock (gate)
            {
                if (_cache.TryGetValue(name, out cached))
                    return cached;

                var loaded = _loader(name, false);
                Store(name, loaded);
                return loaded;
            }
        }

        /// <summary>
        /// Drops the cached structure for one entity. Called after DDL and after writes.
        /// </summary>
        public bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            _loadLocks.TryRemove(name, out _);
            return _cache.TryRemove(name, out _);
        }

        /// <summary>Drops everything.</summary>
        public void Clear()
        {
            _cache.Clear();
            _loadLocks.Clear();
        }

        public int Count => _cache.Count;

        private void Store(string name, EntityStructure structure)
        {
            // Do not cache a null. The loader yields one when the entity cannot be read, and caching
            // it would serve that failure to every subsequent caller.
            if (structure == null)
                return;

            if (_cache.Count >= MaxEntries && !_cache.ContainsKey(name))
                return;

            _cache[name] = structure;
        }
    }
}
