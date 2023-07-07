using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct EntityEnumerator
    {
        private readonly bool[] _filter;
        private readonly DataWorld _world;
        private int _index;
        private readonly EntityData[] _pool;

        internal EntityEnumerator(EntityData[] entities, bool[] filter, DataWorld world)
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
                
                return _world.GetEntity(_pool[_index - 1].eid);
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
                var isActive = _pool[_index - 1].isActive;
                var eid = _pool[_index - 1].eid;
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