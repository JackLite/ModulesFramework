#if MODULES_DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using ModulesFramework.Watchers;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly WatchersFacade _watchersFacade = new WatchersFacade();

        internal IEnumerable<Type> GetWatcherTypes(Type moduleType)
        {
            return _cache.WatchersType.GetWatcherTypes(moduleType);
        }

        internal void CallComponentWatchers(Type systemType)
        {
        }

        public WatcherControl StartWatch()
        {
            return null;
        }

        public void StopWatch()
        {
        }

        public void RegisterWatcher(IComponentWatcher watcher)
        {
            _watchersFacade.RegisterComponentWatcher(watcher);
        }
    }
}
#endif
