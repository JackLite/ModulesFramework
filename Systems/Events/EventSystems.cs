using System;
using System.Collections.Generic;
using System.Linq;
using DataWorld = ModulesFramework.Data.DataWorld;

namespace ModulesFramework.Systems.Events
{
    internal class EventSystems
    {
        private readonly Dictionary<Type, List<IEventSystem>> _systems =
            new Dictionary<Type, List<IEventSystem>>()
            {
                {
                    typeof(IRunEventSystem), new List<IEventSystem>(64)
                },
                {
                    typeof(IPostRunEventSystem), new List<IEventSystem>(64)
                },
                {
                    typeof(IFrameEndEventSystem), new List<IEventSystem>(64)
                }
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

        internal void HandleEvent<T>(T ev, Type eventSystemType, DataWorld world) where T : struct
        {
            if (eventSystemType == typeof(IRunEventSystem))
            {
                RunEvent(ev, world);
                return;
            }

            if (eventSystemType == typeof(IPostRunEventSystem))
            {
                PostRunEvent(ev, world);
                return;
            }

            if (eventSystemType == typeof(IFrameEndEventSystem))
            {
                FrameEndEvent(ev, world);
            }
        }

        private void RunEvent<T>(T ev, DataWorld world) where T : struct
        {
            foreach (var system in _systems[typeof(IRunEventSystem)])
            {
                try
                {
                    ((IRunEventSystem<T>)system).RunEvent(ev);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        private void PostRunEvent<T>(T ev, DataWorld world) where T : struct
        {
            foreach (var system in _systems[typeof(IPostRunEventSystem)])
            {
                try
                {
                    ((IPostRunEventSystem<T>)system).PostRunEvent(ev);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        private void FrameEndEvent<T>(T ev, DataWorld world) where T : struct
        {
            foreach (var system in _systems[typeof(IFrameEndEventSystem)])
            {
                try
                {
                    ((IFrameEndEventSystem<T>)system).FrameEndEvent(ev);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }
    }
}