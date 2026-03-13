using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Exceptions
{
    public class ModuleNotSetupException : Exception
    {
        public ModuleNotSetupException(EcsModule module, string message) 
            : base($"[Modules] Module {module.GetType()}. {message}")
        {
            
        }
    }
}