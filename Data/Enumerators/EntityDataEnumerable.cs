namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntityDataEnumerable
    {
        private readonly bool[] _data;
        private readonly bool[] _filter;

        public EntityDataEnumerable(bool[] data, bool[] filter)
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