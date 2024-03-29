﻿namespace ModulesFramework.Data.Enumerators
{
    public readonly struct EntitiesEnumerable
    {
        private readonly bool[] _active;
        private readonly bool[] _inc;
        private readonly DataWorld _world;

        public EntitiesEnumerable(bool[] active, bool[] inc, DataWorld world)
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