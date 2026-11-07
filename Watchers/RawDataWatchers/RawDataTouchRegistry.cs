namespace ModulesFramework.Watchers.RawDataWatchers
{
    public class RawDataTouchRegistry<T> : IRawDataTouchRegistry where T : struct
    {
        public void CallWatchers(WatchersGlobalRegistry watchersRegistry, object source)
        {
            var list = watchersRegistry.GetRawDataWatchers<T>();
            // using for to avoid IEnumerator<T> allocation
            for (var index = 0; index < list.Count; index++)
            {
                var watcher = list[index];
                ((IRawDataWatcher<T>)watcher).Watch(source.GetType());
            }
        }
    }
}
