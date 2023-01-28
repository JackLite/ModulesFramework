namespace ModulesFramework.Systems.Events
{
    public interface IPostRunEventSystem : IEventSystem
    {
        
    }

    public interface IPostRunEventSystem<T> : IPostRunEventSystem where T : struct
    {
        void PostRunEvent(T ev);
    }
}