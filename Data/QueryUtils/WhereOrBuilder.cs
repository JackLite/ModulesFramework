using System;
using System.Collections.Generic;

namespace ModulesFramework.Data.QueryUtils
{
    public class WhereOrBuilder
    {
        internal Dictionary<Type, WhereOrWrapper> wrappers = new();

        public WhereOrBuilder OrWhere<T>(Func<T, bool> customFilter) where T : struct
        {
            wrappers[typeof(T)] = new WhereOrWrapper<T>(customFilter);
            return this;
        }

        public bool Check(int eid, World.DataWorld world)
        {
            foreach (var (_, or) in wrappers)
            {
                if (or.Check(eid, world))
                    return true;
            }

            return false;
        }
    }
}