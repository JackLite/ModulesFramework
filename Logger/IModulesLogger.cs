namespace ModulesFramework
{
    public interface IModulesLogger
    {
        public void LogDebug(string msg, LogFilter logFilter);
        public void LogDebug(object msg, LogFilter logFilter);

        public void LogWarning(string msg);
        public void LogWarning(object msg);

        public void LogError(string msg);
        public void LogError(object msg);
        void SetLogType(LogFilter logFilter);
    }
}