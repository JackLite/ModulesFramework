using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityDataEnumerator
    {
        private readonly ulong[] _pool;
        private readonly ulong[] _filter;
        private int _index;

        internal EntityDataEnumerator(ulong[] pool, ulong[] filter)
        {
            _pool = pool;
            _filter = filter;
            _index = 0;
        }

        public int Current
        {
            get
            {
                if (_pool == null || _index == 0)
                    throw new InvalidOperationException();

                return _index - 1;
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