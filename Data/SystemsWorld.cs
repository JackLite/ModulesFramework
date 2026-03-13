using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly HashSet<Type> _systemTypes = new HashSet<Type>
        {
            typeof(IPreInitSystem),
            typeof(IInitSystem),
            typeof(IActivateSystem),
            typeof(IDeactivateSystem),
            typeof(IDestroySystem),

            typeof(IRunSystem)
        };

        private readonly Dictionary<Type, RunEventSystemDefinition> _eventSystems =
            new Dictionary<Type, RunEventSystemDefinition>
            {
                { typeof(IRunEventSystem), new RunEventSystemDefinition(new RunEventSystemInvoker()) }
            };

        /// <summary>
        ///     Registers a system type to call later 
        /// </summary>
        /// <seealso cref="CallSystems"/>
        public void RegisterSystemType<TSystemType>() where TSystemType : ISystem
        {
            _systemTypes.Add(typeof(TSystemType));
        }

        /// <summary>
        ///     Call registered systems. Use it with your own types of systems
        /// </summary>
        /// <param name="call">Delegate so MF knows how to call your systems</param>
        /// <param name="isNeedToBeActive">If true, only active modules will be called</param>
        public void CallSystems<TSystemType>(Action<TSystemType> call, bool isNeedToBeActive = false)
        {
            _embeddedGlobalModule.CallSystems(call);

            foreach (var module in _modules.Values)
            {
                if (module.IsSubmodule || !module.IsInitialized)
                    continue;

                if (!module.IsActive && isNeedToBeActive)
                    continue;

                module.CallSystems(call);
            }
        }

        /// <summary>
        ///     Call registered systems async. Use it with your own types of systems.
        ///     Note: modules called one after another, but systems inside called within their order.
        ///     In other words, systems with the same order called simultaneously.
        /// </summary>
        /// <param name="call">Delegate so MF knows how to call your systems</param>
        /// <param name="isNeedToBeActive">If true, only active modules will be called</param>
        public async Task CallSystemsAsync<TSystemType>(Func<TSystemType, Task> call, bool isNeedToBeActive = false)
        {
            await _embeddedGlobalModule.CallSystemsAsync(call);

            foreach (var module in _modules.Values)
            {
                if (module.IsSubmodule || !module.IsInitialized)
                    continue;

                if (!module.IsActive && isNeedToBeActive)
                    continue;

                await module.CallSystemsAsync(call);
            }
        }

        /// <summary>
        ///     Returns all registered types of systems
        /// </summary>
        public HashSet<Type> GetSystemTypes()
        {
            return _systemTypes;
        }

        public void RegisterEventSystem<TSystemType>(IRunEventSystemInvoker invoker) where TSystemType : IEventSystem
        {
            _eventSystems.Add(typeof(TSystemType), new RunEventSystemDefinition(invoker));
        }
        
        public Dictionary<Type, RunEventSystemDefinition> GetEventSystemTypes()
        {
            return _eventSystems;
        }

        public void CallEventSystems<TSystemType>() where TSystemType : IEventSystem
        {
            _embeddedGlobalModule.CallEventSystems<TSystemType>();
            foreach (var module in _modules.Values)
            {
                if (module.IsSubmodule || !module.IsInitialized)
                    continue;

                if (!module.IsActive)
                    continue;

                module.CallEventSystems<TSystemType>();
            }
        }
    }
}