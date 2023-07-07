namespace ModulesFramework.Data.Enumerators
{
    public readonly struct MultipleComponentsEnumerable<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly int _eid;

        public MultipleComponentsEnumerable(EcsTable<T> table, int eid)
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