using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityDataEnumerator
    {
        private readonly bool[] _pool;
        private readonly bool[] _filter;
        private int _index;

        internal EntityDataEnumerator(bool[] pool, bool[] filter)
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
                var outOfRange = _index > _pool.Length;
                if (outOfRange)
                    break;
                var isActive = _pool[_index - 1];
                var eid = _index - 1;
                if (isActive && _filter[eid])
                    break;
                ++_index;
            }

            return _index <= _pool.Length;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}