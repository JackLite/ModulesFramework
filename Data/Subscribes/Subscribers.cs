using System.Collections.Generic;
using ModulesFramework.Systems;

namespace ModulesFramework.Data.Subscribes
{
    internal class Subscribers
    {
        private readonly Dictionary<int, List<SystemsGroup>> _subscribers = new Dictionary<int, List<SystemsGroup>>();

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

        public void HandleEvent<T>(T ev, bool isInit = false) where T : struct
        {
            foreach (var (_, systems) in _subscribers)
            {
                foreach (var systemsGroup in systems)
                {
                    systemsGroup.ProceedSubscriptions(ev, isInit);
                }
            }
        }
    }
}