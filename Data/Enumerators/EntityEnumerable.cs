namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntitiesEnumerable
    {
        private readonly ulong[] _active;
        private readonly ulong[] _inc;
        private readonly DataWorld _world;

        public EntitiesEnumerable(ulong[] active, ulong[] inc, DataWorld world)
        {
            _active = active;
            _inc = inc;
            _world = world;
        }

        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(_active, _inc, _world);
        }
    }
}