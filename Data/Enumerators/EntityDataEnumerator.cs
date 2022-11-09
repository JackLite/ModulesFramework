using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityDataEnumerator
    {
        private readonly EntityData[] _pool;
        private int _index;

        internal EntityDataEnumerator(EntityData[] pool)
        {
            _pool = pool;
            _index = 0;
        }

        public int Current
        {
            get
            {
                if (_pool == null || _index == 0)
                    throw new InvalidOperationException();
                
                return _pool[_index - 1].eid;
            }
        }

        public bool MoveNext()
        {
            ++_index;
            if (_pool == null) return false;
            while (_index <= _pool.Length && (!_pool[_index - 1].isActive || _pool[_index - 1].exclude))
                ++_index;
            return _pool != null && _pool.Length >= _index;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}