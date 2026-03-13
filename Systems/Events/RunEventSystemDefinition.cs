namespace ModulesFramework.Systems.Events
{
    /// <summary>
    ///     Definition of an event system.
    ///     It will describe when an event system is active, i.e., when it stores an event to handle it later.
    /// </summary>
    public readonly struct RunEventSystemDefinition
    {
        public readonly IRunEventSystemInvoker systemInvoker;

        public RunEventSystemDefinition(IRunEventSystemInvoker systemInvoker)
        {
            this.systemInvoker = systemInvoker;
        }
    }
}