using System;

namespace ModulesFramework.Exceptions
{
    public class SubmoduleException : Exception
    {
        public SubmoduleException(string msg) : base(msg)
        {}
    }
}