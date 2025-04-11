using ModulesFramework.Modules;
using System.Collections.Generic;
using ModulesFramework.Data.Events;
using ModulesFramework.Utils;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly List<EcsModule> _externalSubscribers = new(4);
        private readonly Map<List<IExternalEventListener>> _externalListeners = new Map<List<IExternalEventListener>>();

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

            if (_externalListeners.TryGet<T>(out var listenersList))
            {
                foreach (var listener in listenersList)
                    ((IExternalEventListener<T>)listener).OnEvent(ev);

                wasHandled = true;
            }

            if (!wasHandled)
            {
#if MODULES_DEBUG
                Logger.LogWarning($"No listeners for {typeof(T).Name} event");
#endif
            }
        }

        private static bool HandleEvent<T>(T ev, EcsModule module) where T : struct
        {
            var isSubscribersExists = false;
            if (module.IsRoot)
                isSubscribersExists = module.RunSubscribers(ev);
            var isHandlerExists = module.AddEvent(ev);
            return isSubscribersExists || isHandlerExists;
        }

        internal void RegisterEventSubscriber(EcsModule module)
        {
            _externalSubscribers.Add(module);
        }

        public void RegisterListener<T>(IExternalEventListener<T> listener) where T : struct
        {
            if (!_externalListeners.TryGet<T>(out var list))
            {
                list = new List<IExternalEventListener>(4);
                _externalListeners.Add<T>(list);
            }

            list.Add(listener);
        }

        public void UnregisterListener<T>(IExternalEventListener<T> listener) where T : struct
        {
            if (!_externalListeners.TryGet<T>(out var list))
            {
#if MODULES_DEBUG
                Logger.LogWarning($"Listener {listener.GetType().Name} is not registered");
#endif
                return;
            }

            list.Remove(listener);
            if (list.Count == 0)
            {
                _externalListeners.Remove<T>();
            }
        }
    }
}