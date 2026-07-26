using System;
using System.Collections;
using System.Collections.Generic;
using ModulesFramework.Data;
using ModulesFramework.Utils;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    /// <summary>
    ///     Holds component touch repositories
    /// </summary>
    public class ComponentTouchWatchersGlobalRegistry
    {
        /// <summary>
        ///     Component type -> count of watchers. When the count is 0, the repository is removed
        /// </summary>
        private readonly Dictionary<Type, List<IComponentWatcher>> _watchers = new Dictionary<Type, List<IComponentWatcher>>();

        public void RegisterWatcherComponents(IComponentWatcher watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IComponentWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_watchers.TryGetValue(type, out var watchersList))
                    {
                        watchersList = new List<IComponentWatcher>();
                        _watchers[type] = watchersList;
                    }
                    watchersList.Add(watcher);
                }
            }
        }

        public void UnregisterWatcher(IComponentWatcher watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IComponentWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];
                    if (!_watchers.TryGetValue(type, out var watchersList))
                        continue;
                    
                    watchersList.Remove(watcher);
                    
                    if (watchersList.Count == 0)
                        _watchers.Remove(type);
                }
            }
        }

        public bool IsWatcherRegistered<T>() where T : struct
        {
            return _watchers.ContainsKey(typeof(T));
        }

        public IReadOnlyCollection<IComponentWatcher> GetWatchers<T>() where T : struct
        {
            if (!_watchers.TryGetValue(typeof(T), out var watchersList))
                return Array.Empty<IComponentWatcher>();

            return watchersList;
        }
    }
}
