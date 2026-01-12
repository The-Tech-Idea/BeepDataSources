using System;
using System.Collections.Concurrent;

namespace TheTechIdea.Beep.DataBase.Helpers
{
    internal class EntityStructureCache
    {
        private readonly ConcurrentDictionary<string, EntityStructure> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, bool, EntityStructure> _loader;

        public EntityStructureCache(Func<string, bool, EntityStructure> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public EntityStructure Get(string name, bool refresh)
        {
            if (refresh)
            {
                var refreshed = _loader(name, true);
                _cache[name] = refreshed;
                return refreshed;
            }
            return _cache.GetOrAdd(name, k => _loader(k, false));
        }
    }
}
