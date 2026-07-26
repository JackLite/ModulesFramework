using System;
using ModulesFramework.Data;
using ModulesFramework.Utils;
using ModulesFramework.Watchers.ComponentsTouchWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Specific control. When it creates, it starts watching components. After disposing, it clears all touches.
    /// </summary>
    public sealed class WatcherControl : IDisposable
    {
        private readonly WatcherControlsContainer _container;
        private readonly Map<IComponentTouchRepository> _componentTouchRepositories = new Map<IComponentTouchRepository>();

        private readonly ComponentTouchWatchersGlobalRegistry _componentTouchWatchersGlobalRegistry;
        public object Owner
        {
            get;
            private set;
        }

        public WatcherControl(WatcherControlsContainer parentContainer, ComponentTouchWatchersGlobalRegistry watchersRegistry)
        {
            _container = parentContainer;
            _componentTouchWatchersGlobalRegistry = watchersRegistry;
        }

        public void SetOwner(object owner)
        {
            Owner = owner;
        }

        internal void RegisterComponentTouch<T>(int eid, ComponentTouchType touch) where T : struct
        {
            // check by global watchers registry if some watcher registered for this component
            // if not, ignore
            if (!_componentTouchWatchersGlobalRegistry.IsWatcherRegistered<T>())
                return;

            // else, register touch in local registry
            if (!_componentTouchRepositories.TryGet<T>(out var repository))
            {
                repository = new ComponentTouchRepository<T>();
                _componentTouchRepositories.Add<T>(repository);
            }

            repository.RegisterTouch(eid, touch);
        }

        public void CallWatchers()
        {
            foreach (var repository in _componentTouchRepositories)
            {
                repository.CallWatchersAndClear(_componentTouchWatchersGlobalRegistry, Owner);
            }
        }

        public void Dispose()
        {
            _componentTouchRepositories.Clear();
            _container.Return(this);
        }
    }
}
