using System;
using System.Collections.Generic;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Data.Events
{
    internal class EventsHandler
    {
        private readonly Action _handle;
        private readonly Dictionary<Type, Queue<Action<Type>>> _handlers = new Dictionary<Type, Queue<Action<Type>>>
        {
            { typeof(IRunEventSystem), new Queue<Action<Type>>(64) },
            { typeof(IPostRunEventSystem), new Queue<Action<Type>>(64) },
            { typeof(IFrameEndEventSystem), new Queue<Action<Type>>(64) },
        };

        internal void AddHandler<T>(Action<Type> handler) where T : IEventSystem
        {
            _handlers[typeof(T)].Enqueue(handler);
        }

        public void Run<T>() where T : IEventSystem
        {
            var type = typeof(T);
            var queue = _handlers[type];
            while(queue.Count > 0)
                queue.Dequeue().Invoke(type);
        }
    }
}