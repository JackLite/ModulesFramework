using System;
using System.Collections.Generic;
using ModulesFramework.Data.Subscribes;
using ModulesFramework.Systems;

namespace ModulesFramework.Modules
{
    public abstract partial class EcsModule
    {
        private readonly Dictionary<Type, Subscribers> _subscribeInitSystems = new Dictionary<Type, Subscribers>();
        private readonly Dictionary<Type, Subscribers> _subscribeActivateSystems = new Dictionary<Type, Subscribers>();

        /// <summary>
        ///     Return true if event was handled
        /// </summary>
        public bool RunSubscribers<T>(T ev) where T : struct
        {
            var type = typeof(T);
            if (_subscribeInitSystems.TryGetValue(type, out var subscribers))
                subscribers.HandleEvent(world, ev, true);

            var wasHandled = subscribers != null;

            if (_subscribeActivateSystems.TryGetValue(type, out subscribers))
                subscribers.HandleEvent(world, ev);

            return wasHandled || subscribers != null;
        }

        internal void RegisterSubscriber(Type eventType, SystemsGroup systemsGroup, int order, bool isInit = false)
        {
            var systemsList = isInit ? _subscribeInitSystems : _subscribeActivateSystems;
            if (systemsList.TryGetValue(eventType, out var systems))
            {
                systems.AddSystems(order, systemsGroup);
                return;
            }

            var subscribers = new Subscribers();
            subscribers.AddSystems(order, systemsGroup);
            systemsList[eventType] = subscribers;
        }

        internal void UnregisterSubscriber(Type eventType, SystemsGroup systemsGroup, bool isInit = false)
        {
            var systemsList = isInit ? _subscribeInitSystems : _subscribeActivateSystems;
            if (!systemsList.TryGetValue(eventType, out var systems))
                return;
            systems.RemoveSystems(systemsGroup);
        }
    }
}