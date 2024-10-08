﻿namespace ModulesFramework.Data.Enumerators
{
    public readonly struct MultipleComponentsEnumerable<T> where T : struct
    {
        private readonly BaseEcsTable<T> _table;
        private readonly int _eid;

        public MultipleComponentsEnumerable(BaseEcsTable<T> table, int eid)
        {
            _table = table;
            _eid = eid;
        }

        public MultipleComponentsEnumerator<T> GetEnumerator()
        {
            return new MultipleComponentsEnumerator<T>(_table, _eid);
        }
    }
}