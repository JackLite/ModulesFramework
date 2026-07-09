using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    internal readonly struct ComponentTouch
    {
        public readonly int eid;
        public readonly ComponentTouchType touchType;

        public ComponentTouch(int eid, ComponentTouchType touchType)
        {
            this.eid = eid;
            this.touchType = touchType;
        }
    }
}
