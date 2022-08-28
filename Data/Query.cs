using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Core
{
    public partial class DataWorld
    {
        public class Query<T> where T : struct
        {
            private readonly DataWorld _world;
            private readonly EntityData[] _entityFilter;

            public Query(DataWorld world, EcsTable<T> table)
            {
                _world = world;
                _entityFilter = table.GetEntitiesFilter();
            }

            public Query<T> With<TW>() where TW : struct
            {
                var table = _world.GetEscTable<TW>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ef = ref _entityFilter[i];
                    ef.exclude = ef.exclude || !table.Contains(ef.eid);
                }
                return this;
            }

            public Query<T> Without<TW>() where TW : struct
            {
                var table = _world.GetEscTable<TW>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ef = ref _entityFilter[i];
                    ef.exclude = ef.exclude || table.Contains(ef.eid);
                }
                return this;
            }

            public Query<T> Where<TW>(Func<TW, bool> customFilter) where TW : struct
            {
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    ref var c = ref _world.GetComponent<TW>(ed.eid);
                    ed.exclude = !customFilter.Invoke(c);
                }
                return this;
            }

            public Entity[] GetEntities()
            {
                var entitiesId = GetEntitiesId().ToArray();
                var entities = new Entity[entitiesId.Length];
                for (var i = 0; i < entitiesId.Length; ++i)
                {
                    entities[i] = _world.GetEntity(entitiesId[i]);
                }
                return entities;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<int> GetEntitiesId()
            {
                foreach (var ef in _entityFilter)
                {
                    if (!ef.exclude && ef.isActive)
                        yield return ef.eid;
                }
            }
        }
    }
}