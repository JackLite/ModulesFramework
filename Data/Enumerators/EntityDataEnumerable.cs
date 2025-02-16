namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntityDataEnumerable
    {
        private readonly ulong[] _data;
        private readonly ulong[] _filter;

        public EntityDataEnumerable(ulong[] data, ulong[] filter)
        {
            _data = data;
            _filter = filter;
        }

        public EntityDataEnumerator GetEnumerator()
        {
            return new EntityDataEnumerator(_data, _filter);
        }
    }
}