using System;

namespace ModulesFramework.Exceptions
{
    public class IndexAlreadyExistsException<T> : Exception where T : struct
    {
        public int EntityId { get; private set; }
        public IndexAlreadyExistsException(int eid) : base($"Index for {typeof(T).Name} already exists for entity {eid}")
        {
            EntityId = eid;
        }
    }
}