using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsQueryEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly bool[] _filter;
        private int _index;
        private int _innerIndex;

        internal MultipleComponentsQueryEnumerator(EcsTable<T> table, bool[] filter)
        {
            _table = table;
            _filter = filter;
            _index = 0;
            _innerIndex = 0;
        }

        public ref T Current
        {
            get
            {
                if (_index == 0 || _table == null)
                    throw new InvalidOperationException();

                var eid = _index - 1;
                var indices = _table.GetMultipleDenseIndices(eid);
                return ref _table.At(indices[_innerIndex - 1]);
            }
        }

        public bool MoveNext()
        {
            if(_index == 0)
                ++_index;
            {
                ++_innerIndex;
                var eid = _index - 1;
                var indices = _table.GetMultipleDenseIndices(eid);
                if (_innerIndex <= indices.Length)
                    return true;
            }

            ++_index;
            _innerIndex = 0;
            while (true)
            {
                var outOfRange = _index > _table.ActiveEntities.Length;
                if (outOfRange)
                    break;

                var eid = _index - 1;
                var isActive = _table.ActiveEntities[eid];
                if (isActive && _filter[eid])
                    break;

                ++_index;
            }

            return _index < _table.ActiveEntities.Length;
        }

        public void Reset()
        {
            _index = 0;
            _innerIndex = 0;
        }
    }
}