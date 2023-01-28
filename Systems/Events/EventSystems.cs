using System;
using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Systems.Events
{
    internal class EventSystems
    {
        private readonly Dictionary<Type, List<IEventSystem>> _systems = new Dictionary<Type, List<IEventSystem>>()
        {
            { typeof(IRunEventSystem), new List<IEventSystem>(64) },
            { typeof(IPostRunEventSystem), new List<IEventSystem>(64) },
            { typeof(IFrameEndEventSystem), new List<IEventSystem>(64) }
        };

        internal void AddSystem(IEventSystem eventSystem)
        {
            foreach (var type in _systems.Keys)
            {
                if (type.IsInstanceOfType(eventSystem))
                    _systems[type].Add(eventSystem);
            }
        }
        
        internal void HandleEvent<T>(T ev, Type eventSystemType) where T : struct
        {
            if(eventSystemType == typeof(IRunEventSystem))
            {
                RunEvent(ev);
                return;
            }
            
            if (eventSystemType == typeof(IPostRunEventSystem))
            {
                PostRunEvent(ev);
                return;
            }
            
            if (eventSystemType == typeof(IFrameEndEventSystem))
            {
                FrameEndEvent(ev);
                return;
            }
        }

        internal void RunEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IRunEventSystem)])
            {
                ((IRunEventSystem<T>) system).RunEvent(ev);
            }
        }
        
        internal void PostRunEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IPostRunEventSystem)])
            {
                ((IPostRunEventSystem<T>) system).PostRunEvent(ev);
            }
        }
        
        internal void FrameEndEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IFrameEndEventSystem)])
            {
                ((IFrameEndEventSystem<T>) system).FrameEndEvent(ev);
            }
        }
    }
}