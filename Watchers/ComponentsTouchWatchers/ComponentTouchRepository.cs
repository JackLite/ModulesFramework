using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    /// <summary>
    ///     Type-specific component touch repository
    /// </summary>
    internal class ComponentTouchRepository<T> : IComponentTouchRepository where T : struct
    {
        private readonly Queue<ComponentTouch> _touches = new Queue<ComponentTouch>();
        
        public void RegisterTouch(int eid, ComponentTouchType touch)
        {
            _touches.Enqueue(new ComponentTouch(eid, touch));
        }
        
        public ComponentTouch PopTouch()
        {
            return _touches.Dequeue();
        }
    }
}
