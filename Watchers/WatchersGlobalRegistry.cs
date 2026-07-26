using System;
using System.Collections.Generic;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Holds component touch repositories
    /// </summary>
    public class WatchersGlobalRegistry
    {
        /// <summary>
        ///     Component type -> count of watchers
        /// </summary>
        private readonly Dictionary<Type, List<IComponentTouchWatcher>> _componentTouchWatchers =
            new Dictionary<Type, List<IComponentTouchWatcher>>();

        /// <summary>
        ///     Component type -> count of watchers
        /// </summary>
        private readonly Dictionary<Type, List<IRawDataWatcher>> _rawDataWatchers =
            new Dictionary<Type, List<IRawDataWatcher>>();


        public void RegisterWatcher(object watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (!@interface.IsGenericType)
                    continue;

                if (@interface.GetGenericTypeDefinition() == typeof(IComponentTouchWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_componentTouchWatchers.TryGetValue(type, out var watchersList))
                    {
                        watchersList = new List<IComponentTouchWatcher>();
                        _componentTouchWatchers[type] = watchersList;
                    }
                    watchersList.Add((IComponentTouchWatcher)watcher);
                }

                if (@interface.GetGenericTypeDefinition() == typeof(IRawDataWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_rawDataWatchers.TryGetValue(type, out var watchersList))
                    {
                        watchersList = new List<IRawDataWatcher>();
                        _rawDataWatchers[type] = watchersList;
                    }
                    watchersList.Add((IRawDataWatcher)watcher);
                }
            }
        }

        public void UnregisterWatcher(object watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (!@interface.IsGenericType)
                    continue;

                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IComponentTouchWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];
                    if (!_componentTouchWatchers.TryGetValue(type, out var watchersList))
                        continue;

                    watchersList.Remove((IComponentTouchWatcher)watcher);

                    if (watchersList.Count == 0)
                        _componentTouchWatchers.Remove(type);
                }

                if (@interface.GetGenericTypeDefinition() == typeof(IRawDataWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_rawDataWatchers.TryGetValue(type, out var watchersList))
                        continue;

                    watchersList.Remove((IRawDataWatcher)watcher);

                    if (watchersList.Count == 0)
                        _rawDataWatchers.Remove(type);
                }
            }
        }

        public bool IsComponentWatcherRegistered<T>() where T : struct
        {
            return _componentTouchWatchers.ContainsKey(typeof(T));
        }
        
        public bool IsRawDataWatcherRegistered<T>() where T : struct
        {
            return _rawDataWatchers.ContainsKey(typeof(T));
        }

        public IReadOnlyCollection<IComponentTouchWatcher> GetComponentWatchers<T>() where T : struct
        {
            if (!_componentTouchWatchers.TryGetValue(typeof(T), out var watchersList))
                return Array.Empty<IComponentTouchWatcher>();

            return watchersList;
        }

        public IReadOnlyCollection<IRawDataWatcher> GetRawDataWatchers<T>() where T : struct
        {
            if (!_rawDataWatchers.TryGetValue(typeof(T), out var watchersList))
                return Array.Empty<IRawDataWatcher>();

            return watchersList;
        }
    }
}
