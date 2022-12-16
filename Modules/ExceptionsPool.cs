using System;
using System.Collections.Concurrent;

namespace ModulesFramework.Modules
{
    public static class ExceptionsPool
    {
        private static readonly ConcurrentQueue<Exception> _exceptions = new ConcurrentQueue<Exception>();

        public static void AddException(Exception exception)
        {
            _exceptions.Enqueue(exception);
        }

        public static bool TryPop(out Exception e)
        {
            return _exceptions.TryDequeue(out e);
        }
    }
}