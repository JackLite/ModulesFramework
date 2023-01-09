using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Exceptions
{
    public sealed class ModuleNotFoundException<T> : Exception where T : EcsModule
    {
        public ModuleNotFoundException() : base("Can't find module with type " + typeof(T))
        {
        }
    }
}