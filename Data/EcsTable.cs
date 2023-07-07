using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public abstract class EcsTable
    {
        internal abstract EntityData[] EntitiesData { get; }
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
        private int[] _tableReverseMap;
        private EntityData[] _entityData;

        private DenseArray<int>?[] _multipleTableMap;

        private bool _isMultiple;
        private bool _isUsed;

        internal override bool IsMultiple => _isMultiple;

        internal override EntityData[] EntitiesData => _entityData;

        public EcsTable()
        {
            _denseTable = new DenseArray<T>();
            _tableMap = new int[64];
            _tableReverseMap = new int[64];
            _entityData = new EntityData[64];
            _multipleTableMap = new DenseArray<int>[64];
        }

        public void AddData(int eid, in T data)
        {
            CheckSingle();
            _isUsed = true;
            var index = _denseTable.AddData(data);
            while (eid >= _tableMap.Length)
            {
                Array.Resize(ref _tableMap, _tableMap.Length * 2);
                Array.Resize(ref _entityData, _tableMap.Length);
            }

            if (index >= _tableReverseMap.Length)
            {
                Array.Resize(ref _tableReverseMap, _tableReverseMap.Length * 2);
            }

            _tableReverseMap[index] = eid;
            _tableMap[eid] = index;
            _entityData[eid] = new EntityData
            {
                eid = eid,
                isActive = true
            };
        }

        public void AddNewData(int eid, T data)
        {
            CheckMultiple();
            _isUsed = true;
            _isMultiple = true;
            var index = _denseTable.AddData(data);
            while (eid >= _multipleTableMap.Length)
            {
                Array.Resize(ref _multipleTableMap, _multipleTableMap.Length * 2);
            }

            while (eid >= _entityData.Length)
            {
                Array.Resize(ref _entityData, _entityData.Length * 2);
            }

            _multipleTableMap[eid] ??= new DenseArray<int>();

            _multipleTableMap[eid].AddData(index);
            _entityData[eid] = new EntityData
            {
                eid = eid,
                isActive = true
            };
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

        public Span<int> GetMultipleDataIndices(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return Span<int>.Empty;

            return _multipleTableMap[eid].GetData();
        }

        public int GetMultipleDataLength(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return 0;

            return _multipleTableMap[eid].Length;
        }

        public ref T At(int index)
        {
            return ref _denseTable.At(index);
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
            var indices = GetMultipleDataIndices(eid);
            for (int i = 0; i < indices.Length; i++)
            {
                result.Add(At(indices[i]));
            }
        }

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
            ref var ed = ref _entityData[eid];
            ed.isActive = false;
        }

        public void RemoveAt(int eid, int index)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;
            
            _denseTable.RemoveData(index);
            RemoveMultipleFromTableMap(eid, index);
        }

        public void RemoveFirst(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;

            var indices = GetMultipleDataIndices(eid);
            var index = indices[0];
            _denseTable.RemoveData(index);
            RemoveMultipleFromTableMap(eid, index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveMultipleFromTableMap(int eid, int index)
        {
            if (_multipleTableMap[eid].Length == 1)
                ClearMultipleForEntity(eid);
            else
                _multipleTableMap[eid].RemoveData(index);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearMultipleForEntity(int eid)
        {
            _multipleTableMap[eid] = null;
            ref var ed = ref _entityData[eid];
            ed.isActive = false;
        }

        public void RemoveAll(int eid)
        {
            CheckMultiple();
            if (!Contains(eid))
                return;

            var indices = GetMultipleDataIndices(eid);
            foreach (var index in indices)
            {
                _denseTable.RemoveData(index);
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
            return _entityData[eid].isActive;
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

        public int GetEidByIndex(int denseIndex)
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