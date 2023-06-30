namespace ModulesFramework.Data.Enumerators
{
    public readonly struct MultipleComponentsEnumerable<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly bool[] _filter;

        public MultipleComponentsEnumerable(EcsTable<T> table, bool[] filter)
        {
            _table = table;
            _filter = filter;
        }
        
        public MultipleComponentsEnumerator<T> GetEnumerator()
        {
            return new MultipleComponentsEnumerator<T>(_table, _filter);
        }
    }
}