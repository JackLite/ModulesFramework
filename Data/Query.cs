using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        public sealed class Query : IDisposable
        {
            private readonly DataWorld _world;
            private EntityData[] _entityFilter = Array.Empty<EntityData>();
            private int _count;
            public Query(DataWorld world)
            {
                _world = world;
            }

            public void Dispose()
            {
                _world.ReturnQuery(this);
            }

            internal void Init(EntityData[] entityFilter)
            {
                _entityFilter = entityFilter;
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    ed.exclude = false;
                    if (ed.isActive) 
                        _count++;
                }
            }

            public Query With<T>() where T : struct
            {
                var table = _world.GetEscTable<T>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive || ed.exclude) continue;
                    var exclude = !table.Contains(ed.eid);
                    ed.exclude |= exclude;
                    if (exclude)
                        _count--;
                }

                return this;
            }

            public Query Without<T>() where T : struct
            {
                var table = _world.GetEscTable<T>();
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive || ed.exclude) continue;
                    var exclude = table.Contains(ed.eid);
                    ed.exclude |= exclude;
                    if (exclude)
                        _count--;
                }

                return this;
            }

            public Query Where<T>(Func<T, bool> customFilter) where T : struct
            {
                for (var i = 0; i < _entityFilter.Length; ++i)
                {
                    ref var ed = ref _entityFilter[i];
                    if (!ed.isActive || ed.exclude) continue;
                    ref var c = ref _world.GetComponent<T>(ed.eid);
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

            public bool TrySelectFirstEntity(out Entity e)
            {
                e = new Entity();
                foreach (var entity in GetEntities())
                {
                    e = entity;
                    return true;
                }

                return false;
            }

            public Entity SelectFirstEntity()
            {
                foreach (var entity in GetEntities())
                {
                    return entity;
                }
                
                throw new QuerySelectEntityException();
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