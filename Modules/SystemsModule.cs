using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly SortedList<int, SystemsGroup> _systems = new SortedList<int, SystemsGroup>();
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

            var globalTypes = world.GetSystemTypes();

            return globalTypes;
        }

        protected virtual Dictionary<Type, RunEventSystemDefinition> GetEventSystems()
        {
            return world.GetEventSystemTypes();
        }

        /// <summary>
        ///     This method is used to call systems of a specified type in module and submodules.
        ///     Note: it works only if the module is set up.
        /// </summary>
        /// <param name="call">How to call systems</param>
        /// <param name="includeSubmodules">Call also in submodules</param>
        /// <param name="isSubmoduleNeedToBeActive">Submodule need to be active to be called</param>
        /// <seealso cref="CallSystemsAsync"/>
        public virtual void CallSystems<TSystemType>(
            Action<TSystemType> call,
            bool includeSubmodules = true,
            bool isSubmoduleNeedToBeActive = true)
        {
            EnsureSetup();

#if MODULES_DEBUG
            if (!SystemTypes.Contains(typeof(TSystemType)))
            {
                world.Logger.LogWarning(
                    $"Module {ConcreteType.GetTypeName()} tries to call {typeof(TSystemType)} systems" +
                    $" but this type is not registered");
            }
#endif

            SystemsCall(call, includeSubmodules, isSubmoduleNeedToBeActive);
        }

        internal void WorldCallSystems<TSystemType>(
            Action<TSystemType> call,
            bool includeSubmodules = true,
            bool isSubmoduleNeedToBeActive = true)
        {
            EnsureSetup();
            SystemsCall(call, includeSubmodules, isSubmoduleNeedToBeActive);
        }

        private void EnsureSetup()
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }
        }

        private void SystemsCall<TSystemType>(
            Action<TSystemType> call,
            bool includeSubmodules = true,
            bool isSubmoduleNeedToBeActive = true)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                var group = _systems.Values[i];
                group.CallSystems(world, call);
            }

            if (!includeSubmodules)
                return;

            foreach (var submodulesGroup in _submodulesGroups)
            {
                foreach (var submodule in submodulesGroup.modules)
                {
                    if (!submodule.IsInitialized)
                        continue;

                    if (isSubmoduleNeedToBeActive && !submodule.IsActive)
                        continue;

                    submodule.WorldCallSystems(call, true, isSubmoduleNeedToBeActive);
                }
            }
        }

        /// <summary>
        ///     This method is used to call async systems of a specified type in module and submodules.
        ///     Note: it works only if the module is set up.
        /// </summary>
        /// <param name="call">How to call systems. Must be async</param>
        /// <param name="includeSubmodules">Call also in submodules</param>
        /// <param name="isSubmoduleNeedToBeActive">Is submodule need to be active to be called</param>
        /// <seealso cref="CallSystemsAsync"/>
        public virtual async Task CallSystemsAsync<TSystemType>(
            Func<TSystemType, Task> call,
            bool includeSubmodules = true,
            bool isSubmoduleNeedToBeActive = true)
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }

            for (var i = 0; i < _systems.Count; i++)
            {
                var group = _systems.Values[i];
                await group.CallSystemsAsync(world, call);
            }

            if (!includeSubmodules)
                return;

            foreach (var submodulesGroup in _submodulesGroups)
            {
                foreach (var submodule in submodulesGroup.modules)
                {
                    if (!submodule.IsInitialized)
                        continue;

                    if (isSubmoduleNeedToBeActive && !submodule.IsActive)
                        continue;

                    await submodule.CallSystemsAsync(call, true, isSubmoduleNeedToBeActive);
                }
            }
        }

        /// <summary>
        ///     Allows calling event systems of a specified type in module and submodules.
        ///     Note: it works only if the module is set up.
        /// </summary>
        public void CallEventSystems<TSystem>(bool includeSubmodules = true) where TSystem : IEventSystem
        {
            if (!_isSetup)
            {
                throw new ModuleNotSetupException(
                    this,
                    $"You can't run systems before setup finished. Use {nameof(OnSetupEnd)} or {nameof(PreInitSystems)}"
                );
            }

            for (var i = 0; i < _systems.Count; i++)
            {
                var group = _systems.Values[i];
                foreach (var eventType in group.EventTypes)
                {
                    RunEvents(eventType, typeof(TSystem));
                }
            }

            if (includeSubmodules)
            {
                foreach (var submodulesGroup in _submodulesGroups)
                {
                    foreach (var submodule in submodulesGroup.modules)
                    {
                        if (submodule.IsActive)
                            submodule.CallEventSystems<TSystem>();
                    }
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

        private void CreateSystemsGroup()
        {
            var systemOrder = GetSystemsOrder();
            if (_createdSystem == null)
                _createdSystem = GetSystems().ToList();
            _systems.Clear();
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
    }
}
