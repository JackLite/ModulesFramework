using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Exceptions
{
    public class ModuleAlreadyInitializedException<T> : Exception where T : EcsModule
    {
        public ModuleAlreadyInitializedException()
            : base($"You try init {typeof(T)}, that already initialized")
        {
        }
    }
}