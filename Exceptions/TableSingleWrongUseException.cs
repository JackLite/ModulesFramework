using System;

namespace ModulesFramework.Exceptions
{
    public class TableSingleWrongUseException<T> : Exception where T : struct
    {
        public TableSingleWrongUseException()
            : base($"Table for {typeof(T)} is single but you try to use it as multiple")
        {
        }
    }
}