using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModulesFramework.Data;
using ModulesFramework.Data.Events;
using ModulesFramework.DependencyInjection;
using ModulesFramework.Exceptions;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;
using ModulesFramework.Utils.Types;

namespace ModulesFramework.Modules
{
    public partial class EcsModule
    {
        private List<ISystem>? _createdSystem;
        private HashSet<Type>? _systemTypes;
        private Dictionary<Type, RunEventSystemDefinition>? _eventSystems;
        private readonly SortedDictionary<int, SystemsGroup> _systems = new SortedDictionary<int, SystemsGroup>();
        private SystemsGroup[] _systemsArr = Array.Empty<SystemsGroup>();

        public HashSet<Type> SystemTypes => _systemTypes ?? GetSystemTypes();
        public Dictionary<Type, RunEventSystemDefinition> EventSystems => _eventSystems ?? GetEventSystems();

        /// <summary>
        ///     Return all system types. You can override this method to register specific system interface.
        ///     <br/>
        ///     <b>Warning!</b> Do not forget to use base method or list all system types that you want to use!
        /// </summary>
        protected virtual HashSet<Type> GetSystemTypes()
        {
            if (IsSubmodule)
            {
                return Parent!.GetSystemTypes();
            }

            return new HashSet<Type>
            {
                typeof(IPreInitSystem),
                typeof(IInitSystem),
                typeof(IActivateSystem),
                typeof(IDeactivateSystem),
                typeof(IDestroySystem),
                typeof(IRunSystem)
            };
        }

        protected virtual Dictionary<Type, RunEventSystemDefinition> GetEventSystems()
        {
            return new Dictionary<Type, RunEventSystemDefinition>
            {
                { typeof(IRunEventSystem), new RunEventSystemDefinition(new RunEventSystemInvoker()) }
            };
        }

        public virtual void CallSystems<TSystemType>(Action<TSystemType> call, bool includeSubmodules = true)
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }

#if MODULES_DEBUG
            if (!SystemTypes.Contains(typeof(TSystemType)))
            {
                world.Logger.LogWarning(
                    $"Module {ConcreteType.GetTypeName()} tries to call {typeof(TSystemType)} systems" +
                    $" but this type is not registered");
            }
#endif

            foreach (var (_, group) in _systems)
            {
                group.CallSystems(world, call);
            }

            if (!includeSubmodules)
                return;

            foreach (var submodulesGroup in _submodulesGroups)
            {
                foreach (var submodule in submodulesGroup.modules)
                {
                    submodule.CallSystems(call, includeSubmodules);
                }
            }
        }

        public virtual async Task CallSystemsAsync<TSystemType>(Func<TSystemType, Task> call)
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }

            foreach (var (_, group) in _systems)
            {
                await group.CallSystemsAsync(world, call);
            }
        }

        protected void CallEventSystems(Type systemType)
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }

            foreach (var (_, group) in _systems)
            {
                foreach (var eventType in group.EventTypes)
                {
                    RunEvents(eventType, systemType);
                }
            }
        }

        /// <summary>
        /// Let you set order of systems. Default order is 0. Systems will be ordered by ascending
        /// </summary>
        /// <returns>Dictionary with key - type of system and value - order</returns>
        protected virtual Dictionary<Type, int> GetSystemsOrder()
        {
            return new Dictionary<Type, int>(0);
        }

        internal IEnumerable<ISystem> GetSystems(Type systemType)
        {
            return _systemsArr.SelectMany(g => g.GetSystems(systemType));
        }

        private void InsertDependencies(ISystem system, DataWorld world)
        {
            var setupMethod = GetSetupMethod(system);
            if (setupMethod != null)
            {
                var parameters = setupMethod.GetParameters();
                var injections = new object[parameters.Length];
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var t = parameter.ParameterType;
                    if (t == typeof(DataWorld))
                    {
                        injections[i++] = world;
                        continue;
                    }

                    if (t.BaseType == typeof(OneData))
                    {
                        var data = world.GetOneData(t);
                        if (data == null)
                            ThrowOneDataException(t);
                        else
                            injections[i++] = data;
                        continue;
                    }

                    object? dependency = GetDependency(t);

                    if (dependency == null)
                    {
                        foreach (var module in _globalModules)
                        {
                            dependency = module.GetDependency(t);
                            if (dependency != null)
                                break;
                        }
                    }

                    if (dependency == null)
                    {
                        throw new Exception(
                            $"Can't find injection {parameter.ParameterType} in method {setupMethod.Name}" +
                            $" for system {system.GetType().GetTypeName()}");
                    }

                    injections[i++] = dependency;
                }

                setupMethod.Invoke(system, injections);
                return;
            }

            var fields = system.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var t = field.FieldType;
                if (t == typeof(DataWorld))
                {
                    field.SetValue(system, world);
                    continue;
                }

                if (t.BaseType == typeof(OneData))
                {
                    var data = world.GetOneData(t);
                    if (data == null)
                        ThrowOneDataException(t);
                    else
                        field.SetValue(system, data);
                    continue;
                }

                object? dependency = GetDependency(t);

                if (dependency == null)
                {
                    foreach (var module in _globalModules)
                    {
                        dependency = module.GetDependency(t);
                        if (dependency != null)
                            break;
                    }
                }

                if (dependency != null)
                    field.SetValue(system, dependency);
                else
                    world.Logger.LogDebug(
                        $"Can't inject dependency for {field.Name} for system {system.GetType().GetTypeName()}." +
                        " Ignore this message if you create field by yourself",
                        LogFilter.ModulesFull
                    );
            }
        }

        private void CreateSystems()
        {
            var systemOrder = GetSystemsOrder();
            _createdSystem = GetSystems().ToList();
            foreach (var system in _createdSystem)
            {
                var order = 0;
                if (systemOrder.ContainsKey(system.GetType()))
                    order = systemOrder[system.GetType()];

                if (!_systems.ContainsKey(order))
                    _systems[order] = new SystemsGroup(order);

                _systems[order].Add(system, this);
            }
        }

        protected virtual IEnumerable<ISystem> GetSystems()
        {
            return world.GetSystems(ConcreteType);
        }

        private MethodInfo? GetSetupMethod(ISystem system)
        {
            var methods = system.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.GetCustomAttribute<SetupAttribute>() == null)
                    continue;
                return methodInfo;
            }

            return null;
        }
    }
}