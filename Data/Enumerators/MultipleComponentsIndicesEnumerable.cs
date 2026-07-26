namespace ModulesFramework.Data.Enumerators
{
    public struct MultipleComponentsIndicesEnumerable<T> where T : struct
    {
        private MultipleComponentsIndicesEnumerator<T> _enumerator;

        public MultipleComponentsIndicesEnumerable(BaseEcsTable<T> table, int eid)
        {
            _enumerator = new MultipleComponentsIndicesEnumerator<T>(table, eid);
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