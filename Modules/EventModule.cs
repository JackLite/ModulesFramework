using System;
using System.Collections.Generic;
using ModulesFramework.Data.Events;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Modules
{
    public abstract partial class EcsModule
    {
        private readonly Dictionary<Type, List<SystemsGroup>> _eventListeners = new();

        private readonly Dictionary<Type, Queue<IEventRunner>> _runEvents = new();
        private readonly Dictionary<Type, Queue<IEventRunner>> _postRunEvents = new();
        private readonly Dictionary<Type, Queue<IEventRunner>> _frameEndEvents = new();

        /// <summary>
        ///     Return true if event has listener
        /// </summary>
        public bool AddEvent<T>(T ev) where T : struct
        {
            if (!IsActivating)
                return false;

            var type = typeof(T);
            CheckRunEventType(type);
            var wasAdded = false;
            foreach (var systemsGroup in _eventListeners[type])
            {
                wasAdded = true;
                var runner = new EventRunner<T>(ev, systemsGroup);
                _runEvents[type].Enqueue(runner);
                _postRunEvents[type].Enqueue(runner);
                _frameEndEvents[type].Enqueue(runner);
            }

            return wasAdded;
        }

        private void CheckRunEventType(Type type)
        {
            CreateQueueIfNeed(type, _runEvents);
            CreateQueueIfNeed(type, _postRunEvents);
            CreateQueueIfNeed(type, _frameEndEvents);
        }

        private void CreateQueueIfNeed(Type type, Dictionary<Type, Queue<IEventRunner>> runners)
        {
            runners.TryAdd(type, new Queue<IEventRunner>());
        }

        internal void RunEvents(Type eventType)
        {
            RunEvents<IRunEventSystem>(eventType, _runEvents);
        }

        internal void PostRunEvents(Type eventType)
        {
            RunEvents<IPostRunEventSystem>(eventType, _postRunEvents);
        }

        internal void FrameEndEvents(Type eventType)
        {
            RunEvents<IFrameEndEventSystem>(eventType, _frameEndEvents);
        }

        private void RunEvents<TSystem>(Type eventType, Dictionary<Type, Queue<IEventRunner>> runners)
            where TSystem : IEventSystem
        {
            if (!runners.TryGetValue(eventType, out var queue))
                return;
            while (queue.Count > 0)
            {
                var runner = queue.Dequeue();
                try
                {
                    runner.Run<TSystem>(world);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        private void RegisterListener(Type eventType, SystemsGroup systemsGroup)
        {
            if (!_eventListeners.ContainsKey(eventType))
                _eventListeners[eventType] = new List<SystemsGroup>(64);
            _eventListeners[eventType].Add(systemsGroup);
        }

        private void UnregisterListener(Type eventType, SystemsGroup listener)
        {
            if (!_eventListeners.ContainsKey(eventType))
                _eventListeners[eventType] = new List<SystemsGroup>(64);
            _eventListeners[eventType].Remove(listener);
        }
    }
}