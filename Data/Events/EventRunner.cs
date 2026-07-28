using System;
using System.Collections.Generic;
using ModulesFramework.Systems;

namespace ModulesFramework.Data.Events
{
    /// <summary>
    ///     Common interface to run events
    ///     Contains event and system group to handle it
    /// </summary>
    internal interface IEventRunner
    {
        public Type EventType { get; }
        public void AddSystemsGroup(SystemsGroup systemsGroup);
        public void Invoke(DataWorld world);
        public void RemoveSystemsGroup(SystemsGroup systemsGroup);
        public void Clear();
    }

    internal abstract class EventRunner<TEvent> : IEventRunner where TEvent : struct
    {
        protected readonly Queue<TEvent> events = new Queue<TEvent>();

        public Type EventType => typeof(TEvent);
        public abstract void AddSystemsGroup(SystemsGroup systemsGroup);
        public abstract void RemoveSystemsGroup(SystemsGroup systemsGroup);
        public void Clear()
        {
            events.Clear();
        }

        public abstract void Invoke(DataWorld world);

        public void AddEvent(TEvent ev)
        {
            events.Enqueue(ev);
        }
    }

    internal class EventRunner<TEvent, TSystem> : EventRunner<TEvent> where TEvent : struct
    {
        private readonly SortedDictionary<int, SystemsGroup> _systemsGroups = new SortedDictionary<int, SystemsGroup>();

        public override void AddSystemsGroup(SystemsGroup group)
        {
#if MODULES_DEBUG
            if (_systemsGroups.ContainsKey(group.Order))
                throw new ArgumentException($"Same order {group.Order} already exists", nameof(group.Order));
#endif

            _systemsGroups[group.Order] = group;
        }

        public override void RemoveSystemsGroup(SystemsGroup systemsGroup)
        {
            _systemsGroups.Remove(systemsGroup.Order);
        }

        public override void Invoke(DataWorld world)
        {
            while (events.TryDequeue(out var ev))
            {
                foreach (var (_, systemsGroup) in _systemsGroups)
                    systemsGroup.HandleEvent(ev, typeof(TSystem), world);
            }
        }
    }
}