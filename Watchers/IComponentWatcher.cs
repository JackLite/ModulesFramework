using System;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Called after a system adds, gets, or removes a component
    /// </summary>
    public interface IComponentWatcher<T> : IComponentWatcher where T : struct
    {
        public void Watch(int eid, Type systemType, ComponentTouchType touchType);
    }

    /// <summary>
    ///     Common interface to find all watchers at the start of MF
    /// </summary>
    public interface IComponentWatcher
    {

    }
}
