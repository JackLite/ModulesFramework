using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    /// <summary>
    ///     Type-specific local component touch repository
    /// </summary>
    internal class ComponentTouchRepository<T> : IComponentTouchRepository where T : struct
    {
        private readonly Queue<ComponentTouch> _touches = new Queue<ComponentTouch>();
        
        public void RegisterTouch(int eid, ComponentTouchType touch)
        {
            _touches.Enqueue(new ComponentTouch(eid, touch));
        }

        public void CallWatchersAndClear(WatchersGlobalRegistry registry, object touchOwner)
        {
            while (_touches.Count > 0)
            {
                var touch = _touches.Dequeue();
                foreach (var watcher in registry.GetComponentWatchers<T>())
                {
                    if (watcher is IComponentTouchWatcher<T> typedWatcher)
                        typedWatcher.Watch(touch.eid, touchOwner.GetType(), touch.touchType);
                }
            }
        }
    }
}
