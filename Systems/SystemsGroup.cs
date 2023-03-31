using System;
using System.Collections.Generic;
using System.Reflection;
using ModulesFramework.Data;
using ModulesFramework.Data.Events;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Systems
{
    internal class SystemsGroup
    {
        private readonly Dictionary<Type, List<ISystem>> _systems = new Dictionary<Type, List<ISystem>>();

        // Type is type of event
        private readonly Dictionary<Type, EventSystems> _eventSystems = new Dictionary<Type, EventSystems>();

        private static readonly Type[] _systemTypes = new Type[]
        {
            typeof(IPreInitSystem),
            typeof(IInitSystem),
            typeof(IActivateSystem),
            typeof(IRunSystem),
            typeof(IRunPhysicSystem),
            typeof(IPostRunSystem),
            typeof(IDeactivateSystem),
            typeof(IDestroySystem)
        };

        internal IEnumerable<Type> EventTypes => _eventSystems.Keys;

        internal SystemsGroup()
        {
            foreach (var type in _systemTypes)
            {
                _systems[type] = new List<ISystem>(64);
            }
        }

        internal void PreInit()
        {
            foreach (var s in _systems[typeof(IPreInitSystem)])
                ((IPreInitSystem)s).PreInit();
        }

        internal void Init()
        {
            foreach (var s in _systems[typeof(IInitSystem)])
                ((IInitSystem)s).Init();
        }

        internal void Activate()
        {
            foreach (var s in _systems[typeof(IActivateSystem)])
                ((IActivateSystem)s).Activate();
        }

        internal void Run()
        {
            foreach (var s in _systems[typeof(IRunSystem)])
                ((IRunSystem)s).Run();
        }

        internal void RunPhysic()
        {
            foreach (var s in _systems[typeof(IRunPhysicSystem)])
                ((IRunPhysicSystem)s).RunPhysic();
        }

        internal void PostRun()
        {
            foreach (var s in _systems[typeof(IPostRunSystem)])
                ((IPostRunSystem)s).PostRun();
        }

        internal void Deactivate()
        {
            foreach (var s in _systems[typeof(IDeactivateSystem)])
                ((IDeactivateSystem)s).Deactivate();
        }

        internal void Destroy()
        {
            foreach (var s in _systems[typeof(IDestroySystem)])
                ((IDestroySystem)s).Destroy();
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
                    if (!_eventSystems.ContainsKey(eventType))
                        _eventSystems[eventType] = new EventSystems();
                    
                    if (isRun)
                        _eventSystems[eventType].AddRunEventSystem(eventSystem);
                    
                    if (isPostRun)
                        _eventSystems[eventType].AddPostRunEventSystem(eventSystem);
                    
                    if (isFrameEnd)
                        _eventSystems[eventType].AddFrameEndEventSystem(eventSystem);
                }
            }
        }

        internal void HandleEvent<T>(T ev, Type eventSystemType) where T : struct
        {
            var eventType = typeof(T);
            if (!_eventSystems.TryGetValue(eventType, out var systems))
                return;

            systems.HandleEvent(ev, eventSystemType);
        }
    }
}