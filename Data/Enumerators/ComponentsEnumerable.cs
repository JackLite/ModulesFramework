namespace ModulesFramework.Data.Enumerators
{
    public readonly struct ComponentsEnumerable<T> where T : struct
    {
        private readonly EcsTable<T> _table;
        private readonly ulong[] _filter;

        public ComponentsEnumerable(EcsTable<T> table, ulong[] filter)
        {
            _table = table;
            _filter = filter;
        }

        public ComponentsEnumerator<T> GetEnumerator()
        {
            return new ComponentsEnumerator<T>(_table, _filter);
        }
    }
}