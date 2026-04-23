using System;
using ModulesFramework.Utils.Types;
namespace ModulesFramework.Exceptions
{
    public class IndexerAlreadyExistsException<T, TIndex> : Exception where T : struct
    {
        public IndexerAlreadyExistsException() 
            : base($"Index for {typeof(T).GetTypeName()} and type {typeof(TIndex).GetTypeName()} already exists. " +
                "Every index for component must have unique type.")
        {
        }
    }
}
