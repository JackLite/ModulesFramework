using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct ComponentsEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly bool[] _filter;
        private int _index;

        internal ComponentsEnumerator(EcsTable<T> table, bool[] filter)
        {
            _table = table;
            _filter = filter;
            _index = 0;
        }

        public ref T Current
        {
            get
            {
                if (_index == 0 || _table == null)
                    throw new InvalidOperationException();

                var eid = _index - 1;
                return ref _table.GetData(eid);
            }
        }

        public bool MoveNext()
        {
            ++_index;
            var eid = _index - 1;
            while (true)
            {
                var outOfRange = _index > _table.ActiveEntities.Length;
                if (outOfRange)
                    break;
                
                eid = _index - 1;
                if (eid >= _filter.Length)
                    break;
                var isActive = _table.ActiveEntities[_index - 1];
                if (isActive && _filter[eid])
                    break;
                ++_index;
            }

            return _index <= _table.ActiveEntities.Length && eid < _filter.Length;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}