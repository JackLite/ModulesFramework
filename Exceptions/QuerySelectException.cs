using System;

namespace ModulesFramework.Exceptions
{
    public sealed class QuerySelectException<T> : ArgumentOutOfRangeException where T : struct
    {
        public QuerySelectException() : base($"There is no entity with component {typeof(T)}")
        {
            
        }
    }
}