using System;
using ModulesFramework.Data;
using ModulesFramework.Utils;
using ModulesFramework.Watchers.ComponentsTouchWatchers;
using ModulesFramework.Watchers.OneDataWatchers;
using ModulesFramework.Watchers.RawDataWatchers;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Specific control. When it creates, it starts watching components. After disposing, it clears all touches.
    /// </summary>
    public sealed class WatcherControl : IDisposable
    {
        private readonly WatcherControlsContainer _container;
        private readonly Map<IComponentTouchRepository> _componentTouchRepositories = new Map<IComponentTouchRepository>();
        private readonly Map<IRawDataTouchRegistry> _rawDataCalls = new Map<IRawDataTouchRegistry>();
        private readonly OneDataTouchRegistry _oneDataTouchRegistry = new OneDataTouchRegistry();

        private readonly WatchersGlobalRegistry _watchersGlobalRegistry;
        private object _owner;
        private bool _paused;

        internal WatcherControl(WatcherControlsContainer parentContainer, WatchersGlobalRegistry watchersRegistry)
        {
            _container = parentContainer;
            _watchersGlobalRegistry = watchersRegistry;
            _owner = this;
        }

        public void SetOwner(object owner)
        {
            _owner = owner;
        }

        internal void RegisterComponentTouch<T>(int eid, ComponentTouchType touch) where T : struct
        {
            if (_paused)
                return;

            if (!_watchersGlobalRegistry.IsComponentWatcherRegistered<T>())
                return;

            if (!_componentTouchRepositories.TryGet<T>(out var repository))
            {
                repository = new ComponentTouchRepository<T>();
                _componentTouchRepositories.Add<T>(repository);
            }

            repository.RegisterTouch(eid, touch);
        }

        internal void RegisterRawDataCall<T>() where T : struct
        {
            if (_paused)
                return;

            if (!_watchersGlobalRegistry.IsRawDataWatcherRegistered<T>())
                return;

            if (_rawDataCalls.TryGet<T>(out _))
                return;

            _rawDataCalls.Add<T>(new RawDataTouchRegistry<T>());
        }

        internal void RegisterOneDataTouch(Type oneDataType, OneDataTouchType touchType)
        {
            if (_paused)
                return;

            if (!_watchersGlobalRegistry.IsOneDataWatcherRegistered(oneDataType))
                return;

            _oneDataTouchRegistry.RegisterTouch(oneDataType, touchType);
        }

        public void CallWatchers()
        {
            _paused = true;
            foreach (var repository in _componentTouchRepositories)
                repository.CallWatchersAndClear(_watchersGlobalRegistry, _owner);

            foreach (var registry in _rawDataCalls)
                registry.CallWatchers(_watchersGlobalRegistry, _owner);

            _rawDataCalls.Clear();

            _oneDataTouchRegistry.CallWatchersAndClear(_watchersGlobalRegistry, _owner);
            _paused = false;
        }

        public void Dispose()
        {
            _componentTouchRepositories.Clear();
            _rawDataCalls.Clear();
            _owner = this;
            _container.Return(this);
        }
    }
}
