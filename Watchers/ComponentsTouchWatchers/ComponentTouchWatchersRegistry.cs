using System;
using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.ComponentsTouchWatchers
{
    /// <summary>
    ///     Holds component touch repositories
    /// </summary>
    public class ComponentTouchWatchersRegistry
    {
        /// <summary>
        ///     Component type -> touches repository
        /// </summary>
        private readonly Dictionary<Type, IComponentTouchRepository> _componentTypes = new Dictionary<Type, IComponentTouchRepository>();

        /// <summary>
        ///     Component type -> count of watchers. When the count is 0, the repository is removed
        /// </summary>
        private readonly Dictionary<Type, int> _watchersCount = new Dictionary<Type, int>();

        public void RegisterWatcherComponents(IComponentWatcher watcher)
        {
            foreach (var @interface in watcher.GetType().GetInterfaces())
            {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IComponentWatcher<>))
                {
                    var type = @interface.GetGenericArguments()[0];

                    _watchersCount.TryAdd(type, 0);
                    _watchersCount[type]++;

                    var repositoryType = typeof(ComponentTouchRepository<>).MakeGenericType(type);
                    var repository = (IComponentTouchRepository)Activator.CreateInstance(repositoryType);
                    _componentTypes.TryAdd(type, repository);
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

                    if (!_componentTypes.ContainsKey(type))
                        continue;

                    if (_watchersCount.ContainsKey(type))
                        _watchersCount[type]--;

                    if (_watchersCount.GetValueOrDefault(type, 0) <= 0)
                        _componentTypes.Remove(type);
                }
            }
        }

        public void RegisterTouch<T>(int eid, ComponentTouchType touch) where T : struct
        {
            if (!_componentTypes.TryGetValue(typeof(T), out var repository))
                return;

            var typedRepository = (ComponentTouchRepository<T>)repository;
            typedRepository.RegisterTouch(eid, touch);
        }
    }
}
