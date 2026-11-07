using System.Collections.Generic;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Creates and contains watcher controls.
    /// </summary>
    public class WatcherControlsContainer
    {
        private readonly WatchersGlobalRegistry _watchersGlobalRegistry;
        private readonly Stack<WatcherControl> _pool = new Stack<WatcherControl>();

        public List<WatcherControl> ActiveControls { get; } = new List<WatcherControl>();

        public WatcherControlsContainer(WatchersGlobalRegistry watchersGlobalRegistry)
        {
            _watchersGlobalRegistry = watchersGlobalRegistry;
        }

        public WatcherControl Pop(object controlOwner)
        {
            var control = _pool.Count > 0 ? _pool.Pop() : new WatcherControl(this, _watchersGlobalRegistry);
            control.SetOwner(controlOwner);
            ActiveControls.Add(control);
            return control;
        }

        public void Return(WatcherControl watcherControl)
        {
            ActiveControls.Remove(watcherControl);
            _pool.Push(watcherControl);
        }
    }
}
