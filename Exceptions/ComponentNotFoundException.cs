using System;

namespace ModulesFramework.Exceptions
{
    public class ComponentNotFoundException<T> : Exception
    {
        public ComponentNotFoundException()
            : base($"Component {typeof(T).Name} not found")
        {
        }
        
        public ComponentNotFoundException(string msg)
            : base(msg)
        {
        }
    }
}