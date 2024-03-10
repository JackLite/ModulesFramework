using System;
using System.Collections.Generic;

namespace ModulesFramework.Data.QueryUtils
{
    public class OrBuilder
    {
        internal Dictionary<Type, OrWrapper> orWrappers = new();

        public OrBuilder Or<T>() where T : struct
        {
            orWrappers[typeof(T)] = new OrWrapper<T>();
            return this;
        }

        public bool Check(int eid, World.DataWorld world)
        {
            foreach (var (_, or) in orWrappers)
            {
                if (or.HasComponent(eid, world))
                    return true;
            }

            return false;
        }
    }
}