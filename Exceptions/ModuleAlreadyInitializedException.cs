using System;

namespace ModulesFramework.Exceptions
{
    public class ModuleAlreadyInitializedException : Exception
    {
        public ModuleAlreadyInitializedException(Type moduleType)
            : base($"You try init {moduleType}, that already initialized")
        {
        }
    }
}