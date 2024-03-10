namespace ModulesFramework.Data.World
{
    public partial class DataWorld
    {
        internal IModulesLogger Logger { get; private set; } = new DefaultLogger();

        /// <summary>
        /// Set custom logger instead default
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <seealso cref="SetLogType"/>
        public void SetLogger(IModulesLogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Setup log filter. LogFilter is a filter for log message
        /// By default filter set in full so all log messages will be send in logger
        /// NOTE: Debug messages works only when MODULES_DEBUG defined
        /// </summary>
        /// <param name="logFilter">Filter of log</param>
        /// <see cref="LogFilter"/>
        public void SetLogType(LogFilter logFilter)
        {
            Logger.SetLogType(logFilter);
        }
    }
}