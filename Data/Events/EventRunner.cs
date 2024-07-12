using ModulesFramework.Systems;
using ModulesFramework.Systems.Events;

namespace ModulesFramework.Data.Events
{
    internal interface IEventRunner
    {
        public void Run<T>(DataWorld world) where T : IEventSystem;
    }

    internal class EventRunner<T> : IEventRunner where T : struct
    {
        private readonly T _ev;
        private readonly SystemsGroup _systemsGroup;

        public EventRunner(T ev, SystemsGroup systemsGroup)
        {
            _ev = ev;
            _systemsGroup = systemsGroup;
        }

        public void Run<TSystem>(DataWorld world) where TSystem : IEventSystem
        {
            _systemsGroup.HandleEvent(_ev, typeof(TSystem), world);
        }
    }
}