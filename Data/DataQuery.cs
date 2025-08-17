using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Data.QueryUtils;
using ModulesFramework.Exceptions;
using ModulesFramework.Utils;

namespace ModulesFramework.Data
{
    public sealed class DataQuery : IDisposable
    {
        private readonly DataWorld _world;

        private EcsTable _mainTable;
        private ulong[] _bitMask = new ulong[4];
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

            // if (_inc.Length < _mainTable.ActiveEntities.Length)
            //     _inc = new bool[_mainTable.ActiveEntities.Length];

            if (_bitMask.Length < _mainTable.ActiveEntitiesBits.Length)
                Array.Resize(ref _bitMask, _mainTable.ActiveEntitiesBits.Length);
            // _mainTable.ActiveEntities.AsSpan().CopyTo(_inc);
            // _mainTable.optimized.AsSpan().CopyTo(_bitMask);
            Buffer.BlockCopy(_mainTable.ActiveEntitiesBits, 0, _bitMask, 0,
                _mainTable.ActiveEntitiesBits.Length * sizeof(ulong));
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

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                if (i >= table.ActiveEntitiesBits.Length)
                {
                    _bitMask[i] = 0;
                    continue;
                }

                _bitMask[i] &= table.ActiveEntitiesBits[i];
            }

            return this;
        }

        public DataQuery With(OrBuilder or)
        {
            if (_isEmpty)
                return this;

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                for (var j = 0; j < 64; j++)
                {
                    var shift = j % 64;
                    var check = or.Check(j + i * 64, _world);
                    _bitMask[i] &= ~((1UL << shift) & Convert.ToUInt64(!check) << shift);
                }
            }

            return this;
        }

        public DataQuery Without<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();
            if (_isEmpty)
                return this;

            var length = Math.Min(_bitMask.Length, table.ActiveEntitiesBits.Length);
            for (var i = 0; i < length; ++i)
            {
                if (i >= table.ActiveEntitiesBits.Length)
                {
                    _bitMask[i] = 0;
                    continue;
                }

                _bitMask[i] &= ~table.ActiveEntitiesBits[i];
            }

            return this;
        }

        public DataQuery Where<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            _isEmpty |= table.IsEmpty;
            if (_isEmpty)
                return this;

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                if (i >= table.ActiveEntitiesBits.Length)
                {
                    _bitMask[i] = 0;
                    continue;
                }

                _bitMask[i] &= table.ActiveEntitiesBits[i];
            }

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                if (MFMath.CountBits(_bitMask[i]) == 0)
                    continue;

                for (var j = 0; j < 64; j++)
                {
                    var shift = j % 64;
                    var eid = j + i * 64;
                    if ((_bitMask[i] & (1UL << shift)) == 0)
                        continue;

                    var filterResult = customFilter.Invoke(table.GetData(eid));
                    _bitMask[i] &= ~((1UL << shift) & Convert.ToUInt64(!filterResult) << shift);
                }
            }

            return this;
        }

        public DataQuery Where(WhereOrBuilder whereOr)
        {
            if (_isEmpty)
                return this;

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                for (var j = 0; j < 64; j++)
                {
                    var shift = j % 64;
                    var check = whereOr.Check(j + i * 64, _world);
                    _bitMask[i] &= ~((1UL << shift) & Convert.ToUInt64(!check) << shift);
                }
            }

            return this;
        }

        public DataQuery WhereAny<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            _isEmpty |= table.IsEmpty;
            if (_isEmpty)
                return this;

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                for (var j = 0; j < 64; j++)
                {
                    var eid = j + i * 64;
                    if (!table.Contains(eid))
                        continue;

                    var indices = table.GetMultipleDenseIndices(eid);
                    var inc = false;
                    foreach (var index in indices)
                    {
                        inc |= customFilter.Invoke(table.At(index));
                    }

                    var shift = j % 64;
                    _bitMask[i] &= ~((1UL << shift) & Convert.ToUInt64(!inc) << shift);
                }
            }

            return this;
        }

        public DataQuery WhereAll<T>(Func<T, bool> customFilter) where T : struct
        {
            var table = _world.GetEcsTable<T>();
            _isEmpty |= table.IsEmpty;
            if (_isEmpty)
                return this;

            for (var i = 0; i < _bitMask.Length; ++i)
            {
                for (var j = 0; j < 64; j++)
                {
                    var eid = j + i * 64;
                    if (!table.Contains(eid))
                        continue;

                    var indices = table.GetMultipleDenseIndices(eid);
                    var inc = true;
                    foreach (var index in indices)
                    {
                        inc &= customFilter.Invoke(table.At(index));
                    }

                    var shift = j % 64;
                    _bitMask[i] &= ~((1UL << shift) & Convert.ToUInt64(!inc) << shift);
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntitiesEnumerable GetEntities()
        {
            if (_isEmpty)
                return new EntitiesEnumerable(Array.Empty<ulong>(), Array.Empty<ulong>(), _world);

            return new EntitiesEnumerable(_mainTable.ActiveEntitiesBits, _bitMask, _world);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityDataEnumerable GetEntitiesId()
        {
            if (_isEmpty)
                return new EntityDataEnumerable(Array.Empty<ulong>(), Array.Empty<ulong>());

            return new EntityDataEnumerable(_mainTable.ActiveEntitiesBits, _bitMask);
        }

        public ComponentsEnumerable<T> GetComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();

#if MODULES_DEBUG
            if (table.IsMultiple)
                throw new TableMultipleWrongUseException<T>();
#endif
                if (_isEmpty)
                    return new ComponentsEnumerable<T>(table, Array.Empty<ulong>());

            return new ComponentsEnumerable<T>(table, _bitMask);
        }

        public MultipleComponentsQueryEnumerable<T> GetMultipleComponents<T>() where T : struct
        {
            var table = _world.GetEcsTable<T>();

            if (_isEmpty)
                return new MultipleComponentsQueryEnumerable<T>(table, Array.Empty<ulong>());

            return new MultipleComponentsQueryEnumerable<T>(table, _bitMask);
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

            foreach (var mask in _bitMask)
            {
                count += MFMath.CountBits(mask);
            }

            return count;
        }

        public bool Contains(int eid)
        {
            if (_isEmpty)
                return false;

            var maskIdx = eid / 64;
            if (maskIdx >= _bitMask.Length)
                return false;

            var shift = eid % 64;
            var mask = _bitMask[maskIdx];
            var isActiveBit = mask & (1UL << shift);

            return isActiveBit > 0;
        }
    }
}