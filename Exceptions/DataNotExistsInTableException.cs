using System;

namespace ModulesFramework.Exceptions
{
    public class DataNotExistsInTableException<T> : Exception where T : struct
    {
        public DataNotExistsInTableException(int eid) 
            : base($"There is no entity with id={eid} in {typeof(T)} table")
        {
            
        }
    }
}