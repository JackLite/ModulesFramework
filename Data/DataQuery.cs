using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Data.QueryUtils;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public sealed class DataQuery : IDisposable
    {
        private readonly DataWorld _world;

        private EcsTable _mainTable;
        private bool[] _inc = new bool[64];
        private bool _isEmpty;

        public DataQuery(DataWorld world)
        {
            _world = world;
        }

        internal void Init(EcsTable table)
        {
            _mainTable = table;
            _isEmpty = table.IsEmpty;
            if (_isEmpty)
                return;

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
            _isEmpty |= table.IsEmpty;
            if (_isEmpty)
                return this;

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
            if (_isEmpty)
                return this;

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
            if (table.IsEmpty)
            {
                _isEmpty = true;
                return this;
            }

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
            if (table.IsEmpty)
            {
                _isEmpty = true;
                return this;
            }

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
            if (table.IsEmpty)
            {
                _isEmpty = true;
                return this;
            }

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
            if (_isEmpty)
                return new EntitiesEnumerable(Array.Empty<bool>(), Array.Empty<bool>(), _world);

            return new EntitiesEnumerable(_mainTable.ActiveEntities, _inc, _world);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDataEnumerable GetEntitiesId()
        {
            if (_isEmpty)
                return new EntityDataEnumerable(Array.Empty<bool>(), Array.Empty<bool>());

            return new EntityDataEnumerable(_mainTable.ActiveEntities, _inc);
        }

        public ComponentsEnumerable<T> GetComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();

            if (_isEmpty)
                return new ComponentsEnumerable<T>(table, Array.Empty<bool>());

            return new ComponentsEnumerable<T>(table, _inc);
        }

        public MultipleComponentsQueryEnumerable<T> GetMultipleComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();

            if (_isEmpty)
                return new MultipleComponentsQueryEnumerable<T>(table, Array.Empty<bool>());

            return new MultipleComponentsQueryEnumerable<T>(table, _inc);
        }

        public bool Any()
        {
            if (_isEmpty)
                return false;

            foreach (var _ in GetEntitiesId())
                return true;

            return false;
        }

        public bool TrySelectFirst<TRet>(out TRet c) where TRet : struct
        {
            if (_isEmpty)
            {
                c = new TRet();
                return false;
            }

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
            if (_isEmpty)
                throw new QuerySelectException<TRet>();

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
            if (_isEmpty)
                return false;

            foreach (var entity in GetEntities())
            {
                e = entity;
                return true;
            }

            return false;
        }

        public Entity SelectFirstEntity()
        {
            if (_isEmpty)
                throw new QuerySelectEntityException();

            foreach (var entity in GetEntities())
            {
                return entity;
            }

            throw new QuerySelectEntityException();
        }

        public void DestroyAll()
        {
            if (_isEmpty)
                return;

            foreach (var eid in GetEntitiesId())
            {
                _world.DestroyEntity(eid);
            }
        }

        public int Count()
        {
            if (_isEmpty)
                return 0;

            var count = 0;
            foreach (var inc in _inc)
            {
                count += Convert.ToInt32(inc);
            }

            return count;
        }

        public bool Contains(int eid)
        {
            if (_isEmpty)
                return false;

            return eid >= 0 && _inc.Length > eid && _inc[eid];
        }
    }
}