using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Data
{
    public abstract class EcsTable
    {
        public abstract EntityData[] EntitiesData { get; }
        public abstract object GetDataObject(int eid);
        public abstract bool Contains(int eid);
        public abstract void Remove(int eid);
    }

    public class EcsTable<T> : EcsTable where T : struct
    {
        private readonly DenseArray<T> _denseTable;
        private int[] _tableMap;
        private int[] _tableReverseMap;
        private EntityData[] _entityData;

        private DenseArray<int>?[] _newTableMap;

        public override EntityData[] EntitiesData => _entityData;

        public EcsTable()
        {
            _denseTable = new DenseArray<T>();
            _tableMap = new int[64];
            _tableReverseMap = new int[64];
            _entityData = new EntityData[64];
            _newTableMap = new DenseArray<int>[64];
        }

        public void AddData(int eid, in T data)
        {
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
            _entityData[eid] = new EntityData { eid = eid, isActive = true };
        }

        public void AddNewData(int eid, T data)
        {
            var index = _denseTable.AddData(data);
            while (eid >= _newTableMap.Length)
            {
                Array.Resize(ref _newTableMap, _newTableMap.Length * 2);
            }

            while (eid >= _entityData.Length)
            {
                Array.Resize(ref _entityData, _entityData.Length * 2);
            }

            _newTableMap[eid] ??= new DenseArray<int>();

            _newTableMap[eid].AddData(index);
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
            if (!Contains(eid))
                throw new DataNotExistsInTableException<T>(eid);
            return ref _denseTable.At(_tableMap[eid]);
        }

        public Span<int> GetMultipleDataIndices(int eid)
        {
            if (!ContainsMultiple(eid))
                return Span<int>.Empty;

            return _newTableMap[eid].GetData();
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
        public override object GetDataObject(int eid)
        {
            return _denseTable.At(_tableMap[eid]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Remove(int eid)
        {
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

        public void RemoveFirst(int eid)
        {
            if (!ContainsMultiple(eid))
                return;

            var indices = GetMultipleDataIndices(eid);
            _denseTable.RemoveData(indices[0]);
            if (_newTableMap[eid].Length == 1)
            {
                _newTableMap[eid] = null;
                ref var ed = ref _entityData[eid];
                ed.isActive = false;
            }
            else
            {
                _newTableMap[eid].RemoveData(0);
            }
        }

        /// <summary>
        /// Check if table contains entity
        /// </summary>
        /// <param name="eid">Entity id</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Contains(int eid)
        {
            if (eid >= _tableMap.Length)
                return false;
            return _entityData[eid].isActive;
        }

        public bool ContainsMultiple(int eid)
        {
            if (eid >= _newTableMap.Length)
                return false;

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
    }
}