using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Exceptions
{
    public class ModuleNotFoundException<T> : Exception where T : EcsModule
    {
        public ModuleNotFoundException() : base("Can't find module with type " + typeof(T))
        {
        }
    }
}