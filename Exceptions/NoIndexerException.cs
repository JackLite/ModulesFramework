using System;
using ModulesFramework.Data;
using ModulesFramework.Utils.Types;

namespace ModulesFramework.Exceptions
{
    public class NoIndexerException<T> : Exception where T : struct
    {
        public NoIndexerException()
            : base($"There is no indexer for table of {typeof(T).GetTypeName()}. " +
                $"Create one with {nameof(EcsTable<T>.CreateKey)}")
        {
        }
    }
}