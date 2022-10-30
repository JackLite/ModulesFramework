using System;

namespace ModulesFramework.Modules.Components
{
    /// <summary>
    /// Simple signal for activate module
    /// Also you can set dependenciesModule field if you want to get dependencies from another module
    /// </summary>
    public struct ModuleInitSignal
    {
        public Type moduleType;
        public Type dependenciesModule;
    }
}