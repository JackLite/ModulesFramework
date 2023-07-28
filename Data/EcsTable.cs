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
        internal abstract bool IsMultiple { get; }
        internal abstract object GetDataObject(int eid);
        internal abstract void GetDataObjects(int eid, List<object> result);
        public abstract bool Contains(int eid);
        public abstract void Remove(int eid);
        internal abstract void RemoveInternal(int eid);
    }

    public class EcsTable<T> : EcsTable where T : struct
    {
        private readonly DenseArray<T> _denseTable;
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

        internal override bool IsMultiple => _isMultiple;

        internal override bool[] ActiveEntities => _entities;

        public EcsTable()
        {
            _denseTable = new DenseArray<T>();
            _tableMap = new int[64];
            _tableReverseMap = new int[64];
            _entities = new bool[64];
            _multipleTableMap = new DenseArray<int>[64];
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
            CheckSingle();
            if (!Contains(eid))
                throw new DataNotExistsInTableException<T>(eid);
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
        public int GetMultipleDataLength(int eid)
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

        /// <summary>
        /// Only for internal usage!
        /// This method is for debugging. You should never use it in production code.
        /// </summary>
        /// <param name="eid">Id of Entity</param>
        /// <returns>Boxed struct T</returns>
        /// <seealso cref="GetData"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void GetDataObjects(int eid, List<object> result)
        {
            var components = GetMultipleForEntity(eid);
            foreach (var comp in components)
            {
                result.Add(comp);
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
            _denseTable.RemoveData(index);
            var updateEid = _tableReverseMap[_denseTable.Length];
            _tableReverseMap[index] = updateEid;
            _tableMap[updateEid] = index;
            _entities[eid] = false;
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
            #if MODULES_DEBUG
            if (_isMultiple)
                throw new TableMultipleWrongUseException<T>();
            #endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckMultiple()
        {
            #if MODULES_DEBUG
            if (_isUsed && !_isMultiple)
                throw new TableSingleWrongUseException<T>();
            #endif
        }
    }
}