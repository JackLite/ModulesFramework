namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntityDataEnumerable
    {
        private readonly EntityData[] _data;

        public EntityDataEnumerable(EntityData[] data)
        {
            _data = data;
        }
        
        public EntityDataEnumerator GetEnumerator()
        {
            return new EntityDataEnumerator(_data);
        }
    }
}