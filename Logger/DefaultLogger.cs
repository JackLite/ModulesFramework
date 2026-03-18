using System;

namespace ModulesFramework
{
    public class DefaultLogger : IModulesLogger
    {
        public LogFilter LogFilter { get; private set; } = LogFilter.Full;

        public virtual void LogDebug(string msg, LogFilter logFilter = LogFilter.Full)
        {
            if((LogFilter & logFilter) != LogFilter.None)
                Console.WriteLine(msg);
        }

        public virtual void LogDebug(object msg, LogFilter logFilter = LogFilter.Full)
        {
            if((LogFilter & logFilter) != LogFilter.None)
                LogDebug(msg.ToString());
        }

        public virtual void LogWarning(string msg)
        {
            LogDebug(msg);
        }

        public virtual void LogWarning(object msg)
        {
            LogDebug(msg);
        }

        public virtual void LogError(string msg)
        {
            LogDebug(msg);
        }

        public virtual void LogError(object msg)
        {
            LogDebug(msg);
        }

        public virtual void RethrowException(Exception e)
        {
            Console.WriteLine(e.StackTrace);
            throw e;
        }

        public virtual void SetLogType(LogFilter logFilter)
        {
            LogFilter = logFilter;
        }
    }
}