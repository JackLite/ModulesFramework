using System;
using System.Collections.Generic;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.OneDataWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Holds component touch repositories
    /// </summary>
    public class WatchersGlobalRegistry
    {
        /// <summary>
        ///     Component type -> list of watchers
        /// </summary>
        private readonly Dictionary<Type, List<IComponentTouchWatcher>> _componentTouchWatchers =
            new Dictionary<Type, List<IComponentTouchWatcher>>();

        /// <summary>
        ///     Component type -> list of watchers
        /// </summary>
        private readonly Dictionary<Type, List<IRawDataWatcher>> _rawDataWatchers =
            new Dictionary<Type, List<IRawDataWatcher>>();
        
        /// <summary>
        ///     Component type -> list of watchers
        /// </summary>
        private readonly Dictionary<Type, List<IOneDataWatcher>> _oneDataWatchers =
            new Dictionary<Type, List<IOneDataWatcher>>();


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
                
                if (@interface.GetGenericTypeDefinition() == typeof(IOneDataWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_oneDataWatchers.TryGetValue(type, out var watchersList))
                    {
                        watchersList = new List<IOneDataWatcher>();
                        _oneDataWatchers[type] = watchersList;
                    }
                    watchersList.Add((IOneDataWatcher)watcher);
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
                
                if (@interface.GetGenericTypeDefinition() == typeof(IOneDataWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    if (!_oneDataWatchers.TryGetValue(type, out var watchersList))
                        continue;

                    watchersList.Remove((IOneDataWatcher)watcher);

                    if (watchersList.Count == 0)
                        _oneDataWatchers.Remove(type);
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
        
        public bool IsOneDataWatcherRegistered(Type dataType)
        {
            return _oneDataWatchers.ContainsKey(dataType);
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

        public IReadOnlyCollection<IOneDataWatcher> GetOneDataWatchers(Type type)
        {
            if (!_oneDataWatchers.TryGetValue(type, out var watchersList))
                return Array.Empty<IOneDataWatcher>();

            return watchersList;
        }
    }
}
