using System;
using System.Collections.Generic;
using ModulesFramework.Data;

namespace ModulesFramework.Watchers.OneDataWatchers
{
    public class OneDataTouchRegistry
    {
        private readonly Dictionary<Type, HashSet<OneDataTouchType>> _touchTypes = new Dictionary<Type, HashSet<OneDataTouchType>>();

        public void RegisterTouch(Type oneDataType, OneDataTouchType touchType)
        {
            if (!_touchTypes.TryGetValue(oneDataType, out var set))
            {
                set = new HashSet<OneDataTouchType>();
                _touchTypes[oneDataType] = set;
            }

            set.Add(touchType);
        }

        public void CallWatchersAndClear(WatchersGlobalRegistry registry, object owner)
        {
            foreach (var (dataType, set) in _touchTypes)
            {
                var watchers = registry.GetOneDataWatchers(dataType);
                foreach (var watcher in watchers)
                {
                    foreach (var touchType in set)
                    {
                        watcher.Watch(owner.GetType(), touchType);
                    }
                }
            }
            
            _touchTypes.Clear();
        }
    }
}
