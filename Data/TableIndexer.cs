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
        
        public int this[TIndex index] => _indices[index];

        public TableIndexer(Func<T, TIndex> getIndex)
        {
            _getIndex = getIndex;
        }

        public override void Add(T data, int eid)
        {
            _indices[_getIndex(data)] = eid;
        }

        public void Update(TIndex old, T data, int eid)
        {
            _indices.Remove(old);
            Add(data, eid);
        }

        public override void Remove(T data)
        {
            _indices.Remove(_getIndex(data));
        }

        public bool Contains(TIndex index)
        {
            return _indices.ContainsKey(index);
        }
    }
}