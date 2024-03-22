namespace ModulesFramework.Systems.Subscribes
{
    public interface ISubscribeActivateSystem : ISubscribeSystem
    {
    }

    public interface ISubscribeActivateSystem<T> : ISubscribeActivateSystem where T : struct
    {
        public void HandleEvent(T ev);
    }
}