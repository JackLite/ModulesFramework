using System;

namespace EcsCore
{
    /// <summary>
    /// Simple signal for deactivate module
    /// </summary>
    public struct ModuleDestroySignal
    {
        public Type ModuleType;
    }
}