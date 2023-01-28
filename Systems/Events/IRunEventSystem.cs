namespace ModulesFramework.Systems.Events
{
    public interface IRunEventSystem : IEventSystem
    {
    }
    public interface IRunEventSystem<T> : IRunEventSystem where T : struct
    {
        void RunEvent(T ev);
    }
}