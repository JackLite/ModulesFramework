using System;
using System.Collections.Generic;

namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsIndicesEnumerator<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly int _eid;
        private readonly LinkedList<int> _queueMain;

        internal MultipleComponentsIndicesEnumerator(EcsTable<T> table, int eid)
        {
            _table = table;
            _eid = eid;
            _queueMain = new();
            Current = -1;
            for (var i = 0; i < table.GetMultipleDenseIndices(eid).Length; i++)
            {
                _queueMain.AddLast(i);
            }
        }

        /// <summary>
        ///     Return multiple table map index
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public int Current { get; private set; }

        public bool MoveNext()
        {
            if (_queueMain.Count == 0)
                return false;
            var indices = _table.GetMultipleDenseIndices(_eid);
            if (indices.Length == 0)
                return false;

            Current = _queueMain.First.Value;
            _queueMain.RemoveFirst();

            return true;
        }

        public void RemoveAt(int mtmIndex)
        {
            _table.RemoveAt(_eid, mtmIndex);

            if (_queueMain.Count <= 0)
                return;
            
            _queueMain.RemoveLast();
            _queueMain.AddFirst(mtmIndex);
        }

        public void Reset()
        {
            _queueMain.Clear();
            Current = -1;
            for (var i = 0; i < _table.GetMultipleDenseIndices(_eid).Length; i++)
            {
                _queueMain.AddLast(i);
            }
        }
    }
}