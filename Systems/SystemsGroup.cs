using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ModulesFramework.Modules;
using ModulesFramework.Systems.Events;
using ModulesFramework.Systems.Subscribes;
using DataWorld = ModulesFramework.Data.DataWorld;

namespace ModulesFramework.Systems
{
    internal class SystemsGroup
    {
        private readonly List<Task> _tasks = new List<Task>(16);
        private readonly Dictionary<Type, List<ISystem>> _systems = new Dictionary<Type, List<ISystem>>();


        // Type of event -> list of tuples (isActivate, subscriber)
        private readonly Dictionary<Type, List<(bool, ISubscribeSystem)>> _subscribes
            = new Dictionary<Type, List<(bool, ISubscribeSystem)>>();

        // Type of event -> (interface of event system -> list of wrappers)
        private readonly Dictionary<Type, Dictionary<Type, List<RunEventSystemWrapper>>> _runEventSystems
            = new Dictionary<Type, Dictionary<Type, List<RunEventSystemWrapper>>>();

        internal IEnumerable<Type> EventTypes => _runEventSystems.Keys;
        internal IEnumerable<Type> SubscriptionTypes => _subscribes.Keys;

        internal IEnumerable<Type> AllSystems =>
            _systems.SelectMany(kvp => kvp.Value.Select(s => s.GetType()))
                .Concat(
                    _runEventSystems.SelectMany(kvp =>
                        kvp.Value.SelectMany(p => p.Value.Select(w => w.system.GetType()))
                    )
                );

        public int Order { get; private set; }

        public SystemsGroup(int order)
        {
            Order = order;
        }

        internal void PreInit(DataWorld world)
        {
            CallSystems<IPreInitSystem>(world, s => s.PreInit());
        }

        internal void Init(DataWorld world)
        {
            CallSystems<IInitSystem>(world, s => s.Init());
        }

        internal void Activate(DataWorld world)
        {
            CallSystems<IActivateSystem>(world, s => s.Activate());
        }

        internal void Run(DataWorld world)
        {
            CallSystems<IRunSystem>(world, s => s.Run());
        }

        public void CallSystems<TSystemType>(DataWorld world, Action<TSystemType> call)
        {
            if (!_systems.TryGetValue(typeof(TSystemType), out var systems))
                return;

            foreach (var s in systems)
            {
                try
                {
                    call((TSystemType)s);
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }
        }

        public async Task CallSystemsAsync<TSystemType>(DataWorld world, Func<TSystemType, Task> call)
        {
            if (!_systems.TryGetValue(typeof(TSystemType), out var systems))
                return;

            _tasks.Clear();
            foreach (var s in systems)
            {
                try
                {
                    _tasks.Add(call((TSystemType)s));
                }
                catch (Exception e)
                {
                    world.Logger.RethrowException(e);
                }
            }

            await Task.WhenAll(_tasks);
        }

        internal void Deactivate(DataWorld world)
        {
            CallSystems<IDeactivateSystem>(world, s => s.Deactivate());
        }

        internal void Destroy(DataWorld world)
        {
            CallSystems<IDestroySystem>(world, s => s.Destroy());
        }

        internal void Add(ISystem s, EcsModule module)
        {
            foreach (var type in module.SystemTypes)
            {
                if (type.IsInstanceOfType(s))
                {
                    if (!_systems.ContainsKey(type))
                        _systems[type] = new List<ISystem>(32);

                    _systems[type].Add(s);
                }
            }

            CheckEvents(s, module.EventSystems);
            CheckSubscriptions(s);
        }

        private void CheckEvents(ISystem s, Dictionary<Type, RunEventSystemDefinition> systemTypes)
        {
            if (s is not IEventSystem eventSystem) return;

            var interfaces = eventSystem.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (!type.IsGenericType)
                    continue;

                var parentInterfaces = type.GetInterfaces();
                foreach (var parentInterface in parentInterfaces)
                {
                    if (!systemTypes.ContainsKey(parentInterface))
                        continue;
                    
                    var eventType = type.GetGenericArguments()[0];
                    if (!_runEventSystems.ContainsKey(eventType))
                        _runEventSystems[eventType] = new Dictionary<Type, List<RunEventSystemWrapper>>();

                    if (!_runEventSystems[eventType].ContainsKey(parentInterface))
                        _runEventSystems[eventType][parentInterface] = new List<RunEventSystemWrapper>();

                    var wrapper = new RunEventSystemWrapper(eventSystem, systemTypes[parentInterface]);
                    _runEventSystems[eventType][parentInterface].Add(wrapper);
                }
            }
        }

        private void CheckSubscriptions(ISystem system)
        {
            if (system is not ISubscribeSystem subscribeSystem)
                return;

            var interfaces = subscribeSystem.GetType().GetInterfaces();
            foreach (var type in interfaces)
            {
                if (!type.IsGenericType)
                    continue;
                var isSubInit = type.GetInterface(nameof(ISubscribeInitSystem)) != null;
                var isSubActivate = type.GetInterface(nameof(ISubscribeActivateSystem)) != null;

                if (!isSubInit && !isSubActivate)
                    continue;

                var eventType = type.GetGenericArguments()[0];
                if (_subscribes.TryGetValue(eventType, out var systems))
                {
                    systems.Add((isSubActivate, subscribeSystem));
                    continue;
                }

                systems = new List<(bool, ISubscribeSystem)>
                {
                    (isSubActivate, subscribeSystem)
                };
                _subscribes[eventType] = systems;
            }
        }

        internal void HandleEvent<T>(T ev, Type systemType, DataWorld world) where T : struct
        {
            var eventType = typeof(T);
            if (!_runEventSystems.TryGetValue(eventType, out var systems))
                return;

            if (!systems.TryGetValue(systemType, out var wrappers))
                return;

            foreach (var wrapper in wrappers)
            {
                world.Logger.LogDebug($"Handle event {eventType} by {wrapper.system.GetType()}", LogFilter.EventsFull);
                wrapper.definition.systemInvoker.Invoke(ev, wrapper.system);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ProceedSubscriptions<T>(T ev, bool isInit = false) where T : struct
        {
            if (!_subscribes.TryGetValue(typeof(T), out var subscribeSystems))
                return;

            foreach (var (isActivate, system) in subscribeSystems)
            {
                if (isActivate != !isInit)
                    continue;

                if (!isInit && system is ISubscribeActivateSystem<T> activateSystem)
                    activateSystem.HandleEvent(ev);

                if (isInit && system is ISubscribeInitSystem<T> initSystem)
                    initSystem.HandleEvent(ev);
            }
        }

        internal IEnumerable<ISystem> GetSystems(Type systemType)
        {
            return _systems[systemType];
        }

        internal IEnumerable<Type> GetEventSystemsGenericTypes(Type eventType)
        {
            if (!_runEventSystems.TryGetValue(eventType, out var systems))
                throw new ArgumentException($"Event {eventType} does not exists in systems group");

            return systems.Select(p => p.Key);
        }
    }
}