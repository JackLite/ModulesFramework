using System;
using System.Collections.Generic;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.OneDataWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Modules
{
    #if MODULES_DEBUG
    public partial class EcsModule
    {
        private List<IComponentTouchWatcher>? _componentWatchers;
        private List<IRawDataWatcher>? _rawDataWatchers;
        private List<IOneDataWatcher>? _oneDataWatchers;

        private void CreateWatchers()
        {
            CreateSpecificWatchersIfNeed(ref _componentWatchers);
            CreateSpecificWatchersIfNeed(ref _rawDataWatchers);
            CreateSpecificWatchersIfNeed(ref _oneDataWatchers);
        }

        private void CreateSpecificWatchersIfNeed<T>(ref List<T>? watchersList)
        {
            if (watchersList != null)
                return;

            watchersList = new List<T>();

            foreach (var watcherType in world.GetWatcherTypes(GetType()))
            {
                if (!typeof(T).IsAssignableFrom(watcherType))
                    continue;
                var watcher = (T)Activator.CreateInstance(watcherType);
                watchersList.Add(watcher);
            }
        }

        private void RegisterWatchers()
        {
            if (_componentWatchers != null)
            {
                foreach (var watcher in _componentWatchers)
                    world.RegisterComponentWatcher(watcher);
            }

            if (_rawDataWatchers != null)
            {
                foreach (var watcher in _rawDataWatchers)
                    world.RegisterRawDataWatcher(watcher);
            }

            if (_oneDataWatchers != null)
            {
                foreach (var watcher in _oneDataWatchers)
                    world.RegisterOneDataWatcher(watcher);
            }
        }

        private void UnregisterWatchers()
        {
            if (_componentWatchers != null)
            {
                foreach (var watcher in _componentWatchers)
                    world.UnregisterComponentWatcher(watcher);
            }

            if (_rawDataWatchers != null)
            {
                foreach (var watcher in _rawDataWatchers)
                    world.UnregisterRawDataWatcher(watcher);
            }

            if (_oneDataWatchers != null)
            {
                foreach (var watcher in _oneDataWatchers)
                    world.UnregisterOneDataWatcher(watcher);
            }
        }
    }
    #endif
}
