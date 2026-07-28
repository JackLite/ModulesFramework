using System;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.OneDataWatchers
{
    /// <summary>
    ///     Called after code in the scope of the watcher control get, create or remove one data
    /// </summary>
    /// <typeparam name="T">Type of the one data</typeparam>
    public interface IOneDataWatcher<T> : IOneDataWatcher where T : struct
    {
    }

    /// <summary>
    ///     Common interface to find all OneDataWatchers at the start of the MF
    /// </summary>
    public interface IOneDataWatcher
    {
        public void Watch(Type source, OneDataTouchType touchType);
    }
}
