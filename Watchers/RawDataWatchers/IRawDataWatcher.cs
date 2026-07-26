using System;

namespace ModulesFramework.Watchers.RawDataWatchers
{
    /// <summary>
    ///     Called after code in the scope of the watcher control get raw data
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public interface IRawDataWatcher<T> : IRawDataWatcher
    {
        /// <param name="source">Type of the object that was bound to the scope of the watcher control</param>
        public void Watch(Type source);
    }
    
    /// <summary>
    ///     Common interface to find all RawDataWatchers at the start of the MF
    /// </summary>
    public interface IRawDataWatcher
    {
        
    }
}
