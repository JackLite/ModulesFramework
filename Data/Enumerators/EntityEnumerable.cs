using System;

namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntitiesEnumerable
    {
        private readonly EntityData[] _entities;
        private readonly bool[] _inc;
        private readonly DataWorld _world;

        public EntitiesEnumerable(EntityData[] entities, bool[] inc, DataWorld world)
        {
            _entities = entities;
            _inc = inc;
            _world = world;
        }
        
        public EntityEnumerator GetEnumerator()
        {
            return new EntityEnumerator(_entities, _inc, _world);
        }
    }
}