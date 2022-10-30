using System;

namespace ModulesFramework.Modules.Components
{
    /// <summary>
    /// Simple signal for turn on/off module
    /// </summary>
    public struct ModuleChangeStateSignal
    {
        public Type moduleType;
        public bool state;
    }
}