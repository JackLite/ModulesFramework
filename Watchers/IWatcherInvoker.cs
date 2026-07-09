using System;
using System.Collections.Generic;

namespace ModulesFramework.Watchers
{
    internal interface IWatcherInvoker
    {
        public void Invoke(List<IComponentWatcher> watcher, Type systemType);
        public void Clear();
    }
}
