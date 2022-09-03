using System;
using System.Collections.Concurrent;

namespace ModulesFramework.Modules
{
    public static class ExceptionsPool
    {
        private static readonly ConcurrentQueue<Exception> Exceptions = new ConcurrentQueue<Exception>();

        public static void AddException(Exception exception)
        {
            Exceptions.Enqueue(exception);
        }

        public static bool TryPop(out Exception e)
        {
            return Exceptions.TryDequeue(out e);
        }
    }
}