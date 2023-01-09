using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        public class Query<T> where T : struct
        {
            private readonly DataWorld _world;
            private readonly EntityData[] _entityFilter;
            private int _count;
            public Query(DataWorld world, EcsTable<T> table)
            {
                _world = world;
                _entityFilter = table.GetEntitiesFilter();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    ed.exclude = false;
                    if (ed.isActive) 
                        _count++;
                }
            }

            public Query<T> With<TW>() where TW : struct
            {
                var table = _world.GetEscTable<TW>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive) continue;
                    var exclude = !table.Contains(ed.eid);
                    ed.exclude |= exclude;
                    if (exclude)
                        _count--;
                }

                return this;
            }

            public Query<T> Without<TW>() where TW : struct
            {
                var table = _world.GetEscTable<TW>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive) continue;
                    var exclude = table.Contains(ed.eid);
                    ed.exclude |= exclude;
                    if (exclude)
                        _count--;
                }

                return this;
            }

            public Query<T> Where<TW>(Func<TW, bool> customFilter) where TW : struct
            {
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive) continue;
                    ref var c = ref _world.GetComponent<TW>(ed.eid);
                    var exclude = !customFilter.Invoke(c);
                    ed.exclude |= exclude;
                    if (exclude)
                        _count--;
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

            public bool Any()
            {
                foreach (var _ in GetEntitiesId())
                    return true;

                return false;
            }

            public bool TrySelectFirst<TRet>(out TRet c) where TRet : struct
            {
                foreach (var eid in GetEntitiesId())
                {
                    c = _world.GetComponent<TRet>(eid);
                    return true;
                }

                c = new TRet();
                return false;
            }

            public ref TRet SelectFirst<TRet>() where TRet : struct
            {
                foreach (var eid in GetEntitiesId())
                {
                    if (_world.HasComponent<TRet>(eid))
                        return ref _world.GetComponent<TRet>(eid);
                }

                throw new QuerySelectException<TRet>();
            }

            public void DestroyAll()
            {
                foreach (var eid in GetEntitiesId())
                {
                    _world.DestroyEntity(eid);
                }
            }

            public int Count()
            {
                return _count;
            }
        }
    }
}