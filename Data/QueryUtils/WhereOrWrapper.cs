using System;

namespace ModulesFramework.Data.QueryUtils
{
    internal abstract class WhereOrWrapper
    {
        internal abstract bool Check(int eid, World.DataWorld world);
    }

    internal class WhereOrWrapper<T> : WhereOrWrapper where T : struct
    {
        private readonly Func<T, bool> _customFilter;

        public WhereOrWrapper(Func<T, bool> customFilter)
        {
            _customFilter = customFilter;
        }

        internal override bool Check(int eid, World.DataWorld world)
        {
            if (!world.HasComponent<T>(eid))
                return false;

            return _customFilter.Invoke(world.GetComponent<T>(eid));
        }
    }
}