using System.Collections.Generic;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Creates and contains watcher controls.
    /// </summary>
    public class WatcherControlPool
    {
        private readonly Stack<WatcherControl> _pool = new Stack<WatcherControl>();
        private readonly List<WatcherControl> _activeControls = new List<WatcherControl>();

        public IReadOnlyCollection<WatcherControl> ActiveControls => _activeControls;

        public WatcherControl Pop()
        {
            var control = _pool.Count > 0 ? _pool.Pop() : new WatcherControl();
            _activeControls.Add(control);
            return control;
        }

        public void Return(WatcherControl watcherControl)
        {
            if (!watcherControl.IsDisposed)
                watcherControl.Dispose();

            _activeControls.Remove(watcherControl);
            _pool.Push(watcherControl);
        }
    }
}
