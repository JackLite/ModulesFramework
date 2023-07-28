using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly int _eid;
        private int _index;

        internal MultipleComponentsEnumerator(EcsTable<T> table, int eid)
        {
            _table = table;
            _eid = eid;
            _index = 0;
        }

        public ref T Current
        {
            get
            {
                if (_index == 0 || _table == null)
                    throw new InvalidOperationException();

                var indices = _table.GetMultipleDenseIndices(_eid);
                var index = indices[_index - 1];
                return ref _table.At(index);
            }
        }

        public bool MoveNext()
        {
            _index++;
            var indices = _table.GetMultipleDenseIndices(_eid);
            return _index <= indices.Length;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}