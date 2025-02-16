using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityEnumerator
    {
        private readonly ulong[] _pool;
        private readonly ulong[] _filter;
        private readonly DataWorld _world;
        private int _index;

        internal EntityEnumerator(ulong[] entities, ulong[] filter, DataWorld world)
        {
            _pool = entities;
            _filter = filter;
            _world = world;
            _index = 0;
        }

        public Entity Current
        {
            get
            {
                if (_filter == null || _index == 0)
                    throw new InvalidOperationException();

                return _world.GetEntity(_index - 1);
            }
        }

        public bool MoveNext()
        {
            ++_index;
            while (true)
            {
                var outOfRange = _index > _pool.Length * 64;
                if (outOfRange)
                    break;

                var eid = _index - 1;
                var optIdx = eid / 64;
                var bitMask = eid % 64;

                var isActiveBit = _pool[optIdx] & (1UL << bitMask);
                var isFilteredBit = _filter[optIdx] & (1UL << bitMask);
                if ((isActiveBit & isFilteredBit) > 0)
                    break;
                ++_index;
            }

            return _index <= _pool.Length * 64;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}