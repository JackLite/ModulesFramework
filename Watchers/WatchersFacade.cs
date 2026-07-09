using ModulesFramework.Data;
using ModulesFramework.Watchers.ComponentsTouchWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Main watcher. It saves when a component is added, removed or changed to call proper component watchers
    /// </summary>
    internal class WatchersFacade
    {
        private readonly ComponentTouchWatchersRegistry _componentTouchWatchersRegistry;
        private readonly WatcherControlPool _watcherControlPool;

        public WatchersFacade()
        {
            _componentTouchWatchersRegistry = new ComponentTouchWatchersRegistry();
            _watcherControlPool = new WatcherControlPool();
        }
        
        public void RegisterComponentTouch<T>(int eid, ComponentTouchType touchType) where T : struct
        {
            foreach (var watcherControl in _watcherControlPool.ActiveControls)
                watcherControl.RegisterComponentTouch<T>(eid, touchType);
        }
        
        public void RegisterComponentWatcher(IComponentWatcher watcher)
        {
            _componentTouchWatchersRegistry.RegisterWatcherComponents(watcher);
        }
        
        public void UnregisterComponentWatcher(IComponentWatcher watcher)
        {
            _componentTouchWatchersRegistry.UnregisterWatcher(watcher);
        }
    }
}
