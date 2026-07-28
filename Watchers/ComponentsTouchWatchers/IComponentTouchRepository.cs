using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{

    /// <summary>
    ///     Common interface for component touch repositories
    /// </summary>
    internal interface IComponentTouchRepository
    {
        public void RegisterTouch(int eid, ComponentTouchType touch);
        public void CallWatchersAndClear(WatchersGlobalRegistry registry, object touchOwner);
    }
}
