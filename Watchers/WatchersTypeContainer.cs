#if MODULES_DEBUG
using System;
using System.Collections.Generic;
using System.Reflection;
using ModulesFramework.Modules;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.OneDataWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Filter watchers and hold them to create in future in modules.
    /// </summary>
    internal class WatchersTypeContainer
    {
        // module type -> list of watcher types
        private readonly Dictionary<Type, List<Type>> _watchersTypes = new Dictionary<Type, List<Type>>();

        public void ProcessType(Type type)
        {
            if (!typeof(IComponentTouchWatcher).IsAssignableFrom(type)
                && !typeof(IRawDataWatcher).IsAssignableFrom(type)
                && !typeof(IOneDataWatcher).IsAssignableFrom(type))
            {
                return;
            }

            var moduleAttribute = type.GetCustomAttribute<WatcherAttribute>();
            var moduleType = typeof(EmbeddedGlobalModule);
            if (moduleAttribute != null)
                moduleType = moduleAttribute.moduleType;

            if (!_watchersTypes.ContainsKey(moduleType))
                _watchersTypes.Add(moduleType, new List<Type>());

            _watchersTypes[moduleType].Add(type);
        }

        public IEnumerable<Type> GetWatcherTypes(Type moduleType)
        {
            if (_watchersTypes.TryGetValue(moduleType, out var watchers))
                return watchers;

            return Array.Empty<Type>();
        }
    }
}
#endif
