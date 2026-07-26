using System.Collections.Generic;
using ModulesFramework.Watchers.ComponentsTouchWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Creates and contains watcher controls.
    /// </summary>
    public class WatcherControlsContainer
    {
        private readonly ComponentTouchWatchersGlobalRegistry _componentTouchWatchersGlobalRegistry;
        private readonly Stack<WatcherControl> _pool = new Stack<WatcherControl>();
        private readonly List<WatcherControl> _activeControls = new List<WatcherControl>();

        public IReadOnlyCollection<WatcherControl> ActiveControls => _activeControls;

        public WatcherControlsContainer(ComponentTouchWatchersGlobalRegistry componentTouchWatchersGlobalRegistry)
        {
            _componentTouchWatchersGlobalRegistry = componentTouchWatchersGlobalRegistry;
        }

        public WatcherControl Pop(object controlOwner)
        {
            var control = _pool.Count > 0 ? _pool.Pop() : new WatcherControl(this, _componentTouchWatchersGlobalRegistry);
            control.SetOwner(controlOwner);
            _activeControls.Add(control);
            return control;
        }

        public void Return(WatcherControl watcherControl)
        {
            _activeControls.Remove(watcherControl);
            _pool.Push(watcherControl);
        }
    }
}
