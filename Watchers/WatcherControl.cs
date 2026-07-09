using System;
using ModulesFramework.Data;
using ModulesFramework.Watchers.ComponentsTouchWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Specific control. When it creates, it starts watching components. After disposing, it clears all touches.
    /// </summary>
    public sealed class WatcherControl : IDisposable
    {
        private readonly ComponentTouchWatchersRegistry _componentTouchWatchersRegistry;
        private bool _disposed;
        public bool IsDisposed => _disposed;

        internal void RegisterComponentTouch<T>(int eid, ComponentTouchType touch) where T : struct
        {
            _componentTouchWatchersRegistry.RegisterTouch<T>(eid, touch);
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
