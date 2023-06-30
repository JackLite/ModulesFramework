using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly bool[] _filter;
        private int _index;
        private int _innerIndex;

        internal MultipleComponentsEnumerator(EcsTable<T> table, bool[] filter)
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

                var eid = _table.EntitiesData[_index - 1].eid;
                var indices = _table.GetMultipleDataIndices(eid);
                return ref _table.At(indices[_innerIndex - 1]);
            }
        }

        public bool MoveNext()
        {
            if(_index == 0)
                ++_index;
            {
                ++_innerIndex;
                var eid = _table.EntitiesData[_index - 1].eid;
                var indices = _table.GetMultipleDataIndices(eid);
                if (_innerIndex <= indices.Length)
                    return true;
            }

            ++_index;
            _innerIndex = 0;
            while (true)
            {
                var outOfRange = _index > _table.EntitiesData.Length;
                if (outOfRange)
                    break;

                var isActive = _table.EntitiesData[_index - 1].isActive;
                var eid = _table.EntitiesData[_index - 1].eid;
                if (isActive && _filter[eid])
                    break;

                ++_index;
            }

            return _index < _table.EntitiesData.Length;
        }

        public void Reset()
        {
            _index = 0;
            _innerIndex = 0;
        }
    }
}