namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntitiesEnumerable
    {
        private readonly EntityData[] _data;
        private readonly DataWorld _world;

        public EntitiesEnumerable(EntityData[] data, DataWorld world)
        {
            _data = data;
            _world = world;
        }
        
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(_data, _world);
        }
    }
}