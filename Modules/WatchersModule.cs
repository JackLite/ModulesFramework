using System;
using System.Collections.Generic;
using ModulesFramework.Watchers;

namespace ModulesFramework.Modules
{
    #if MODULES_DEBUG
    public partial class EcsModule
    {
        private List<IComponentWatcher>? _watchers;

        private void CreateWatchers()
        {
            if (_watchers != null)
                return;

            _watchers = new List<IComponentWatcher>();

            foreach (var watcherType in world.GetWatcherTypes(GetType()))
            {
                var watcher = (IComponentWatcher)Activator.CreateInstance(watcherType);
                _watchers.Add(watcher);
            }
        }

        private void RegisterWatchers()
        {
            if (_watchers == null)
                return;

            foreach (var watcher in _watchers)
            {
                world.RegisterWatcher(watcher);
            }
        }

        private void UnregisterWatchers()
        {
            if (_watchers == null)
                return;

            foreach (var watcher in _watchers)
            {
                world.UnregisterWatcher(watcher);
            }
        }

        internal IEnumerable<IComponentWatcher> GetWatchers()
        {
            return _watchers == null ? Array.Empty<IComponentWatcher>() : _watchers;
        }
    }
    #endif
}
