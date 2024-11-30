using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public abstract class EcsTable
    {
        internal abstract bool[] ActiveEntities { get; }
        public abstract bool IsEmpty { get; }
        public abstract bool IsMultiple { get; }
        internal abstract Type Type { get; }
        public abstract void AddData(int eid, object component);
        public abstract void AddNewData(int eid, object component);
        internal abstract object GetDataObject(int eid);
        internal abstract object GetAt(int denseIndex);
        internal abstract object SetDataObject(int eid, object component);
        internal abstract void GetDataObjects(int eid, Dictionary<int, object> result);
        internal abstract void SetDataObjects(int eid, List<object> result);
        public abstract bool Contains(int eid);
        public abstract void Remove(int eid);
        internal abstract void RemoveInternal(int eid);
        internal abstract void RemoveByDenseIndex(int eid, int denseIndex);
        public abstract int GetMultipleDataLength(int eid);
    }

    public class EcsTable<T> : BaseEcsTable<T> where T : struct
    {
        protected readonly DataWorld _world;

        public EcsTable(DataWorld world)
        {
            _world = world;
            OnAddComponent += _world.RiseEntityChanged;
            OnRemoveComponent += _world.RiseEntityChanged;
        }
    }

    public class BaseEcsTable<T> : EcsTable where T : struct
    {
        private readonly DenseArray<T> _denseTable;

        /// <summary>
        ///     Eid -> dense indices
        /// </summary>
        private int[] _tableMap;

        /// <summary>
        ///     Dense index -> eid
        /// </summary>
        private int[] _tableReverseMap;

        private bool[] _entities;

        /// <summary>
        ///     Eid -> dense indices
        /// </summary>
        private DenseArray<int>?[] _multipleTableMap;

        private bool _isMultiple;
        private bool _isUsed;

        private TableIndexer<T>? _indexer;

        public override bool IsEmpty => _denseTable.Length == 0;
        public override bool IsMultiple => _isMultiple;

        internal override bool[] ActiveEntities => _entities;
        internal override Type Type => typeof(T);

        public event Action<int> OnAddComponent = delegate
        {
        };

        public event Action<int> OnRemoveComponent = delegate
        {
        };

        protected BaseEcsTable()
        {
            _denseTable = new DenseArray<T>();
            _tableMap = new int[64];
            _tableReverseMap = new int[64];
            _entities = new bool[64];
            _multipleTableMap = new DenseArray<int>[64];
        }

        public override void AddData(int eid, object component)
        {
            AddData(eid, (T)component);
        }

        /// <summary>
        ///     Add component to the entity by entity id
        ///     If component exists it will NOT be replaced so be careful
        /// </summary>
        public void AddData(int eid, in T data)
        {
            CheckSingle();
            _isUsed = true;
            var index = _denseTable.AddData(data);
            while (eid >= _tableMap.Length)
            {
                Array.Resize(ref _tableMap, _tableMap.Length * 2);
                Array.Resize(ref _entities, _tableMap.Length);
            }

            while (index >= _tableReverseMap.Length)
            {
                Array.Resize(ref _tableReverseMap, _tableReverseMap.Length * 2);
            }

            _tableReverseMap[index] = eid;
            _tableMap[eid] = index;
            _entities[eid] = true;

            if (_indexer != null)
                _indexer.Add(data, eid);

            OnAddComponent(eid);
        }

        /// <summary>
        ///     Add new multiple component to entity by entity id
        /// </summary>
        public void AddNewData(int eid, T data)
        {
            CheckMultiple();
            _isUsed = true;
            _isMultiple = true;
            var index = _denseTable.AddData(data);
            while (eid >= _multipleTableMap.Length)
            {
                Array.Resize(ref _multipleTableMap, _multipleTableMap.Length * 2);
                Array.Resize(ref _entities, _entities.Length * 2);
            }

            while (index >= _tableReverseMap.Length)
            {
                Array.Resize(ref _tableReverseMap, _tableReverseMap.Length * 2);
            }

            _multipleTableMap[eid] ??= new DenseArray<int>();

            _multipleTableMap[eid].AddData(index);
            _tableReverseMap[index] = eid;
            _entities[eid] = true;
            OnAddComponent(eid);
        }

        public override void AddNewData(int eid, object data)
        {
            AddNewData(eid, (T)data);
        }

        /// <summary>
        /// Return component by entity id
        /// Use this method when you need more fast iterations without using query
        /// </summary>
        /// <param name="eid">Entity id</param>
        /// <returns></returns>
        /// <exception cref="DataNotExistsInTableException{T}"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetData(int eid)
        {
#if MODULES_DEBUG
            CheckSingle();
            if (!Contains(eid))
                throw new DataNotExistsInTableException<T>(eid);
#endif
            return ref _denseTable.At(_tableMap[eid]);
        }

        /// <summary>
        ///     Returns indices of internal components array for entity id
        ///     It allows to get data by <see cref="MultipleAt"/>
        ///     Note: only for multiple components
        /// </summary>
        /// <param name="eid">Id of entity</param>
        /// <returns>Enumerable of indices with ability to delete in cycle</returns>
        public MultipleComponentsIndicesEnumerable<T> GetMultipleIndices(int eid)
        {
            CheckMultiple();
            return new MultipleComponentsIndicesEnumerable<T>(this, eid);
        }

        /// <summary>
        ///     Returns internal indices of components array for entity id
        ///     It allows to get data by <see cref="At"/>
        ///     Note: only for multiple components
        /// </summary>
        /// <param name="eid">Id of entity</param>
        /// <returns>Span of indices</returns>
        public Span<int> GetMultipleDenseIndices(int eid)
        {
            if (!Contains(eid))
                return Span<int>.Empty;
            return _multipleTableMap[eid].GetData();
        }

        /// <summary>
        ///     Returns counts of multiple components at entity
        /// </summary>
        public override int GetMultipleDataLength(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return 0;

            return _multipleTableMap[eid].Length;
        }

        /// <summary>
        ///     Return component by internal index
        /// </summary>
        public ref T At(int index)
        {
            return ref _denseTable.At(index);
        }

        /// <summary>
        ///     Return component by internal multiple index
        /// </summary>
        public ref T MultipleAt(int eid, int mtmIndex)
        {
            CheckMultiple();
            return ref _denseTable.At(_multipleTableMap[eid][mtmIndex]);
        }

        /// <summary>
        /// Only for internal usage!
        /// This method is for debugging. You should never use it in production code.
        /// </summary>
        /// <param name="eid">Id of Entity</param>
        /// <returns>Boxed struct T</returns>
        /// <seealso cref="GetData"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override object GetDataObject(int eid)
        {
            return _denseTable.At(_tableMap[eid]);
        }

        internal override object GetAt(int denseIndex)
        {
            return At(denseIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override object SetDataObject(int eid, object component)
        {
            return _denseTable[_tableMap[eid]] = (T)component;
        }

        /// <summary>
        /// Only for internal usage!
        /// This method is for debugging. You should never use it in production code.
        /// Fill result by map of denseIndex into component
        /// </summary>
        /// <param name="eid">Id of Entity</param>
        /// <seealso cref="GetData"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void GetDataObjects(int eid, Dictionary<int, object> result)
        {
            var indices = GetMultipleDenseIndices(eid);
            foreach (var index in indices)
            {
                var component = _denseTable.At(index);
                result.Add(index, component);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void SetDataObjects(int eid, List<object> newData)
        {
            var data = _multipleTableMap[eid].GetData();
            for (var index = 0; index < data.Length; index++)
            {
                var denseIndex = data[index];
                _denseTable[denseIndex] = (T)newData[index];
            }
        }

        /// <summary>
        ///     Remove component from entity by entity id
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Remove(int eid)
        {
            CheckSingle();
            RemoveSingle(eid);
        }

        internal override void RemoveInternal(int eid)
        {
            if (_isMultiple)
                RemoveAll(eid);
            else
                RemoveSingle(eid);
        }

        private void RemoveSingle(int eid)
        {
            CheckSingle();
            if (!Contains(eid))
                return;
            var index = _tableMap[eid];
            _indexer?.Remove(_denseTable[index]);
            _denseTable.RemoveData(index);
            var updateEid = _tableReverseMap[_denseTable.Length];
            _tableReverseMap[index] = updateEid;
            _tableMap[updateEid] = index;
            _entities[eid] = false;
            OnRemoveComponent(eid);
        }

        /// <summary>
        ///     Remove multiple component from entity by multiple index (NOT dense index)
        ///     <para>Note: you can use it with <seealso cref="GetMultipleIndices"/></para>
        /// </summary>
        internal void RemoveAt(int eid, int mtmIndex)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;

            var map = _multipleTableMap[eid];
            var denseIndex = map[mtmIndex];
            _denseTable.RemoveData(denseIndex);
            RemoveMultipleFromTableMap(eid, mtmIndex);

            var affectedEid = _tableReverseMap[_denseTable.Length];
            _tableReverseMap[denseIndex] = affectedEid;
            var affectedMap = _multipleTableMap[affectedEid];

            UpdateMultipleMap(affectedMap, denseIndex);
            OnRemoveComponent(eid);
        }

        /// <summary>
        ///     Remove component by dense index.
        ///     This is MultipleComponents API and should be used only for debug
        ///     Don't forget that removing from dense array shifts last element
        /// </summary>
        internal override void RemoveByDenseIndex(int eid, int denseIndex)
        {
            var map = _multipleTableMap[eid];
            for(var mtmIndex = 0; mtmIndex < map.Length; mtmIndex++)
            {
                if (map[mtmIndex] == denseIndex)
                {
                    RemoveAt(eid, mtmIndex);
                    break;
                }
            };

        }

        private void UpdateMultipleMap(DenseArray<int>? map, int denseIndex)
        {
            if (map == null)
                return;

            for (var i = 0; i < map.Length; i++)
            {
                if (map[i] == _denseTable.Length)
                {
                    map[i] = denseIndex;
                    break;
                }
            }
        }

        /// <summary>
        ///     Remove first component from entity by entity id
        /// </summary>
        internal void RemoveFirst(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;

            RemoveAt(eid, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveMultipleFromTableMap(int eid, int mtmIndex)
        {
            if (_multipleTableMap[eid].Length == 1)
                ClearMultipleForEntity(eid);
            else
                _multipleTableMap[eid].RemoveData(mtmIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearMultipleForEntity(int eid)
        {
            _multipleTableMap[eid] = null;
            _entities[eid] = false;
        }

        /// <summary>
        ///     Remove all multiple components from entity by entity id
        /// </summary>
        public void RemoveAll(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;

            var indices = GetMultipleIndices(eid);
            foreach (var index in indices)
            {
                indices.RemoveAt(index);
            }

            ClearMultipleForEntity(eid);
            OnRemoveComponent(eid);
        }

        /// <summary>
        /// Check if table contains entity
        /// </summary>
        /// <param name="eid">Entity id</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Contains(int eid)
        {
            if (!ContainsSingle(eid) && !ContainsMultiple(eid))
                return false;
            return IsActive(eid);
        }

        /// <summary>
        ///     Return enumerable for iteration through multiple component at entity
        /// </summary>
        public MultipleComponentsEnumerable<T> GetMultipleForEntity(int eid)
        {
#if MODULES_DEBUG
            if (_isUsed && !_isMultiple)
                throw new TableSingleWrongUseException<T>();
#endif
            return new MultipleComponentsEnumerable<T>(this, eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsSingle(int eid)
        {
            return eid < _tableMap.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ContainsMultiple(int eid)
        {
            return eid < _multipleTableMap.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsActive(int eid)
        {
            return _entities[eid];
        }

        /// <summary>
        /// Return span of memory for fast iteration
        /// </summary>
        /// <returns>Span{T}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> GetRawData()
        {
            return _denseTable.GetData();
        }

        internal int GetEidByIndex(int denseIndex)
        {
            CheckSingle();
            return _tableReverseMap[denseIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckSingle()
        {

            if (_isMultiple)
                throw new TableMultipleWrongUseException<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMultiple()
        {
#if MODULES_DEBUG
            if (_isUsed && !_isMultiple)
                throw new TableSingleWrongUseException<T>();
#endif
        }

        public void CreateKey<TIndex>(Func<T, TIndex> getIndex) where TIndex : notnull
        {
            CheckSingle();
            var indexer = new TableIndexer<T, TIndex>(getIndex);
            for (var i = 0; i < _tableMap.Length; i++)
            {
                if (!ActiveEntities[i])
                    continue;
                var data = _denseTable[_tableMap[i]];
                indexer.Add(data, i);
            }

            _indexer = indexer;
        }

        public ref T ByKey<TIndex>(TIndex index) where TIndex : notnull
        {
            CheckSingle();
            var eid = FindEidByKey(index);
            if (eid < 0)
                throw new ComponentNotFoundException<T>($"Component {typeof(T).Name} not found by index {index}");

            var denseIndex = _tableMap[eid];
            return ref _denseTable.At(denseIndex);
        }

        public int FindEidByKey<TIndex>(TIndex index) where TIndex : notnull
        {
            CheckSingle();
            if (_indexer == null)
                throw new NoIndexerException<T>();
            var indexer = (TableIndexer<T, TIndex>)_indexer;
            if (!indexer.Contains(index))
                return -1;

            return indexer[index];
        }

        public bool HasKey<TIndex>(TIndex index) where TIndex : notnull
        {
            CheckSingle();
            if (_indexer == null)
                throw new NoIndexerException<T>();
            var indexer = (TableIndexer<T, TIndex>)_indexer;

            return indexer.Contains(index);
        }

        public void UpdateKey<TIndex>(TIndex old, T component, int eid) where TIndex : notnull
        {
            if (_indexer == null)
                throw new NoIndexerException<T>();
            var indexer = (TableIndexer<T, TIndex>)_indexer;
            indexer.Update(old, component, eid);
        }

        public IEnumerable<T> GetInternalData()
        {
            return _denseTable.Enumerate();
        }

        public void ClearTable()
        {
            for (var eid = 0; eid < _entities.Length; eid++)
            {
                var isActive = _entities[eid];
                if (isActive)
                    RemoveInternal(eid);
            }
        }
    }
}