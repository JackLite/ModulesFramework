using System;
using System.Collections.Generic;
using ModulesFramework.Systems;

namespace ModulesFramework.Data.Subscribes
{
    internal class Subscribers
    {
        private readonly SortedDictionary<int, List<SystemsGroup>> _subscribers 
            = new SortedDictionary<int, List<SystemsGroup>>();
        private readonly Queue<SystemsGroup> _groupsToCall = new Queue<SystemsGroup>();

        public void AddSystems(int order, SystemsGroup systemsGroup)
        {
            if (_subscribers.TryGetValue(order, out var systems))
            {
                systems.Add(systemsGroup);
                return;
            }

            _subscribers[order] = new List<SystemsGroup>
            {
                systemsGroup
            };
        }

        public void RemoveSystems(SystemsGroup systemsGroup)
        {
            foreach (var (_, systems) in _subscribers)
            {
                systems.Remove(systemsGroup);
            }
        }

        public void HandleEvent<T>(DataWorld world, T ev, bool isInit = false) where T : struct
        {
            foreach (var (_, systems) in _subscribers)
            {
                foreach (var systemsGroup in systems)
                {
                    _groupsToCall.Enqueue(systemsGroup);
                }
            }

            while (_groupsToCall.Count > 0)
            {
                var systemsGroup = _groupsToCall.Dequeue();
                try
                {
                    systemsGroup.ProceedSubscriptions(ev, isInit);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }
    }
}