namespace ModulesFramework.Data.Events
{
    /// <summary>
    ///     Common interface for all external event listeners
    /// </summary>
    public interface IExternalEventListener
    {
        
    }

    public interface IExternalEventListener<in T> : IExternalEventListener
        where T : struct
    {
        public void OnEvent(T ev);
    }
}