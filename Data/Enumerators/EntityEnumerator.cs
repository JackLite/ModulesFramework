using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityEnumerator
    {
        private readonly bool[] _filter;
        private readonly World.DataWorld _world;
        private int _index;
        private readonly bool[] _pool;

        internal EntityEnumerator(bool[] entities, bool[] filter, World.DataWorld world)
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