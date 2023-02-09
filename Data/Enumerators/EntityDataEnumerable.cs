namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntityDataEnumerable
    {
        private readonly EntityData[] _data;
        private readonly bool[] _filter;

        public EntityDataEnumerable(EntityData[] data, bool[] filter)
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