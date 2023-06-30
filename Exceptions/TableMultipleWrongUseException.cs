using System;

namespace ModulesFramework.Exceptions
{
    public class TableMultipleWrongUseException<T> : Exception where T : struct
    {
        public TableMultipleWrongUseException()
            : base($"Table for {typeof(T)} is multiple but you try to use it as single")
        {
        }
    }
}