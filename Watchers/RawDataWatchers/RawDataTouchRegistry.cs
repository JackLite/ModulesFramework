namespace ModulesFramework.Watchers.RawDataWatchers
{
    public class RawDataTouchRegistry<T> : IRawDataTouchRegistry where T : struct
    {
        public void CallWatchers(WatchersGlobalRegistry watchersRegistry, object source)
        {
            foreach (var watcher in watchersRegistry.GetRawDataWatchers<T>())
            {
                ((IRawDataWatcher<T>)watcher).Watch(source.GetType());
            }
        }
    }
}
