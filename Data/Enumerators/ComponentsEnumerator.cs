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
            while (true)
            {
                var outOfRange = _index > _table.ActiveEntities.Length;
                if (outOfRange)
                    break;
                var isActive = _table.ActiveEntities[_index - 1];
                var eid = _index - 1;
                if (isActive && _filter[eid])
                    break;
                ++_index;
            }

            return _index <= _table.ActiveEntities.Length;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}