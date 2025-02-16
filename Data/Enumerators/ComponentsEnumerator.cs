using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct ComponentsEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly ulong[] _filter;
        private int _index;

        internal ComponentsEnumerator(EcsTable<T> table, ulong[] filter)
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
                var outOfRange = _index > _table.ActiveEntitiesBits.Length * 64;
                if (outOfRange)
                    break;

                eid = _index - 1;
                if (eid >= _filter.Length * 64)
                    break;

                var optIdx = eid / 64;
                var bitMask = eid % 64;
                var isActiveBit = _table.ActiveEntitiesBits[optIdx] & (1UL << bitMask);
                var isFilteredBit = _filter[optIdx] & (1UL << bitMask);
                if ((isActiveBit & isFilteredBit) > 0)
                    break;
                ++_index;
            }

            return _index <= _table.ActiveEntitiesBits.Length * 64 && eid < _filter.Length * 64;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}