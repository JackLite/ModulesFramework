#if MODULES_DEBUG
using System;
using System.Collections.Generic;
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

        public WatcherControl StartWatch(object owner)
        {
            return _watchersFacade.StartWatch(owner);
        }

        public void RegisterWatcher(IComponentWatcher watcher)
        {
            _watchersFacade.RegisterComponentWatcher(watcher);
        }
        
        public void UnregisterWatcher(IComponentWatcher watcher)
        {
            _watchersFacade.UnregisterComponentWatcher(watcher);
        }
    }
}
#endif
