using System;

namespace ModulesFramework.Watchers
{
    /// <summary>
    ///     Use it if you want to bind watcher to a specific module
    /// </summary>
    public class WatcherAttribute : Attribute
    {
        public readonly Type moduleType;

        public WatcherAttribute(Type moduleType)
        {
            this.moduleType = moduleType;
        }
    }
}
