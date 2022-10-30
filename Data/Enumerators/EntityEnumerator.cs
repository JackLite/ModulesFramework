using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityEnumerator
    {
        private readonly EntityData[] _pool;
        private readonly DataWorld _world;
        private int _index;

        internal EntityEnumerator(EntityData[] pool, DataWorld world)
        {
            _pool = pool;
            _world = world;
            _index = 0;
        }

        public Entity Current
        {
            get
            {
                if (_pool == null || _index == 0)
                    throw new InvalidOperationException();
                
                return _world.GetEntity(_pool[_index - 1].eid);
            }
        }

        public bool MoveNext()
        {
            ++_index;
            if (_pool == null) return false;
            while (_index <= _pool.Length && (!_pool[_index - 1].isActive ||_pool[_index - 1].exclude))
                ++_index;
            return _pool != null && _pool.Length >= _index;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}