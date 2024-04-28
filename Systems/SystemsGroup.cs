using System;
using System.Collections.Generic;
using System.Linq;
using ModulesFramework.Data;
using ModulesFramework.Systems.Events;
using DataWorld = ModulesFramework.Data.DataWorld;

namespace ModulesFramework.Systems
{
    internal class SystemsGroup
    {
        private readonly Dictionary<Type, List<ISystem>> _systems = new Dictionary<Type, List<ISystem>>();

        // Type is type of event
        private readonly Dictionary<Type, EventSystems> _eventSystems = new Dictionary<Type, EventSystems>();

        private static readonly Type[] _systemTypes = new Type[]
        {
            typeof(IPreInitSystem), typeof(IInitSystem), typeof(IActivateSystem), typeof(IRunSystem),
            typeof(IRunPhysicSystem), typeof(IPostRunSystem), typeof(IDeactivateSystem), typeof(IDestroySystem)
        };

        internal IEnumerable<Type> EventTypes => _eventSystems.Keys;

        internal IEnumerable<Type> AllSystems =>
            _systems.SelectMany(kvp => kvp.Value.Select(s => s.GetType()))
                .Concat(_eventSystems.SelectMany(kvp => kvp.Value.AllSystems));

        internal SystemsGroup()
        {
            foreach (var type in _systemTypes)
            {
                _systems[type] = new List<ISystem>(64);
            }
        }

        internal void PreInit(DataWorld world)
        {
            foreach (var s in _systems[typeof(IPreInitSystem)])
            {
                try
                {
                    ((IPreInitSystem)s).PreInit();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Init(DataWorld world)
        {
            foreach (var s in _systems[typeof(IInitSystem)])
            {
                try
                {
                    ((IInitSystem)s).Init();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Activate(DataWorld world)
        {
            foreach (var s in _systems[typeof(IActivateSystem)])
            {
                try
                {
                    ((IActivateSystem)s).Activate();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Run(DataWorld world)
        {
            foreach (var s in _systems[typeof(IRunSystem)])
            {
                try
                {
                    ((IRunSystem)s).Run();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void RunPhysic(DataWorld world)
        {
            foreach (var s in _systems[typeof(IRunPhysicSystem)])
            {
                try
                {
                    ((IRunPhysicSystem)s).RunPhysic();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void PostRun(DataWorld world)
        {
            foreach (var s in _systems[typeof(IPostRunSystem)])
            {
                try
                {
                    ((IPostRunSystem)s).PostRun();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Deactivate(DataWorld world)
        {
            foreach (var s in _systems[typeof(IDeactivateSystem)])
            {
                try
                {
                    ((IDeactivateSystem)s).Deactivate();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Destroy(DataWorld world)
        {
            foreach (var s in _systems[typeof(IDestroySystem)])
            {
                try
                {
                    ((IDestroySystem)s).Destroy();
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        internal void Add(ISystem s)
        {
            foreach (var type in _systemTypes)
            {
                if (type.IsInstanceOfType(s))
                    _systems[type].Add(s);
            }

            if (s is not IEventSystem eventSystem) return;

            var interfaces = eventSystem.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (!type.IsGenericType)
                    continue;
                var isRun = type.GetInterface(nameof(IRunEventSystem)) != null;
                var isPostRun = type.GetInterface(nameof(IPostRunEventSystem)) != null;
                var isFrameEnd = type.GetInterface(nameof(IFrameEndEventSystem)) != null;
                if (isRun || isPostRun || isFrameEnd)
                {
                    var eventType = type.GetGenericArguments()[0];
                    _eventSystems.TryAdd(eventType, new EventSystems());

                    if (isRun)
                        _eventSystems[eventType].AddRunEventSystem(eventSystem);

                    if (isPostRun)
                        _eventSystems[eventType].AddPostRunEventSystem(eventSystem);

                    if (isFrameEnd)
                        _eventSystems[eventType].AddFrameEndEventSystem(eventSystem);
                }
            }
        }

        internal void HandleEvent<T>(T ev, Type eventSystemType, DataWorld world) where T : struct
        {
            var eventType = typeof(T);
            if (!_eventSystems.TryGetValue(eventType, out var systems))
                return;

            systems.HandleEvent(ev, eventSystemType, world);
        }
    }
}