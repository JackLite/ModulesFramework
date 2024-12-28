using ModulesFramework.Modules;
using System.Collections.Generic;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly List<EcsModule> _externalSubscribers = new(4);

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

            var wasHandled = false;
            foreach (var module in _externalSubscribers)
                wasHandled |= HandleEvent(ev, module);

            foreach (var module in _modules.Values)
                wasHandled |= HandleEvent(ev, module);

            if (!wasHandled)
            {
#if MODULES_DEBUG
                Logger.LogWarning($"No listeners for {typeof(T).Name} event");
#endif
            }
        }

        private static bool HandleEvent<T>(T ev, EcsModule module) where T : struct
        {
            var isSubscribersExists = module.RunSubscribers(ev);
            var isHandlerExists = module.AddEvent(ev);
            return isSubscribersExists || isHandlerExists;
        }

        internal void RegisterEventSubscriber(EcsModule module)
        {
            _externalSubscribers.Add(module);
        }
    }
}