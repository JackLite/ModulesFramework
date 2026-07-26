using ModulesFramework.Data;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Main watcher. It saves when a component is added, removed or changed to call proper component watchers
    /// </summary>
    internal class WatchersFacade
    {
        private readonly WatchersGlobalRegistry _watchersGlobalRegistry;
        private readonly WatcherControlsContainer _watcherControlsContainer;

        public WatchersFacade()
        {
            _watchersGlobalRegistry = new WatchersGlobalRegistry();
            _watcherControlsContainer = new WatcherControlsContainer(_watchersGlobalRegistry);
        }
        
        public WatcherControl StartWatch(object owner)
        {
            return _watcherControlsContainer.Pop(owner);
        }
        
        public void RegisterComponentTouch<T>(int eid, ComponentTouchType touchType) where T : struct
        {
            foreach (var watcherControl in _watcherControlsContainer.ActiveControls)
                watcherControl.RegisterComponentTouch<T>(eid, touchType);
        }

        public void RegisterRawDataCall<T>() where T : struct
        {
            foreach (var watcherControl in _watcherControlsContainer.ActiveControls)
                watcherControl.RegisterRawDataCall<T>();
        }
        
        public void RegisterComponentWatcher(IComponentTouchWatcher watcher)
        {
            _watchersGlobalRegistry.RegisterWatcher(watcher);
        }

        public void UnregisterComponentWatcher(IComponentTouchWatcher watcher)
        {
            _watchersGlobalRegistry.UnregisterWatcher(watcher);
        }
        
        public void RegisterRawDataWatcher(IRawDataWatcher watcher)
        {
            _watchersGlobalRegistry.RegisterWatcher(watcher);
        }
        
        public void UnregisterRawDataWatcher(IRawDataWatcher watcher)
        {
            _watchersGlobalRegistry.UnregisterWatcher(watcher);
        }
    }
}
