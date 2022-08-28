using System;

namespace EcsCore
{
    /// <summary>
    /// Simple signal for deactivate module
    /// Example: world.NewEntity().Replace(new EcsModuleDeactivationSignal {Type = typeof(YourModule)});
    /// </summary>
    public struct ModuleDeactivationSignal
    {
        public Type ModuleType;
    }
}