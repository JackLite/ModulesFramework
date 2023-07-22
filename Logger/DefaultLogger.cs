using System;

namespace ModulesFramework
{
    public class DefaultLogger : IModulesLogger
    {
        private LogFilter _logFilter = LogFilter.Full;
        public void LogDebug(string msg, LogFilter logFilter = LogFilter.Full)
        {
            if((_logFilter & logFilter) != LogFilter.None)
                Console.WriteLine(msg);
        }

        public void LogDebug(object msg, LogFilter logFilter = LogFilter.Full)
        {
            if((_logFilter & logFilter) != LogFilter.None)
                LogDebug(msg.ToString());
        }

        public void LogWarning(string msg)
        {
            LogDebug(msg);
        }

        public void LogWarning(object msg)
        {
            LogDebug(msg);
        }

        public void LogError(string msg)
        {
            LogDebug(msg);
        }

        public void LogError(object msg)
        {
            LogDebug(msg);
        }

        public void RethrowException(Exception e)
        {
            throw e;
        }

        public void SetLogType(LogFilter logFilter)
        {
            _logFilter = logFilter;
        }
    }
}