namespace ModulesFramework.Systems.Subscribes
{
    public interface ISubscribeInitSystem : ISubscribeSystem
    {
    }

    public interface ISubscribeInitSystem<T> : ISubscribeInitSystem where T : struct
    {
        public void HandleEvent(T ev);
    }
}