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

        /// <summary>
        /// Create default event T and rise it
        /// The event will be handled by event systems
        /// </summary>
        /// <typeparam name="T">Type of event</typeparam>
        /// <seealso cref="RiseEvent{T}(T)"/>
        public void RiseEvent<T>() where T : struct
        {
            var ev = new T();
            RiseEvent(ev);
        }
        
        /// <summary>
        /// Rise event that will be handled by event systems
        /// </summary>
        /// <param name="ev">Event</param>
        /// <typeparam name="T">Type of event</typeparam>
        /// <seealso cref="RiseEvent{T}()"/>
        public void RiseEvent<T>(T ev) where T : struct
        {
            var type = typeof(T);
            #if MODULES_DEBUG
            Logger.LogDebug($"Rising {typeof(T).Name} event", LogFilter.EventsFull);
            #endif
            if (!_eventListeners.ContainsKey(type))
            {
                #if MODULES_DEBUG
                Logger.LogWarning($"No listeners for {typeof(T).Name} event");
                #endif
                return;
            }

            if (!_eventsHandlers.ContainsKey(type))
                _eventsHandlers[type] = new EventsHandler();
            foreach (var systemsGroup in _eventListeners[type])
            {
                void Handler(Type systemType) => systemsGroup.HandleEvent(ev, systemType, this);
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