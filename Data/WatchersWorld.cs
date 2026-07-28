#if MODULES_DEBUG
using System;
using System.Collections.Generic;
using ModulesFramework.Watchers;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.OneDataWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

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

        public void RegisterComponentWatcher(IComponentTouchWatcher watcher)
        {
            _watchersFacade.RegisterComponentWatcher(watcher);
        }
        
        public void UnregisterComponentWatcher(IComponentTouchWatcher watcher)
        {
            _watchersFacade.UnregisterComponentWatcher(watcher);
        }

        public void RegisterRawDataWatcher(IRawDataWatcher watcher)
        {
            _watchersFacade.RegisterRawDataWatcher(watcher);
        }
        
        public void UnregisterRawDataWatcher(IRawDataWatcher watcher)
        {
            _watchersFacade.UnregisterRawDataWatcher(watcher);
        }

        public void RegisterOneDataWatcher(IOneDataWatcher watcher)
        {
            _watchersFacade.RegisterOneDataWatcher(watcher);
        }
        
        public void UnregisterOneDataWatcher(IOneDataWatcher watcher)
        {
            _watchersFacade.UnregisterOneDataWatcher(watcher);
        }
    }
}
#endif
