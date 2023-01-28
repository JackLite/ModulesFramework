namespace ModulesFramework.Systems.Events
{
    public interface IFrameEndEventSystem : IEventSystem
    {
        
    }

    public interface IFrameEndEventSystem<T> : IFrameEndEventSystem where T : struct
    {
        void FrameEndEvent(T ev);
    }
}