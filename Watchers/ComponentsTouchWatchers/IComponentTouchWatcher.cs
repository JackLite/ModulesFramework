using System;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    /// <summary>
    ///     Called after code in scope of control adds, gets, or removes a component
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public interface IComponentTouchWatcher<T> : IComponentTouchWatcher where T : struct
    {
        public void Watch(int eid, Type systemType, ComponentTouchType touchType);
    }

    /// <summary>
    ///     Common interface to find all ComponentTouchWatchers at the start of the MF
    /// </summary>
    public interface IComponentTouchWatcher
    {

    }
}
