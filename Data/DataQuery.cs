using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Data.QueryUtils;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public sealed class DataQuery : IDisposable
    {
        private readonly World.DataWorld _world;

        private EcsTable _mainTable;
        private bool[] _inc = new bool[64];

        public DataQuery(World.DataWorld world)
        {
            _world = world;
        }

        internal void Init(EcsTable table)
        {
            _mainTable = table;

            if (_inc.Length < _mainTable.ActiveEntities.Length)
                _inc = new bool[_mainTable.ActiveEntities.Length];
            Array.Copy(_mainTable.ActiveEntities, _inc, _mainTable.ActiveEntities.Length);
        }

        public void Dispose()
        {
            _world.ReturnQuery(this);
        }

        public DataQuery With<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (_inc[i])
                    _inc[i] &= table.Contains(i);
            }

            return this;
        }

        public DataQuery With(OrBuilder or)
        {
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (_inc[i])
                    _inc[i] &= or.Check(i, _world);
            }

            return this;
        }

        public DataQuery Without<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (_inc[i])
                    _inc[i] &= !table.Contains(i);
            }

            return this;
        }

        public DataQuery Where<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (_inc[i])
                    _inc[i] &= table.Contains(i);
                if (_inc[i])
                    _inc[i] &= customFilter.Invoke(table.GetData(i));
            }

            return this;
        }

        public DataQuery Where(WhereOrBuilder whereOr)
        {
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (_inc[i])
                    _inc[i] &= whereOr.Check(i, _world);
            }

            return this;
        }

        public DataQuery WhereAny<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (!_inc[i])
                    continue;

                var indices = table.GetMultipleDenseIndices(i);
                var inc = false;
                foreach (var index in indices)
                {
                    inc |= customFilter.Invoke(table.At(index));
                }

                _inc[i] &= inc;
            }

            return this;
        }

        public DataQuery WhereAll<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            for (var i = 0; i < _inc.Length; ++i)
            {
                if (!_inc[i])
                    continue;

                var indices = table.GetMultipleDenseIndices(i);
                var inc = true;
                foreach (var index in indices)
                {
                    inc &= customFilter.Invoke(table.At(index));
                }

                _inc[i] &= inc;
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntitiesEnumerable GetEntities()
        {
            return new EntitiesEnumerable(_mainTable.ActiveEntities, _inc, _world);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDataEnumerable GetEntitiesId()
        {
            return new EntityDataEnumerable(_mainTable.ActiveEntities, _inc);
        }

        public ComponentsEnumerable<T> GetComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();
            return new ComponentsEnumerable<T>(table, _inc);
        }

        public MultipleComponentsQueryEnumerable<T> GetMultipleComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();
            return new MultipleComponentsQueryEnumerable<T>(table, _inc);
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
            var count = 0;
            foreach (var inc in _inc)
            {
                count += Convert.ToInt32(inc);
            }

            return count;
        }
    }
}