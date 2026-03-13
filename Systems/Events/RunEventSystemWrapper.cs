namespace ModulesFramework.Systems.Events
{
    /// <summary>
    ///     Wraps up a system and its definition
    /// </summary>
    /// <seealso cref="RunEventSystemDefinition"/>>
    public readonly struct RunEventSystemWrapper
    {
        public readonly IEventSystem system;
        public readonly RunEventSystemDefinition definition;

        public RunEventSystemWrapper(IEventSystem system, RunEventSystemDefinition definition)
        {
            this.system = system;
            this.definition = definition;
        }
    }
}