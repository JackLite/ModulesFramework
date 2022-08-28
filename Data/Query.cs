using System;
using System.Runtime.CompilerServices;
using Core.Enumerators;

namespace Core
{
    public partial class DataWorld
    {
        public readonly struct Query<T> where T : struct
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntitiesEnumerable GetEntities()
            {
                return new EntitiesEnumerable(_entityFilter, _world);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntityDataEnumerable GetEntitiesId()
            {
                return new EntityDataEnumerable(_entityFilter);
            }
        }
    }
}