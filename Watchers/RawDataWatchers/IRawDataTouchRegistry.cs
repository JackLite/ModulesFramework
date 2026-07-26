namespace ModulesFramework.Watchers.RawDataWatchers
{
    public interface IRawDataTouchRegistry
    {
        public void CallWatchers(WatchersGlobalRegistry watchersRegistry, object source);
    }
}
