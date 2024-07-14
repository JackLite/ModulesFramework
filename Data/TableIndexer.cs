using System;
using System.Collections.Generic;

namespace ModulesFramework.Data
{
    internal abstract class TableIndexer<T> where T : struct
    {
        public abstract void Add(T data, int eid);

        public abstract void Remove(T data);

    }
    internal class TableIndexer<T, TIndex> : TableIndexer<T>
        where T : struct
        where TIndex : notnull
    {
        private readonly Func<T, TIndex> _getIndex;
        private readonly Dictionary<TIndex, int> _indices = new();
        private readonly Dictionary<int, TIndex> _reverseMap = new();

        public int this[TIndex index] => _indices[index];

        public TableIndexer(Func<T, TIndex> getIndex)
        {
            _getIndex = getIndex;
        }

        public override void Add(T data, int eid)
        {
            _indices[_getIndex(data)] = eid;
            _reverseMap[eid] = _getIndex(data);
        }

        public void Update(TIndex old, T data, int eid)
        {
            _indices.Remove(old);
            _reverseMap.Remove(eid);
            Add(data, eid);
        }

        public override void Remove(T data)
        {
            var index = _getIndex(data);
            var eid = _indices[index];
            _indices.Remove(index);
            _reverseMap.Remove(eid);
        }

        public bool Contains(TIndex index)
        {
            return _indices.ContainsKey(index);
        }

        public TIndex GetKey(int eid)
        {
            return _reverseMap[eid];
        }
    }
}