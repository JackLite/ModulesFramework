﻿using System;

namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsQueryEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly ulong[] _filter;
        private int _index;
        private int _innerIndex;
        private bool _innerEnumeration;

        internal MultipleComponentsQueryEnumerator(EcsTable<T> table, ulong[] filter)
        {
            _table = table;
            _filter = filter;
            _index = 0;
            _innerIndex = 0;
            _innerEnumeration = false;
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
            // just start enumeration
            if (_index == 0)
            {
                ++_index;
            }

            while (true)
            {
                var outOfRange = _index > _table.ActiveEntitiesBits.Length * 64;
                if (outOfRange)
                    return false;

                var eid = _index - 1;

                if (!_innerEnumeration)
                {
                    var optIdx = eid / 64;
                    var bitMask = eid % 64;

                    var isActiveBit = _table.ActiveEntitiesBits[optIdx] & (1UL << bitMask);
                    var isFilteredBit = _filter[optIdx] & (1UL << bitMask);
                    if (isActiveBit == 0 || isFilteredBit == 0)
                    {
                        ++_index;
                        _innerIndex = 0;
                        continue;
                    }
                }

                _innerIndex++;
                var indices = _table.GetMultipleDenseIndices(eid);
                if (_innerIndex > indices.Length)
                {
                    ++_index;
                    _innerIndex = 0;
                    _innerEnumeration = false;
                    continue;
                }
                _innerEnumeration = true;
                return true;

            }
        }

        public void Reset()
        {
            _index = 0;
            _innerIndex = 0;
        }
    }
}