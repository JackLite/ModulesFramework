using System;
using System.Collections.Generic;
using System.Linq;

namespace ModulesFramework.Systems.Events
{
    internal class EventSystems
    {
        private readonly Dictionary<Type, List<IEventSystem>> _systems =
            new Dictionary<Type, List<IEventSystem>>()
            {
                { typeof(IRunEventSystem), new List<IEventSystem>(64) },
                { typeof(IPostRunEventSystem), new List<IEventSystem>(64) },
                { typeof(IFrameEndEventSystem), new List<IEventSystem>(64) }
            };

        internal IEnumerable<Type> AllSystems => _systems.SelectMany(kvp => kvp.Value.Select(s => s.GetType()));

        internal void AddRunEventSystem(IEventSystem system)
        {
            _systems[typeof(IRunEventSystem)].Add(system);
        }
        
        internal void AddPostRunEventSystem(IEventSystem system)
        {
            _systems[typeof(IPostRunEventSystem)].Add(system);
        }
        
        internal void AddFrameEndEventSystem(IEventSystem system)
        {
            _systems[typeof(IFrameEndEventSystem)].Add(system);
        }

        internal void HandleEvent<T>(T ev, Type eventSystemType) where T : struct
        {
            if (eventSystemType == typeof(IRunEventSystem))
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
            }
        }

        private void RunEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IRunEventSystem)])
            {
                ((IRunEventSystem<T>)system).RunEvent(ev);
            }
        }

        private void PostRunEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IPostRunEventSystem)])
            {
                ((IPostRunEventSystem<T>)system).PostRunEvent(ev);
            }
        }

        private void FrameEndEvent<T>(T ev) where T : struct
        {
            foreach (var system in _systems[typeof(IFrameEndEventSystem)])
            {
                ((IFrameEndEventSystem<T>)system).FrameEndEvent(ev);
            }
        }
    }
}