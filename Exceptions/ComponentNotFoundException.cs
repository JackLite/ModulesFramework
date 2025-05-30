using System;
using ModulesFramework.Utils.Types;

namespace ModulesFramework.Exceptions
{
    public class ComponentNotFoundException<T> : Exception
    {
        public ComponentNotFoundException()
            : base($"Component {typeof(T).GetTypeName()} not found")
        {
        }
        
        public ComponentNotFoundException(string msg)
            : base(msg)
        {
        }
    }
}