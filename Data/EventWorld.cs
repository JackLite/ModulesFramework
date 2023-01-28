using System;
using System.Collections.Generic;
using ModulesFramework.Data.Events;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, EventsHandler> _eventsHandlers = new Dictionary<Type, EventsHandler>();
        private readonly Dictionary<Type, List<SystemsGroup>> _eventListeners = new Dictionary<Type, List<SystemsGroup>>();

        public void RiseEvent<T>() where T : struct
        {
            var ev = new T();
            RiseEvent(ev);
        }
        
        public void RiseEvent<T>(T ev) where T : struct
        {
            var type = typeof(T);
            if (!_eventListeners.ContainsKey(type))
                return;

            if (!_eventsHandlers.ContainsKey(type))
                _eventsHandlers[type] = new EventsHandler();
            foreach (var systemsGroup in _eventListeners[type])
            {
                void Handler(Type systemType) => systemsGroup.HandleEvent(ev, systemType);
                _eventsHandlers[type].AddHandler<IRunEventSystem>(Handler);
                _eventsHandlers[type].AddHandler<IPostRunEventSystem>(Handler);
                _eventsHandlers[type].AddHandler<IFrameEndEventSystem>(Handler);
            }
        }

        internal EventsHandler GetHandlers(Type eventType)
        {
            if (!_eventsHandlers.ContainsKey(eventType))
                return new EventsHandler();
            return _eventsHandlers[eventType];
        }

        internal void RegisterListener(Type eventType, SystemsGroup systemsGroup)
        {
            if (!_eventListeners.ContainsKey(eventType))
                _eventListeners[eventType] = new List<SystemsGroup>(64);
            _eventListeners[eventType].Add(systemsGroup);
        }
        
        internal void UnregisterListener(Type eventType, SystemsGroup listener)
        {
            if (!_eventListeners.ContainsKey(eventType))
                _eventListeners[eventType] = new List<SystemsGroup>(64);
            _eventListeners[eventType].Remove(listener);
        }
    }
}