using System;
using System.Collections.Generic;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Stores info about what components have what watchers
    /// </summary>
    public class WatchersRegistry
    {
        /// <summary>
        ///     Types of components that have component watchers
        /// </summary>
        private readonly HashSet<Type> _componentTypes = new HashSet<Type>();

        public void RegisterWatcher(IComponentWatcher watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IComponentWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];
                    _componentTypes.Add(type);
                }
            }
        }
    }
}
