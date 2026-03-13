namespace ModulesFramework.Systems.Events
{
    public interface IRunEventSystemInvoker
    {
        public void Invoke<TEvent>(TEvent ev, IEventSystem system) where TEvent : struct;
    }

    public class RunEventSystemInvoker : IRunEventSystemInvoker
    {
        public void Invoke<TEvent>(TEvent ev, IEventSystem system) where TEvent : struct
        {
            ((IRunEventSystem<TEvent>)system).RunEvent(ev);
        }
    }
}