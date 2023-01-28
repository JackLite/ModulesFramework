using System;

namespace ModulesFramework.Exceptions
{
    public sealed class QuerySelectException<T> : ArgumentOutOfRangeException where T : struct
    {
        public QuerySelectException() : base(null, $"There is no entity with component {typeof(T)}")
        {
            
        }
    }

    public sealed class QuerySelectEntityException : ArgumentOutOfRangeException
    {
        public QuerySelectEntityException() : base(null, "No entity filtered by query")
        {
            
        }
    }
}