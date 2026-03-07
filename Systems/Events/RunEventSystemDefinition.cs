using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Systems.Events
{
    /// <summary>
    ///     Definition of an event system.
    ///     It describes when an event system is active, i.e., when it stores an event to handle it later.
    /// </summary>
    public readonly struct RunEventSystemDefinition
    {
        public readonly IRunEventSystemInvoker systemInvoker;
        public readonly Type activeModuleState;

        public RunEventSystemDefinition(IRunEventSystemInvoker systemInvoker, bool isActiveInInit = false)
        {
            this.systemInvoker = systemInvoker;
            if (isActiveInInit)
                activeModuleState = typeof(ModuleRunningState);
            else
                activeModuleState = typeof(ModuleInitializedState);
        }
    }
}