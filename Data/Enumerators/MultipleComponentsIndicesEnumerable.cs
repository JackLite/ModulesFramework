namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsIndicesEnumerable<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly int _eid;
        private MultipleComponentsIndicesEnumerator<T> _enumerator;

        public MultipleComponentsIndicesEnumerable(EcsTable<T> table, int eid)
        {
            _table = table;
            _eid = eid;
            _enumerator = new MultipleComponentsIndicesEnumerator<T>(_table, _eid);
        }
        
        public MultipleComponentsIndicesEnumerator<T> GetEnumerator()
        {
            _enumerator.Reset();
            return _enumerator;
        }
        
        public int Count()
        {
            var count = 0;
            foreach (var _ in this)
            {
                count++;
            }
            return count;
        }
        
        public void RemoveAt(int mtmIndex)
        {
            _enumerator.RemoveAt(mtmIndex);
        }
    }
}