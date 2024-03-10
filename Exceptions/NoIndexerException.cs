using System;
using ModulesFramework.Data;

namespace ModulesFramework.Exceptions
{
    public class NoIndexerException<T> : Exception where T : struct
    {
        public NoIndexerException()
            : base($"There is no indexer for table of {typeof(T).Name}. " +
                $"Create one with {nameof(EcsTable<T>.CreateKey)}")
        {
        }
    }
}