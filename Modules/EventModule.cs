using System;
using System.Collections.Generic;
using ModulesFramework.Data.Events;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Modules
{
    public abstract partial class EcsModule
    {
        // type of event -> (type of system -> list of runners)
        private readonly Dictionary<Type, Dictionary<Type, IEventRunner>> _eventRunners = new();

        /// <summary>
        ///     Return true if the event has a runner
        /// </summary>
        public bool RegisterEvent<T>(T ev) where T : struct
        {
            var type = typeof(T);

            if (!_eventRunners.TryGetValue(type, out var eventRunners))
                return false;

            foreach (var (_, runner) in eventRunners)
            {
                ((EventRunner<T>)runner).AddEvent(ev);
            }

            return true;
        }

        internal void RunEvents(Type eventType)
        {
            RunEvents(eventType, typeof(IRunEventSystem));
        }

        private void RunEvents(Type eventType, Type systemType)
        {
            if (!_eventRunners.TryGetValue(eventType, out var eventRunners))
                return;

            if (!eventRunners.TryGetValue(systemType, out var runner))
                return;

            runner.Invoke(world);
        }

        private void RegisterSystemsGroupForEvent(Type eventType, Type systemType, SystemsGroup systemsGroup)
        {
            if (!_eventRunners.ContainsKey(eventType))
                _eventRunners[eventType] = new Dictionary<Type, IEventRunner>();

            if (!_eventRunners[eventType].ContainsKey(systemType))
            {
                var runnerType = typeof(EventRunner<,>).MakeGenericType(eventType, systemType);
                var runner = (IEventRunner)Activator.CreateInstance(runnerType)!;
                _eventRunners[eventType][systemType] = runner;
            }

            _eventRunners[eventType][systemType].AddSystemsGroup(systemsGroup);
        }

        private void UnregisterSystemsGroupForEvent(Type eventType, Type systemType, SystemsGroup systemsGroup)
        {
            if (!_eventRunners.TryGetValue(eventType, out var runners))
                return;

            if (!runners.ContainsKey(systemType))
                return;

            _eventRunners[eventType][systemType].RemoveSystemsGroup(systemsGroup);
        }
    }
}