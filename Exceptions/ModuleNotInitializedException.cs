using System;

namespace ModulesFramework.Exceptions
{
    public sealed class ModuleNotInitializedException : Exception
    {
        public ModuleNotInitializedException(Type moduleType)
        : base($"You try change active state in module {moduleType}, that not initialized")
        {
        }
    }
}