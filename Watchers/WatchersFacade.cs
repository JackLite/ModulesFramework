using ModulesFramework.Data;
using ModulesFramework.Watchers.ComponentsTouchWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Main watcher. It saves when a component is added, removed or changed to call proper component watchers
    /// </summary>
    internal class WatchersFacade
    {
        private readonly ComponentTouchWatchersGlobalRegistry _componentTouchWatchersGlobalRegistry;
        private readonly WatcherControlsContainer _watcherControlsContainer;

        public WatchersFacade()
        {
            _componentTouchWatchersGlobalRegistry = new ComponentTouchWatchersGlobalRegistry();
            _watcherControlsContainer = new WatcherControlsContainer(_componentTouchWatchersGlobalRegistry);
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
        
        public void RegisterComponentWatcher(IComponentWatcher watcher)
        {
            _componentTouchWatchersGlobalRegistry.RegisterWatcherComponents(watcher);
        }
        
        public void UnregisterComponentWatcher(IComponentWatcher watcher)
        {
            _componentTouchWatchersGlobalRegistry.UnregisterWatcher(watcher);
        }
    }
}
