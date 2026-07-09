using System;
using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Holds component touches and can call watchers bypassing touches
    /// </summary>
    internal class WatcherInvoker<T> : IWatcherInvoker where T : struct
    {
        private readonly List<Touch> _touchQueue = new(128);

        public void Register(int eid, ComponentTouchType touchType)
        {
            _touchQueue.Add(new Touch(eid, touchType));
        }

        public void Invoke(List<IComponentWatcher> watchers, Type systemType)
        {
            foreach (var touch in _touchQueue)
            {
                foreach (var watcher in watchers)
                {
                    if (watcher is IComponentWatcher<T> typedWatcher)
                        typedWatcher.Watch(touch.eid, systemType, touch.touchType);
                }
            }
        }
        public void Clear()
        {
            _touchQueue.Clear();
        }

        private readonly struct Touch
        {
            public readonly int eid;
            public readonly ComponentTouchType touchType;

            public Touch(int eid, ComponentTouchType touchType)
            {
                this.eid = eid;
                this.touchType = touchType;
            }
        }
    }

}
