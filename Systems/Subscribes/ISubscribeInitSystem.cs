namespace ModulesFramework.Systems.Subscribes
{
    public interface ISubscribeInitSystem : ISubscribeSystem
    {
    }

    public interface ISubscribeInitSystem<T> : ISubscribeInitSystem
    {
        public void HandleEvent(T ev);
    }
}