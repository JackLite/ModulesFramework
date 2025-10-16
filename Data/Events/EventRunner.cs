using System;
using System.Collections.Generic;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Data.Events
{
    internal interface IEventRunner
    {
        public Type EventType { get; }
        public SystemsGroup SystemsGroup { get; }
        public void Run<T>(DataWorld world) where T : IEventSystem;
        public void RunSystem<T>(T system) where T : IEventSystem;
        public IEnumerable<TSystem> GetSystems<TSystem>() where TSystem : IEventSystem;
    }

    internal class EventRunner<T> : IEventRunner where T : struct
    {
        private readonly T _ev;
        public Type EventType => typeof(T);
        public SystemsGroup SystemsGroup { get; }

        public EventRunner(T ev, SystemsGroup systemsGroup)
        {
            _ev = ev;
            SystemsGroup = systemsGroup;
        }

        void IEventRunner.Run<TSystem>(DataWorld world)
        {
            SystemsGroup.HandleEvent(_ev, typeof(TSystem), world);
        }

        public void RunSystem<TSystem>(TSystem system) where TSystem : IEventSystem
        {
            if (system is IRunEventSystem<T> runEventSystem)
                runEventSystem.RunEvent(_ev);
            else if (system is IPostRunEventSystem<T> postRunEventSystem)
                postRunEventSystem.PostRunEvent(_ev);
            else if (system is IFrameEndEventSystem<T> frameEndEventSystem)
                frameEndEventSystem.FrameEndEvent(_ev);
        }

        public IEnumerable<TSystem> GetSystems<TSystem>() where TSystem : IEventSystem
        {
            var eventSystems = SystemsGroup.GetEventSystems<T>();
            if (eventSystems == null)
                return Array.Empty<TSystem>();

            return eventSystems.GetEventSystems<TSystem>();
        }
    }
}